/***********************************************************************************************************************
 * TorrentDotNET - A BitTorrent library based on the .NET platform                                                     *
 * Copyright (C) 2004, Peter Ward                                                                                      *
 *                                                                                                                     *
 * This library is free software; you can redistribute it and/or modify it under the terms of the                      *
 * GNU Lesser General Public License as published by the Free Software Foundation;                                     *
 * either version 2.1 of the License, or (at your option) any later version.                                           *
 *                                                                                                                     *
 * This library is distributed in the hope that it will be useful,                                                     *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. *
 * See the GNU Lesser General Public License for more details.                                                         *
 *                                                                                                                     *
 * You should have received a copy of the GNU Lesser General Public License along with this library;                   *
 * if not, write to the Free Software Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA          *
 ***********************************************************************************************************************/

using System.Collections;
using System.Collections.Generic;
using Threading = System.Threading;
using IO = System.IO;
using Net = System.Net;
using Sockets = System.Net.Sockets;


namespace BitTorrent
{
	/// <summary>Delegate for torrent checking status</summary>
	/// <param name="torrent">Torrent being checked</param>
	/// <param name="pieceId">Piece ID which has just been checked</param>
	/// <param name="good">True if the piece has been downloaded, false otherwise</param>
	/// <param name="percentDone">Percentage of checking complete</param>
	public delegate void TorrentCheckIntegrityCallback(Torrent torrent, int pieceId, bool good, float percentDone);

	public delegate void TorrentStatusChangedCallback(Torrent torrent, TorrentStatus status);

	public delegate void TorrentPeerConnectedCallback(Torrent torrent, Peer peer, bool connected);

	public delegate void TorrentTrackerErrorCallback(Torrent torrent, string errorMessage);

	public delegate ByteField20 CalculatePeerIdCallback(MetainfoFile infofile);


	/// <summary>Status of the torrent</summary>
	public enum TorrentStatus
	{
		/// <summary>Currently checking the file</summary>
		Checking,
		/// <summary>Downloading the torrent</summary>
		Working,
		/// <summary>Seeding the torrent</summary>
		Seeding,
		/// <summary>Queued for checking</summary>
		QueuedForChecking,
		/// <summary>Queued for downloading (file has been checked)</summary>
		QueuedForWorking
	}


	/// <summary>
	/// Represents a torrent. Torrents must be added to the Session class
	/// </summary>
	public class Torrent : System.IDisposable
	{
		private Session mSession;
		private int seedCount = 0, leecherCount = 0, finishedCount = 0;
		private MetainfoFile infofile;
		private DownloadFile downloadFile;

		private TorrentStatus status = TorrentStatus.QueuedForChecking;

		private TrackerProtocol tp;

		private List<Peer> mPeers = new List<Peer>(); // Arraylist of Peers

		private PeerManager peerManager;
		private DownloadStrategyManager mDownloadStrategy;// downloadManager;
		private UploadManager uploadManager;

		public TorrentCheckIntegrityCallback CheckIntegrity;
		public TorrentStatusChangedCallback StatusChanged;
		public TorrentPeerConnectedCallback PeerConnected;
		public TorrentTrackerErrorCallback TrackerError;

		private ArrayList mWaitingPeers = new ArrayList();

		private int /*bytesDownloaded = 0, */bytesUploaded = 0;


		/// <summary>Status of the torrent</summary>
		public TorrentStatus Status
		{
			get { return this.status; }
		}


		public bool Seeding
		{
			get { return this.downloadFile.Bitfield.AllTrue; }
		}

		public int PieceCount
		{
			get { return this.infofile.PieceCount; }
		}

		public float PercentComplete
		{
			get { return this.downloadFile.PercentComplete; }
		}


		public event PercentChangedCallback PercentChanged
		{
			add { this.downloadFile.PercentChanged += value; }
			remove { this.downloadFile.PercentChanged -= value; }
		}


		public int BytesDownloaded
		{
			get { return this.mDownloadStrategy.BytesDownloaded; }
		}


		public int BytesUploaded
		{
			get { return this.bytesUploaded; }
		}


		/// <summary></summary>
		/// <returns>Metainfo file associated with the torrent</returns>
		public MetainfoFile Metainfo
		{
			get { return this.infofile; }
		}


		/// <summary></summary>
		/// <returns>Download file associated with the torrent</returns>
		public DownloadFile DownloadFile
		{
			get { return this.downloadFile; }
		}


		/// <summary></summary>
		/// <returns>Number of seeds on the torrent</returns>
		public int SeedCount
		{
			get { return this.seedCount; }
		}


		/// <summary></summary>
		/// <returns>Number of leechers (people downloading) on the torrent</returns>
		public int LeecherCount
		{
			get { return this.leecherCount; }
		}


		/// <summary></summary>
		/// <returns>Number of total finished downloads on this torrent. Sometimes the tracker does not support this, so it could be zero</returns>
		public int FinishedCount
		{
			get { return this.finishedCount; }
		}


