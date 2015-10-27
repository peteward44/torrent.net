using System;
using WinForms = System.Windows.Forms;
using BT = BitTorrent;


/// <summary>
/// Summary description for TorrentListViewItem.
/// </summary>
public class TorrentListViewItem : WinForms.ListViewItem
{
	private BT.Torrent torrent;

	private delegate void UpdateSubItemTextDelegate( int subitem, string text );


	public TorrentListViewItem(BT.Torrent torrent)
	{
		this.torrent = torrent;

		this.Text = torrent.ToString();
		this.SubItems.Add(torrent.Status.ToString());
		this.SubItems.Add("");
		this.SubItems.Add("");

		torrent.StatusChanged += new BT.TorrentStatusChangedCallback(OnTorrentChangeStatus);
		torrent.PercentChanged += new BT.PercentChangedCallback( torrent_PercentChanged );
		torrent.TrackerError += new BT.TorrentTrackerErrorCallback(torrent_TrackerError);
	}


	public override string ToString()
	{
		return this.torrent.ToString();
	}


	private void UpdateSubItemText( int subitem, string text )
	{
		this.SubItems[ subitem ].Text = text;
	}


	private void OnTorrentChangeStatus(BT.Torrent torrent, BT.TorrentStatus status)
	{
		this.ListView.BeginInvoke( new UpdateSubItemTextDelegate( UpdateSubItemText ), new object[] { 1, status.ToString() } );
	}

	private void torrent_PercentChanged(BitTorrent.DownloadFile file, float percent)
	{
		this.ListView.BeginInvoke( new UpdateSubItemTextDelegate( UpdateSubItemText ), new object[] { 2, percent.ToString( "0.0" ) } );
	}

	private void torrent_TrackerError(BT.Torrent torrent, string message)
	{
		this.ListView.BeginInvoke( new UpdateSubItemTextDelegate( UpdateSubItemText ), new object[] { 3, message } );
	}
}

