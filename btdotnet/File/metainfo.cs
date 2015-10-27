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
using PWLib.Platform;

namespace BitTorrent
{
	/// <summary>
	/// Every torrent has it's own Metainfo file that the user downloads (usually ends in .torrent).
	/// This file uses "bEncode" encoding to format it's data, the actual parsing code is found in class BEncode.
	/// Class MetainfoFile simply gets the data out of this file and presents it in a usable fashion.
	/// </summary>
	public class MetainfoFile
	{
		private ByteField20 infoDigest;

		private ArrayList shaDigestList = new ArrayList(); // ArrayList of SHADigest
		private ArrayList fileList = new ArrayList(); // ArrayList of IO.FileInfo
		private ArrayList fileLengthList = new ArrayList(); // ArrayList of int
		
		private int pieceLength, totalSize = 0;
		private string name, announceUrl, comment, createdBy, pieceFileName;
		private System.DateTime creationDate;
		
		
		/// <summary></summary>
		/// <returns>Date the torrent was created</returns>
		public System.DateTime CreationDate
		{
			get { return this.creationDate; }
		}

		
		/// <summary>
		/// The metainfo file has a section refered to as the information dictionary. To confirm between peers
		/// and the tracker that we are dealing with the same torrent, the SHA-1 digest of this part of the file
		/// is calculated and sent to the peers/tracker.
		/// </summary>
		/// <returns>The SHA-1 digest of the information dictionary</returns>
		public ByteField20 InfoDigest
		{
			get { return infoDigest; }
		}
		
		/// <summary>
		/// Name of the torrent. In single-file torrents, this is the name of the file.
		/// </summary>
		/// <returns>Name of the torrent</returns>
		public string Name
		{
			get { return name; }
		}


		public string PieceFileName
		{
			get { return this.pieceFileName; }
		}
		
	
		/// <summary>
		/// Sometimes a torrent may have a comment, although it is optional.
		/// </summary>
		/// <returns>Torrent comment</returns>
		public string Comment
		{
			get { return comment; }
		}
		
		
		/// <summary>
		/// Optional field in metainfo - this may describe which program or person created the metainfo file.
		/// </summary>
		/// <returns>Name of the program or person who created the torrent</returns>
		public string CreatedBy
		{
			get { return createdBy; }
		}
		
		
		/// <summary>
		/// URL of the tracker announce. HTTP requests are sent here to keep the tracker abreast of the torrent's status and is
		/// used for gathering information about peers. See class TrackerProtocol.
		/// </summary>
		/// <returns>Tracker announce URL</returns>
		public string AnnounceUrl
		{
			get { return announceUrl; }
		}

		
		/// <summary></summary>
		/// <returns>Number of pieces in a torrent</returns>
		public int PieceCount
		{
			get { return shaDigestList.Count; }
		}
		
		
		/// <summary></summary>
		/// <returns>Number of files in a torrent</returns>
		public int FileCount
		{
			get { return fileList.Count; }
		}
		
		
		/// <summary>Note that PieceLength * PieceCount != TotalSize, as the final
		/// piece is usually smaller than the rest.</summary>
		/// <returns>Total size of the torrent, in bytes.</returns>
		public int TotalSize
		{
			get { return totalSize; }
		}
		
		
		/// <summary></summary>
		/// <param name="index">Index of file to get length of</param>
		/// <returns>The length of the file at the specified index</returns>
		public int GetFileLength(int index)
		{
			return (int)fileLengthList[index];
		}
		
		
		/// <summary></summary>
		/// <param name="index">Index of file to get name of</param>
		/// <returns>The name of the file at the specified index</returns>
		public string GetFileName(int index)
		{
			return (string)fileList[index];
		}


		/// <summary>Each piece has it's own SHA-1 digest, to confirm the validity of the downloaded data</summary>
		/// <param name="index">Index of piece</param>
		/// <returns>SHA-1 digest of specified piece</returns>
		public ByteField20 GetSHADigest(int index)
		{
			return (ByteField20)shaDigestList[index];
		}


