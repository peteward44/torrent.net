using System;
using WinForms = System.Windows.Forms;


public class Entrypoint
{
	private Entrypoint()
	{
	}


	[STAThread]
	public static int Main(string[] args)
	{
		try
		{
			WinForms.Application.Run(new MainForm());
		}
		catch (Exception e)
		{
			WinForms.MessageBox.Show(e.Message + "\r\n\r\nStackTrace: " + e.StackTrace, "Exception caught", WinForms.MessageBoxButtons.OK);
		}

		return 0;
	}
}
