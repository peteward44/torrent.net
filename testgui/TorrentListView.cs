using System;
using WinForms = System.Windows.Forms;
using BT = BitTorrent;


/// <summary>
/// Summary description for TorrentListView.
/// </summary>
public class TorrentListView : WinForms.ListView
{
	public TorrentListView()
	{
		// prepare torrent list view
		this.View = WinForms.View.Details;
		this.FullRowSelect = true;
		this.Columns.Add("Name", 200, WinForms.HorizontalAlignment.Left);
		this.Columns.Add("Status", 100, WinForms.HorizontalAlignment.Left);
		this.Columns.Add("Complete", 100, WinForms.HorizontalAlignment.Left);
		this.Columns.Add("Messages", 200, WinForms.HorizontalAlignment.Left);
	}
}

