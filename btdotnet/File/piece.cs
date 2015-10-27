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

using IO = System.IO;


namespace BitTorrent
{

	public class PieceRequest
	{
		public Peer Peer;
		public int PieceId, Begin, Length;

		public PieceRequest( Peer peer, int pieceId, int begin, int length )
		{
			this.Peer = peer;
			this.PieceId = pieceId;
			this.Begin = begin;
			this.Length = length;
		}
	}



	/// <summary>
	/// Represents a piece of the torrent file
	/// </summary>
	public class Piece : System.ICloneable
	{
		private MetainfoFile infofile;
		private int pieceId, dataWritten = 0;
		private byte[] data;


		/// <summary></summary>
		/// <returns>True if the piece has been completely written, false otherwise</returns>
		public bool FullyDownloaded
		{
			get { return this.infofile.GetPieceLength(this.pieceId)-1 <= this.dataWritten; }
		}
		
		
		/// <summary></summary>
		/// <returns>Piece ID</returns>
		public int PieceID
		{
			get { return this.pieceId; }
		}


		/// <summary>
		/// Constructs a piece
		/// </summary>
		/// <param name="infofile">Metainfo file for torrent</param>
		/// <param name="pieceId">ID for piece</param>
		public Piece(MetainfoFile infofile, int pieceId)
		{
			this.infofile = infofile;
			this.pieceId = pieceId;

			this.data = new byte[ this.infofile.GetPieceLength(pieceId) ];
		}


		/// <summary></summary>
		/// <returns>Unique hashcode for this piece</returns>
		public override int GetHashCode()
		{
			return this.pieceId;
		}


		/// <summary></summary>
		/// <param name="obj">Object to compare to</param>
		/// <returns>True if the objects are the same, false otherwise</returns>
		public override bool Equals(object obj)
		{
			if (obj is Piece)
			{
				Piece req = (Piece)obj;
				return req.pieceId == this.pieceId;
			}
			else
				return false;
		}


		/// <summary>Writes data to the piece</summary>
		/// <param name="data">Data to write</param>
		/// <param name="offset">Offset within data to start copying from</param>
		/// <param name="length">Amount of data to write</param>
		/// <param name="poffset">Offset within the piece to write data to</param>
		public void Write(byte[] data, int offset, int length, int poffset)
		{
			System.Array.Copy(data, offset, this.data, poffset, length);
			this.dataWritten += length;
		}
		
		
		/// <summary>Writes data to the piece</summary>
		/// <param name="stream">Data to write</param>
		/// <param name="length">Amount of data to write</param>
		/// <param name="poffset">Offset within the piece to write data to</param>
		public void Write(IO.Stream stream, int length, int poffset)
		{
			byte[] tdata = new byte[length];
			stream.Read(tdata, 0, length);
			this.Write(tdata, 0, length, poffset);
		}


		/// <summary>Reads data from the piece</summary>
		/// <param name="data">Data to read into</param>
		/// <param name="offset">Offset within data to start copying from</param>
		/// <param name="length">Amount of data to read</param>
		/// <param name="poffset">Offset within the piece to read data from</param>
		public void Read(byte[] data, int offset, int length, int poffset)
		{
			System.Array.Copy(this.data, poffset, data, offset, length);
		}
		
		
		/// <summary>Reads data from the piece</summary>
		/// <param name="stream">Data to read into</param>
		/// <param name="length">Amount of data to read</param>
		/// <param name="poffset">Offset within the piece to read data from</param>
		public void Read(IO.Stream stream, int length, int poffset)
		{
			byte[] tdata = new byte[length];
			this.Read(tdata, 0, length, poffset);
			stream.Write(tdata, 0, length);
		}
		
		
		#region ICloneable members
		
		
		/// <summary>Creates a clone of the piece</summary>
		/// <returns>Clone</returns>
		object System.ICloneable.Clone()
		{
			return this.Clone();
		}
		
		
		/// <summary>Creates a clone of the piece</summary>
		/// <returns>Clone</returns>
		public Piece Clone()
		{
			return (Piece)this.MemberwiseClone();
		}
		
		
		#endregion
	}
}
