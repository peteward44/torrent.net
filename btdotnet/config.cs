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

using Net = System.Net;


namespace BitTorrent
{
	public delegate void DebugMessageCallback(string message);


	/// <summary>Stores all configuration values</summary>
	public class Config
	{
		/// <summary>Currently active configuration</summary>
		public static Config ActiveConfig = Config.Load();

		
		/// <summary>File path to save torrents to</summary>
		public string PathPrefix = "";
		
		
		/// <summary>IP to send to trackers to report in their peer lists. This should be the IP address of a NAT gateway or otherwise.
		/// Usually trackers are clever enough to determine NAT gateways, however if your ISP uses a transparent web-proxy this would
		/// need to be specified</summary>
//		public string ServerIP = "81.101.93.237";
//		public string ServerIP = "192.168.0.5";
		public string ServerIP = "";
		
		
		/// <summary>This is different to ServerIP as ServerIP is sent to the _tracker_ and thereby should be your
		/// NAT gateway IP (most, if not all, trackers can detect the ServerIP for you), whilst IPToBindServerTo is
		/// a local IP address you want the server to run on. This is for computers with multiple interfaces.</summary>
		public string IPToBindServerTo = "";
		
		/// <summary>Port on which server is running</summary>
		public int MinServerPort = 7000;
		public int MaxServerPort = 7009;

		public int ChosenPort = 0;
		
		/// <summary>Web proxy URL</summary>
		public string ProxyURL = "";

		/// <summary>Maximum amount of data to ask another peer for</summary>
		public const int MaxRequestSize = 16384;
		
		/// <summary>Major version number of the software</summary>
		public const int MajorVersionNumber = 0;
		
		/// <summary>Minor version number of the software</summary>
		public const int MinorVersionNumber = 1;
		
		public const int KeepAliveInterval = 30;


		public int SimultaneousDownloadsLimit = 20;
		public int SimultaneousUploadsLimit = 4;

		public int MaxPeersConnected = 40;

		public bool OnlyOneConnectionFromEachIP = true;
		public bool EnableEndGame = true;

		/// <summary>
		/// Percentage of pieces in which are left for EndGame to kick in
		/// </summary>
		public float EndGamePercentage = 5.0f;

		public const int DefaultNumWant = 200;

		public const int ChokeInterval = 10 * 1000; // milliseconds between each choke change

	//	public int FileCacheSize = 32 * 1024 * 1024; // cache size in bytes to use for the file cache. the bigger it is, less file access needed



		public static readonly string ApplicationDataDirectory = GetApplicationDataDirectory();


		private static string GetApplicationDataDirectory()
		{
			string dir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData) + System.IO.Path.DirectorySeparatorChar + "btdotnet";
			System.IO.Directory.CreateDirectory(dir);
			return dir;
		}


		public static bool OutputDebugMessages = true;
		public static event DebugMessageCallback DebugMessageEvent;


		public static void LogDebugMessage(string message)
		{
			if (OutputDebugMessages && DebugMessageEvent != null)
				DebugMessageEvent(message);
		}


		public static void LogException(System.Exception e)
		{
			LogDebugMessage("Exception caught: " + e.Message + " Stack: " + e.StackTrace);
		}


		/// <summary>Constructs a config object</summary>
		public Config()
		{}


		/// <summary>Loads a config from a file</summary>
		public static Config Load()
		{
			return new Config();
		}
		
		
		/// <summary>Saves the config to a file</summary>
		public void Save()
		{
		}
	}
}
