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
	public enum PeerMessage
	{
		Choke = 0,
		Unchoke = 1,
		Interested = 2,
		Uninterested = 3,
		Have = 4,
		Bitfield = 5,
		Request = 6,
		Piece = 7,
		Cancel = 8,
		KeepAlive = 9
	}


	public delegate void PeerFinishedPieceTransfer(object state, bool success);



	public class PeerProtocol
	{
		private const string protocolString = "BitTorrent protocol";
		private Throttle upthrottle = new Throttle(), downThrottle = new Throttle();



		public Throttle UpThrottle
		{
			get { return upthrottle; }
		}

		public Throttle DownThrottle
		{
			get { return downThrottle; }
		}

	
		public PeerProtocol()
		{}


		#region Handshaking methods
		
		
		public static void SendHandshake(IO.Stream stream, ByteField20 infoDigest)
		{
			stream.WriteByte((byte)protocolString.Length);
			stream.Write(System.Text.ASCIIEncoding.ASCII.GetBytes(protocolString), 0, protocolString.Length);

			// 8 zeros
			stream.Write(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 0, 8);

			// SHA digest
			stream.Write(infoDigest.Data, 0, infoDigest.Data.Length);
		}


		public static void SendPeerId(IO.Stream netStream, ByteField20 peerId)
		{
			netStream.Write(peerId.Data, 0, peerId.Data.Length);
		}
		
		
		public static void ReceiveHandshake(IO.Stream stream, ref ByteField20 infoDigest)
		{
			// read in protocol string
			byte[] protocolVersionLength = new byte[1];
			if ( stream.Read( protocolVersionLength, 0, protocolVersionLength.Length ) != protocolVersionLength.Length )
				throw new System.Exception( "Invalid handshake protocol" );
			if ( protocolVersionLength[0] != 19 )
				throw new System.Exception( "Invalid handshake protocol" );
			byte[] protocolBytes = new byte[ protocolVersionLength[ 0 ] ];
			if ( stream.Read( protocolBytes, 0, protocolBytes.Length ) != protocolBytes.Length )
				throw new System.Exception( "Invalid handshake protocol" );
			string protocol = System.Text.ASCIIEncoding.ASCII.GetString(protocolBytes, 0, protocolBytes.Length);
			if (protocol != protocolString)
				throw new System.Exception( "Invalid handshake protocol" );

			// 8 zeros
			byte[] zeroes = new byte[ 8 ];
			if ( stream.Read( zeroes, 0, zeroes.Length ) != zeroes.Length )
				throw new System.Exception( "Invalid handshake protocol" );

			// SHA digest
			stream.Read(infoDigest.Data, 0, infoDigest.Data.Length);
		}


		public static bool ReceivePeerId(IO.Stream stream, ref ByteField20 peerId)
		{
			return stream.Read(peerId.Data, 0, peerId.Data.Length) > 0;
		}


		#endregion
		
		
		#region Sending messages methods
		
		
		// i would use IO.BinaryWriter but that caused problems
		private static void WriteInt(IO.Stream stream, int val)
		{
			byte[] b = new byte[4];
			b[3] = (byte)(val & 0x000000FF);
			b[2] = (byte)((val & 0x0000FF00) >> 8);
			b[1] = (byte)((val & 0x00FF0000) >> 16);
			b[0] = (byte)((val & 0xFF000000) >> 24);
			stream.Write(b, 0, b.Length);
//			stream.Write(System.BitConverter.GetBytes(val), 0, 4);
		}
		
		
		private static void SendMessageHeader(IO.Stream stream, PeerMessage type, int length)
		{
//			Config.LogDebugMessage("Message sent: " + type.ToString());

			WriteInt(stream, length+1);
			stream.WriteByte((byte)type);
		}
		
		
		public static void SendBitfieldMessage(IO.Stream stream, DownloadFile downloadFile)
		{
			int bitfieldLength = downloadFile.Bitfield.Count/8 + ((downloadFile.Bitfield.Count % 8) > 0 ? 1 : 0);
			byte[] bitfield = new byte[bitfieldLength];
			downloadFile.Bitfield.CopyTo(bitfield, 0);
			SendMessageHeader(stream, PeerMessage.Bitfield, bitfieldLength);
			stream.Write(bitfield, 0, bitfield.Length);
		}
		
		
		public void SendInterestedMessage(IO.Stream stream, bool interested)
		{
			SendMessageHeader(stream, (interested ? PeerMessage.Interested : PeerMessage.Uninterested), 0);
			stream.Flush();
		}
		
		
		public void SendChokeMessage(IO.Stream stream, bool choked)
		{
			SendMessageHeader(stream, (choked ? PeerMessage.Choke : PeerMessage.Unchoke), 0);
			stream.Flush();
		}
		
		
		public void SendPieceRequest(IO.Stream stream, int pieceId, int begin, int length)
		{
			SendMessageHeader(stream, PeerMessage.Request, 12);
			WriteInt(stream, pieceId);
			WriteInt(stream, begin);
			WriteInt(stream, length);
			stream.Flush();
		}
		
		
		public void SendPieceCancel(IO.Stream stream, int pieceId, int begin, int length)
		{
			SendMessageHeader(stream, PeerMessage.Cancel, 12);
			WriteInt(stream, pieceId);
			WriteInt(stream, begin);
			WriteInt(stream, length);
			stream.Flush();
		}
		
		
		public void SendPiece(IO.Stream ostream, int pieceId, int begin, int length, IO.Stream istream, PeerFinishedPieceTransfer callback, object state)
		{
			SendMessageHeader(ostream, PeerMessage.Piece, 8 + length);
			WriteInt(ostream, pieceId);
			WriteInt(ostream, begin);

			byte[] writeData = new byte[length];
			istream.Read(writeData, 0, length);

			object[] objs = new object[4];
			objs[0] = ostream;
			objs[1] = callback;
			objs[2] = state;
			objs[3] = length;

			ostream.BeginWrite(writeData, 0, writeData.Length, new System.AsyncCallback(OnWriteFinished), (object)objs);
		}


		private void OnWriteFinished(System.IAsyncResult ar)
		{
			object[] objs = (object[])ar.AsyncState;
			IO.Stream ostream = (IO.Stream)objs[0];
			PeerFinishedPieceTransfer callback = (PeerFinishedPieceTransfer)objs[1];
			object state = objs[2];

			try
			{
				ostream.EndWrite(ar);
				this.upthrottle.AddData((int)objs[3]);
				Config.LogDebugMessage("Piece sent: " + (int)objs[3]);

				callback(state, true);
			}
			catch (System.Exception)
			{
				callback(state, false);
			}
		}


		public void SendHaveMessage(IO.Stream stream, int pieceId)
		{
			SendMessageHeader(stream, PeerMessage.Have, 4);
			WriteInt(stream, pieceId);
			stream.Flush();
		}


		public void SendKeepAlive(IO.Stream stream)
		{
//			WriteInt(stream, 0);
		}
		
		
		#endregion
		
		
		#region Incoming message methods
		
		
		private int ReadInt(IO.Stream stream)
		{
			int p1 = stream.ReadByte();
			int p2 = stream.ReadByte();
			int p3 = stream.ReadByte();
			int p4 = stream.ReadByte();
			int final = 0;
			final |= (p1 << 24);
			final |= (p2 << 16);
			final |= (p3 << 8);
			final |= (p4);
			return final;
			
//			byte[] b = new byte[4];
//			stream.Read(b, 0, 4);
//			return (int)System.BitConverter.ToUInt32(b, 0);
		}
		
		
		public void ReadMessageHeader(IO.Stream stream, out int length, out PeerMessage type)
		{
			length = ReadInt(stream);
			
			if (length > 0)
				type = (PeerMessage)stream.ReadByte();
			else
				type = PeerMessage.KeepAlive;
		}
		
		
		public BitField ReadBitfieldMessage(IO.Stream stream, int length)
		{
			byte[] bitfield = new byte[ length ];
			stream.Read(bitfield, 0, bitfield.Length);
			return new BitField(bitfield, 0, bitfield.Length);
		}
		
		
		public int ReadHaveMessage(IO.Stream stream)
		{
			return ReadInt(stream);
		}
		
		
		public void ReadRequestMessage(IO.Stream stream, out int index, out int begin, out int pieceLength)
		{
			index = ReadInt(stream);
			begin = ReadInt(stream);
			pieceLength = ReadInt(stream);
		}

		
		public void ReadCancelMessage(IO.Stream stream, out int index, out int begin, out int pieceLength)
		{
			index = ReadInt(stream);
			begin = ReadInt(stream);
			pieceLength = ReadInt(stream);
		}


		// a bit different, leaves the actual piece reading in alone.
		public void ReadPieceMessageHeader(IO.Stream stream, int length, out int index, out int begin, out int pieceLength)
		{
			index = ReadInt(stream);
			begin = ReadInt(stream);
			pieceLength = length - 8;
		}

		
		#endregion
	}
}
