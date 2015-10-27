using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;

using WinForms = System.Windows.Forms;
using BT = BitTorrent;


/// <summary>
/// Summary description for Form1.
/// </summary>
public class MainForm : System.Windows.Forms.Form
{
	private System.Windows.Forms.MainMenu mainMenu;
	private System.Windows.Forms.MenuItem fileMenuItem;
	private System.Windows.Forms.MenuItem openMenuItem;
	private System.Windows.Forms.MenuItem exitMenuItem;
	/// <summary>
	/// Required designer variable.
	/// </summary>
	private System.ComponentModel.Container components = null;
	private TorrentListView torrentListView;
	private PeerListView peerListView;
	private System.Windows.Forms.TextBox debugMessageBox;
	private System.Windows.Forms.Splitter splitter2;
	private System.Windows.Forms.Splitter splitter1;


	private BT.Session btsession;
	private volatile bool mThreadRunning = true;

	private static MainForm sInstance = null;


	public void OnIdle( object sender, EventArgs e )
	{
//		btsession.ProcessWaitingData();
	}


	private void Thread()
	{
		// initialise bittorrent session
		this.btsession = new BT.Session();

		while ( mThreadRunning )
		{
			btsession.ProcessWaitingData();
			System.Threading.Thread.Sleep( 10 );
		}
	}


	public MainForm()
	{
		sInstance = this;
		//
		// Required for Windows Form Designer support
		//
		InitializeComponent();

		BT.Config.DebugMessageEvent += new BT.DebugMessageCallback(Config_DebugMessageEvent);

		Application.Idle += new EventHandler( OnIdle );

		System.Threading.Thread thread = new System.Threading.Thread( new System.Threading.ThreadStart( Thread ) );
		thread.Start();
	}

	/// <summary>
	/// Clean up any resources being used.
	/// </summary>
	protected override void Dispose( bool disposing )
	{
		mThreadRunning = false;

		if( disposing )
		{
			if (components != null) 
			{
				components.Dispose();
			}
		}

		if (this.btsession != null)
			this.btsession.Dispose();
		this.btsession = null;

		base.Dispose( disposing );
	}


