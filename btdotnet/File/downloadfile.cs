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
using Crypto = System.Security.Cryptography;


namespace BitTorrent
{
	/// <summary>
	/// Delegate for reporting the progress of an integrity check. This is called once for each piece checked.
	/// </summary>
	/// <param name="file">Instance of DownloadFile which is being reported on</param>
	/// <param name="pieceId">Piece index</param>
	/// <param name="good">True if the piece matches the SHA-1 digest, false otherwise</param>
	/// <param name="percentDone">Percentage of file checked</param>
	public delegate void CheckIntegrityCallback(DownloadFile file, int pieceId, bool good, float percentDone);

	public delegate void DownloadStatusChangeCallback(DownloadFile file, int val);
	public delegate void PercentChangedCallback(DownloadFile file, float percent);


	/// <summary>
	/// DownloadFile represents the all the data involved in the torrent. Read and Write operations can be performed piece-by-piece.
	/// Due to the complexity of multi-file torrents, that pieces can encompass several files, an abstraction layer is required.
	/// </summary>
	public class DownloadFile : System.IDisposable
	{
		private MetainfoFile infofile;
		private BitField piecesDownloaded;
		private int numBytesLeft = 0;


		/// <summary>
		/// Bitfiled represents the torrent file's status. Each bit in the bitfield represents a piece. If the piece's bit
		/// is set, it exists on the user's hard drive.
		/// </summary>
		/// <returns>Torrent bitfield</returns>
		public BitField Bitfield
		{
			get { return piecesDownloaded; }
		}
		
		
		/// <summary></summary>
		/// <returns>Number of bytes left to download</returns>
		public int NumBytesLeft
		{
			get { return numBytesLeft; }
		}


		public float PercentComplete
		{
			get 
			{
				return this.piecesDownloaded.PercentageTrue;
			}
		}


		public event PercentChangedCallback PercentChanged;

	
		/// <summary>Constructs a DownloadFile</summary>
		/// <param name="infofile">Metainfo file for the torrent</param>
		public DownloadFile(MetainfoFile infofile)
		{
			this.infofile = infofile;

			this.piecesDownloaded = new BitField( this.infofile.PieceCount );
			this.numBytesLeft = 0;
			for ( int i = 0; i < this.piecesDownloaded.Count; ++i )
			{
				this.piecesDownloaded.Set( i, false );
				this.numBytesLeft += this.infofile.GetPieceLength( i );
			}

			GetPieceInfoFromFile( null );
		}


		/// <summary>
		/// This *must* be called before any operations are performed. This should be in the constructor, but as it can be a time-consuming process
		/// it was decided against. This analyzes the file to look for which pieces are downloaded, or if the torrent has just started it will create the
		/// new, empty files.
		/// </summary>
		public void CheckIntegrity( bool forced )
		{
			this.CheckIntegrity( forced, null );
		}
		

