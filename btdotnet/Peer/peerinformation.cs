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

namespace BitTorrent
{
	/// <summary>
	/// Different BitTorrent clients use their own stylized ID's, so the client
	/// used can be calculated from it's id.
	/// </summary>
	public enum PeerClientType
	{
		Unknown,
		// begin "Azureus style"
		Ares,
		Arctic,
		Artemis,
    Azureus,
		BitPump,
		BitBuddy,
		BitComet,
		BitFlu,
		BTG,
		BitTorrentPro,
		BitRocket,
		BTSlave,
		BitWombat,
		BittorrentX,
		CTorrent,
		CTorrentEnhanced,
		DelugeTorrent,
		PropagateDataClient,
		EBit,
		ElectricSheep,
		FileCroc,
		FoxTorrent,
		GSTorrent,
		Halite,
		Hydranode,
		KGet,
		KTorrent,
		LeechCraft,
		LHABC,
		Lphant,
		LibTorrent,
		LimeWire,
		MonoTorrent,
		MooPolice,
		Miro,
		MoonlightTorrent,
		NetTransport,
		OmegaTorrent,
		Pando,
		qBittorrent,
		QQDownload,
		Qt4TorrentExample,
		Retriever,
		ShareazaAlpha,
		SwiftBit,
		SymTorrent,
		SharkTorrent,
		Shareaza,
		SwarmScope,
		TorrentDotNet, // that's us!
		Torrentstorm,
		Transmission,
		TuoTu,
		uLeecher,
		uTorrentMac,
		uTorrent,
		Vagaa,
		BitLet,
		FireTorrent,
		XanTorrent,
		Xunlei,
		XTorrent,
		ZipTorrent,

		// begin "Shadow's style"
		ABC,
		OspreyPermaSeed,
		BTQueue,
		Tribler,
    ShadowsClient,
		BitTornado,
    UPnPNATBitTorrent,

		// begin unique style
		BramsClient,
		BitLord,
		XBTClient,
		Opera,
		MLDonkey,
		BitsOnWheels,
		QueenBee,

		BitTyrant,
		TorrenTopia,
		BitSpirit,
		Rufus,
		G3Torrent,
		FlashGet,
		BTNextEvolution,
		AllPeers,
		Qvod,
	}


	/// <summary>
	/// A simple POD to store information on a peer. Used by class TrackerProtocol to keep
	/// a list of available peers
	/// </summary>
	public class PeerInformation
	{
		private string ip;
		private int port;
		private ByteField20 id;
		private PeerClientType clientType;
		private string versionNumber;


		/// <summary></summary>
		/// <returns>IP address of the peer</returns>
		public string IP
		{
			get { return this.ip; }
		}


		/// <summary></summary>
		/// <returns>Peer's self-chosen ID</returns>
		public ByteField20 ID
		{
			get { return this.id; }
			set
			{
				this.id = value;
				this.clientType = DetermineClientType(this.id, out this.versionNumber);
			}
		}


		/// <summary></summary>
		/// <returns>Port number to connect to</returns>
		public int Port
		{
			get { return this.port; }
		}
		
		
		/// <summary></summary>
		/// <returns>Peer client type</returns>
		public PeerClientType ClientType
		{
			get { return this.clientType; }
		}
		
		
		/// <summary></summary>
		/// <returns>Version number of the client. If it could not be determined, return zero</returns>
		public string ClientVersion
		{
			get { return this.versionNumber; }
		}


		/// <summary>Constructs a PeerInformation object</summary>
		/// <param name="ip">IP address of peer</param>
		/// <param name="port">Port number</param>
		/// <param name="id">Peer ID</param>
		public PeerInformation(string ip, int port, ByteField20 id)
		{
			this.ip = ip;
			this.port = port;
			this.id = id;
			
			// determine client type from id
			this.clientType = DetermineClientType(this.id, out this.versionNumber);
		}


		/// <summary>Constructs a PeerInformation object</summary>
		/// <param name="ip">IP address of peer</param>
		/// <param name="port">Port number</param>
		public PeerInformation(string ip, int port)
			: this(ip, port, null)
		{
		}


