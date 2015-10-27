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
using IO = System.IO;

// 2/7/03

namespace PWLib.Platform
{
	/// <summary>
	/// Provides a self-expanding circular buffer.
	/// </summary>
	public class CircularBuffer : System.ICloneable
	{
		/// <summary>
		/// Stream class to enable reading to the stream. Supports seeking but does not support writing.
		/// </summary>
		private class CircularBufferReader : IO.Stream
		{
			private CircularBuffer buffer;


			/// <summary>Constructs a CircularBufferReader</summary>
			/// <param name="buffer">Buffer to use</summary>		
			public CircularBufferReader(CircularBuffer buffer)
			{
				this.buffer = buffer;
			}
			
			
			/// <summary>Seeks to the specified offset. Origin is always Current, Begin and End are ignored.</summary>
			/// <param name="offset">Offset to seek to</param>
			/// <param name="origin">Origin. Always System.IO.SeekOrigin.Current</param>
			/// <returns>New position, meaningless in a circularbuffer</returns>
			public override long Seek(long offset, IO.SeekOrigin origin)
			{
				lock (this.buffer)
				{
					if (this.buffer.readPosition + offset >= this.buffer.internalData.Length)
					{
						this.buffer.readPosition = (this.buffer.readPosition + offset) % this.buffer.internalData.Length;
					}
					else if (this.buffer.readPosition + offset < 0)
					{
						this.buffer.readPosition = this.buffer.internalData.Length + this.buffer.readPosition + offset;
					}
					else
						this.buffer.readPosition += offset;
					
					return this.buffer.readPosition;
				}
			}
			
			
			/// <summary></summary>
			/// <returns>Always true</returns>
			public override bool CanRead
			{
				get { return true; }
			}


			/// <summary></summary>
			/// <returns>Always false</returns>
			public override bool CanWrite
			{
				get { return false; }
			}


			/// <summary></summary>
			/// <returns>Always true</returns>
			public override bool CanSeek
			{
				get { return true; }
			}


			/// <summary></summary>
			/// <returns>Size of the buffer</returns>
			public override long Length
			{
				get { return this.buffer.internalData.Length; }
			}
			
			
			/// <summary>
			/// Not supported - a circular buffer does not have any positions!
			/// </summary>
			/// <returns></returns>
			public override long Position
			{
				get { throw new NotSupportedException(); }
				set { throw new NotSupportedException(); }
			}
			
			
			/// <summary>Reads data from the buffer</summary>
			/// <param name="data">Data to read into</param>
			/// <param name="offset">Offset within data to write to</param>
			/// <param name="length">Amount of data to read</param>
			/// <returns>Amount of data read</returns>
			public override int Read(byte[] data, int offset, int length)
			{
				return this.buffer.Read(data, offset, length);
			}


			public override int ReadByte()
			{
				return this.buffer.ReadByte();
			}
			
			
			/// <summary>Not supported</summary>
			/// <param name="data"></param>
			/// <param name="offset"></param>
			/// <param name="length"></param>
			public override void Write(byte[] data, int offset, int length)
			{
				throw new NotSupportedException();
			}


			public override void WriteByte(byte val)
			{
				throw new NotSupportedException();
			}

			
			
			/// <summary>Does nothing</summary>
			public override void Flush()
			{
			}
			
			
			/// <summary>Sets the length of the buffer, will truncate if smaller than the current size</summary>
			/// <param name="size">New size</param>
			public override void SetLength(long size)
			{
				this.buffer.SetLength(size);
			}
		}
		
		
		/// <summary>
		/// Stream class to enable writing to the stream. Supports seeking but does not support reading.
		/// </summary>
		private class CircularBufferWriter : IO.Stream
		{
			private CircularBuffer buffer;


			/// <summary>Constructs a CircularBufferWriter</summary>
			/// <param name="buffer">Buffer to use</summary>		
			public CircularBufferWriter(CircularBuffer buffer)
			{
				this.buffer = buffer;
			}
			
			
			/// <summary>Seeks to the specified offset. Origin is always Current, Begin and End are ignored.</summary>
			/// <param name="offset">Offset to seek to</param>
			/// <param name="origin">Origin. Always System.IO.SeekOrigin.Current</param>
			/// <returns>New position, meaningless in a circularbuffer</returns>
			public override long Seek(long offset, IO.SeekOrigin origin)
			{
				if (this.buffer.writePosition + offset >= this.buffer.internalData.Length)
				{
					this.buffer.writePosition = this.buffer.writePosition + offset - this.buffer.internalData.Length;
				}
				else if (this.buffer.writePosition + offset < 0)
				{
					this.buffer.writePosition = this.buffer.internalData.Length + this.buffer.writePosition + offset;
				}
				else
					this.buffer.writePosition += offset;
					
				return this.buffer.writePosition;
			}
			
			
			/// <summary></summary>
			/// <returns>Always false</returns>
			public override bool CanRead
			{
				get { return false; }
			}


