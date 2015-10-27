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
using Net = System.Net;
using IO = System.IO;
using Threading = System.Threading;
using PWLib.Platform;


namespace BitTorrent
{
	/// <summary>
	/// For when the peer list updates
	/// </summary>
	/// <param name="tp">Associated tracker protocol</param>
	/// <param name="numNewPeers">New number of peers</param>
	public delegate void TrackerUpdateCallback(ArrayList peers, bool success, string errorMessage);


	public class TrackerException : System.Exception
	{
		public TrackerException(string err)
			: base(err)
		{}
	}


	/// <summary>
	/// Encapsulates the Tracker protocol. Provides all tracker-related communication.
	/// </summary>
	public class TrackerProtocol : System.IDisposable
	{
		private Torrent torrent;
		private MetainfoFile infofile;
		private DownloadFile file;
		private bool autoUpdate = true, badTracker = false;
		private int updateInterval = 0;
		private ArrayList peerList = new ArrayList();
		private Threading.Timer updateTimer = null;
		

		/// <summary>
		/// Called whenever the peer list is updated
		/// </summary>
		public event TrackerUpdateCallback TrackerUpdate;

		
		/// <summary>
		/// Interval in seconds at which the client should communicate with the tracker to keep
		/// it up-to-date with the torrent's progress. It isn't recommended that this is changed
		/// from the default, but it is possible.
		/// </summary>
		/// <returns>Update interval in seconds</returns>
		public int UpdateInterval
		{
			get { return this.updateInterval; }
		}
		
	
		/// <summary>
		/// Whether the TrackerProtocol should automatically update the tracker itself. Defaults to true.
		/// </summary>
		/// <returns>True if autoupdate is on, false otherwise</returns>
		public bool AutoUpdate
		{
			get { return this.autoUpdate; }
			set
			{
				if (this.autoUpdate != value)
				{
					if (value)
					{
						this.updateTimer = new Threading.Timer(new Threading.TimerCallback(OnUpdate), null,
							this.updateInterval * 1000, this.updateInterval * 1000);
					}
					else
					{
						this.updateTimer.Dispose();
						this.updateTimer = null;
					}

					this.autoUpdate = value;
				}
			}
		}


	
		/// <summary>
		/// Constructs a TrackerProtocol
		/// </summary>
		public TrackerProtocol(Torrent torrent, MetainfoFile infofile, DownloadFile file)
		{
			this.torrent = torrent;
			this.infofile = infofile;
			this.file = file;
		}
		

		/// <summary>
		/// URI escapes an array of data
		/// </summary>
		/// <returns>Escaped string</returns>
		public static string UriEscape(byte[] data)
		{
			string escaped = "";

			for (int i=0; i<data.Length; ++i)
			{
      	char c = (char)data[i];

        // this is a list of allowed characters in web escaping
//        if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')))
////					|| c == '$' || c == '-' || c == '_' ||
////					c == '.' || c == '+' || c == '!' || c == '*' || c == '\'' || c == '(' || c == ')'))
//        {
          escaped += System.Uri.HexEscape(c);
				//}
				//else
				//  escaped += c;
			}

			return escaped;
		}


		/// <summary>
		/// Used by the auto-updater, this simply called Update()
		/// </summary>
		/// <param name="state">Not used</param>
		private void OnUpdate(object state)
		{
			this.Update();
		}

		
		/// <summary>
		/// Initiate the connection with the tracker, retrieve a list of peers to use.
		/// </summary>
		/// <param name="infofile">Metainfo file for the torrent</param>
		/// <param name="file">Download file for the torrent</param>
		public void Initiate()
		{
			this.SendWebRequest("started", Config.DefaultNumWant, true);
		}


		/// <summary>
		/// Every now and again the tracker needs to be informed of the torrent's progress as well as knowing that we are
		/// still connected. This tells the tracker how we are doing.
		/// </summary>
		public void Update()
		{
			this.SendWebRequest("", Config.DefaultNumWant, true);
		}


		/// <summary>
		/// This tells the tracker we have finished downloading. This should *not* be called when the torrent starts with
		/// a full torrent, just when it has finished downloading.
		/// </summary>
		public void Completed()
		{
			this.SendWebRequest("completed", 0, true);
		}


		/// <summary>
		/// This is called when a torrent is stopping gracefully.
		/// </summary>
		public void Stop()
		{
			this.SendWebRequest("stopped", 0, false);
		}


		public void Stop(int bytesUploaded, int bytesDownloaded)
		{
			this.SendWebRequest("stopped", 0, false, bytesUploaded, bytesDownloaded);
		}


		public void SendWebRequest(string eventString, int numwant, bool compact)
		{
			this.SendWebRequest(eventString, numwant, compact, this.torrent.BytesUploaded, this.torrent.BytesDownloaded);
		}