		private static bool IsValidAzereusStyle( string strId, ref PeerClientType type, ref string versionNumber )
		{
			// first check for Azurues-style
			if ( strId[ 0 ] != '-' || strId[ 7 ] != '-' )
				return false;

			switch ( strId.Substring( 1, 2 ) )
			{
				case "AG":
				case "A~":
					type = PeerClientType.Ares;
					break;
				case "AR":
					type = PeerClientType.Arctic;
					break;
				case "AT":
					type = PeerClientType.Artemis;
					break;
				case "AX":
					type = PeerClientType.BitPump;
					break;
				case "AZ":
					type = PeerClientType.Azureus;
					break;
				case "BB":
					type = PeerClientType.BitBuddy;
					break;
				case "BC":
					type = PeerClientType.BitComet;
					break;
				case "BF":
					type = PeerClientType.BitFlu;
					break;
				case "BG":
					type = PeerClientType.BTG;
					break;
				case "BP":
					type = PeerClientType.BitTorrentPro;
					break;
				case "BR":
					type = PeerClientType.BitRocket;
					break;
				case "BS":
					type = PeerClientType.BTSlave;
					break;
				case "BW":
					type = PeerClientType.BitWombat;
					break;
				case "BX":
					type = PeerClientType.BittorrentX;
					break;
				case "CD":
					type = PeerClientType.CTorrentEnhanced;
					break;
				case "CT":
					type = PeerClientType.CTorrent;
					break;
				case "DE":
					type = PeerClientType.DelugeTorrent;
					break;
				case "DP":
					type = PeerClientType.PropagateDataClient;
					break;
				case "EB":
					type = PeerClientType.EBit;
					break;
				case "ES":
					type = PeerClientType.ElectricSheep;
					break;
				case "FC":
					type = PeerClientType.FileCroc;
					break;
				case "FT":
					type = PeerClientType.FoxTorrent;
					break;
				case "GS":
					type = PeerClientType.GSTorrent;
					break;
				case "HL":
					type = PeerClientType.Halite;
					break;
				case "HN":
					type = PeerClientType.Hydranode;
					break;
				case "KG":
					type = PeerClientType.KGet;
					break;
				case "KT":
					type = PeerClientType.KTorrent;
					break;
				case "LC":
					type = PeerClientType.LeechCraft;
					break;
				case "LH":
					type = PeerClientType.LHABC;
					break;
				case "LP":
					type = PeerClientType.Lphant;
					break;
				case "LT":
				case "lt":
					type = PeerClientType.LibTorrent;
					break;
				case "LW":
					type = PeerClientType.LimeWire;
					break;
				case "MO":
					type = PeerClientType.MonoTorrent;
					break;
				case "MP":
					type = PeerClientType.MooPolice;
					break;
				case "MR":
					type = PeerClientType.Miro;
					break;
				case "MT":
					type = PeerClientType.MoonlightTorrent;
					break;
				case "NX":
					type = PeerClientType.NetTransport;
					break;
				case "OT":
					type = PeerClientType.OmegaTorrent;
					break;
				case "PD":
					type = PeerClientType.Pando;
					break;
				case "qB":
					type = PeerClientType.qBittorrent;
					break;
				case "QD":
					type = PeerClientType.Qt4TorrentExample;
					break;
				case "RT":
					type = PeerClientType.Retriever;
					break;
				case "S~":
					type = PeerClientType.ShareazaAlpha;
					break;
				case "SB":
					type = PeerClientType.SwiftBit;
					break;
				case "SS":
					type = PeerClientType.SwarmScope;
					break;
				case "ST":
					type = PeerClientType.SymTorrent;
					break;
				case "st":
					type = PeerClientType.SharkTorrent;
					break;
				case "SZ":
					type = PeerClientType.Shareaza;
					break;
				case "TN":
					type = PeerClientType.TorrentDotNet;
					break;
				case "TR":
					type = PeerClientType.Transmission;
					break;
				case "TS":
					type = PeerClientType.Torrentstorm;
					break;
				case "TT":
					type = PeerClientType.TuoTu;
					break;
				case "UL":
					type = PeerClientType.uLeecher;
					break;
				case "UM":
					type = PeerClientType.uTorrentMac;
					break;
				case "UT":
					type = PeerClientType.uTorrent;
					break;
				case "VG":
					type = PeerClientType.Vagaa;
					break;
				case "WT":
					type = PeerClientType.BitLet;
					break;
				case "WY":
					type = PeerClientType.FireTorrent;
					break;
				case "XL":
					type = PeerClientType.Xunlei;
					break;
				case "XT":
					type = PeerClientType.XanTorrent;
					break;
				case "XX":
					type = PeerClientType.XTorrent;
					break;
				case "ZT":
					type = PeerClientType.ZipTorrent;
					break;
				default:
					return false;
			}

			versionNumber = strId.Substring( 3, 4 );

			return true;
		}


