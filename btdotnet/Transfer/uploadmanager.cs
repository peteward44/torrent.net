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
using IO = System.IO;


namespace BitTorrent
{

	public delegate void PieceSectionFinishedCallback(int pieceId, int begin, int length);


	// Like the download manager, except manages uploading piece-sections to other peers
	public class UploadManager
	{
		private MetainfoFile infofile;
		private DownloadFile downloadFile;

		private ArrayList incomingRequests = new ArrayList();

		private System.Threading.Timer chokeTimer;

		private ArrayList unchokedPeers = new ArrayList();
		private ArrayList interestedPeers = new ArrayList();


		public event PieceSectionFinishedCallback PieceSectionFinished;
		

		public UploadManager(MetainfoFile infofile, DownloadFile downloadFile)
		{
			this.infofile = infofile;
			this.downloadFile = downloadFile;

			this.chokeTimer = new System.Threading.Timer(new System.Threading.TimerCallback(OnChokeTimer), null, Config.ChokeInterval, Config.ChokeInterval);
		}
		
		
		public void AddPeer(Peer peer)
		{
			peer.Disconnected += new PeerDisconnectedCallback(peer_Disconnected);
			peer.HeIsInterestedChange += new PeerStatusChangeCallback(peer_HeIsInterestedChange);
			peer.PieceCancel += new PeerPieceCallback(peer_PieceCancel);
			peer.PieceRequest += new PeerPieceCallback(peer_PieceRequest);
		}


		private void peer_Disconnected(Peer peer)
		{
			lock ( this.interestedPeers.SyncRoot )
			{
				if (this.interestedPeers.Contains(peer))
					this.interestedPeers.Remove(peer);
			}
		}


		private void peer_HeIsInterestedChange(Peer peer, bool newStatus)
		{
			lock ( this.interestedPeers.SyncRoot )
			{
				if (newStatus)
				{
					if (!this.interestedPeers.Contains(peer))
						this.interestedPeers.Add(peer);
				}
				else
				{
					if (this.interestedPeers.Contains(peer))
						this.interestedPeers.Remove(peer);
				}
			}
		}


		private void peer_PieceCancel(Peer peer, int pieceId, int begin, int length, byte[] data)
		{
			for (int i=0; i<this.incomingRequests.Count; ++i)
			{
				PieceRequest request = (PieceRequest)this.incomingRequests[i];
				if (request.Peer.Equals(peer) && request.PieceId == pieceId && request.Begin == begin && request.Length == length)
				{
					this.incomingRequests.RemoveAt(i);
					break;
				}
			}
		}


		private void peer_PieceRequest(Peer peer, int pieceId, int begin, int length, byte[] data)
		{
			// if i am choking, ignore
			if (!peer.AmIChoking)
			{
				this.incomingRequests.Add( new PieceRequest(peer, pieceId, begin, length) );

				// TODO: check if we can serve the request
				// TODO: make more efficient than loading the whole piece each time
				IO.MemoryStream stream = new IO.MemoryStream(this.infofile.GetPieceLength(pieceId));
				this.downloadFile.LoadFromFile(pieceId, stream);
				stream.Seek(begin, IO.SeekOrigin.Begin);
				peer.SendPiece(pieceId, begin, length, stream, new PeerPieceCallback(PieceSendFinished));
			}
		}


		private void PieceSendFinished(Peer peer, int pieceId, int begin, int length, byte[] data)
		{
			foreach (PieceRequest pieceRequest in this.incomingRequests)
			{
				if (pieceRequest.Peer.Equals(peer) && pieceRequest.PieceId == pieceId && pieceRequest.Begin == begin && pieceRequest.Length == length)
				{
					this.incomingRequests.Remove(pieceRequest);
					break;
				}
			}

			if (this.PieceSectionFinished != null)
				this.PieceSectionFinished(pieceId, begin, length);
		}


		private void OnChokeTimer(object state)
		{
			if (this.unchokedPeers.Count < Config.ActiveConfig.SimultaneousUploadsLimit)
			{
//				float worstDownload = -1;

				// find some interested peers to unchoke
				foreach ( Peer peer in this.interestedPeers )
				{
					//					if (worstDownload < 0 || worstDownload > peer.BytesDownloaded)
					//					{
					peer.AmIChoking = false;
					this.unchokedPeers.Add( peer );
					//					}
					if ( this.unchokedPeers.Count >= Config.ActiveConfig.SimultaneousUploadsLimit )
						break;
				}
			}
		}
	}
}