		public TrackerProtocol Tracker
		{
			get { return this.tp; }
		}


		public Session Session
		{
			get { return mSession; }
		}


		public List<Peer> Peers
		{
			get { return mPeers; }
		}


		/// <summary>Contructs a torrent using the metainfo filename</summary>
		/// <param name="metafilename">Filename of the metainfo file</param>
		internal Torrent( Session session, string metafilename )
		{
			this.mSession = session;
			this.infofile = new MetainfoFile(metafilename);
			this.downloadFile = new DownloadFile(infofile);
			this.peerManager = new PeerManager();
			this.mDownloadStrategy = new DownloadStrategyManager( this );
			this.uploadManager = new UploadManager(infofile, downloadFile);
			this.tp = new TrackerProtocol(this, infofile, downloadFile);

	//		this.downloadManager.PieceFinished += new PieceFinishedCallback(downloadManager_PieceFinished);
			this.uploadManager.PieceSectionFinished += new PieceSectionFinishedCallback(uploadManager_PieceSectionFinished);

			this.tp.TrackerUpdate += new TrackerUpdateCallback(tp_TrackerUpdate);
		}


		public void ProcessWaitingData()
		{
			lock ( this.mWaitingPeers.SyncRoot )
			{
				foreach ( object[] objs in mWaitingPeers )
				{
					AddPeerPrivate( (Sockets.Socket)objs[ 0 ], (Sockets.NetworkStream)objs[ 1 ], (PeerInformation)objs[ 2 ] );
				}
				mWaitingPeers.Clear();
			}
			this.peerManager.ProcessWaitingData();
		}


		private void OnIntegrityCallback(DownloadFile file, int pieceId, bool good, float percentDone)
		{
			if (this.CheckIntegrity != null)
				this.CheckIntegrity(this, pieceId, good, percentDone);
		}


		/// <summary>Checks the integrity of the current file, or if it does not exists creates it. This must be called before
		/// each torrent is started</summary>
		/// <param name="callback">Delegate which is called every time a piece is checked</param>
		public void CheckFileIntegrity( bool forced )
		{
			this.status = TorrentStatus.Checking;
			if (this.StatusChanged != null)
				this.StatusChanged(this, this.status);
			this.downloadFile.CheckIntegrity( forced, new CheckIntegrityCallback( OnIntegrityCallback ) );
		}


		/// <summary>Starts the torrent</summary>
		public void Start()
		{
			this.status = TorrentStatus.Working;
			if (this.StatusChanged != null)
				this.StatusChanged(this, this.status);

			// scrape the tracker to find out info
			TrackerProtocol.Scrape(this.infofile, out this.seedCount, out this.leecherCount, out this.finishedCount);

			DefaultDownloadStrategy defaultStrategy = new DefaultDownloadStrategy( this.mDownloadStrategy );
			this.mDownloadStrategy.AddNewStrategy( defaultStrategy );
			this.mDownloadStrategy.SetActiveStrategy( defaultStrategy );

			// then tell the tracker we are starting/resuming a download
			this.tp.Initiate();
		}


		/// <summary>
		/// Helper thread method to start the connection to peers
		/// </summary>
		/// <param name="state"></param>
		private void StartPeerConnectionThread(System.IAsyncResult result)
		{
			object[] objs = (object[])result.AsyncState;
			Sockets.Socket socket = (Sockets.Socket)objs[0];
			PeerInformation peerinfo = (PeerInformation)objs[1];
			ByteField20 infoDigest = new ByteField20(), peerId = new ByteField20();
			Sockets.NetworkStream netStream;

			try
			{
				socket.EndConnect(result);

				netStream = new Sockets.NetworkStream(socket, true);

				// send handshake info
				PeerProtocol.SendHandshake(netStream, this.infofile.InfoDigest);
				PeerProtocol.ReceiveHandshake(netStream, ref infoDigest);
				PeerProtocol.SendPeerId(netStream, this.mSession.LocalPeerID );

				if (!PeerProtocol.ReceivePeerId(netStream, ref peerId))
				{ // NAT check
					socket.Close();
					return;
				}

				// check info digest matches and we are not attempting to connect to ourself
				if ( infoDigest.Equals( this.infofile.InfoDigest ) && !peerId.Equals( this.mSession.LocalPeerID ) )
				{
					peerinfo.ID = peerId;
					this.AddPeer(socket, netStream, peerinfo);
				}
				else // info digest doesn't match, close the connection
					socket.Close();
			}
			catch (System.Exception e)
			{
				Config.LogException(e);
				// die if the connection failed
				if (socket != null)
					socket.Close();
				return;
			}
		}


		// Called by the Session class for peers connecting through the server, adds it to a list which can then be added when
		// ProcessWaitingData() is called so we keep it on one thread
		internal void AddPeer( Sockets.Socket socket, Sockets.NetworkStream netStream, PeerInformation peerInformation )
		{
			lock ( mWaitingPeers.SyncRoot )
			{
				mWaitingPeers.Add( new object[] { socket, netStream, peerInformation } );
			}
		}