		/// <summary>Constructs a MetainfoFile</summary>
		/// <param name="filename">Name of the file to load</param>	
		public MetainfoFile(string filename)
			: this(new IO.FileStream(filename, IO.FileMode.Open))
		{
		}
		
		
		/// <summary>Constructs a MetainfoFile</summary>
		/// <param name="istream">Stream to read data from</param>
		public MetainfoFile(IO.Stream istream)
		{
			BEncode.Dictionary mainDictionary = (BEncode.Dictionary)BEncode.NextElement(istream);
			this.announceUrl = mainDictionary.GetString(new BEncode.String("announce"));
			
			if (mainDictionary.Contains("comment"))
				this.comment = mainDictionary.GetString("comment");
			if (mainDictionary.Contains("created by"))
				this.createdBy = mainDictionary.GetString("created by");
			if (mainDictionary.Contains("creation date"))
			{
				int creation = mainDictionary.GetInteger("creation date");
				this.creationDate = new System.DateTime(1970, 1, 1, 0, 0, 0);
				this.creationDate = this.creationDate.AddSeconds(creation);
			}
			
			BEncode.Dictionary infoDictionary = mainDictionary.GetDictionary("info");
			this.name = infoDictionary.GetString("name");
			this.pieceLength = infoDictionary.GetInteger("piece length");

			this.pieceFileName = this.name.ToLower().Replace(' ', '_');
			
			// Get SHA digests
			byte[] pieces = infoDictionary.GetBytes("pieces");
			int numPieces = pieces.Length / 20;
			
			this.shaDigestList.Capacity = numPieces;
			
			for (int i=0; i<numPieces; ++i)
			{
				this.shaDigestList.Add( new ByteField20(pieces, i*20) );
			}
			
			// Get filenames and lengths
			if (infoDictionary.Contains("length"))
			{
				// one file
				this.fileList.Add(name);
				
				int fileLength = infoDictionary.GetInteger("length");
				this.fileLengthList.Add(fileLength);
				this.totalSize = fileLength;
			}
			else
			{
				// multiple files - a list of dictionaries containing the filename and length
				BEncode.List files = infoDictionary.GetList("files");
				this.fileList.Capacity = this.fileLengthList.Capacity = files.Count;
				this.totalSize = 0;
				
				foreach (BEncode.Dictionary fileDic in files)
				{
					BEncode.List pathList = fileDic.GetList("path");
					string path = this.name + IO.Path.DirectorySeparatorChar;
					
					for (int i=0; i<pathList.Count-1; ++i)
					{
						path += pathList[i].ToString() + IO.Path.DirectorySeparatorChar;
					}

					path += pathList[ pathList.Count-1 ];
					
					this.fileList.Add(path);
					
					int fileLength = fileDic.GetInteger("length");
					this.fileLengthList.Add(fileLength);
					this.totalSize += fileLength;
				}
			}
			
			// calculate the SHA-1 digest of the info dictionary - this is required for the tracker protocol
			istream.Seek(infoDictionary.Position, IO.SeekOrigin.Begin);
			byte[] infoData = new byte[ infoDictionary.Length ];
			istream.Read(infoData, 0, infoData.Length);
			
			this.infoDigest = ByteField20.ComputeSHAHash(infoData);
		}
		
		
		/// <summary> This determines the piece range a certain file falls under - used to download individual files
		/// from a torrent</summary>
		/// <param name="file">Name of the file inside the torrent (including subdirectories)</param>
		/// <param name="first">First piece index at which the file starts.</param>
		/// <param name="last">Last piece index at which the file ends. Note that they are inclusive and may overlap to other files</param>
		public void GetPieceNumbersForFile(string file, out int first, out int last)
		{
			int totalFileLength = 0;
			first = last = 0;
			
			for (int i=0; i<this.fileList.Count; ++i)
			{
				if (this.GetFileName(i) == file)
				{
					first = totalFileLength / this.pieceLength;
					last = first + (this.GetFileLength(i) / pieceLength) + 1;
					return;
				}
				else
					totalFileLength += this.GetFileLength(i);
			}
		}


		public int GetPieceLength(int pieceId)
		{
			// if its the last piece it'll (probably) be slightly shorter
			if (pieceId == this.PieceCount-1)
				return (this.totalSize % this.pieceLength != 0 ? this.totalSize % this.pieceLength : this.pieceLength);
			else
				return this.pieceLength;
		}
	}
}
