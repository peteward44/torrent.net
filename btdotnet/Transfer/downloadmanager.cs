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


//namespace BitTorrent
//{
//  public delegate void PieceFinishedCallback(int pieceId);


//  public class DownloadManager
//  {
//    private MetainfoFile infofile;
//    private DownloadFile downloadFile;

//    private ArrayList piecesDownloading = new ArrayList(); // ArrayList of ints of pieces currently being downloaded
//    private int[] peerPieceList; // An array of ints which counts the number of pieces in the swarm. (piece id = array index)

//    private bool endGame = false;
//    private ArrayList availablePeers = new ArrayList(); // list of peers that are not doing anything and have unchoked us
//    private ArrayList endGamePeers = new ArrayList(); // list of peers we sent the same request to which we need to cancel
//    private PieceRequest endGameRequest = null; // current request sent by endgame
//    private Piece endGamePiece = null; // current piece downloading with endgame


//    public event PieceFinishedCallback PieceFinished;


	
//    public DownloadManager(MetainfoFile infofile, DownloadFile downloadFile)
//    {
//      this.infofile = infofile;
//      this.downloadFile = downloadFile;

//      this.peerPieceList = new int[ this.infofile.PieceCount ];
//      for (int i=0; i<this.peerPieceList.Length; ++i)
//        this.peerPieceList[i] = 0;
//    }


//    public void Start()
//    {
//      this.CheckForEndGame();
//    }
		
		
//    public void AddPeer(Peer peer)
//    {
//      // hook peer events
//      peer.BitfieldChange += new PeerBitfieldChangeCallback(peer_BitfieldChange);
//      peer.Disconnected += new PeerDisconnectedCallback(peer_Disconnected);
//      peer.HeIsChokingChange += new PeerStatusChangeCallback(peer_HeIsChokingChange);
//      peer.PieceIncoming += new PeerPieceCallback(peer_PieceIncoming);
//    }


//    private void peer_BitfieldChange(Peer peer, int pieceId)
//    {
////			Config.LogDebugMessage( "DownloadManager::peer_BitfieldChange" );

//      if (pieceId != -1)
//      {
//        // new piece came in
//        this.peerPieceList[pieceId]++;
//        if (!this.downloadFile.Bitfield.Get(pieceId))
//          peer.AmIInterested = true;

//        // if the peer has a piece we want now, see if we want to/can download it
//        this.StartDownloadingNextPiece(peer);
//      }
//      else
//      {
//        // whole bitfield has changed
//        for (int i=0; i<this.infofile.PieceCount; ++i)
//        {
//          if (peer.BitField.Get(i))
//          {
//            this.peerPieceList[i]++;
//            if (!this.downloadFile.Bitfield.Get(i))
//              peer.AmIInterested = true;
//          }
//        }
//      }
//    }


//    private void peer_Disconnected(Peer peer)
//    {
//      // remove from peer-piece index
//      if (peer.BitField != null)
//      {
//        for (int i=0; i<this.peerPieceList.Length; ++i)
//        {
//          if (peer.BitField.Get(i))
//            this.peerPieceList[i]--;
//        }
//      }

//      // remove from pieces downloading if downloading
//      if (peer.PieceDownloading != null && this.piecesDownloading.Contains(peer.PieceDownloading.PieceID))
//      {
//        this.piecesDownloading.Remove(peer.PieceDownloading.PieceID);
//        peer.PieceDownloading = null;
//      }

//      // remove from available peer list (if there)
//      lock ( this.availablePeers.SyncRoot )
//      {
//        if ( this.availablePeers.Contains( peer ) )
//          this.availablePeers.Remove( peer );
//      }

//      lock ( this.endGamePeers.SyncRoot )
//      {
//        if ( this.endGamePeers.Contains( peer ) )
//          this.endGamePeers.Remove( peer );
//      }
//    }


//    private void peer_HeIsChokingChange(Peer peer, bool choked)
//    {
//      if (choked)
//      {
//        // remove from pieces downloading if downloading
//        if (peer.PieceDownloading != null && this.piecesDownloading.Contains(peer.PieceDownloading.PieceID))
//        {
//          this.piecesDownloading.Remove(peer.PieceDownloading.PieceID);
//          peer.PieceDownloading = null;
//        }
//      }
//      else
//      {
//        // peer has unchoked us, see if we can download from him
//        this.StartDownloadingNextPiece(peer);
//      }
//    }


