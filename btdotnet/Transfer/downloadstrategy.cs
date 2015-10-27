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

using System.Collections.Generic;
using IO = System.IO;


namespace BitTorrent
{

	public interface DownloadStrategy
	{
		void peer_BitfieldChange( bool isActive, Peer peer, int pieceId );
		void peer_Disconnected( bool isActive, Peer peer );
		void peer_HeIsChokingChange( bool isActive, Peer peer, bool choked );
		void peer_PieceSectionFinished( bool isActive, Peer peer, int pieceId, int begin, int length );
		void peer_PieceFinished( bool isActive, Peer peer, int pieceId );

		void OnDownloadFinished( bool isActive );

		void OnActivate( bool activate );

	}


	public class PieceCounter
	{
		private int[] mPeerPieceList; // An array of ints which counts the number of pieces in the swarm. (piece id = array index)


		public PieceCounter( int pieceCount )
		{
			this.mPeerPieceList = new int[ pieceCount ];
			for ( int i = 0; i < this.mPeerPieceList.Length; ++i )
				this.mPeerPieceList[ i ] = 0;
		}


		public int GetPieceFrequency( int pieceId )
		{
			return mPeerPieceList[ pieceId ];
		}

		public int this[ int pieceId ]
		{
			get { return GetPieceFrequency( pieceId ); }
		}

		public int PieceCount
		{
			get { return mPeerPieceList.Length; }
		}


		public void HookPeerEvents( Peer peer )
		{
			peer.BitfieldChange += new PeerBitfieldChangeCallback( peer_BitfieldChange );
			peer.Disconnected += new PeerDisconnectedCallback( peer_Disconnected );
		}


		private void AddPeerBitfieldToPieceCount( Peer peer )
		{
			// add to peer-piece index
			if ( peer.BitField != null )
			{
				for ( int i = 0; i < this.mPeerPieceList.Length; ++i )
				{
					if ( peer.BitField.Get( i ) )
						this.mPeerPieceList[ i ]++;
				}
			}
		}


		private void RemovePeerBitfieldFromPieceCount( Peer peer )
		{
			// remove from peer-piece index
			if ( peer.BitField != null )
			{
				for ( int i = 0; i < this.mPeerPieceList.Length; ++i )
				{
					if ( peer.BitField.Get( i ) )
						this.mPeerPieceList[ i ]--;
				}
			}
		}


		private void peer_Disconnected( Peer peer )
		{
			RemovePeerBitfieldFromPieceCount( peer );

			peer.BitfieldChange -= new PeerBitfieldChangeCallback( peer_BitfieldChange );
			peer.Disconnected -= new PeerDisconnectedCallback( peer_Disconnected );
		}


		private void peer_BitfieldChange( Peer peer, int pieceId )
		{
			if ( pieceId != -1 )
			{
				// new piece came in
				this.mPeerPieceList[ pieceId ]++;
			}
			else
			{
				// whole bitfield has changed, remove the old peer information then add the new one
				RemovePeerBitfieldFromPieceCount( peer );
				AddPeerBitfieldToPieceCount( peer );
			}
		}
	}


	public class DownloadStrategyManager
	{
		private Torrent mTorrent;
		private PieceCounter mPieceCounter;

		private List<DownloadStrategy> mStrategies = new List<DownloadStrategy>();
		private DownloadStrategy mActiveStrategy = null;

		private Dictionary<int, Piece> mCurrentlyDownloadingPieces = new Dictionary<int, Piece>();
		private List<Peer> mDownloadingPeers = new List<Peer>();
		private List<Peer> mPeersAvailableToDownload = new List<Peer>();

		private int mBytesDownloaded = 0;


		public Torrent Torrent
		{
			get { return mTorrent; }
		}


		public int BytesDownloaded
		{
			get { return mBytesDownloaded; }
		}


		public List<Peer> PeersAvailableToDownload
		{
			get { return mPeersAvailableToDownload; }
		}


		public PieceCounter PieceCounter
		{
			get { return mPieceCounter; }
		}


		public bool IsPieceDownloading( int pieceId )
		{
			return mCurrentlyDownloadingPieces.ContainsKey( pieceId );
		}

		public bool IsPeerDownloading( Peer peer )
		{
			return mDownloadingPeers.Contains( peer );
		}


		public DownloadStrategyManager( Torrent torrent )
		{
			this.mTorrent = torrent;
			this.mPieceCounter = new PieceCounter( this.mTorrent.PieceCount );
		}


		public void AddNewStrategy( DownloadStrategy strategy )
		{
			mStrategies.Add( strategy );
		}


		public void SetActiveStrategy( DownloadStrategy strategy )
		{
			mActiveStrategy = strategy;
		}


		public void HookPeerEvents( Peer peer )
		{
			peer.BitfieldChange += new PeerBitfieldChangeCallback( peer_BitfieldChange );
			peer.Disconnected += new PeerDisconnectedCallback( peer_Disconnected );
			peer.HeIsChokingChange += new PeerStatusChangeCallback( peer_HeIsChokingChange );
			peer.PieceIncoming += new PeerPieceCallback( peer_PieceIncoming );

			mPieceCounter.HookPeerEvents( peer );
		}


