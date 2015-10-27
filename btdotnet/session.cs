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
using Net = System.Net;
using Sockets = System.Net.Sockets;
using Threading = System.Threading;
using IO = System.IO;


namespace BitTorrent
{
	public class TorrentCheckQueue
	{
		private bool mInProgress = false;
		private bool mUseThread = false;
		private bool mStartEachTorrentAfterCheck = false;
		private Queue<Torrent> mTorrents = new Queue<Torrent>();

		public bool InProgress
		{
			get { return mInProgress; }
		}


		public TorrentCheckQueue( bool useBackgroundThread, bool startEachTorrentAfterCheck )
		{
			mUseThread = useBackgroundThread;
			mStartEachTorrentAfterCheck = startEachTorrentAfterCheck;
		}


		public void AddTorrent( Torrent torrent )
		{
			mTorrents.Enqueue( torrent );
			if ( !mInProgress )
			{
				mInProgress = true;
				if ( mUseThread )
					Threading.ThreadPool.QueueUserWorkItem( new Threading.WaitCallback( PrivateStart ) );
				else
					PrivateStart( null );
			}
		}


		private void PrivateStart( object o )
		{
			while ( mTorrents.Count > 0 )
			{
				Torrent torrent = mTorrents.Dequeue();
				torrent.CheckFileIntegrity( false );
				if ( mStartEachTorrentAfterCheck )
					torrent.Start();
			}
			mInProgress = false;
		}
	}


	// main class. all torrents are started from here
	public class Session : System.IDisposable
	{
		private List<Torrent> mTorrents = new List<Torrent>();
		private Sockets.Socket mListener;
		private ByteField20 mLocalPeerId;

		public List<Torrent> Torrents
		{
			get { return this.mTorrents; }
		}


		public ByteField20 LocalPeerID
		{
			get { return mLocalPeerId; }
		}


		private static ByteField20 DefaultCalculatePeerId()
		{
			// calculate our peer id
			string peerIdString = "-TN" + string.Format("{0:00}", Config.MajorVersionNumber)
				+ string.Format("{0:00}", Config.MinorVersionNumber) + "-";
			
			ByteField20 peerId = new ByteField20();
			System.Array.Copy(System.Text.ASCIIEncoding.ASCII.GetBytes(peerIdString), 0, peerId.Data, 0, peerIdString.Length);
			
			const string peerChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			
			System.Random rand = new System.Random();

			for (int i=peerIdString.Length; i<20; ++i)
			{
				peerId.Data[i] = (byte)peerChars[rand.Next(0, peerChars.Length-1)];
			}

			return peerId;
		}

		/// <summary>
		/// This should be called once to prevent errors communicating with the tracker. This is a workaround for a bug in the .NET framework
		/// regarding parsing the http headers which contain 'unsafe' characters.
		/// </summary>
		/// <returns></returns>
		private static bool SetAllowUnsafeHeaderParsing20()
		{
			//Get the assembly that contains the internal class
			System.Reflection.Assembly aNetAssembly = System.Reflection.Assembly.GetAssembly( typeof( System.Net.Configuration.SettingsSection ) );
			if ( aNetAssembly != null )
			{
				//Use the assembly in order to get the internal type for the internal class
				System.Type aSettingsType = aNetAssembly.GetType( "System.Net.Configuration.SettingsSectionInternal" );
				if ( aSettingsType != null )
				{
					//Use the internal static property to get an instance of the internal settings class.
					//If the static instance isn't created allready the property will create it for us.
					object anInstance = aSettingsType.InvokeMember( "Section",
						System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.NonPublic, null, null, new object[] { } );

					if ( anInstance != null )
					{
						//Locate the private bool field that tells the framework is unsafe header parsing should be allowed or not
						System.Reflection.FieldInfo aUseUnsafeHeaderParsing = aSettingsType.GetField( "useUnsafeHeaderParsing", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance );
						if ( aUseUnsafeHeaderParsing != null )
						{
							aUseUnsafeHeaderParsing.SetValue( anInstance, true );
							return true;
						}
					}
				}
			}
			return false;
		}


		public Session()
			: this( DefaultCalculatePeerId() )
		{
		}