//    private void CheckWeAreStillInterested()
//    {

//    }


//    private void peer_PieceIncoming(Peer peer, int pieceId, int begin, int length, byte[] data)
//    {
//      if (this.endGame && this.endGameRequest != null
//        && pieceId == this.endGameRequest.PieceId && begin == this.endGameRequest.Begin && length == this.endGameRequest.Length)
//      {
//        Config.LogDebugMessage("End game piece section received from peer: " + peer.ToString());

//        // if an end game piece just came in, cancel the rest of the peers
//        lock ( this.endGamePeers.SyncRoot )
//        {
//          foreach ( Peer endGamePeer in this.endGamePeers )
//          {
//            if ( !endGamePeer.Equals( peer ) )
//            {
//              endGamePeer.SendPieceCancel( this.endGameRequest.PieceId, this.endGameRequest.Begin, this.endGameRequest.Length );
//              Config.LogDebugMessage( "Sent cancel to peer: " + endGamePeer.ToString() );
//            }

//            lock ( this.availablePeers.SyncRoot )
//            {
//              if ( !this.availablePeers.Contains( endGamePeer ) )
//                this.availablePeers.Add( endGamePeer );
//            }
//          }

//          this.endGamePeers.Clear();
//        }

//        // save data
//        this.endGamePiece.Write(data, 0, length, begin);

//        if (this.endGamePiece.FullyDownloaded)
//        {
//          IO.MemoryStream pstream = new IO.MemoryStream(this.infofile.GetPieceLength(pieceId));
//          this.endGamePiece.Read(pstream, this.infofile.GetPieceLength(pieceId), 0);
//          pstream.Seek(0, IO.SeekOrigin.Begin);

//          if (this.downloadFile.SaveToFile(pieceId, pstream))
//          {
//            if (this.PieceFinished != null)
//              this.PieceFinished(pieceId);
//          }
//        }

//        // choose the next end game piece
//        this.GetEndGameRequest();

//        // send it to all available peers
//        lock ( this.availablePeers.SyncRoot )
//        {
//          foreach ( Peer availablePeer in this.availablePeers )
//          {
//            availablePeer.SendPieceRequest( this.endGameRequest.PieceId, this.endGameRequest.Begin, this.endGameRequest.Length );

//            lock ( this.endGamePeers.SyncRoot )
//            {
//              this.endGamePeers.Add( availablePeer );
//            }

//            Config.LogDebugMessage( "Sent end game request to peer: " + availablePeer.ToString() );
//          }

//          this.availablePeers.Clear();
//        }
//      }
//      else if (peer.PieceDownloading != null)
//      {
//        // not an end game piece, continue as normal
//        peer.PieceDownloading.Write(data, 0, length, begin);

//        if (peer.PieceDownloading.FullyDownloaded)
//        {
//          IO.MemoryStream pstream = new IO.MemoryStream(this.infofile.GetPieceLength(pieceId));
//          peer.PieceDownloading.Read(pstream, this.infofile.GetPieceLength(pieceId), 0);
//          pstream.Seek(0, IO.SeekOrigin.Begin);

//          this.piecesDownloading.Remove(peer.PieceDownloading.PieceID);

//          if (this.downloadFile.SaveToFile(peer.PieceDownloading.PieceID, pstream))
//          {
//            // we finished one piece successfully, see if we can start another
//            peer.PieceDownloading = null;

//            if (this.PieceFinished != null)
//              this.PieceFinished(pieceId);

//            // now a piece has finished, check if we have reached the requirements to use endgame.
//            this.CheckForEndGame();
//            this.StartDownloadingNextPiece(peer);
//          }
//          else
//          {
//            // TODO: bad data, ban peer?
//          }
//        }
//        else
//        {
//          // start downloading the next piece-section
//          int pieceDataLeft = this.infofile.GetPieceLength(pieceId) - (begin + length);
//          int pieceSectionLength = pieceDataLeft < Config.MaxRequestSize ? pieceDataLeft : Config.MaxRequestSize;

//          peer.SendPieceRequest(pieceId, begin + length, pieceSectionLength);
//        }
//      }
//    }


