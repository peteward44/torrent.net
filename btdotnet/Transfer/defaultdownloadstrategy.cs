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


	public class DownloadStrategyHelp
	{
		public static int DecideNextPiece( DownloadStrategyManager manager, Peer peer )
		{
			int pieceId = -1;
			int lowestNumPeers = -1;

			// find the rarest piece in the swarm which is owned by the peer
			for ( int i = 0; i < manager.PieceCounter.PieceCount; ++i )
			{
				// make sure the peer owns the piece, the piece hasn't already been downloaded,
				// has at least one peer and isn't in the process of being downloaded
				// by another peer
				if ( !manager.Torrent.DownloadFile.Bitfield.Get( i ) && manager.PieceCounter[ i ] > 0 && !manager.IsPieceDownloading( i ) )
				{
					if ( peer != null && !peer.BitField.Get( i ) )
						continue;

					// see if the piece has the lowest number of peers so far
					if ( manager.PieceCounter[ i ] < lowestNumPeers || lowestNumPeers == -1 )
					{
						lowestNumPeers = manager.PieceCounter[ i ];
						pieceId = i;
					}
				}
			}

			return pieceId;
		}


		public static int CalculatePieceSectionLength( DownloadStrategyManager manager, int pieceId, int begin )
		{
			int pieceDataLeft = manager.Torrent.Metainfo.GetPieceLength( pieceId ) - begin;
			return pieceDataLeft < Config.MaxRequestSize ? pieceDataLeft : Config.MaxRequestSize;
		}


		public static void SetInterestedOnPeerIfNecessary( DownloadStrategyManager manager, Peer peer )
		{
			SetInterestedOnPeerIfNecessary( manager, peer, -1 );
		}

		public static void SetInterestedOnPeerIfNecessary( DownloadStrategyManager manager, Peer peer, int pieceId )
		{
			if ( pieceId >= 0 )
			{
				// new piece came in
				if ( !manager.Torrent.DownloadFile.Bitfield.Get( pieceId ) )
					peer.AmIInterested = true;
			}
			else
			{
				// whole bitfield has changed
				if ( DoesPeerHavePieceWeDont( manager, peer ) )
					peer.AmIInterested = true;
			}
		}

		public static bool DoesPeerHavePieceWeDont( DownloadStrategyManager manager, Peer peer )
		{
			for ( int i = 0; i < manager.Torrent.Metainfo.PieceCount; ++i )
			{
				if ( peer.BitField.Get( i ) && !manager.Torrent.DownloadFile.Bitfield.Get( i ) )
					return true;
			}
			return false;
		}
	}


	public class EndGameDownloadStrategy : DownloadStrategy
	{
		private DownloadStrategyManager mManager;

		//private ArrayList availablePeers = new ArrayList(); // list of peers that are not doing anything and have unchoked us
		//private ArrayList endGamePeers = new ArrayList(); // list of peers we sent the same request to which we need to cancel
		private PieceRequest mEndGameRequest = null; // current request sent by endgame
		private List<Peer> mEndGamePeers = new List<Peer>();


		public EndGameDownloadStrategy( DownloadStrategyManager manager )
		{
			mManager = manager;
		}


		private void SendCurrentSectionRequestToPeer( Peer peer )
		{
			// if the peer isn't choking, isn't currently downloading anything and the maximum number of downloads hasn't been reached
			// we can authorise a request to be sent
			if ( !peer.HeIsChoking && peer.AmIInterested && !this.mManager.IsPeerDownloading( peer ) )// && this.piecesDownloading.Count < Config.ActiveConfig.SimultaneousDownloadsLimit)
			{
				if ( this.mEndGameRequest == null )
					this.UpdateRequestWithNextPiece();

				if ( this.mEndGameRequest != null )
				{
					if ( peer.BitField.Get( this.mEndGameRequest.PieceId ) )
					{
						mManager.SendPieceRequestToPeer( peer, this.mEndGameRequest.PieceId, this.mEndGameRequest.Begin, this.mEndGameRequest.Length );
						this.mEndGamePeers.Add( peer );
						Config.LogDebugMessage( "Sent end game request to peer: " + peer.ToString() );
					}
				}
			}
		}


		private void UpdateRequestWithNextPiece()
		{
			// else get a new piece to find
			int pieceId = DownloadStrategyHelp.DecideNextPiece( mManager, null );
			if ( pieceId >= 0 )
			{
				int pieceSectionLength = DownloadStrategyHelp.CalculatePieceSectionLength( mManager, pieceId, 0 );
				this.mEndGameRequest = new PieceRequest( null, pieceId, 0, pieceSectionLength );
			}
		}


		private void UpdateEndGameRequest()
		{
			if ( this.mEndGameRequest == null )
				UpdateRequestWithNextPiece();
			else
			{
				// continue the old piece if it hasnt finished
				int pieceLength = DownloadStrategyHelp.CalculatePieceSectionLength( mManager, this.mEndGameRequest.PieceId, this.mEndGameRequest.Begin + this.mEndGameRequest.Length );
				if ( pieceLength <= 0 )
					UpdateRequestWithNextPiece();
				else
				{
					this.mEndGameRequest.Begin += this.mEndGameRequest.Length;
					this.mEndGameRequest.Length = pieceLength;
				}
			}
		}


		public void peer_BitfieldChange( bool isActive, Peer peer, int pieceId )
		{
			if ( isActive )
			{
				DownloadStrategyHelp.SetInterestedOnPeerIfNecessary( mManager, peer, pieceId );
				SendCurrentSectionRequestToPeer( peer );
			}
		}


		public void peer_Disconnected( bool isActive, Peer peer )
		{
		}


		public void peer_HeIsChokingChange( bool isActive, Peer peer, bool choked )
		{
			if ( isActive )
			{
				if ( !choked )
				{
					// peer has unchoked us, see if we can download from him
					this.SendCurrentSectionRequestToPeer( peer );
				}
			}
		}


		public void peer_PieceSectionFinished( bool isActive, Peer peer, int pieceId, int begin, int length )
		{
			if ( this.mEndGameRequest != null
				&& pieceId == this.mEndGameRequest.PieceId && begin == this.mEndGameRequest.Begin && length == this.mEndGameRequest.Length )
			{
				Config.LogDebugMessage( "End game piece section received from peer: " + peer.ToString() );

				// if an end game piece just came in, cancel the rest of the peers
				foreach ( Peer endGamePeer in this.mEndGamePeers )
				{
					if ( !endGamePeer.Equals( peer ) )
					{
						mManager.SendPieceCancelToPeer( endGamePeer, this.mEndGameRequest.PieceId, this.mEndGameRequest.Begin, this.mEndGameRequest.Length );
						Config.LogDebugMessage( "Sent cancel to peer: " + endGamePeer.ToString() );
					}
				}

				this.mEndGamePeers.Clear();

				// choose the next end game piece
				this.UpdateEndGameRequest();

				// send it to all available peers
				foreach ( Peer availablePeer in this.mManager.PeersAvailableToDownload )
				{
					SendCurrentSectionRequestToPeer( availablePeer );
					Config.LogDebugMessage( "Sent end game request to peer: " + availablePeer.ToString() );
				}
			}
		}


		public void peer_PieceFinished( bool isActive, Peer peer, int pieceId )
		{
			// check all peers we are interested in to see if we should turn off interest now we have a new piece.
			if ( !DownloadStrategyHelp.DoesPeerHavePieceWeDont( mManager, peer ) )
				peer.AmIInterested = false;
		}


		public void OnActivate( bool activate )
		{
		}


		public void OnDownloadFinished( bool isActive )
		{
			if ( isActive )
			{
				// Add to a new list as the Torrent.Peers list will change while we are iterating over it
				List<Peer> seeds = new List<Peer>();
				foreach ( Peer peer in this.mManager.Torrent.Peers )
				{
					if ( peer.IsSeed )
						seeds.Add( peer );
				}
				foreach ( Peer peer in seeds )
				{
					peer.Disconnect();
				}
			}
		}
	}


	public class DefaultDownloadStrategy : DownloadStrategy
	{
		private EndGameDownloadStrategy mEndGameStrategy;
		private DownloadStrategyManager mManager;


		private void StartDownloadingNextPieceIfPossible( Peer peer )
		{
			// if the peer isn't choking, isn't currently downloading anything and the maximum number of downloads hasn't been reached
			// we can authorise a request to be sent
			if ( !peer.HeIsChoking && peer.AmIInterested && !this.mManager.IsPeerDownloading( peer ) )// && this.piecesDownloading.Count < Config.ActiveConfig.SimultaneousDownloadsLimit)
			{
				int pieceId = DownloadStrategyHelp.DecideNextPiece( mManager, peer );
				if ( pieceId >= 0 )
					mManager.SendPieceRequestToPeer( peer, pieceId, 0, DownloadStrategyHelp.CalculatePieceSectionLength( mManager, pieceId, 0 ) );
			}
		}


		private void CheckForEndGame()
		{
			// check if endgame needs to be enabled
			if ( Config.ActiveConfig.EnableEndGame
				&& Config.ActiveConfig.EndGamePercentage >= this.mManager.Torrent.DownloadFile.Bitfield.PercentageFalse
				&& !this.mManager.Torrent.DownloadFile.Bitfield.AllTrue )
			{
				Config.LogDebugMessage( "End game enabled" );
				this.mManager.SetActiveStrategy( this.mEndGameStrategy );
			}
		}


		public DefaultDownloadStrategy( DownloadStrategyManager manager )
		{
			this.mManager = manager;
			this.mEndGameStrategy = new EndGameDownloadStrategy( manager );
			this.mManager.AddNewStrategy( this.mEndGameStrategy );
		}


		public void peer_BitfieldChange( bool isActive, Peer peer, int pieceId )
		{
			if ( isActive )
			{
				DownloadStrategyHelp.SetInterestedOnPeerIfNecessary( mManager, peer, pieceId );

				// if the peer has a piece we want now, see if we want to/can download it
				this.StartDownloadingNextPieceIfPossible( peer );
			}
		}


		public void peer_Disconnected( bool isActive, Peer peer )
		{

		}


		public void peer_HeIsChokingChange( bool isActive, Peer peer, bool choked )
		{
			if ( !choked )
			{
				// peer has unchoked us, see if we can download from him
				if ( isActive )
					this.StartDownloadingNextPieceIfPossible( peer );
			}
		}


		public void peer_PieceSectionFinished( bool isActive, Peer peer, int pieceId, int begin, int length )
		{
			if ( isActive )
			{
				mManager.SendPieceRequestToPeer( peer, pieceId, begin + length,
					DownloadStrategyHelp.CalculatePieceSectionLength( mManager, pieceId, begin + length ) );
			}
		}


		public void peer_PieceFinished( bool isActive, Peer peer, int pieceId )
		{
			if ( isActive )
			{
				this.CheckForEndGame();
				this.StartDownloadingNextPieceIfPossible( peer );

				// check all peers we are interested in to see if we should turn off interest now we have a new piece.
				foreach ( Peer ipeer in this.mManager.Torrent.Peers )
				{
					if ( !DownloadStrategyHelp.DoesPeerHavePieceWeDont( mManager, ipeer ) )
						ipeer.AmIInterested = false;
				}
			}
		}


		public void OnActivate( bool activate )
		{
		}


		public void OnDownloadFinished( bool isActive ) { }
	}

}


