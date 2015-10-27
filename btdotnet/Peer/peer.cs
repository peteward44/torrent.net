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
using Net = System.Net;
using Sockets = System.Net.Sockets;
using IO = System.IO;
using Threading = System.Threading;


namespace BitTorrent
{
	public delegate void PeerDisconnectedCallback(Peer peer);

	public delegate void PeerStatusChangeCallback(Peer peer, bool newStatus);
	public delegate void PeerBitfieldChangeCallback(Peer peer, int pieceId);
	public delegate void PeerPieceCallback(Peer peer, int pieceId, int begin, int length, byte[] data);


	public class Peer
	{
		private bool amiChoking = true, amiInterested = false, heisChoking = true, heisInterested = false;
		private bool connected = true;

		private MetainfoFile infofile;
		private DownloadFile downloadFile;

		private Sockets.Socket socket;
		private Sockets.NetworkStream netStream;
		private PeerInformation peerInformation;
		private PeerProtocol peerProtocol = new PeerProtocol();

		private byte[] data = new byte[ Config.MaxRequestSize ];
		private PWLib.Platform.CircularBuffer buffer = new PWLib.Platform.CircularBuffer( Config.MaxRequestSize / 2 );

		private BitField piecesDownloaded;

		private ArrayList messageQueue = new ArrayList();
		private volatile bool asyncInProgress = false;

		private int numBytesDownloaded = 0, numBytesUploaded = 0;

//		internal Piece PieceDownloading = null;

		
		public Throttle UpThrottle
		{
			get { return peerProtocol.UpThrottle; }
		}

		public Throttle DownThrottle
		{
			get { return peerProtocol.DownThrottle; }
		}


		public bool IsSeed
		{
			get { return this.piecesDownloaded.AllTrue; }
		}


		internal Sockets.Socket Socket
		{
			get { return this.socket; }
		}


		#region Events


		public event PeerStatusChangeCallback IAmChokingChange, IAmInterestedChange, HeIsChokingChange, HeIsInterestedChange;
		public event PeerBitfieldChangeCallback BitfieldChange;
		public event PeerPieceCallback PieceRequest, PieceCancel, PieceIncoming;

		public event PeerDisconnectedCallback Disconnected;

		
		#endregion


		#region Properties


		public int BytesDownloaded
		{
			get { return this.numBytesDownloaded; }
		}


		public int BytesUploaded
		{
			get { return this.numBytesUploaded; }
		}


		/// <summary>
		/// Downloaded:Uploaded ratio. Higher ratio than 1.0 means we've downloaded more than we've uploaded
		/// </summary>
		public float Ratio
		{
			get
			{
				if (this.numBytesUploaded == 0)
					return 0.0f;
				else
					return (float)this.numBytesDownloaded / (float)this.numBytesUploaded;
			}
		}


		public bool Connected
		{
			get { return this.connected; }
		}


		public PeerInformation Information
		{
			get { return this.peerInformation; }
		}


		public BitField BitField
		{
			get { return this.piecesDownloaded; }
		}


		public bool AmIChoking
		{
			get { return this.amiChoking; }
			set
			{
				if (this.amiChoking != value)
				{
					this.amiChoking = value;
					if (this.IAmChokingChange != null)
						this.IAmChokingChange(this, this.amiChoking);
					this.SendChokeMessage(this.amiChoking);
				}
			}
		}
		
		
		public bool AmIInterested
		{
			get { return amiInterested; }
			set
			{
				if (this.amiInterested != value)
				{
					this.amiInterested = value;
					if (this.IAmInterestedChange != null)
						this.IAmInterestedChange(this, this.amiInterested);
					this.SendInterestedMessage(this.amiInterested);
				}
			}
		}
		
		
		public bool HeIsChoking
		{
			get { return heisChoking; }
		}
		
		
		public bool HeIsInterested
		{
			get { return heisInterested; }
		}


		#endregion


		public Peer(MetainfoFile infofile, DownloadFile downloadFile,
			Sockets.Socket socket, Sockets.NetworkStream netStream, PeerInformation peerInformation)
		{
			this.infofile = infofile;
			this.downloadFile = downloadFile;
			this.socket = socket;
			this.netStream = netStream;
			this.peerInformation = peerInformation;

			this.piecesDownloaded = new BitField(this.infofile.PieceCount);
			this.peerProtocol.UpThrottle.Start();
			this.peerProtocol.DownThrottle.Start();
		}