//    private void CheckForEndGame()
//    {
//      // check if endgame needs to be enabled
//      if (!this.endGame && Config.ActiveConfig.EnableEndGame
//        && Config.ActiveConfig.EndGamePercentage >= this.downloadFile.Bitfield.PercentageFalse
//        && !this.downloadFile.Bitfield.AllTrue)
//      {
//        Config.LogDebugMessage("End game enabled");
//        this.endGame = true;

//        // get a piece to download
//        this.GetEndGameRequest();
//      }
//    }


//    private void GetEndGameRequest()
//    {
//      if (this.endGameRequest != null && this.endGameRequest.Begin != (this.infofile.GetPieceLength(this.endGameRequest.PieceId) - this.endGameRequest.Length))
//      {
//        // continue the old piece if it hasnt finished
//        int pieceDataLeft = this.infofile.GetPieceLength(this.endGameRequest.PieceId) - (this.endGameRequest.Begin + this.endGameRequest.Length);
//        int pieceSectionLength = pieceDataLeft < Config.MaxRequestSize ? pieceDataLeft : Config.MaxRequestSize;

//        this.endGameRequest.Begin += this.endGameRequest.Length;
//        this.endGameRequest.Length = pieceSectionLength;
//      }
//      else
//      {
//        // else get a new piece to find
//        int pieceId = this.DecideNextPiece(null);
//        if (pieceId != -1)
//        {
//          int pieceSectionLength = this.infofile.GetPieceLength(pieceId) < Config.MaxRequestSize ? this.infofile.GetPieceLength(pieceId) : Config.MaxRequestSize;
//          this.endGameRequest = new PieceRequest(null, pieceId, 0, pieceSectionLength);
//          this.endGamePiece = new Piece(this.infofile, pieceId);
//        }
//      }
//    }


//    private void StartDownloadingNextPiece(Peer peer)
//    {
//      // if the peer isn't choking, isn't currently downloading anything and the maximum number of downloads hasn't been reached
//      // we can authorise a request to be sent
//      if (!peer.HeIsChoking && peer.PieceDownloading == null)// && this.piecesDownloading.Count < Config.ActiveConfig.SimultaneousDownloadsLimit)
//      {
//        if (!this.endGame)
//        {
//          int pieceId = this.DecideNextPiece(peer);
//          if (pieceId >= 0)
//          {
//            int pieceSectionLength = this.infofile.GetPieceLength(pieceId) < Config.MaxRequestSize ? this.infofile.GetPieceLength(pieceId) : Config.MaxRequestSize;

//            peer.PieceDownloading = new Piece(this.infofile, pieceId);
//            this.piecesDownloading.Add(pieceId);

//            peer.SendPieceRequest(pieceId, 0, pieceSectionLength);
//          }
//          else
//          {
//            lock ( this.availablePeers.SyncRoot )
//            {
//              if ( !this.availablePeers.Contains( peer ) )
//                this.availablePeers.Add( peer );
//            }
//          }
//        }
//        else
//        {
//          if (this.endGameRequest == null)
//            this.GetEndGameRequest();

//          if (this.endGameRequest != null)
//          {
//            peer.SendPieceRequest(this.endGameRequest.PieceId, this.endGameRequest.Begin, this.endGameRequest.Length);

//            lock ( this.endGamePeers.SyncRoot )
//            {
//              this.endGamePeers.Add( peer );
//            }
//            Config.LogDebugMessage("Sent end game request to peer: " + peer.ToString());
//          }
//        }
//      }
//    }


//    private int DecideNextPiece(Peer peer)
//    {
//      int pieceId = -1;
//      int lowestNumPeers = -1;

//      // find the rarest piece in the swarm which is owned by the peer
//      for (int i=0; i<this.peerPieceList.Length; ++i)
//      {
//        // make sure the peer owns the piece, the piece hasn't already been downloaded,
//        // has at least one peer and isn't in the process of being downloaded
//        // by another peer
//        if (!this.downloadFile.Bitfield.Get(i) && this.peerPieceList[i] > 0 && !this.piecesDownloading.Contains(i))
//        {
//          if (peer != null && !peer.BitField.Get(i))
//            continue;

//          // see if the piece has the lowest number of peers so far
//          if (this.peerPieceList[i] < lowestNumPeers || lowestNumPeers == -1)
//          {
//            lowestNumPeers = this.peerPieceList[i];
//            pieceId = i;
//          }
//        }
//      }
			
//      return pieceId;
//    }
//  }
//}