		private static bool IsValidShadowStyle( string strId, ref PeerClientType clientType, ref string versionNumber )
		{
			if ( strId.Substring( 5, 3 ) != "---" )
				return false;

			versionNumber = strId.Substring( 1, 5 );

			switch ( strId[ 0 ] )
			{
				case 'A':
					clientType = PeerClientType.ABC;
					break;
				case 'O':
					clientType = PeerClientType.OspreyPermaSeed;
					break;
				case 'Q':
					clientType = PeerClientType.BTQueue;
					break;
				case 'R':
					clientType = PeerClientType.Tribler;
					break;
				case 'S':
					clientType = PeerClientType.ShadowsClient;
					break;
				case 'T':
					clientType = PeerClientType.BitTornado;
					break;
				case 'U':
					clientType = PeerClientType.UPnPNATBitTorrent;
					break;
				default:
					return false;
			}

			return true;
		}


		private static bool IsValidBramStyle( string strId, ref PeerClientType clientType, ref string versionNumber )
		{
			if ( strId[0] == 'M' )
			{
				versionNumber = strId.Substring( 1, 7 );
				clientType = PeerClientType.BramsClient;
				return true;
			}
			else if ( strId[ 0 ] == 'Q' )
			{
				versionNumber = strId.Substring( 1, 7 );
				clientType = PeerClientType.QueenBee;
				return true;
			}

			return false;
		}


		private static bool IsOldBitCometStyle( string strId, ref PeerClientType clientType, ref string versionNumber )
		{
			if ( !strId.StartsWith( "exbc" ) && !strId.StartsWith( "FUTB" ) ) // old BitComet style, now uses Azereus style
				return false;

			versionNumber = ((int)strId[ 4 ]) + "." + ((int)strId[ 5 ]);

			if ( strId.Substring( 6, 4 ) == "LORD" )
				clientType = PeerClientType.BitLord;
			else
				clientType = PeerClientType.BitComet;

			return true;
		}


		private static bool IsXBTStyle( string strId, ref PeerClientType clientType, ref string versionNumber )
		{
			if ( strId.Substring( 0, 3 ) != "XBT" )
				return false;

			versionNumber = strId.Substring( 3, 3 );
			clientType = PeerClientType.XBTClient;

			return true;
		}


		private static bool IsOperaStyle( string strId, ref PeerClientType clientType, ref string versionNumber )
		{
			if ( strId.Substring( 0, 2 ) != "OP" )
				return false;

			versionNumber = strId.Substring( 2, 4 );
			clientType = PeerClientType.Opera;

			return true;
		}


		private static bool IsMLDonkeyStyle( string strId, ref PeerClientType clientType, ref string versionNumber )
		{
			if ( strId.Substring( 0, 3 ) != "-ML" )
				return false;

			int versionEnd = strId.IndexOf( '-', 3 );
			if ( versionEnd < 0 )
				return false;

			versionNumber = strId.Substring( 3, versionEnd-3 );
			clientType = PeerClientType.MLDonkey;

			return true;
		}


		private static bool IsBitsOnWheelsStyle( string strId, ref PeerClientType clientType, ref string versionNumber )
		{
			if ( strId.Substring( 0, 4 ) != "-BOW" )
				return false;

			versionNumber = strId.Substring( 4, 3 );
			clientType = PeerClientType.BitsOnWheels;

			return true;
		}


		private static bool IsBitTyrantStyle( string strId, ref PeerClientType clientType, ref string versionNumber )
		{
			if ( strId.StartsWith( "AZ2500BT" ) )
			{
				clientType = PeerClientType.BitTyrant;
				return true;
			}
			else
				return false;
		}


		private static bool IsTorrenTopiaStyle( string strId, ref PeerClientType clientType, ref string versionNumber )
		{
			if ( strId.StartsWith( "346------" ) )
			{
				clientType = PeerClientType.TorrenTopia;
				return true;
			}
			else
				return false;
		}


		private static bool IsBitSpiritStyle( string strId, ref PeerClientType clientType, ref string versionNumber )
		{
			// BitSpirit has several modes for its peer ID. In one mode it reads the ID of its peer
			// and reconnects using the first eight bytes as a basis for its own ID.
			// Its real ID appears to use '\0\3BS' (C notation) as the first four bytes for version 3.x
			// and '\0\2BS' for version 2.x. In all modes the ID may end in 'UDP0'.
			if ( strId.Substring( 2, 2 ) != "BS" )
				return false;

			if ( strId[0] == 0 && strId[1] == 3 )
			{
				versionNumber = "3.x";
				clientType = PeerClientType.BitSpirit;
				return true;
			}
			else if ( strId[ 0 ] == 0 && strId[ 1 ] == 2 )
			{
				versionNumber = "2.x";
				clientType = PeerClientType.BitSpirit;
				return true;
			}

			return false;
		}


		private static bool IsRufusStyle( string strId, ref PeerClientType clientType, ref string versionNumber )
		{
			if ( strId.Substring( 2, 2 ) != "RS" )
				return false;

			clientType = PeerClientType.Rufus;
			versionNumber = strId.Substring( 0, 2 );
			return true;
		}