		/// <summary>
		/// Creates the WebRequest object for communicating with the tracker
		/// </summary>
		/// <param name="eventString">Event to report. Can be either "started", "completed", "stopped", or "" (for tracker updates)</param>
		/// <param name="numwant">How many peers to request. Defaults to 50.</param>
		/// <param name="compact">True if we want the tracker to respond in compact form. The tracker ignores this if it does not support it</param>
		/// <returns>WebRequest object</returns>
		private void SendWebRequest(string eventString, int numwant, bool compact,
			int bytesUploaded, int bytesDownloaded)
		{
			string newUrl = infofile.AnnounceUrl;

			newUrl += newUrl.IndexOf("?") >= 0 ? "&" : "?";

			newUrl += "info_hash=" + UriEscape(infofile.InfoDigest.Data);
			newUrl += "&peer_id=" + UriEscape(torrent.Session.LocalPeerID.Data);
			newUrl += "&port=" + Config.ActiveConfig.ChosenPort;
			newUrl += "&uploaded=" + bytesUploaded;
			newUrl += "&downloaded=" + bytesDownloaded;
			newUrl += "&left=" + this.file.NumBytesLeft;

			if (Config.ActiveConfig.ServerIP != "")
				newUrl += "&ip=" + Config.ActiveConfig.ServerIP;

			if (numwant > 0)
				newUrl += "&numwant=" + numwant;

			if (compact)
			{
				newUrl += "&compact=1";
				newUrl += "&no_peer_id=1";
			}

//			newUrl += "&key=54644";

			if (eventString != "")
				newUrl += "&event=" + eventString;

			Net.HttpWebRequest request = (Net.HttpWebRequest)Net.WebRequest.Create(newUrl);

			request.KeepAlive = false;
			request.UserAgent = "BitTorrent/3.4.2";
			request.Headers.Add("Cache-Control", "no-cache");
			request.ProtocolVersion = new System.Version(1, 0);

			if (eventString == "stopped" && badTracker)
				return;
			else
				request.BeginGetResponse(new System.AsyncCallback(OnResponse), request);
		}


		private void OnResponse(System.IAsyncResult result)
		{
			try
			{
				Net.HttpWebRequest request = (Net.HttpWebRequest)result.AsyncState;
				Net.WebResponse response = request.EndGetResponse(result);

				this.ParseTrackerResponse(response.GetResponseStream());
				response.Close();

				if (this.TrackerUpdate != null)
					this.TrackerUpdate(this.peerList, true, string.Empty);

				if (this.autoUpdate && this.updateTimer == null)
				{
					this.updateTimer = new Threading.Timer(new Threading.TimerCallback(OnUpdate), null,
						this.updateInterval * 1000, this.updateInterval * 1000);
				}
			}
			catch (System.Exception e)
			{
				if (this.TrackerUpdate != null)
					this.TrackerUpdate(null, false, e.Message);
				badTracker = true;
			}
		}


		/// <summary>
		/// Parses the response from the tracker, and updates the peer list
		/// </summary>
		/// <param name="stream">IO stream from response</param>
		private void ParseTrackerResponse(IO.Stream stream)
		{
			this.peerList.Clear();
/*
			// because the response stream does not support seeking, we copy the contents to a memorystream
			// to send to the bencoder. This shouldnt cause too much of a performance penalty as it shouldnt
			// be too large anyway.
			byte[] data = new byte[ 1024 ];
			IO.MemoryStream responseStream = new IO.MemoryStream();
			int dataRead = 0;

			while ((dataRead = stream.Read(data, 0, data.Length)) > 0)
			{
				responseStream.Write(data, 0, dataRead);
			}

			responseStream.Seek(0, IO.SeekOrigin.Begin);
*/
			///

			BEncode.Dictionary dic = BEncode.NextDictionary(stream);

			// note: sometimes IPs can be duplicated in quick disconnection, so there is a check for any duplications
			
			if (dic.Contains("failure reason"))
			{
				throw new IO.IOException("Tracker connection failed: " + dic.GetString("failure reason"));
			}
			else
			{
				this.updateInterval = dic.GetInteger("interval");

				BEncode.Element peers = dic["peers"];

				if (peers is BEncode.List)
				{
					// peer list comes as a list of dictionaries
					BEncode.List dicList = (BEncode.List)peers;

					foreach (BEncode.Dictionary dicPeer in dicList)
					{
						ByteField20 peerId = new ByteField20(dicPeer.GetBytes("peer id"));
						string peerIp = dicPeer.GetString("ip");
						int port = dicPeer.GetInteger("port");
						PeerInformation peerinfo = new PeerInformation(peerIp, port, peerId);

						if (!this.peerList.Contains(peerinfo))
							this.peerList.Add(peerinfo);
					}
				}
				else if (peers is BEncode.String)
				{
					// else its compressed (this is pretty common)
					byte[] compactPeers = ((BEncode.String)peers).Data;

					for (int i=0; i<compactPeers.Length; i += 6)
					{
						int	ip1 = 0xFF & compactPeers[i];
						int	ip2 = 0xFF & compactPeers[i+1];
						int	ip3 = 0xFF & compactPeers[i+2];
						int	ip4 = 0xFF & compactPeers[i+3];
						int	po1 = 0xFF & compactPeers[i+4];
						int	po2 = 0xFF & compactPeers[i+5];

						string peerIp = ip1 + "." + ip2 + "." + ip3 + "." + ip4;
						int	port = (po1 * 256) + po2;
						PeerInformation peerinfo = new PeerInformation(peerIp, port);

						if (!this.peerList.Contains(peerinfo))
							this.peerList.Add(peerinfo);
					}
				}
				else
					throw new TrackerException("Unexcepted error");
			}
		}
		

