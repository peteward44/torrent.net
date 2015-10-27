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

using System;
using System.Collections;
using Threading = System.Threading;
using Net = System.Net;
using Sockets = System.Net.Sockets;


namespace BitTorrent
{
	/// <summary>
	/// Manages the peers.
	/// </summary>
	public class PeerManager
	{
		private ArrayList peerList = new ArrayList(); // list of Peers
		private ArrayList socketList = new ArrayList(); // list of Sockets
		private volatile bool running = true;
		private const int selectTimeout = 0;// 1000000 / 10; // 10 times a second


		public PeerManager()
		{
		}


		public void AddPeer(Peer peer)
		{
			peer.Disconnected += new PeerDisconnectedCallback(peer_Disconnected);

			lock ( this )
			{
				this.peerList.Add( peer );
				this.socketList.Add( peer.Socket );
			}
		}



		public void Stop()
		{
			this.running = false;
		}


		public void ProcessWaitingData()
		{
			byte[] data = new byte[ Config.MaxRequestSize ];
			ArrayList socketCloneList = null, errorList = null;

			if ( this.peerList.Count == 0 )
				return;

			if (this.running)
			{
				try
				{
					socketCloneList = (ArrayList)this.socketList.Clone();
					errorList = (ArrayList)this.socketList.Clone();

					try
					{
						Sockets.Socket.Select(socketCloneList, null, errorList, selectTimeout);
					}
					catch (System.Net.Sockets.SocketException e)
					{
						Config.LogException(e);
					}

					foreach (Sockets.Socket socket in errorList)
					{
						if (socketList.Contains(socket))
						{
							socketList.Remove(socket);
							Config.LogDebugMessage("Socket removed from list");
						}

						foreach (Peer peer in this.peerList)
						{
							if (peer.Socket != null && peer.Socket.Equals(socket))
							{
								Config.LogDebugMessage("Peer: " + peer.ToString() + " removed from list");
								peer.Disconnect();
							}
						}
					}

					foreach (Sockets.Socket socket in socketCloneList)
					{
						foreach (Peer peer in this.peerList)
						{
							try
							{
								if (peer.Socket != null && peer.Socket.Equals(socket))
								{
									peer.ReadData(data);
									break;
								}
							}
							catch (Sockets.SocketException e)
							{
								Config.LogException(e);
								peer.Disconnect();
							}
						}
					}
				}
				catch (System.Exception e)
				{
					Config.LogException(e);
				}
			}
		}


		private void peer_Disconnected(Peer peer)
		{
			if (this.peerList.Contains(peer))
				this.peerList.Remove(peer);
			if (this.socketList.Contains(peer.Socket))
				this.socketList.Remove(peer.Socket);
		}
	}
}
