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


namespace PWLib.Platform
{
	/// <summary>
	/// BEncode encapsulates the bEncode format used extensively by BitTorrent.
	/// </summary>
	public class BEncode
	{
	
		/// <summary>
		/// bEncode uses different elements to encode it's data. All element types derive from this class.
		/// </summary>
		public abstract class Element : System.ICloneable
		{
			protected int position = -1, length = -1;


			/// <summary></summary>
			/// <returns>Position in the stream at which this element begins.</returns>
			public int Position
			{
				get { return position; }
			}


			/// <summary></summary>
			/// <returns>Length of the element within the stream.</returns>
			public int Length
			{
				get { return length; }
			}


			/// <summary>Clones the object</summary>
			/// <returns>Clone</returns>
			object System.ICloneable.Clone()
			{
				return this.Clone();
			}
			

			/// <summary>Clones the object</summary>
			/// <returns>Clone</returns>
			public Element Clone()
			{
				return (Element)this.MemberwiseClone();
			}

			internal virtual void Write(IO.Stream ostream)
			{
				throw new System.NotImplementedException();
			}
		}


		/// <summary>
		/// String class
		/// </summary>
		public class String : Element, System.IComparable, IEnumerable
		{
			private byte[] data;

			
			/// <summary></summary>
			/// <returns>Raw byte value for the string, as not all bencoded strings are actually strings</returns>
			public byte[] Data
			{
				get { return this.data; }
			}


			/// <summary>Constructs a string from the bEncoded stream</summary>
			/// <param name="istream">Stream to construct string from</param>
			public String(IO.Stream istream, int firstchar)
			{
				if (istream.CanSeek)
					this.position = (int)istream.Position - 1;

				string numstr = new string((char)firstchar, 1);
				while (true)
				{
					int c = istream.ReadByte();
					if (c != ':')
						numstr += (char)c;
					else
						break;
				}
				int length = System.Int32.Parse(numstr);
				if (length < 0)
					throw new System.Exception("Invalid string length");

				this.data = new byte[length];
				istream.Read(this.data, 0, length);

				if (istream.CanSeek)
					this.length = (int)(istream.Position - this.position);

//				System.Diagnostics.Debugger.Log(0, "BEncode", "String: " + this.ToString() + "\n");
			}
			
			
			//// <summary>Constructs a String from the bytes (Assumes ASCII format, as that is what bEncode uses</summary>
			/// <param name="bytes">Data to read from</param>
			/// <param name="offset">Offset to read from</param>
			/// <param name="length">Length to read</param>
			public String(byte[] bytes, int offset, int length)
			{
				this.data = new byte[length];
				System.Array.Copy(bytes, offset, this.data, 0, length);
			}
			
			
			/// <summary>Constructs a String from the string passed in</summary>
			/// <param name="str">String to use</param>
			public String(string str)
			{
				this.data = System.Text.ASCIIEncoding.ASCII.GetBytes(str);
			}
			
			
			/// <summary>Compares the object passed in to see if it is the same</summary>
			/// <param name="obj">Object to compare to</param>
			/// <returns>True if the same, false otherwise</returns>
			public override bool Equals(object obj)
			{
				if (obj is BEncode.String)
				{
					BEncode.String str = (BEncode.String)obj;
					if (str.data.Length != this.data.Length)
						return false;
						
					for (int i=0; i<str.data.Length; ++i)
					{
						if (str.data[i] != this.data[i])
							return false;
					}
					
					return true;
				}
				else
					return false;
			}
			

