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


namespace BitTorrent
{
	/// <summary>
	/// The Collections.BitArray class uses the *lowest* bit
	/// as index 0, whilst bittorrent uses the *highest* bit
	/// in it's bitfield. Whilst we could use Collections.BitArray
	/// and convert it, but it would be more efficient just to make our own.
	/// </summary>
	public class BitField : ICollection, IEnumerable, System.ICloneable
	{
		/// <summary>Class used to enumerate over the collection</summary>
		public class Enumerator : IEnumerator
		{
			private BitField bitField;
			private int position = -1;


			/// <summary>Constructs an Enumerator</summary>
			/// <param name="bitField">Bitfield to enumerate over</param>
			public Enumerator(BitField bitField)
			{
				this.bitField = bitField;
			}


			#region IEnumerator Members


			/// <summary>Resets the enumerator back to zero</summary>
			public void Reset()
			{
				position = -1;
			}


			/// <summary></summary>
			/// <returns>The current bit in the collection</returns>
			object IEnumerator.Current
			{
				get { return this.Current; }
			}


			/// <summary></summary>
			/// <returns>The current bit in the collection</returns>
			public bool Current
			{
				get { return this.bitField.Get(this.position); }
			}


			/// <summary>Moves the position onto the next bit</summary>
			/// <returns>False if it has reached the end of the collection</returns>
			public bool MoveNext()
			{
				this.position++;
				if (this.bitField.Count <= this.position)
					return false;
				else
					return true;
			}

			#endregion
		}


		private byte[] data;
		private int count = 0;
		private int numTrue = 0;


		public bool AllTrue
		{
			get { return this.count == this.numTrue; }
		}


		public bool AllFalse
		{
			get { return this.numTrue == 0; }
		}


		public int TrueCount
		{
			get { return this.numTrue; }
		}


		public int FalseCount
		{
			get { return this.count - this.numTrue; }
		}


		public float PercentageTrue
		{
			get { return ((float)this.numTrue / (float)this.count) * 100.0f; }
		}


		public float PercentageFalse
		{
			get { return ((float)(this.count - this.numTrue) / (float)this.count) * 100.0f; }
		}


		/// <summary>Constructs a BitField</summary>
		/// <param name="size">Initial size of the collection, in bits</param>
		public BitField(int size)
		{
			this.count = size;
			this.data = new byte[size/8 + ((size % 8) == 0 ? 0 : 1)];
		}


		/// <summary>Constructs a bitfield</summary>
		/// <param name="data">Data to use as the initial field</param>
		/// <param name="offset">Offset within data to start copying</param>
		/// <param name="length">Length of the data to copy from</param>
		public BitField(byte[] data, int offset, int length)
		{
			SetFromRaw( data, offset, length );
		}


		public void SetFromRaw( byte[] data, int offset, int length )
		{
			this.data = new byte[ length ];
			System.Array.Copy( data, offset, this.data, 0, length );
			this.count = length * 8;
			this.CheckStatus();
		}


		/// <summary></summary>
		/// <param name="index">Index to retreive</param>
		/// <returns>Bit at the specified index</returns>
		public bool this[int index]
		{
			get { return this.Get(index); }
			set { this.Set(index, value); }
		}


		/// <summary></summary>
		/// <param name="index">Index to retrieve bit from</param>
		/// <returns>Bit</returns>
		public bool Get(int index)
		{
			byte b = this.data[ index/8 ];
			return (b & (1 << (7-(index%8)))) > 0;
		}


		/// <summary></summary>
		/// <param name="index">Index to set bit at</param>
		/// <param name="val">Value to set the bit</param>
		public void Set(int index, bool val)
		{
			if (val)
			{
				if (!this.Get(index))
					this.numTrue++;
				this.data[ index/8 ] |= (byte)(1 << (7-(index%8)));
			}
			else
			{
				if (this.Get(index))
					this.numTrue--;
				this.data[ index/8 ] &= (byte)(~(1 << (7-(index%8))));
			}
		}


		#region ICollection Members


		/// <summary></summary>
		/// <returns>Whether the collection is synchronized. This is always false</returns>
		public bool IsSynchronized
		{
			get { return false; }
		}


		/// <summary></summary>
		/// <returns>Number of bits in the collection</returns>
		public int Count
		{
			get { return this.count; }
		}


		/// <summary>Copies the data to another array</summary>
		/// <param name="array">Array to copy to</param>
		/// <param name="index">Index to start copying from</param>
		public void CopyTo(System.Array array, int index)
		{
			this.data.CopyTo(array, index);
		}


		/// <summary></summary>
		/// <returns>Synchronization root of the collection</returns>
		public object SyncRoot
		{
			get { return this; }
		}


		#endregion


		#region IEnumerable Members


		/// <summary>Gets the enumerator for the collection</summary>
		/// <returns>Enumerator</returns>
		public IEnumerator GetEnumerator()
		{
			return new Enumerator(this);
		}


		#endregion


		#region ICloneable Members


		/// <summary></summary>
		/// <returns>Clone of the collection</returns>
		object System.ICloneable.Clone()
		{
			return this.Clone();
		}


		/// <summary></summary>
		/// <returns>Clone of the collection</returns>
		public BitField Clone()
		{
			BitField bitField = new BitField(this.data, 0, this.data.Length);
			bitField.count = this.count;
			return bitField;
		}


		#endregion
		
		
		private void CheckStatus()
		{
			this.numTrue = 0;

			foreach (bool b in this)
			{
				if (b)
					this.numTrue++;
			}
		}


		public void SetLength(int bits)
		{
			int numBytes = bits/8 + ((bits % 8) > 0 ? 1 : 0);
			if (numBytes != this.data.Length)
			{
				byte[] newData = new byte[numBytes];
				System.Array.Copy(this.data, 0, newData, 0, numBytes);
				this.data = newData;
			}
			this.count = bits;

			this.CheckStatus();
		}


		public override bool Equals(object obj)
		{
			if (obj is BitField)
			{
				BitField bitfield = (BitField)obj;
				if (this.count == bitfield.count && this.numTrue == bitfield.numTrue)
				{
					for (int i=0; i<this.count; ++i)
					{
						if (this.data[i] != bitfield.data[i])
							return false;
					}

					return true;
				}
				else
					return false;
			}
			else
				return false;
		}


		public override int GetHashCode()
		{
			return this.data.GetHashCode() ^ this.count ^ this.numTrue;
		}
	}
}