		private void peer_Disconnected( Peer peer )
		{
			peer.BitfieldChange -= new PeerBitfieldChangeCallback( peer_BitfieldChange );
			peer.Disconnected -= new PeerDisconnectedCallback( peer_Disconnected );
			peer.HeIsChokingChange -= new PeerStatusChangeCallback( peer_HeIsChokingChange );
			peer.PieceIncoming -= new PeerPieceCallback( peer_PieceIncoming );

			if ( this.mDownloadingPeers.Contains( peer ) )
				this.mDownloadingPeers.Remove( peer );

			if ( this.mPeersAvailableToDownload.Contains( peer ) )
				this.mPeersAvailableToDownload.Remove( peer );

			foreach ( DownloadStrategy strategy in mStrategies )
			{
				strategy.peer_Disconnected( strategy == mActiveStrategy, peer );
			}
		}

		private void peer_BitfieldChange( Peer peer, int pieceId )
		{
			foreach ( DownloadStrategy strategy in mStrategies )
			{
				strategy.peer_BitfieldChange( strategy == mActiveStrategy, peer, pieceId );
			}
		}

		private void peer_HeIsChokingChange( Peer peer, bool choked )
		{
			// remove from pieces downloading if downloading
			if ( choked )
			{
				if ( this.mDownloadingPeers.Contains( peer ) )
					this.mDownloadingPeers.Remove( peer );
				if ( this.mPeersAvailableToDownload.Contains( peer ) )
					this.mPeersAvailableToDownload.Remove( peer );
			}
			else
			{
				if ( !this.mPeersAvailableToDownload.Contains( peer ) )
					this.mPeersAvailableToDownload.Add( peer );
			}

			foreach ( DownloadStrategy strategy in mStrategies )
			{
				strategy.peer_HeIsChokingChange( strategy == mActiveStrategy, peer, choked );
			}
		}

		private void peer_PieceIncoming( Peer peer, int pieceId, int begin, int length, byte[] data )
		{
			// save the piece data then inform the strategies
			if ( !mCurrentlyDownloadingPieces.ContainsKey( pieceId ) )
			{
				Config.LogDebugMessage( "INVALID PIECE INCOMING: " + pieceId );
				return;
			}

			Piece piece = mCurrentlyDownloadingPieces[ pieceId ];
			piece.Write( data, 0, length, begin );

			if ( !peer.HeIsChoking && !this.mPeersAvailableToDownload.Contains( peer ) )
				this.mPeersAvailableToDownload.Add( peer );

			if ( mDownloadingPeers.Contains( peer ) )
				mDownloadingPeers.Remove( peer );

			if ( piece.FullyDownloaded )
			{
				IO.MemoryStream pstream = new IO.MemoryStream( this.mTorrent.Metainfo.GetPieceLength( pieceId ) );
				piece.Read( pstream, this.mTorrent.Metainfo.GetPieceLength( pieceId ), 0 );
				pstream.Seek( 0, IO.SeekOrigin.Begin );

				if ( mCurrentlyDownloadingPieces.ContainsKey( pieceId ) )
					mCurrentlyDownloadingPieces.Remove( pieceId );

				if ( this.mTorrent.DownloadFile.SaveToFile( pieceId, pstream ) )
				{
					// Piece successfully downloaded, inform strategies and update other peers
					foreach ( DownloadStrategy strategy in mStrategies )
					{
						strategy.peer_PieceFinished( strategy == mActiveStrategy, peer, pieceId );
					}

					if ( this.mTorrent.DownloadFile.Bitfield.AllTrue )
					{
						foreach ( DownloadStrategy strategy in mStrategies )
						{
							strategy.OnDownloadFinished( strategy == mActiveStrategy );
						}
					}

					// piece just finished, announce to everyone we have it
					List<Peer> disconnectPeers = new List<Peer>();
					foreach ( Peer ipeer in this.mTorrent.Peers )
					{
						try
						{
							ipeer.SendHaveMessage( pieceId );
						}
						catch ( System.Exception e )
						{
							Config.LogException( e );
							disconnectPeers.Add( ipeer );
						}
					}
					foreach ( Peer ipeer in disconnectPeers )
					{
						ipeer.Disconnect();
					}

					this.mBytesDownloaded += this.mTorrent.Metainfo.GetPieceLength( pieceId );
				}
				else
				{ // bad data has been sent by a peer TODO: ban?
				}
			}

			foreach ( DownloadStrategy strategy in mStrategies )
			{
				strategy.peer_PieceSectionFinished( strategy == mActiveStrategy, peer, pieceId, begin, length );
			}
		}


		public void SendPieceRequestToPeer( Peer peer, int pieceId, int begin, int length )
		{
			if ( !this.mDownloadingPeers.Contains( peer ) )
				this.mDownloadingPeers.Add( peer );
			if ( this.mPeersAvailableToDownload.Contains( peer ) )
				this.mPeersAvailableToDownload.Remove( peer );

			if ( !mCurrentlyDownloadingPieces.ContainsKey( pieceId ) )
			{
				Piece piece = new Piece( this.mTorrent.Metainfo, pieceId );
				mCurrentlyDownloadingPieces.Add( pieceId, piece );
			}

			peer.SendPieceRequest( pieceId, begin, length );
		}


		public void SendPieceCancelToPeer( Peer peer, int pieceId, int begin, int length )
		{
			if ( this.mDownloadingPeers.Contains( peer ) )
				this.mDownloadingPeers.Remove( peer );
			if ( !this.mPeersAvailableToDownload.Contains( peer ) )
				this.mPeersAvailableToDownload.Add( peer );

			peer.SendPieceCancel( pieceId, begin, length );
		}
	}
}