			internal override void Write(System.IO.Stream ostream)
			{
				string str = this.data.Length + ":";
				ostream.Write(System.Text.ASCIIEncoding.ASCII.GetBytes(str), 0, str.Length);
				ostream.Write(this.data, 0, this.data.Length);
			}

			
			/// <summary></summary>
			/// <returns>Unique hashcode for the string</returns>
			public override int GetHashCode()
			{
				if (this.data.Length <= 0)
					return 0;
				else
				{
					int hash = this.data[0];

					for (int i=1; i<this.data.Length; ++i)
					{
						hash ^= this.data[i];
					}

					return hash;
				}
			}
			
			
			/// <summary></summary>
			/// <returns>String representation of the ASCII bencode string</returns>
			public override string ToString()
			{
				return System.Text.ASCIIEncoding.ASCII.GetString(data);
			}
			
			
			/// <summary>Implicitly converts a .NET string to a bencoded one</summary>
			/// <param name="str">.NET string</param>
			/// <returns>bencoded string</returns>
			public static implicit operator BEncode.String(string str)
			{
				return new BEncode.String(str);
			}
			
			
			/// <summary>Implicitly converts a bencoded string to a .NET one</summary>
			/// <param name="str">bencoded string</param>
			/// <returns>.NET string</returns>
			public static implicit operator string(BEncode.String str)
			{
				return str.ToString();
			}
			
			
			/// <summary>Indexer, returns the byte at the specified index</summary>
			/// <param name="index">Index to get/set</param>
			/// <returns>Byte at index</returns>
			public byte this[int index]
			{
				get { return this.data[index]; }
				set { this.data[index] = value; }
			}
			
			
			#region IComparable Members


			/// <summary>Compares the two objects</summary>
			/// <param name="obj">Object to compare to</param>
			/// <returns>Zero if they are the same</returns>
			int System.IComparable.CompareTo(object obj)
			{
				return this.CompareTo((BEncode.String)obj);
			}
			
			
			/// <summary>Compares the two objects</summary>
			/// <param name="str">String to compare to</param>
			/// <returns>Zero if they are the same</returns>
			public int CompareTo(BEncode.String str)
			{
				return this.ToString().CompareTo(str);
			}


			#endregion


			#region IEnumerable Members
			
			
			/// <summary>Gets the enumerator</summary>
			/// <returns>Enumerator</returns>
			public IEnumerator GetEnumerator()
			{
				return this.data.GetEnumerator();
			}


			#endregion
		}


		/// <summary>
		/// Integer class
		/// </summary>
		public class Integer : Element
		{
			private int integer;


			/// <summary></summary>
			/// <returns>Integer element</returns>
			public int Data
			{
				get { return integer; }
			}


			/// <summary>Constructs an integer from the bEncoded stream</summary>
			/// <param name="istream">Stream to construct integer from</param>
			internal Integer(IO.Stream istream)
			{
				if (istream.CanSeek)
					this.position = (int)istream.Position - 1;
				string numstr = "";
				char c;
				while ((c = (char)istream.ReadByte()) != 'e')
				{
					numstr += c;
				}
				this.integer = System.Int32.Parse(numstr);

				if (istream.CanSeek)
					this.length = (int)istream.Position - this.position;
			}


			internal override void Write(System.IO.Stream ostream)
			{
				string str = "i" + this.ToString() + "e";
				ostream.Write(System.Text.ASCIIEncoding.ASCII.GetBytes(str), 0, str.Length);
			}

			
			
			/// <summary>Constructs an integer</summary>
			/// <param name="i">Integer value</param>
			public Integer(int i)
			{
				this.integer = i;
			}
			
			
			/// <summary>Implicitly converts from an integer to a bencoded integer</summary>
			/// <param name="i">Integer</param>
			/// <returns>bencoded integer</returns>
			public static implicit operator Integer(int i)
			{
				return new BEncode.Integer(i);
			}
			
			
			/// <summary>Implicitly converts from an integer to a bencoded integer</summary>
			/// <param name="i">bencoded integer</param>
			/// <returns>Integer</returns>
			public static implicit operator int(BEncode.Integer i)
			{
				return i.Data;
			}
			
			
			/// <summary>Converts the integer to it's string equivalent</summary>
			/// <returns>string</returns>
			public override string ToString()
			{
				return this.integer.ToString();
			}
			
			
			/// <summary>Converts the integer to it's string equivalent</summary>
			/// <param name="format">Format to use</param>
			/// <returns>string</returns>
			public string ToString(string format)
			{
				return this.integer.ToString(format);
			}
		}


