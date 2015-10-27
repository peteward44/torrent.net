using System;
using WinForms = System.Windows.Forms;
using BT = BitTorrent;


/// <summary>
/// Summary description for PeerListViewItem.
/// </summary>
public class PeerListViewItem : WinForms.ListViewItem
{
	private BT.Peer peer;


	public BT.Peer Peer
	{
		get { return this.peer; }
	}


	public PeerListViewItem(BT.Peer peer)
	{
		this.peer = peer;

		this.Text = peer.ToString();

		this.SubItems.Add("Leech");
		this.SubItems.Add("0");
		this.SubItems.Add("0");
		this.SubItems.Add("Y");
		this.SubItems.Add("N");
		this.SubItems.Add("Y");
		this.SubItems.Add("N");
		this.SubItems.Add("0");
		this.SubItems.Add(peer.Information.ClientType.ToString());
		this.SubItems.Add(peer.Information.ID.ToString());

		peer.BitfieldChange += new BitTorrent.PeerBitfieldChangeCallback(peer_BitfieldChange);
		peer.HeIsChokingChange += new BitTorrent.PeerStatusChangeCallback(peer_HeIsChokingChange);
		peer.HeIsInterestedChange += new BitTorrent.PeerStatusChangeCallback(peer_HeIsInterestedChange);
		peer.IAmChokingChange += new BitTorrent.PeerStatusChangeCallback(peer_IAmChokingChange);
		peer.IAmInterestedChange += new BitTorrent.PeerStatusChangeCallback(peer_IAmInterestedChange);

		peer.DownThrottle.RateChange += new BitTorrent.RateChangeCallback(peer_DownloadRateChange);
		peer.UpThrottle.RateChange +=new BitTorrent.RateChangeCallback(peer_UploadRateChange);
	}


	public void UnhookPeerEvents()
	{
		peer.BitfieldChange -= new BitTorrent.PeerBitfieldChangeCallback( peer_BitfieldChange );
		peer.HeIsChokingChange -= new BitTorrent.PeerStatusChangeCallback( peer_HeIsChokingChange );
		peer.HeIsInterestedChange -= new BitTorrent.PeerStatusChangeCallback( peer_HeIsInterestedChange );
		peer.IAmChokingChange -= new BitTorrent.PeerStatusChangeCallback( peer_IAmChokingChange );
		peer.IAmInterestedChange -= new BitTorrent.PeerStatusChangeCallback( peer_IAmInterestedChange );

		peer.DownThrottle.RateChange -= new BitTorrent.RateChangeCallback( peer_DownloadRateChange );
		peer.UpThrottle.RateChange -= new BitTorrent.RateChangeCallback( peer_UploadRateChange );
	}


	private delegate void SetSubItemTextDelegate( int subitem, string text );

	private void SetSubItemText( int subitem, string text )
	{
		this.SubItems[ subitem ].Text = text;
	}

	private void SetSubItemTextAsync( int subitem, string text )
	{
		if ( this.ListView != null )
			this.ListView.BeginInvoke( new SetSubItemTextDelegate( SetSubItemText ), new object[] { subitem, text } );
	}


	public override string ToString()
	{
		return this.peer.ToString();
	}


	private void peer_BitfieldChange(BitTorrent.Peer peer, int pieceId)
	{
		SetSubItemTextAsync( 1, peer.IsSeed ? "Seed" : "Leech" );
		SetSubItemTextAsync( 8, peer.BitField.PercentageTrue.ToString( "0.0" ) );
	}

	private void peer_HeIsChokingChange(BitTorrent.Peer peer, bool newStatus)
	{
		SetSubItemTextAsync( 6, newStatus ? "Y" : "N" );
	}

	private void peer_HeIsInterestedChange(BitTorrent.Peer peer, bool newStatus)
	{
		SetSubItemTextAsync( 7, newStatus ? "Y" : "N" );
	}

	private void peer_IAmChokingChange(BitTorrent.Peer peer, bool newStatus)
	{
		SetSubItemTextAsync( 4, newStatus ? "Y" : "N" );
	}

	private void peer_IAmInterestedChange(BitTorrent.Peer peer, bool newStatus)
	{
		SetSubItemTextAsync( 5, newStatus ? "Y" : "N" );
	}

	private void peer_DownloadRateChange(float newRate)
	{
		if (this.SubItems[2].Text != null)
			SetSubItemTextAsync( 2, newRate.ToString() );
	}

	private void peer_UploadRateChange(float newRate)
	{
		if (this.SubItems[3].Text != null)
			SetSubItemTextAsync( 3, newRate.ToString() );
	}
}