		// Called by the Session class for peers connecting through the server and in StartPeerConnectionThread() method for peers
		// connected to directly
		private void AddPeerPrivate(Sockets.Socket socket, Sockets.NetworkStream netStream, PeerInformation peerInformation)
		{
			try
			{
				if ( Config.ActiveConfig.MaxPeersConnected < this.mPeers.Count )
				{
					socket.Close();
					return;
				}

				bool connect = true;

				if (Config.ActiveConfig.OnlyOneConnectionFromEachIP)
				{
					foreach ( Peer connectedPeer in this.mPeers )
					{
						if (connectedPeer.Information.IP.Equals(peerInformation.IP))
						{
							connect = false;
							break;
						}
					}
				}

				if (!connect)
				{
					socket.Close();
					return;
				}

				if (!this.downloadFile.Bitfield.AllFalse)
					PeerProtocol.SendBitfieldMessage(netStream, this.downloadFile);

				Peer peer = new Peer(this.infofile, this.downloadFile, socket, netStream, peerInformation);
				peer.Disconnected += new PeerDisconnectedCallback(peer_Disconnected);

				Config.LogDebugMessage("Connection accepted from: " + peer.ToString());

				// add to download and upload manager
				this.mDownloadStrategy.HookPeerEvents(peer);
				this.uploadManager.AddPeer(peer);
				this.peerManager.AddPeer(peer);

				if (this.PeerConnected != null)
					this.PeerConnected(this, peer, true);

				this.mPeers.Add( peer );
			}
			catch ( System.Exception e )
			{
				Config.LogException( e );
			}
		}


		/// <summary></summary>
		/// <returns>Name of the torrent</returns>
		public override string ToString()
		{
			return this.Metainfo.Name;
		}


		/// <summary>Disposes the torrent</summary>
		public void Dispose()
		{
			// send shut down message to tracker
			if (this.tp != null)
				this.tp.Stop();

			this.peerManager.Stop();

			if (this.downloadFile != null)
				this.downloadFile.Dispose();
			this.downloadFile = null;

			lock ( this.mPeers )
			{
				// disconnect each peer
				foreach ( Peer peer in this.mPeers )
				{
					peer.Disconnected -= new PeerDisconnectedCallback(peer_Disconnected);
					peer.Disconnect();
					if (this.PeerConnected != null)
						this.PeerConnected(this, peer, false);
				}
			}

			this.mPeers.Clear();

			if (this.tp != null)
				this.tp.Dispose();
			this.tp = null;
		}


		private void peer_Disconnected(Peer peer)
		{
			lock ( this.mPeers )
			{
				this.mPeers.Remove( peer );
			}

			peer.Disconnected -= new PeerDisconnectedCallback(peer_Disconnected);

			if (this.PeerConnected != null)
				this.PeerConnected(this, peer, false);
		}


		//private void downloadManager_PieceFinished(int pieceId)
		//{
		//  // piece just finished, announce to everyone we have it
		//  foreach ( Peer peer in this.mPeers )
		//  {
		//    try
		//    {
		//      peer.SendHaveMessage(pieceId);
		//    }
		//    catch ( System.Exception e )
		//    {
		//      Config.LogException( e );
		//      peer.Disconnect();
		//    }
		//  }

		//  this.bytesDownloaded += this.infofile.GetPieceLength(pieceId);
		//}


		private void uploadManager_PieceSectionFinished(int pieceId, int begin, int length)
		{
			this.bytesUploaded += length;
		}


		private void tp_TrackerUpdate( ArrayList peers, bool success, string errorMessage )
		{
			Config.LogDebugMessage("Tracker update: Success: " + success + " : Num Peers : " + (peers != null ? peers.Count.ToString() : "0"));

			// attempt to connect to each peer given
			if (success)
			{
				if (peers != null)
				{
					foreach (PeerInformation peerinfo in peers)
					{
						Config.LogDebugMessage( "Peer : " + peerinfo.IP + ":" + peerinfo.Port + " ID: " + ( peerinfo.ID != null ? peerinfo.ID.ToString() : "NONE" ) );

						try
						{
							Sockets.Socket socket = new Sockets.Socket(Sockets.AddressFamily.InterNetwork, Sockets.SocketType.Stream, Sockets.ProtocolType.Tcp);
							socket.BeginConnect(new Net.IPEndPoint(Net.IPAddress.Parse(peerinfo.IP), peerinfo.Port),
								new System.AsyncCallback(StartPeerConnectionThread), new object[] { socket, peerinfo});
						}
						catch ( System.Exception e )
						{
							Config.LogException( e );
						}
					}
				}
			}
			else
			{
				if (this.TrackerError != null)
					this.TrackerError(this, errorMessage);
			}
		}
	}
}