		/// <summary>
		/// List class
		/// </summary>
		public class List : Element, ICollection, IList, IEnumerable
		{
			private ArrayList list = new ArrayList();


			/// <summary>Constructs an integer from the bEncoded stream</summary>
			/// <param name="istream">Stream to construct integer from</param>
			public List(IO.Stream istream)
			{
				if (istream.CanSeek)
					this.position = (int)istream.Position - 1;

				int c;

				while ((c = istream.ReadByte()) != 'e')
				{
					BEncode.Element element = BEncode.NextElement(istream, c);
					string el = element.ToString();
					this.list.Add(element);
				}

				if (istream.CanSeek)
					this.length = (int)istream.Position - this.position;
			}
			

			internal override void Write(IO.Stream ostream)
			{
				ostream.WriteByte((int)'l');
				foreach (BEncode.Element element in this.list)
				{
					element.Write(ostream);
				}
				ostream.WriteByte((int)'e');
			}

			
			/// <summary>Constructs an empty list</summary>
			public List()
			{
			}
			
			
			#region ICollection members
			
			
			/// <summary></summary>
			/// <returns>Number of items in the collection</returns>
			public int Count
			{
				get { return this.list.Count; }
			}

			
			/// <summary></summary>
			/// <returns>Whether the collection is synchronized</returns>
			public bool IsSynchronized
			{
				get { return this.list.IsSynchronized; }
			}


			/// <summary></summary>
			/// <returns>Synchronization root</returns>
			public object SyncRoot
			{
				get { return this.list.SyncRoot; }
			}


			/// <summary>Copies the contents of the collection to any array</summary>
			/// <param name="array">Destination array</param>
			/// <param name="index">Destination index to start copying</param>
			public void CopyTo(System.Array array, int index)
			{
				this.list.CopyTo(array, index);
			}


			#endregion
			
			
			#region IList members
			
			
			/// <summary></summary>
			/// <returns>True if the list is of fixed size</returns>
			public bool IsFixedSize
			{
				get { return this.list.IsFixedSize; }
			}


			/// <summary></summary>
			/// <returns>True if the list is read only</returns>
			public bool IsReadOnly
			{
				get { return this.list.IsReadOnly; }
			}


			/// <summary></summary>
			/// <param name="index">Index to get/set</param>
			/// <returns>Object at specified index</returns>
			object IList.this[int index]
			{
				get { return this.list[index]; }
				set { this.list[index] = value; }
			}
			
			
			/// <summary></summary>
			/// <param name="index">Index to get/set</param>
			/// <returns>Element at specified index</returns>
			public BEncode.Element this[int index]
			{
				get { return (BEncode.Element)this.list[index]; }
				set { this.list[index] = value; }
			}


			/// <summary></summary>
			/// <param name="obj">Object to add to list</param>
			/// <returns>Index of object added</returns>
			int IList.Add(object obj)
			{
				return this.Add((BEncode.Element)obj);
			}
			
			
			/// <summary></summary>
			/// <param name="element">Object to add to list</param>
			/// <returns>Index of object added</returns>
			public int Add(BEncode.Element element)
			{
				return this.list.Add(element);
			}


			/// <summary>Clears all elements from the list</summary>
			public void Clear()
			{
				this.list.Clear();
			}


			/// <summary></summary>
			/// <param name="obj">Object to check if it exists in the list</param>
			/// <returns>True if object is in the list, false otherwise</returns>
			bool IList.Contains(object obj)
			{
				return this.Contains((BEncode.Element)obj);
			}
			
			
			/// <summary></summary>
			/// <param name="element">Object to check if it exists in the list</param>
			/// <returns>True if object is in the list, false otherwise</returns>
			public bool Contains(BEncode.Element element)
			{
				return this.list.Contains(element);
			}