			/// <summary></summary>
			/// <returns>Always true</returns>
			public override bool CanWrite
			{
				get { return true; }
			}


			/// <summary></summary>
			/// <returns>Always true</returns>
			public override bool CanSeek
			{
				get { return true; }
			}


			/// <summary></summary>
			/// <returns>Size of the buffer</returns>
			public override long Length
			{
				get { return this.buffer.internalData.Length; }
			}
			
			
			/// <summary>
			/// Not supported - a circular buffer does not have any positions!
			/// </summary>
			public override long Position
			{
				get { throw new NotSupportedException(); }
				set { throw new NotSupportedException(); }
			}
			
			
			public override void Write(byte[] data, int offset, int length)
			{
				this.buffer.Write(data, offset, length);
			}
			

			public override void WriteByte(byte val)
			{
				this.buffer.WriteByte(val);
			}

			
			/// <summary>Not supported</summary>
			/// <param name="data"></param>
			/// <param name="offset"></param>
			/// <param name="length"></param>
			public override int Read(byte[] data, int offset, int length)
			{
				throw new NotSupportedException();
			}


			public override int ReadByte()
			{
				throw new NotSupportedException();
			}
			
			
			/// <summary>Does nothing</summary>
			public override void Flush()
			{
			}
			
			
			/// <summary>Sets the length of the buffer, will truncate if smaller than the current size</summary>
			/// <param name="size">New size</param>
			public override void SetLength(long size)
			{
				this.buffer.SetLength(size);
			}
		}
		
	
		private const int defaultSize = 4092;
		private byte[] internalData;
		private long readPosition = 0, writePosition = 0;
		private CircularBufferWriter writer;
		private CircularBufferReader reader;
		
		
		/// <summary>Provides a seekable stream to read from the buffer</summary>
		/// <returns>Reading stream</returns>
		public IO.Stream Reader
		{
			get { return this.reader; }
		}
		
		
		/// <summary>Provides a seekable stream to write to the buffer</summary>
		/// <returns>Writing stream</returns>
		public IO.Stream Writer
		{
			get { return this.writer; }
		}


		/// <summary>
		/// Number of bytes that are available to read
		/// </summary>
		public long DataAvailable
		{
			get
			{
				if (this.readPosition == this.writePosition)
					return 0;
				else if (this.readPosition > this.writePosition)
					return this.internalData.Length - this.readPosition + this.writePosition;
				else
					return this.writePosition - this.readPosition;
			}
		}



		/// <summary>
		/// Constructs a circular buffer of default size
		/// </summary>
		public CircularBuffer()
			: this(defaultSize)
		{
		}


		/// <summary>
		/// Constructs a circular buffer of specified initial size
		/// </summary>
		/// <param name="size">Initial size of buffer</param>
		public CircularBuffer(int size)
		{
			this.internalData = new byte[ size+1 ];
			
			this.writer = new CircularBufferWriter(this);
			this.reader = new CircularBufferReader(this);
		}


		/// <summary>
		/// Sets the length of the circular buffer. Will truncate if length is 
		/// shorter than the size of the buffer
		/// </summary>
		/// <param name="length">New size of buffer</param>
		public void SetLength(long length)
		{
			long bytesTillEnd = this.internalData.Length - this.writePosition - 1;

			// copy data from the write pointer to the end into a new array
			byte[] rawData = new byte[ this.internalData.Length ];
			if (bytesTillEnd > 0)
				Array.Copy(this.internalData, this.writePosition + 1, rawData, 0, bytesTillEnd);

			// then append the rest of the data to the new array
			Array.Copy(this.internalData, 0, rawData, bytesTillEnd, this.writePosition);

			// set read pointer to the position relative to where it was
			this.readPosition += bytesTillEnd;
			this.readPosition %= this.internalData.Length;

			// set write pointer to the end of the freshly written data
			this.writePosition += bytesTillEnd;
			this.writePosition %= this.internalData.Length;

			// now we have the data all in order, recreate the internal array and then
			// copy the new array back into it
			int streamLength = Math.Min((int)length, rawData.Length);
			this.internalData = new byte[ length+1 ];
			Array.Copy(rawData, 0, this.internalData, 0, streamLength);
		}