	#region Windows Form Designer generated code
	/// <summary>
	/// Required method for Designer support - do not modify
	/// the contents of this method with the code editor.
	/// </summary>
	private void InitializeComponent()
	{
		this.mainMenu = new System.Windows.Forms.MainMenu();
		this.fileMenuItem = new System.Windows.Forms.MenuItem();
		this.openMenuItem = new System.Windows.Forms.MenuItem();
		this.exitMenuItem = new System.Windows.Forms.MenuItem();
		this.torrentListView = new TorrentListView();
		this.peerListView = new PeerListView();
		this.debugMessageBox = new System.Windows.Forms.TextBox();
		this.splitter2 = new System.Windows.Forms.Splitter();
		this.splitter1 = new System.Windows.Forms.Splitter();
		this.SuspendLayout();
		// 
		// mainMenu
		// 
		this.mainMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																																					 this.fileMenuItem});
		// 
		// fileMenuItem
		// 
		this.fileMenuItem.Index = 0;
		this.fileMenuItem.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																																							 this.openMenuItem,
																																							 this.exitMenuItem});
		this.fileMenuItem.Text = "&File";
		// 
		// openMenuItem
		// 
		this.openMenuItem.Index = 0;
		this.openMenuItem.Text = "&Open";
		this.openMenuItem.Click += new System.EventHandler(this.openMenuItem_Click);
		// 
		// exitMenuItem
		// 
		this.exitMenuItem.Index = 1;
		this.exitMenuItem.Text = "E&xit";
		this.exitMenuItem.Click += new System.EventHandler(this.exitMenuItem_Click);
		// 
		// torrentListView
		// 
		this.torrentListView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.torrentListView.Dock = System.Windows.Forms.DockStyle.Top;
		this.torrentListView.FullRowSelect = true;
		this.torrentListView.Location = new System.Drawing.Point(0, 0);
		this.torrentListView.Name = "torrentListView";
		this.torrentListView.Size = new System.Drawing.Size(904, 88);
		this.torrentListView.TabIndex = 5;
		this.torrentListView.View = System.Windows.Forms.View.Details;
		// 
		// peerListView
		// 
		this.peerListView.AllowColumnReorder = true;
		this.peerListView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.peerListView.Dock = System.Windows.Forms.DockStyle.Fill;
		this.peerListView.FullRowSelect = true;
		this.peerListView.Location = new System.Drawing.Point(0, 88);
		this.peerListView.Name = "peerListView";
		this.peerListView.Size = new System.Drawing.Size(904, 417);
		this.peerListView.TabIndex = 7;
		this.peerListView.View = System.Windows.Forms.View.Details;
		// 
		// debugMessageBox
		// 
		this.debugMessageBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.debugMessageBox.Dock = System.Windows.Forms.DockStyle.Bottom;
		this.debugMessageBox.Location = new System.Drawing.Point(0, 281);
		this.debugMessageBox.Multiline = true;
		this.debugMessageBox.Name = "debugMessageBox";
		this.debugMessageBox.ReadOnly = true;
		this.debugMessageBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
		this.debugMessageBox.Size = new System.Drawing.Size(904, 224);
		this.debugMessageBox.TabIndex = 8;
		this.debugMessageBox.Text = "";
		// 
		// splitter2
		// 
		this.splitter2.Dock = System.Windows.Forms.DockStyle.Top;
		this.splitter2.Location = new System.Drawing.Point(0, 88);
		this.splitter2.MinSize = 5;
		this.splitter2.Name = "splitter2";
		this.splitter2.Size = new System.Drawing.Size(904, 3);
		this.splitter2.TabIndex = 10;
		this.splitter2.TabStop = false;
		// 
		// splitter1
		// 
		this.splitter1.Dock = System.Windows.Forms.DockStyle.Bottom;
		this.splitter1.Location = new System.Drawing.Point(0, 278);
		this.splitter1.Name = "splitter1";
		this.splitter1.Size = new System.Drawing.Size(904, 3);
		this.splitter1.TabIndex = 11;
		this.splitter1.TabStop = false;
		// 
		// MainForm
		// 
		this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
		this.ClientSize = new System.Drawing.Size(904, 505);
		this.Controls.Add(this.splitter1);
		this.Controls.Add(this.splitter2);
		this.Controls.Add(this.debugMessageBox);
		this.Controls.Add(this.peerListView);
		this.Controls.Add(this.torrentListView);
		this.Menu = this.mainMenu;
		this.Name = "MainForm";
		this.Text = "Torrent.NET";
		this.ResumeLayout(false);

	}
	#endregion


	private void openMenuItem_Click(object sender, System.EventArgs e)
	{
		WinForms.OpenFileDialog dialog = new WinForms.OpenFileDialog();
		dialog.Title = "Open torrent file";
		dialog.Filter = @"Torrent file (*.torrent)|*.torrent|All files (*.*)|*.*";
		if (dialog.ShowDialog(this) == WinForms.DialogResult.OK)
		{
			BT.Torrent torrent = btsession.CreateTorrent( dialog.FileName );

			this.torrentListView.Items.Add(new TorrentListViewItem(torrent));

			torrent.PeerConnected += new BT.TorrentPeerConnectedCallback(OnPeerConnected);

			torrent.CheckFileIntegrity( true );
			torrent.Start();
		}
	}

	private delegate void StringDelegate( string text );

	public static void LogDebugMessage( string text )
	{
		if ( sInstance != null )
			sInstance.Invoke( new StringDelegate( BT.Config.LogDebugMessage ), new object[] { text } );
	}


	private delegate ListViewItem AddNewPeerDelegate( PeerListViewItem item );
	private delegate void RemovePeerDelegate( BT.Peer peer );

	private void RemovePeer( BT.Peer peer )
	{
		foreach ( PeerListViewItem item in this.peerListView.Items )
		{
			if ( item.Peer.Equals( peer ) )
			{
				item.UnhookPeerEvents();
				this.peerListView.Items.Remove(item);
				this.peerListView.Refresh();
				break;
			}
		}
	}


	private void OnPeerConnected(BT.Torrent torrent, BT.Peer peer, bool connected)
	{
		if (connected)
		{
			this.peerListView.Invoke( new AddNewPeerDelegate( this.peerListView.Items.Add ), new object[] { new PeerListViewItem( peer ) } );
//			this.peerListView.Items.Add(new PeerListViewItem(peer));
		}
		else
		{
			LogDebugMessage( "Peer disconnected from mainform.cs: " + peer.ToString() );
			this.peerListView.Invoke( new RemovePeerDelegate( RemovePeer ), peer );
		}
	}


	private void exitMenuItem_Click(object sender, System.EventArgs e)
	{
		if (this.btsession != null)
			this.btsession.Dispose();
		this.btsession = null;

		WinForms.Application.Exit();
	}

	private delegate void Config_DebugMessageDelegate( string message );

	private void Config_DebugMessageEventAsync( string message )
	{
		this.debugMessageBox.Text += message + "\r\n";
		NativeMethods.VerticalScrollToBottom( this.debugMessageBox );
	}


	private void Config_DebugMessageEvent(string message)
	{
		this.BeginInvoke( new Config_DebugMessageDelegate( Config_DebugMessageEventAsync ), new object[] { message } );
	}
}