			/// <summary>Finds the index at which the object resides</summary>
			/// <param name="obj">Object to check</param>
			/// <returns>Index of object, -1 if it does not exist</returns>
			int IList.IndexOf(object obj)
			{
				return this.IndexOf((BEncode.Element)obj);
			}
			
			
			/// <summary>Finds the index at which the element resides</summary>
			/// <param name="obj">Element to check</param>
			/// <returns>Index of element, -1 if it does not exist</returns>
			public int IndexOf(BEncode.Element element)
			{
				return this.list.IndexOf(element);
			}


			/// <summary>Inserts an object into the list at the specified index</summary>
			/// <param name="index">Index to insert object at</param>
			/// <param name="obj">Object to insert</param>
			void IList.Insert(int index, object obj)
			{
				this.Insert(index, (BEncode.Element)obj);
			}
			
			
			/// <summary>Inserts an element into the list at the specified index</summary>
			/// <param name="index">Index to insert element at</param>
			/// <param name="obj">Element to insert</param>
			public void Insert(int index, BEncode.Element element)
			{
				this.list.Insert(index, element);
			}


			/// <summary>Removes an object from the list</summary>
			/// <param name="obj">Object to remove</param>
			void IList.Remove(object obj)
			{
				this.Remove((BEncode.Element)obj);
			}
			
			
			/// <summary>Removes an element from the list</summary>
			/// <param name="obj">Element to remove</param>
			public void Remove(BEncode.Element element)
			{
				this.list.Remove(element);
			}


			/// <summary>Removes an element at the specified index</summary>
			/// <param name="index">Index to remove element from</param>
			public void RemoveAt(int index)
			{
				this.list.RemoveAt(index);
			}
			
			
			#endregion


			#region IEnumerable Members


			/// <summary>Gets the the enumerator for the list</summary>
			/// <returns>Enumerator</returns>
			public IEnumerator GetEnumerator()
			{
				return this.list.GetEnumerator();
			}


			#endregion
		}


		/// <summary>
		/// Dictionary class
		/// </summary>
		public class Dictionary : Element, IDictionary, ICollection, IEnumerable
		{
			private Hashtable map = new Hashtable();


			/// <summary>
			/// Constructs a dictionary
			/// </summary>
			/// <param name="istream">Stream to construct dictionary from</param>
			public Dictionary(IO.Stream istream)
			{
				if (istream.CanSeek)
					this.position = (int)istream.Position - 1;
				int c;

				while ((c = istream.ReadByte()) != 'e')
				{
					BEncode.String key = new BEncode.String(istream, c); // keys are always strings
					string strkey = key;
					Element element = BEncode.NextElement(istream);

					if (!this.map.Contains(key))
						this.map.Add(key, element);
				}

				if (istream.CanSeek)
					this.length = (int)istream.Position - this.position;
			}


			internal override void Write(IO.Stream ostream)
			{
				ostream.WriteByte((int)'d');
				foreach (BEncode.String key in this.map.Keys)
				{
					key.Write(ostream);
					((BEncode.Element)this.map[key]).Write(ostream);
				}
				ostream.WriteByte((int)'e');
			}


			/// <summary>
			/// Gets the dictionary with the specified key. Same as GetElement(), except performs the casting automatically.
			/// </summary>
			/// <param name="key">Key name</param>
			/// <returns>Dictionary element</returns>
			public Dictionary GetDictionary(BEncode.String key)
			{
				return (BEncode.Dictionary)this[key];
			}


			/// <summary>
			/// Gets the list with the specified key. Same as GetElement(), except performs the casting automatically.
			/// </summary>
			/// <param name="key">Key name</param>
			/// <returns>List element</returns>
			public List GetList(BEncode.String key)
			{
				return (BEncode.List)this[key];
			}