		public Session( ByteField20 localPeerId )
		{
			mLocalPeerId = localPeerId;
			if ( Config.ActiveConfig.ProxyURL != "" ) // set web proxy
			{
				System.Net.WebRequest.DefaultWebProxy = new Net.WebProxy( Config.ActiveConfig.ProxyURL );
			}
			SetAllowUnsafeHeaderParsing20();
			this.StartServer();
		}


		public Torrent CreateTorrent( string metafilename )
		{
			Torrent torrent = new Torrent( this, metafilename );
			mTorrents.Add( torrent );
			return torrent;
		}


		public void Dispose()
		{
			foreach ( Torrent torrent in this.mTorrents )
			{
				torrent.Dispose();
			}

			this.mTorrents.Clear();
			this.StopServer();
		}


		public void ProcessWaitingData()
		{
			foreach ( Torrent torrent in this.mTorrents )
			{
				torrent.ProcessWaitingData();
			}
		}
		
		
		private void StartServer()
		{
			Net.IPAddress addr = (Config.ActiveConfig.IPToBindServerTo != "" ? Net.IPAddress.Parse(Config.ActiveConfig.IPToBindServerTo) : Net.IPAddress.Any);
			int port = Config.ActiveConfig.MinServerPort;

			// attempt to find a port within the given port range
			while (true)
			{
				try
				{
					this.mListener = new Sockets.Socket(Sockets.AddressFamily.InterNetwork, Sockets.SocketType.Stream, Sockets.ProtocolType.Tcp);
					this.mListener.Bind(new Net.IPEndPoint(addr, port));
					this.mListener.Listen(10);
					Config.ActiveConfig.ChosenPort = port;
					break;
				}
				catch (Sockets.SocketException)
				{
					if (++port > Config.ActiveConfig.MaxServerPort)
						throw;
				}
			}

			this.mListener.BeginAccept(new System.AsyncCallback(OnAccept), null);
		}
		
		
		private void StopServer()
		{
			if (this.mListener != null)
				this.mListener.Close();
			this.mListener = null;
		}
		
		
		public Torrent FindTorrent(ByteField20 infoDigest)
		{
			lock ( this.mTorrents )
			{
				foreach ( Torrent torrent in this.mTorrents )
				{
					if (torrent.Metainfo.InfoDigest.Equals(infoDigest))
					{
						return torrent;
					}
				}
			
				return null;
			}
		}


		private void OnAccept(System.IAsyncResult result)
		{
			Sockets.Socket socket;

			try
			{
				// Accept connections from other peers, find the appropriate torrent and add the peer to it
				socket = this.mListener.EndAccept(result);
			}
			catch (System.Exception)
			{
				if (this.mListener != null)
					this.mListener.Close();
				this.mListener = null;
				return;
			}

			try
			{
				ByteField20 infoDigest = new ByteField20(), peerId = new ByteField20();
				Sockets.NetworkStream netStream = new Sockets.NetworkStream(socket, true);
				
				PeerProtocol.ReceiveHandshake(netStream, ref infoDigest);

				Torrent torrent = this.FindTorrent(infoDigest);

				if (torrent != null)
				{
					// found it, finish handshaking and add the peer to the list
					PeerProtocol.SendHandshake(netStream, torrent.Metainfo.InfoDigest);
					PeerProtocol.SendPeerId(netStream, mLocalPeerId );

					if ( !PeerProtocol.ReceivePeerId( netStream, ref peerId ))
					{ // NAT check, discard
						socket.Close();
					}
					else
					{
						if ( !peerId.Equals( mLocalPeerId ) ) // make sure we aren't connecting to ourselves
						{
							Net.IPEndPoint endPoint = (Net.IPEndPoint)socket.RemoteEndPoint;
							PeerInformation peerInformation = new PeerInformation( endPoint.Address.ToString(), endPoint.Port, peerId );

							// add the peer to the torrent
							torrent.AddPeer( socket, netStream, peerInformation );
						}
						else
							socket.Close();
					}
				}
				else
					socket.Close();
			}
			catch (System.Exception e)
			{
				Config.LogException( e );
				socket.Close();
			}

			this.mListener.BeginAccept(new System.AsyncCallback(OnAccept), null);
		}
	}
}
