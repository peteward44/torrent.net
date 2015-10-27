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
using Crypto = System.Security.Cryptography;


namespace BitTorrent
{
	/// <summary>
	/// This is simply a 20-byte field used for both SHA-1 hashes and peer ids
	/// </summary>
	public class ByteField20
	{
		private byte[] data;
		
		
		/// <summary></summary>
		/// <returns>Actual data held by the object</returns>
		public byte[] Data
		{
			get { return data; }
		}
		
		
		/// <summary>Constructs a ByteField20</summary>
		/// <param name="data">Data to use</param>
		public ByteField20(byte[] data)
			: this(data, 0)
		{
		}


		/// <summary>Constructs a ByteField20</summary>
		public ByteField20()
		{
			this.data = new byte[20];
		}
		
		
		/// <summary>Constructs a ByteField20</summary>
		/// <param name="data">Data to use</param>
		/// <param name="offset">Offset in data parameter to copy data from</param>
		public ByteField20(byte[] data, int offset)
		{
			this.data = new byte[20];
			Array.Copy(data, offset, this.data, 0, 20);
		}
		
		
		/// <summary>Computes the SHA-1 hash from the data</summary>
		/// <param name="data">Data to compute hash from</param>
		/// <returns>SHA-1 hash</returns>
		public static ByteField20 ComputeSHAHash(byte[] data)
		{
			Crypto.SHA1 sha = new Crypto.SHA1CryptoServiceProvider();
			byte[] result = sha.ComputeHash(data);
			return new ByteField20(result);
		}

		
		/// <summary></summary>
		/// <returns>True if the objects are equal</returns>
		public override bool Equals(object obj)
		{
			if (obj is ByteField20)
			{
				ByteField20 sha = (ByteField20)obj;
				for (int i=0; i<this.data.Length; ++i)
				{
					if (this.data[i] != sha.data[i])
						return false;
				}
				return true;
			}
			else
				return false;
		}


		/// <summary></summary>
		/// <returns>Hashcode of object</returns>
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		
		/// <summary></summary>
		/// <returns>String representation of the data</returns>
		public override string ToString()
		{
			return System.Text.ASCIIEncoding.ASCII.GetString(this.data);
		}
	}
}