			/// <summary>
			/// Gets the string with the specified key. Same as GetElement(), except performs the casting automatically.
			/// </summary>
			/// <param name="key">Key name</param>
			/// <returns>String element</returns>
			public string GetString(BEncode.String key)
			{
				return (BEncode.String)this[key];
			}
			
			
			/// <summary>
			/// Gets the bytes with the specified key. Same as GetElement(), except performs the casting automatically.
			/// </summary>
			/// <param name="key">Key name</param>
			/// <returns>Bytes element</returns>
			public byte[] GetBytes(BEncode.String key)
			{
				return ((BEncode.String)this[key]).Data;
			}


			/// <summary>
			/// Gets the integer with the specified key. Same as GetElement(), except performs the casting automatically.
			/// </summary>
			/// <param name="key">Key name</param>
			/// <returns>Integer element</returns>
			public int GetInteger(BEncode.String key)
			{
				return (BEncode.Integer)this[key];
			}


			#region IDictionary Members


			/// <summary></summary>
			/// <returns>True if the dictionary is read only</returns>
			public bool IsReadOnly
			{
				get { return this.map.IsReadOnly; }
			}


			/// <summary>Gets the enumerator for the dictionary</summary>
			/// <returns>Dictionary enumerator</returns>
			public IDictionaryEnumerator GetEnumerator()
			{
				return this.map.GetEnumerator();
			}


			/// <summary>Retrieves the value associated with the specified key</summary>
			/// <param name="key">Key</param>
			/// <returns>Value</returns>
			object IDictionary.this[object key]
			{
				get { return this[(BEncode.String)key]; }
				set { this[(BEncode.String)key] = (BEncode.Element)value; }
			}


			/// <summary>Retrieves the value associated with the specified key</summary>
			/// <param name="key">Key</param>
			/// <returns>Value</returns>
			public BEncode.Element this[BEncode.String key]
			{
				get { return (BEncode.Element)this.map[key]; }
				set { this.map[key] = value; }
			}


			/// <summary>Removes a key-value pair from the dictionary</summary>
			/// <param name="key">Key to remove</param>
			void IDictionary.Remove(object key)
			{
				this.Remove((BEncode.String)key);
			}


			/// <summary>Removes a key-value pair from the dictionary</summary>
			/// <param name="key">Key to remove</param>
			public void Remove(BEncode.String key)
			{
				this.map.Remove(key);
			}


			/// <summary>Checks if the key exists in the dictionary</summary>
			/// <param name="key">Key to check</param>
			/// <returns>True if the key exists</returns>
			bool IDictionary.Contains(object key)
			{
				return this.Contains((BEncode.String)key);
			}


			/// <summary>Checks if the key exists in the dictionary</summary>
			/// <param name="key">Key to check</param>
			/// <returns>True if the key exists</returns>
			public bool Contains(BEncode.String key)
			{
				return this.map.Contains(key);
			}


			/// <summary>Clears the dictionary of all key-value pairs</summary>
			public void Clear()
			{
				this.map.Clear();
			}


			/// <summary></summary>
			/// <returns>Values held in the dictionary</returns>
			public ICollection Values
			{
				get { return this.map.Values; }
			}


			/// <summary>Adds a key-value pair to the dictionary</summary>
			/// <param name="key">Key</param>
			/// <param name="val">Value</param>
			void IDictionary.Add(object key, object val)
			{
				this.Add((BEncode.String)key, (BEncode.Element)val);
			}
			
			
			/// <summary>Adds a key-value pair to the dictionary</summary>
			/// <param name="key">Key</param>
			/// <param name="val">Value</param>
			public void Add(BEncode.String key, BEncode.Element val)
			{
				this.map.Add(key, val);
			}


			/// <summary></summary>
			/// <returns>Keys held in the dictionary</returns>
			public ICollection Keys
			{
				get { return this.map.Keys; }
			}