		private static bool IsG3Style( string strId, ref PeerClientType clientType, ref string versionNumber )
		{
			if ( strId.StartsWith( "-G3" ) )
			{
				clientType = PeerClientType.G3Torrent;
				return true;
			}
			else
				return false;
		}


		private static bool IsFlashGetStyle( string strId, ref PeerClientType clientType, ref string versionNumber )
		{
			if ( strId.StartsWith( "-FG" ) )
			{
				versionNumber = strId.Substring( 3, 4 );
				clientType = PeerClientType.FlashGet;
				return true;
			}
			else
				return false;
		}


		private static bool IsBTNextEvolutionStyle( string strId, ref PeerClientType clientType, ref string versionNumber )
		{
			if ( strId.StartsWith( "-NE" ) )
			{
				versionNumber = strId.Substring( 3, 4 );
				clientType = PeerClientType.BTNextEvolution;
				return true;
			}
			else
				return false;
		}


		private static bool IsAllPeersStyle( string strId, ref PeerClientType clientType, ref string versionNumber )
		{
			if ( strId.StartsWith( "AP" ) )
			{
				int versionEnd = strId.IndexOf( '-', 2 );
				if ( versionEnd < 0 )
					return false;
				versionNumber = strId.Substring( 2, versionEnd - 2 );
				clientType = PeerClientType.AllPeers;
				return true;
			}
			else
				return false;
		}


		private static bool IsQvodStyle( string strId, ref PeerClientType clientType, ref string versionNumber )
		{
			if ( strId.StartsWith( "QVOD" ) )
			{
				versionNumber = strId.Substring( 4, 4 );
				clientType = PeerClientType.Qvod;
				return true;
			}
			else
				return false;
		}


		/// <summary>
		/// Determines what client the peer is using.
		/// </summary>
		/// <param name="id">Peer ID</param>
		/// <param name="versionNumber">Version of the client (if it can be determined)</param>
		/// <returns>Type of client used by peer</returns>
		public static PeerClientType DetermineClientType( ByteField20 id, out string versionNumber )
		{
			PeerClientType clientType = PeerClientType.Unknown;
			versionNumber = "";

			if (id == null)
				return PeerClientType.Unknown;

			string strId = id.ToString();
			
			// first check for Azurues-style
			if ( IsValidAzereusStyle( strId, ref clientType, ref versionNumber ) )
				return clientType;
			// then check for shadow-style
			if ( IsValidShadowStyle( strId, ref clientType, ref versionNumber ) )
				return clientType;
			if ( IsOldBitCometStyle( strId, ref clientType, ref versionNumber ) )
				return clientType;
			if ( IsXBTStyle( strId, ref clientType, ref versionNumber ) )
				return clientType;
			if ( IsOperaStyle( strId, ref clientType, ref versionNumber ) )
				return clientType;
			if ( IsMLDonkeyStyle( strId, ref clientType, ref versionNumber ) )
				return clientType;
			if ( IsBitsOnWheelsStyle( strId, ref clientType, ref versionNumber ) )
				return clientType;
			if ( IsBitTyrantStyle( strId, ref clientType, ref versionNumber ) )
				return clientType;
			if ( IsTorrenTopiaStyle( strId, ref clientType, ref versionNumber ) )
				return clientType;
			if ( IsBitSpiritStyle( strId, ref clientType, ref versionNumber ) )
				return clientType;
			if ( IsRufusStyle( strId, ref clientType, ref versionNumber ) )
				return clientType;
			if ( IsG3Style( strId, ref clientType, ref versionNumber ) )
				return clientType;
			if ( IsFlashGetStyle( strId, ref clientType, ref versionNumber ) )
				return clientType;
			if ( IsBTNextEvolutionStyle( strId, ref clientType, ref versionNumber ) )
				return clientType;
			if ( IsAllPeersStyle( strId, ref clientType, ref versionNumber ) )
				return clientType;
			if ( IsQvodStyle( strId, ref clientType, ref versionNumber ) )
				return clientType;
			if ( IsValidBramStyle( strId, ref clientType, ref versionNumber ) )
				return clientType;

			return clientType;
		}


		public override bool Equals(object obj)
		{
			if (obj is PeerInformation)
			{
				PeerInformation peerinfo = (PeerInformation)obj;
				return this.ip == peerinfo.ip && this.port == peerinfo.port;
			}
			else
				return false;
		}


		public override int GetHashCode()
		{
			return this.ip.GetHashCode() ^ this.port ^ this.id.GetHashCode();
		}

	}
}