		/// <summary>
		/// Gathers statistics about the torrent. This is known as "Scraping"
		/// </summary>
		/// <param name="infofile">Metainfo file on the torrent</param>
		/// <param name="numSeeds">Number of seeds on the torrent</param>
		/// <param name="numLeechers">Number of peers (leechers) on the torrent</param>
		/// <param name="numFinished">Number of successful downloads so far</param>
		public static void Scrape(MetainfoFile infofile, out int numSeeds, out int numLeechers, out int numFinished)
		{
			string name;
			Scrape(infofile, out numSeeds, out numLeechers, out numFinished, out name);
		}

		
		/// <summary>
		/// Gathers statistics about the torrent. This is known as "Scraping"
		/// </summary>
		/// <param name="infofile">Metainfo file on the torrent</param>
		/// <param name="numSeeds">Number of seeds on the torrent</param>
		/// <param name="numLeechers">Number of peers (leechers) on the torrent</param>
		/// <param name="numFinished">Number of successful downloads so far</param>
		/// <param name="name">Name of the torrent</param>
		public static void Scrape(MetainfoFile infofile, out int numSeeds, out int numLeechers, out int numFinished, out string name)
		{
			numSeeds = numLeechers = numFinished = 0;
			name = "";

			// determine the scrape url.
			string announceUrl = infofile.AnnounceUrl;
			
			int lastSlashIndex = announceUrl.LastIndexOf('/');
			if (lastSlashIndex < 0)
				return;
			
			const string announce = "announce";
			
			// check that "announce" exists after the last slash in the url - if it doesn't, scraping isn't supported.
			if (announceUrl.Substring(lastSlashIndex+1, announce.Length).CompareTo(announce) != 0)
				return;
			
			string scapeUrl = announceUrl.Substring(0, lastSlashIndex+1) + "scrape" + announceUrl.Substring(lastSlashIndex + 1 + announce.Length);
			
			scapeUrl += "?";
			scapeUrl += "info_hash=" + UriEscape(infofile.InfoDigest.Data);
			
			Net.WebRequest request = Net.WebRequest.Create(scapeUrl);
			Net.WebResponse response = request.GetResponse();
			IO.Stream stream = response.GetResponseStream();
			
			// because the response stream does not support seeking, we copy the contents to a memorystream
			// to send to the bencoder. This shouldnt cause too much of a performance penalty as it shouldnt
			// be too large anyway.
			byte[] data = new byte[ 1024 ];
			IO.MemoryStream responseStream = new IO.MemoryStream();
			int dataRead = 0;

			while ((dataRead = stream.Read(data, 0, data.Length)) > 0)
			{
				responseStream.Write(data, 0, dataRead);
			}

			responseStream.Seek(0, IO.SeekOrigin.Begin);

			///
			
			BEncode.Dictionary mainDic = BEncode.NextDictionary(responseStream);

			if (mainDic.Contains("files"))
			{
				// extract file information - as we supplied the info_hash value, this dictionary should only contain one value
				BEncode.Dictionary filesDic = mainDic.GetDictionary("files");

				foreach (BEncode.String infoHash in filesDic.Keys)
				{
					BEncode.Dictionary dic = filesDic.GetDictionary(infoHash);
				
					if (dic.Contains("downloaded"))
						numFinished = dic.GetInteger("downloaded");
					
					if (dic.Contains("incomplete"))
						numLeechers = dic.GetInteger("incomplete");
					
					if (dic.Contains("complete"))
						numSeeds = dic.GetInteger("complete");
					
					if (dic.Contains("name"))
						name = dic.GetString("name");
				}
			}
			else if (mainDic.Contains("failure reason"))
				throw new TrackerException("Tracker connection failed: " + mainDic.GetString("failure reason"));
		}


		#region IDisposable Members


		/// <summary>
		/// Disposes the object
		/// </summary>
		public void Dispose()
		{
			if (this.updateTimer != null)
				this.updateTimer.Dispose();
			this.updateTimer = null;
		}

		#endregion
	}
}