			/// <summary></summary>
			/// <returns>True if the dictionary is of fixed size</returns>
			public bool IsFixedSize
			{
				get { return this.map.IsFixedSize; }
			}


			#endregion


			#region ICollection Members


			/// <summary></summary>
			/// <returns>True if the dictionary is synchronized</returns>
			public bool IsSynchronized
			{
				get { return this.map.IsSynchronized; }
			}


			/// <summary></summary>
			/// <returns>Number of key-value pairs in the dictionary</returns>
			public int Count
			{
				get { return this.map.Count; }
			}


			/// <summary>Copies the contents of the dictionary to the array</summary>
			/// <param name="array">Destination array</param>
			/// <param name="index">Index of destination array to copy to</param>
			public void CopyTo(System.Array array, int index)
			{
				this.map.CopyTo(array, index);
			}


			/// <summary></summary>
			/// <returns>The synchronization root of the dictionary</returns>
			public object SyncRoot
			{
				get { return this.map.SyncRoot; }
			}


			#endregion


			#region IEnumerable Members


			/// <summary>Gets the enumerator for the dictionary</summary>
			/// <returns>Enumerator</returns>
			IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return this.map.GetEnumerator();
			}


			#endregion
		}


		#region Reading methods

	
		/// <summary>
		/// Returns the next element in the stream.
		/// </summary>
		/// <param name="istream">bEncoded stream to extract element from</param>
		/// <returns>bEncode element</returns>
		public static Element NextElement(IO.Stream istream)
		{
			return NextElement(istream, istream.ReadByte());
		}


		public static Element NextElement(IO.Stream istream, int type)
		{
			switch (type)
			{
				case 'd': // dictionary
					return new Dictionary(istream);
				case 'l': // list
					return new List(istream);
				case 'i': // integer
					return new Integer(istream);
				case '0': // string,  // case zero should never happen, but file may have bugged bencode implementation
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
					return new String(istream, type);
				default:
					throw new IO.IOException("Corrupt bEncode stream");
			}
		}
		
		
		/// <summary>
		/// Returns the next dictionary in the stream, if you are expecting one.
		/// </summary>
		/// <param name="istream">bEncoded stream to extract element from</param>
		/// <returns>bEncoded dictionary</returns>
		public static Dictionary NextDictionary(IO.Stream istream)
		{
			return (Dictionary)NextElement(istream);
		}
		
		
		/// <summary>
		/// Returns the next list in the stream, if you are expecting one.
		/// </summary>
		/// <param name="istream">bEncoded stream to extract element from</param>
		/// <returns>bEncoded list</returns>
		public static List NextList(IO.Stream istream)
		{
			return (List)NextElement(istream);
		}
		
		
		/// <summary>
		/// Returns the next string in the stream, if you are expecting one.
		/// </summary>
		/// <param name="istream">bEncoded stream to extract element from</param>
		/// <returns>string</returns>
		public static string NextString(IO.Stream istream)
		{
			return ((String)NextElement(istream));
		}
		
		
		/// <summary>
		/// Returns the next byte stream in the stream, if you are expecting one.
		/// </summary>
		/// <param name="istream">bEncoded stream to extract element from</param>
		/// <returns>bytes</returns>
		public static byte[] NextBytes(IO.Stream istream)
		{
			return ((String)NextElement(istream)).Data;
		}
		
		
		/// <summary>
		/// Returns the next integer in the stream, if you are expecting one.
		/// </summary>
		/// <param name="istream">bEncoded stream to extract element from</param>
		/// <returns>integer</returns>
		public static int NextInteger(IO.Stream istream)
		{
			return ((Integer)NextElement(istream));
		}


		#endregion


		#region Writing methods

		public static void WriteElement(IO.Stream ostream, Element element)
		{
			element.Write(ostream);
		}

		#endregion

	}
}