		/// <summary>
		/// Reads data from the buffer
		/// </summary>
		/// <param name="data">Array to write to</param>
		/// <param name="offset">Offset in parameter data to write to</param>
		/// <param name="length">Amount of data to read</param>
		/// <returns>Actual amount read</returns>
		public int Read(byte[] data, int offset, int length)
		{
			lock (this)
			{
				if (this.writePosition == this.readPosition)
					return 0; // empty buffer

				long amountRead = 0;

				if (this.writePosition < this.readPosition)
				{
					// write pointer is behind the read pointer, then read up until the end of
					// the buffer and read from the start till the write position
					long bytesTillEnd = this.internalData.Length - this.readPosition;

					if (bytesTillEnd < length)
					{
						long bytesFromStart = Math.Min(this.writePosition, length - bytesTillEnd);
						Array.Copy(this.internalData, this.readPosition, data, offset, bytesTillEnd);
						Array.Copy(this.internalData, 0, data, offset + bytesTillEnd, bytesFromStart);
						amountRead = bytesTillEnd + bytesFromStart;
					}
					else
					{
						Array.Copy(this.internalData, this.readPosition, data, offset, length);
						amountRead = length;
					}
				}
				else
				{
					// write pointer is ahead of the read pointer, just read up until then
					long amountToCopy = Math.Min(this.writePosition - this.readPosition, length);
					Array.Copy(this.internalData, this.readPosition, data, offset, amountToCopy);
					amountRead = amountToCopy;
				}

				this.readPosition += amountRead;
				this.readPosition %= this.internalData.Length;

				return (int)amountRead;
			}
		}


		/// <summary>
		/// Writes data to the buffer
		/// </summary>
		/// <param name="data">Array to read into</param>
		/// <param name="offset">Offset in parameter data to read to</param>
		/// <param name="length">Amount of data to write</param>
		public void Write(byte[] data, int offset, int length)
		{
			lock (this)
			{
				// test if the buffer needs to be resized to accomodate the written data
				long spaceAvailable = 0;

				if (this.readPosition <= this.writePosition)
					spaceAvailable = this.internalData.Length - this.writePosition + this.readPosition - 1;
				else
					spaceAvailable = this.readPosition - this.writePosition - 1;

				if (spaceAvailable < length)
					this.SetLength(this.internalData.Length - spaceAvailable + length - 1);

				if (this.readPosition <= this.writePosition)
				{
					// read pointer is behind the write pointer, write till the end
					// of the buffer then go back to the start
					long bytesTillEnd = this.internalData.Length - this.writePosition;

					if (bytesTillEnd < length)
					{
						// length to copy is greater than the size of the data until the end of the buffer,
						// so copy the remaining data until the end
						Array.Copy(data, offset, this.internalData, this.writePosition, bytesTillEnd);

						long bytesFromStart = Math.Min(this.readPosition, length - bytesTillEnd);

						if (bytesFromStart > 0)
							Array.Copy(data, offset + bytesTillEnd, this.internalData, 0, bytesFromStart);
					}
					else
					{
						// length is not greater than the size of the data until the end, so
						// just copy it over
						Array.Copy(data, offset, this.internalData, this.writePosition, length);
					}
				}
				else
				{
					// write pointer is behind the read pointer, simply write up until then
					long amountToCopy = Math.Min(this.readPosition - this.writePosition - 1, length);
					Array.Copy(data, offset, this.internalData, this.writePosition, amountToCopy);
				}

				this.writePosition += length;
				this.writePosition %= this.internalData.Length;
			}
		}


		public int ReadByte()
		{
			byte[] b = new byte[1];
			if (this.Read(b, 0, 1) > 0)
				return (int)b[0];
			else
				return 0;
		}


		public void WriteByte(byte b)
		{
			byte[] b2 = new byte[] { b };
			this.Write(b2, 0, 1);
		}
		
		
		#region ICloneable members
		
		
		/// <summary></summary>
		/// <returns>Clone of the buffer</returns>
		object System.ICloneable.Clone()
		{
			return this.Clone();
		}
		
		
		/// <summary></summary>
		/// <returns>Clone of the buffer</returns>
		public CircularBuffer Clone()
		{
			CircularBuffer buffer = new CircularBuffer(this.internalData.Length);
			System.Array.Copy(this.internalData, 0, buffer.internalData, 0, this.internalData.Length);
			buffer.readPosition = this.readPosition;
			buffer.writePosition = this.writePosition;
			return buffer;
		}
		
		
		#endregion
	}
}