		public void Disconnect()
		{
			try
			{
				try
				{
					if (this.socket.Connected)
						this.netStream.Close();
					this.netStream = null;
				}
				catch (System.Exception)
				{}

				if (this.connected)
				{
					if (this.Disconnected != null)
						this.Disconnected(this);
				}

				this.socket = null;
				this.connected = false;

				this.peerProtocol.UpThrottle.Stop();
				this.peerProtocol.DownThrottle.Stop();

				//				Config.LogDebugMessage("Peer disconnected: " + this.ToString() + " Stack: " + System.Environment.StackTrace);
			}
			catch ( System.Exception e )
			{
				Config.LogException( e );
			}
		}


		#region Receiving messages


		public int ReadData(byte[] data)
		{
			try
			{
				int totalRead = 0;

				do
				{
					int bytesRead = this.socket.Receive( data, 0, data.Length, Sockets.SocketFlags.None );
					totalRead += bytesRead;
					this.peerProtocol.DownThrottle.AddData( bytesRead );
					if ( bytesRead > 0 )
					{
						this.buffer.Write( data, 0, bytesRead );
					}
					else
					{
						this.Disconnect();
					}
				} while ( this.socket != null && this.socket.Connected && this.socket.Available > 0 );

				this.ProcessAnyMessages();
				return totalRead;
			}
			catch (System.Exception e)
			{
				Config.LogException(e);
				this.Disconnect();
			}

			return 0;
		}


		private void ProcessAnyMessages()
		{
			try
			{
				IO.Stream reader = this.buffer.Reader;

				while ( this.buffer.DataAvailable >= 5 )
				{
					int length;
					PeerMessage message;

					peerProtocol.ReadMessageHeader( reader, out length, out message );

					if ( length > 0 )
					{
						if ( length - 1 > buffer.DataAvailable )
						{
							reader.Seek( -5, IO.SeekOrigin.Current );
							break;
						}
						else
						{
							length--;
							//								Config.LogDebugMessage("Message received: " + message.ToString() + " from " + this.ToString());

							switch ( message )
							{
								case PeerMessage.Bitfield:
									{
										this.piecesDownloaded = peerProtocol.ReadBitfieldMessage( reader, length );
										this.piecesDownloaded.SetLength( this.infofile.PieceCount );
										if ( this.BitfieldChange != null )
											this.BitfieldChange( this, -1 );
									}
									break;

								case PeerMessage.Choke:
									this.heisChoking = true;
									if ( this.HeIsChokingChange != null )
										this.HeIsChokingChange( this, this.heisChoking );
									break;

								case PeerMessage.Unchoke:
									this.heisChoking = false;
									if ( this.HeIsChokingChange != null )
										this.HeIsChokingChange( this, this.heisChoking );
									break;

								case PeerMessage.Interested:
									this.heisInterested = true;
									if ( this.HeIsInterestedChange != null )
										this.HeIsInterestedChange( this, this.heisInterested );
									break;

								case PeerMessage.Uninterested:
									this.heisInterested = false;
									if ( this.HeIsInterestedChange != null )
										this.HeIsInterestedChange( this, this.heisInterested );
									break;

								case PeerMessage.Have:
									{
										// update piecesdownloaded
										int pieceId = peerProtocol.ReadHaveMessage( reader );
										this.piecesDownloaded.Set( pieceId, true );
										if ( this.BitfieldChange != null )
											this.BitfieldChange( this, pieceId );
									}
									break;

								case PeerMessage.Request:
									{
										int index, begin, pieceLength;
										peerProtocol.ReadRequestMessage( reader, out index, out begin, out pieceLength );
										if ( this.PieceRequest != null )
											this.PieceRequest( this, index, begin, pieceLength, null );
									}
									break;

								case PeerMessage.Cancel:
									{
										int index, begin, pieceLength;
										peerProtocol.ReadCancelMessage( reader, out index, out begin, out pieceLength );
										if ( this.PieceCancel != null )
											this.PieceCancel( this, index, begin, pieceLength, null );
									}
									break;

								case PeerMessage.Piece:
									{
										int index, begin, pieceLength;
										peerProtocol.ReadPieceMessageHeader( reader, length, out index, out begin, out pieceLength );
										this.numBytesDownloaded += pieceLength;
										byte[] data = new byte[ pieceLength ];
										reader.Read( data, 0, data.Length );
										if ( this.PieceIncoming != null )
											this.PieceIncoming( this, index, begin, pieceLength, data );
									}
									break;

								default:
									// received an unknown message - something has gone wrong. Disconnect the peer
									this.Disconnect();
									break;
							}
						}
					}
				}
			}
			catch (System.Exception e)
			{
				Config.LogException(e);
				this.Disconnect();
			}
		}


		#endregion