		private bool GetPieceInfoFromFile(CheckIntegrityCallback callback)
		{
			string pieceFilePath = Config.ApplicationDataDirectory + IO.Path.DirectorySeparatorChar + this.infofile.PieceFileName + ".piece";
			IO.FileInfo pieceFileInfo = new IO.FileInfo(pieceFilePath);
			if (pieceFileInfo.Exists)
			{
				// before we rely on the piece file, do a simple check of file existance and lengths to make sure nothing's changed
				for (int i=0; i<this.infofile.FileCount; ++i)
				{
					string path = this.infofile.GetFileName(i);
					int properLength = this.infofile.GetFileLength(i);

					// TODO: introduce modification time check - requires saving time data in piece file

					IO.FileInfo fileInfo = new IO.FileInfo(path);
					if (!fileInfo.Exists)
						return false; // a file doesn't exist, do a proper check

					long realLength = fileInfo.Length;
					if (properLength != realLength)
						return false; // a file doesn't have a proper length, do a proper check
				}

				IO.FileStream pieceFile = pieceFileInfo.OpenRead();
				int numBytes = this.infofile.PieceCount/8 + ((this.infofile.PieceCount % 8) > 0 ? 1 : 0);
				byte[] data = new byte[numBytes];
				pieceFile.Read(data, 0, data.Length);
				pieceFile.Close();

				this.piecesDownloaded.SetFromRaw( data, 0, data.Length );
				this.piecesDownloaded.SetLength(this.infofile.PieceCount);

				this.numBytesLeft = 0;
				for (int i=0; i<this.infofile.PieceCount; ++i)
				{
					if (!this.piecesDownloaded[i])
						this.numBytesLeft += this.infofile.GetPieceLength(i);
				}

				if (callback != null)
					callback(this, this.infofile.PieceCount-1, false, 100.0f);

				return true;
			}
			else
				return false;
		}

		
		/// <summary>
		/// This *must* be called before any operations are performed. This should be in the constructor, but as it can be a time-consuming process
		/// it was decided against. This analyzes the file to look for which pieces are downloaded, or if the torrent has just started it will create the
		/// new, empty files.
		/// </summary>
		/// <param name="callback">Callback delegate to inform the caller of the progress</param>
		public void CheckIntegrity( bool forced, CheckIntegrityCallback callback)
		{
			// if their already is a piece file in the application directory, load it from there. (unless it is forced where is has to do it)
			if ( forced || !GetPieceInfoFromFile(callback))
			{
				int dataPosition = 0, filePosition = 0;
				int i = 0;
				byte[] data = new byte[this.infofile.GetPieceLength(0)];
				Crypto.SHA1 sha = Crypto.SHA1CryptoServiceProvider.Create();

				for (int currentFile=0; currentFile<this.infofile.FileCount; ++currentFile)
				{
					int fileLength = this.infofile.GetFileLength(currentFile);
					string path = this.infofile.GetFileName(currentFile);

					IO.FileInfo fileInfo = new IO.FileInfo(path);
				
					if (!fileInfo.Exists)
					{
						// create the file if it does not exist
						this.CreateEmptyFile(fileInfo, fileLength);

						int fileDataLeft = 0;

						if (dataPosition > 0)
						{
							// if dataPosition does not equal zero, meaning we have some of a piece from the last file. This automatically fails,
							// and we move onto the next piece.
							i++;
							fileDataLeft = fileLength - (this.infofile.GetPieceLength(0) - dataPosition);
						}
						else
							fileDataLeft = fileLength - filePosition;

						int numPieces = fileDataLeft / this.infofile.GetPieceLength(0);
						i += numPieces;
						if (fileDataLeft % this.infofile.GetPieceLength(0) > 0)
						{
							// set the next file's filePosition, and fail the next piece
							filePosition = this.infofile.GetPieceLength(i) - (fileDataLeft % this.infofile.GetPieceLength(i));
							i++;
						}
						else
						{
							filePosition = 0;
						}

						dataPosition = 0;

						if (callback != null)
							callback(this, i, false, ((float)(i+1)/(float)this.infofile.PieceCount) * 100.0f);

						// move onto next file
						continue;
					}
					else
					{
						// check the length, otherwise truncate it
						if (fileInfo.Length != fileLength)
							this.TruncateFile(fileInfo, fileLength);

						// open the file, start checking.
						IO.FileStream fstream = fileInfo.OpenRead();

						while (filePosition < fileLength)
						{
							int dataToRead = System.Math.Min(fileLength - filePosition, this.infofile.GetPieceLength(i) - dataPosition);
							byte[] tempData = new byte[dataToRead];
							fstream.Read(tempData, 0, tempData.Length);
					
							if (dataToRead + dataPosition >= this.infofile.GetPieceLength(i))
							{
								// piece finished
								System.Array.Copy(tempData, 0, data, dataPosition, dataToRead);
								sha.ComputeHash(data, 0, this.infofile.GetPieceLength(i));

								ByteField20 final = new ByteField20(sha.Hash);
								bool good = final.Equals( this.infofile.GetSHADigest(i) );
				
								if (!good) // if piece is good we can subtract it from the bytes left to download
									numBytesLeft += this.infofile.GetPieceLength(i);
					
								this.piecesDownloaded.Set(i, good);

								if (callback != null)
									callback(this, i, good, ((float)(i+1)/(float)this.infofile.PieceCount) * 100.0f);

								i++;
								dataPosition = 0;
							}
							else
							{
								System.Array.Copy(tempData, 0, data, dataPosition, dataToRead);
								dataPosition += dataToRead;
							}

							filePosition += dataToRead;
						}

						filePosition = 0;
						fstream.Close();
					}
				}
			}

			if (this.PercentChanged != null)
				this.PercentChanged(this, this.PercentComplete);
		}
		

