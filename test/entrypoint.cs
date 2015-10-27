
using System;
using IO = System.IO;
using BitTorrent;
using Net = System.Net;


public class Test
{
	public static void OnCheckIntegrity(Torrent torrent, int pieceId, bool good, float percentDone)
	{
		Console.WriteLine("Piece: " + pieceId + " Good: " + good + " Percent: " + percentDone);
	}


	public static int Main(string[] args)
	{
		/*
		try
		{
			System.Console.WriteLine("Enter torrent filename: ");
			string filename = System.Console.ReadLine();
			System.Console.WriteLine("Upload: ");
			System.UInt64 upload = System.UInt64.Parse(System.Console.ReadLine());

			MetainfoFile info = new MetainfoFile(@"H:\download\" + filename);

		{
			string url = info.AnnounceUrl;

			url += url.IndexOf("?") >= 0 ? "&" : "?";

			url += "info_hash=" + TrackerProtocol.UriEscape(info.InfoDigest.Data);
			url += "&peer_id=" + TrackerProtocol.UriEscape(info.PeerID.Data);
			url += "&port=" + 7000;
			url += "&uploaded=" + 0;
			url += "&downloaded=" + 0;
			url += "&left=" + 0;
			url += "&event=started";

			if (Config.ActiveConfig.ServerIP != "")
				url += "&ip=" + Config.ActiveConfig.ServerIP;

			Console.WriteLine("URL: " + url);

			Net.HttpWebRequest request = (Net.HttpWebRequest)Net.WebRequest.Create(url);

			request.KeepAlive = false;
			request.UserAgent = "BitTorrent/3.4.2";
			request.Headers.Add("Cache-Control", "no-cache");
			request.ProtocolVersion = new System.Version(1, 0);

			request.GetResponse();
		}

			System.Threading.Thread.Sleep(20 * 1000);

		{
			string url = info.AnnounceUrl;

			url += url.IndexOf("?") >= 0 ? "&" : "?";

			url += "info_hash=" + TrackerProtocol.UriEscape(info.InfoDigest.Data);
			url += "&peer_id=" + TrackerProtocol.UriEscape(info.PeerID.Data);
			url += "&port=" + 7000;
			url += "&uploaded=" + upload;
			url += "&downloaded=" + 0;
			url += "&left=" + 0;
			url += "&event=stopped";

			if (Config.ActiveConfig.ServerIP != "")
				url += "&ip=" + Config.ActiveConfig.ServerIP;

			Console.WriteLine("URL: " + url);

			Net.HttpWebRequest request = (Net.HttpWebRequest)Net.WebRequest.Create(url);

			request.KeepAlive = false;
			request.UserAgent = "BitTorrent/3.4.2";
			request.Headers.Add("Cache-Control", "no-cache");
			request.ProtocolVersion = new System.Version(1, 0);

			request.GetResponse();
		}

		}
		catch (Exception e)
		{
			Console.WriteLine("Exception caught: " + e.Message);
			Console.WriteLine("StackTrace: " + e.StackTrace);
		}
	*/
		return 0;
	}
}

