using System;
using WinForms = System.Windows.Forms;
using BT = BitTorrent;


/// <summary>
/// Summary description for PeerListView.
/// </summary>
public class PeerListView : WinForms.ListView
{
	public PeerListView()
	{
		// prepare peer list view
		this.View = WinForms.View.Details;
		this.FullRowSelect = true;
		this.Columns.Add("Address", 100, WinForms.HorizontalAlignment.Left);
		this.Columns.Add("Node Type", 70, WinForms.HorizontalAlignment.Left);
		this.Columns.Add("Download Rate KB/s", 100, WinForms.HorizontalAlignment.Left);
		this.Columns.Add("Upload Rate KB/s", 100, WinForms.HorizontalAlignment.Left);
		this.Columns.Add("I am choking", 100, WinForms.HorizontalAlignment.Left);
		this.Columns.Add("I am interested", 100, WinForms.HorizontalAlignment.Left);
		this.Columns.Add("He is choking", 100, WinForms.HorizontalAlignment.Left);
		this.Columns.Add("He is interested", 100, WinForms.HorizontalAlignment.Left);
		this.Columns.Add("Percentage complete", 100, WinForms.HorizontalAlignment.Left);
		this.Columns.Add("Client Type", 100, WinForms.HorizontalAlignment.Left);
		this.Columns.Add("Client ID", 100, WinForms.HorizontalAlignment.Left);
	}
}