		/// <summary>
		/// Creates an empty file
		/// </summary>
		/// <param name="fileInfo">File to create</param>
		/// <param name="length">Length of file</param>		
		private void CreateEmptyFile(IO.FileInfo fileInfo, int length)
		{
			fileInfo.Directory.Create();
			IO.FileStream fstream = fileInfo.OpenWrite();
			fstream.SetLength(length);
			fstream.Close();
		}
		
		
		/// <summary>
		/// Truncates an existing file to a specified length
		/// </summary>
		/// <param name="fileInfo">File to truncate</param>
		/// <param name="length">New length of file</param>
		private void TruncateFile(IO.FileInfo fileInfo, int length)
		{
			IO.FileStream fstream = fileInfo.OpenWrite();
			fstream.SetLength(length);
			fstream.Close();
		}
		
		
		/// <summary>
		/// Finds out which file a piece resides in. This is used by both SaveToFile() and ReadFromFile() methods.
		/// </summary>
		/// <param name="pieceId">Piece index</param>
		/// <param name="fileIndex">Index of the file</param>
		/// <param name="filePosition">Position within the file at which the piece begins</param>
		private void WhichFileIsPieceIn(int pieceId, out int fileIndex, out int filePosition)
		{
			int absolutePosition = pieceId * this.infofile.GetPieceLength(0);
			int totalFileLength = 0;
			fileIndex = filePosition = 0;
			
			for (int i=0; i<this.infofile.FileCount; ++i)
			{
				int fileLength = this.infofile.GetFileLength(i);
				
				if (absolutePosition < totalFileLength + fileLength)
				{
					filePosition = absolutePosition - totalFileLength;
					fileIndex = i;
					break;
				}
				else
					totalFileLength += fileLength;
			}
		}
		
		
		/// <summary>
		/// Saves the stream to the torrent.
		/// </summary>
		/// <param name="pieceId">Piece index to save to</param>
		/// <param name="istream">Stream to read data from</param>
		/// <returns>True if the data saved checks out correctly with the SHA-1 digest, false otherwise. The bitfield
		/// property is automatically updated if true</returns>
		public bool SaveToFile(int pieceId, IO.Stream istream)
		{
			// it starts in this file, as it could be spread across several files keep looping till we finish
			int dataWritten = 0;
			int positionInFile = 0;
			int fileNum = 0;

			Crypto.SHA1 sha = new Crypto.SHA1CryptoServiceProvider();

			WhichFileIsPieceIn(pieceId, out fileNum, out positionInFile);

			while (dataWritten < this.infofile.GetPieceLength(pieceId) && fileNum < this.infofile.FileCount)
			{
				int fileLength = this.infofile.GetFileLength(fileNum);
				int dataToWrite = System.Math.Min(fileLength - positionInFile, this.infofile.GetPieceLength(pieceId) - dataWritten);

				IO.FileStream fstream = new IO.FileStream(this.infofile.GetFileName(fileNum), IO.FileMode.Open);

				// write data to file
				fstream.Seek(positionInFile, IO.SeekOrigin.Begin);
			
				byte[] data = new byte[ dataToWrite ];
				istream.Read(data, 0, data.Length);
			
				fstream.Write(data, 0, data.Length);
				dataWritten += dataToWrite;

				if (dataWritten >= this.infofile.GetPieceLength(pieceId))
					sha.TransformFinalBlock(data, 0, data.Length);
				else
					sha.TransformBlock(data, 0, data.Length, data, 0);

				fstream.Close();

				fileNum++; // move onto next file
				positionInFile = 0;
			}

			if (this.infofile.GetSHADigest(pieceId).Equals(new ByteField20(sha.Hash)))
			{
				this.piecesDownloaded.Set(pieceId, true);
				this.numBytesLeft -= dataWritten;

				if (this.PercentChanged != null)
					this.PercentChanged(this, this.PercentComplete);

				if (this.piecesDownloaded.AllTrue)
					Config.LogDebugMessage("Torrent finished!");

				return true;
			}
			else
				return false;
		}
		
		
		/// <summary>
		/// Loads a piece from the torrent. The torrent piece must already have been downloaded (ie exists in the bitfield)
		/// </summary>
		/// <param name="pieceId">Piece index to load from</param>
		/// <param name="ostream">Stream to save to</param>
		/// <returns>True if the piece was loaded succesfully, false otherwise</returns>
		public bool LoadFromFile(int pieceId, IO.Stream ostream)
		{
			if (!this.piecesDownloaded.Get(pieceId))
				return false;
			else
			{
				int dataRead = 0;
				int positionInFile = 0;
				int fileNum = 0;
				
				WhichFileIsPieceIn(pieceId, out fileNum, out positionInFile);

				while (dataRead < this.infofile.GetPieceLength(pieceId) && fileNum < this.infofile.FileCount)
				{
					int fileLength = this.infofile.GetFileLength(fileNum);
					int dataToRead = System.Math.Min(fileLength - positionInFile, this.infofile.GetPieceLength(pieceId) - dataRead);

					IO.FileStream fstream = new IO.FileStream(this.infofile.GetFileName(fileNum), IO.FileMode.Open);

					// read data from the file
					fstream.Seek(positionInFile, IO.SeekOrigin.Begin);
					
					byte[] data = new byte[ dataToRead ];
					fstream.Read(data, 0, data.Length);
					ostream.Write(data, 0, data.Length);
					dataRead += dataToRead;

					fstream.Close();

					fileNum++; // move onto next file
					positionInFile = 0;
				}
				
				return true;
			}
		}


		#region IDisposable Members

		public void Dispose()
		{
			// save status to file
			string pieceFilePath = Config.ApplicationDataDirectory + IO.Path.DirectorySeparatorChar + this.infofile.PieceFileName + ".piece";
			IO.FileInfo pieceFileInfo = new IO.FileInfo(pieceFilePath);
			IO.FileStream pieceFile = new IO.FileStream(pieceFilePath, IO.FileMode.OpenOrCreate, IO.FileAccess.Write);
			int numBytes = this.infofile.PieceCount/8 + ((this.infofile.PieceCount % 8) > 0 ? 1 : 0);
			byte[] data = new byte[numBytes];
			this.piecesDownloaded.CopyTo(data, 0);
			pieceFile.Write(data, 0, data.Length);
			pieceFile.Close();
		}

		#endregion
	}
}