		#region Sending messages

		
		public void SendInterestedMessage(bool interested)
		{
			try
			{
				if ( socket.Connected )
				{
					if ( !asyncInProgress )
						peerProtocol.SendInterestedMessage( this.netStream, interested );
					else
						this.messageQueue.Add( new object[] { PeerMessage.Interested, interested } );
				}
			}
			catch (System.Exception e)
			{
				Config.LogException(e);
				this.Disconnect();
			}
		}
		
		
		public void SendChokeMessage(bool choked)
		{
			try
			{
				if ( socket.Connected )
				{
					if ( !asyncInProgress )
						peerProtocol.SendChokeMessage( this.netStream, choked );
					else
						this.messageQueue.Add( new object[] { PeerMessage.Choke, choked } );
				}
			}
			catch (System.Exception e)
			{
				Config.LogException(e);
				this.Disconnect();
			}
		}
		

		public void SendHaveMessage(int pieceId)
		{
			try
			{
				if ( socket.Connected )
				{
					if ( !this.asyncInProgress )
						peerProtocol.SendHaveMessage( this.netStream, pieceId );
					else
						this.messageQueue.Add( new object[] { PeerMessage.Have, pieceId } );
				}
			}
			catch (System.Exception e)
			{
				Config.LogException(e);
				this.Disconnect();
			}
		}

		
		public void SendPieceRequest(int pieceId, int begin, int length)
		{
			try
			{
				if ( socket.Connected )
				{
					if ( !this.asyncInProgress )
						peerProtocol.SendPieceRequest( this.netStream, pieceId, begin, length );
					else
						this.messageQueue.Add( new object[] { PeerMessage.Request, pieceId, begin, length } );
				}
			}
			catch (System.Exception e)
			{
				Config.LogException(e);
				this.Disconnect();
			}
		}
		
		
		public void SendPieceCancel(int pieceId, int begin, int length)
		{
			try
			{
				if ( socket.Connected )
				{
					if ( !this.asyncInProgress )
						peerProtocol.SendPieceCancel( this.netStream, pieceId, begin, length );
					else
						this.messageQueue.Add( new object[] { PeerMessage.Cancel, pieceId, begin, length } );
				}
			}
			catch (System.Exception e)
			{
				Config.LogException(e);
				this.Disconnect();
			}
		}
		
		
		public void SendPiece(int pieceId, int begin, int length, IO.Stream istream, PeerPieceCallback pieceSentCallback)
		{
			try
			{
				if ( socket.Connected )
				{
					this.asyncInProgress = true;
					peerProtocol.SendPiece( this.netStream, pieceId, begin, length, istream, new PeerFinishedPieceTransfer( OnWriteFinished ), (object)( new object[] { pieceId, begin, length, pieceSentCallback } ) );
					this.numBytesUploaded += length;
				}
			}
			catch (System.Exception e)
			{
				Config.LogException(e);
				this.Disconnect();
			}
		}

		
		private void OnWriteFinished(object state, bool success)
		{
			try
			{
				this.asyncInProgress = false;

				if (this.netStream == null || !success)
				{
					this.Disconnect();
					return;
				}

				int pieceId = (int)((object[])state)[0];
				int begin = (int)((object[])state)[1];
				int length = (int)((object[])state)[2];
				PeerPieceCallback pieceSentCallback = (PeerPieceCallback)((object[])state)[3];
				pieceSentCallback(this, pieceId, begin, length, null);

				// flush message queue
				foreach (object[] objs in this.messageQueue)
				{
					PeerMessage message = (PeerMessage)objs[0];

					switch (message)
					{
						case PeerMessage.Request:
							this.SendPieceRequest((int)objs[1], (int)objs[2], (int)objs[3]);
							break;
						case PeerMessage.Cancel:
							this.SendPieceCancel((int)objs[1], (int)objs[2], (int)objs[3]);
							break;
						case PeerMessage.Choke:
							this.SendChokeMessage((bool)objs[1]);
							break;
						case PeerMessage.Interested:
							this.SendInterestedMessage((bool)objs[2]);
							break;
						case PeerMessage.Have:
							this.SendHaveMessage((int)objs[1]);
							break;
					}
				}

				this.messageQueue.Clear();
			}
			catch (System.Exception e)
			{
				Config.LogException(e);
				this.Disconnect();
			}
		}


		#endregion


		public override string ToString()
		{
			return this.peerInformation.IP + ":" + this.peerInformation.Port;
		}


		public override int GetHashCode()
		{
			return this.peerInformation.ID.GetHashCode() ;
		}


		public override bool Equals(object obj)
		{
			if (obj is Peer)
			{
				Peer peer = (Peer)obj;
				return peer.peerInformation.Equals(this.peerInformation);
			}
			else
				return false;
		}
	}
}
