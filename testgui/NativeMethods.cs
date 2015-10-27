using System;
using System.Runtime.InteropServices;
using WinForms = System.Windows.Forms;

// 29/6/03

/// <summary>
/// Flags for the PlaySound() method
/// </summary>
public enum PlaySoundFlags
{
	/// <summary>
	/// The sound is played using an application-specific association.
	/// </summary>
	Application = 0x80,
	/// <summary>
	/// The pszSound parameter is a system-event alias in the registry or the WIN.INI file.
	/// Do not use with either SND_FILENAME or SND_RESOURCE.
	/// </summary>
	Alias = 0x10000,
	/// <summary>
	/// The pszSound parameter is a predefined sound identifier.
	/// </summary>
	AliasId = 0x110000,
	/// <summary>
	/// The sound is played asynchronously and PlaySound returns immediately
	/// after beginning the sound. To terminate an asynchronously played waveform sound,
	/// call PlaySound with pszSound set to NULL.
	/// </summary>
	Asynchronous = 0x01,
	/// <summary>
	/// The sound plays repeatedly until PlaySound is called again with the pszSound parameter
	/// set to NULL. You must also specify the SND_ASYNC flag to indicate an asynchronous
	/// sound event.
	/// </summary>
	Loop = 0x08,
	/// <summary>
	/// No default sound event is used. If the sound cannot be found,
	/// PlaySound returns silently without playing the default sound.
	/// </summary>
	NoDefault = 0x02,
	/// <summary>
	/// The specified sound event will yield to another sound event that is already playing.
	/// If a sound cannot be played because the resource needed to generate that sound
	/// is busy playing another sound, the function immediately returns FALSE without
	/// playing the requested sound. 
	/// If this flag is not specified,
	/// PlaySound attempts to stop the currently playing sound so that the device
	/// can be used to play the new sound.
	/// </summary>
	NoStop = 0x10,
	/// <summary>
	/// If the driver is busy, return immediately without playing the sound.
	/// </summary>
	NoWait = 0x2000,
	/// <summary>
	/// Sounds are to be stopped for the calling task.
	/// If pszSound is not NULL, all instances of the specified sound are stopped.
	/// If pszSound is NULL, all sounds that are playing on behalf of the calling task are stopped. 
	/// You must also specify the instance handle to stop SND_RESOURCE events.
	/// </summary>
	Purge = 0x40,
	/// <summary>
	/// Synchronous playback of a sound event. PlaySound returns after the sound event completes. 
	/// </summary>
	Synchronous = 0x00
}


/// <summary>
/// Flags for the FlashWindow() method
/// </summary>
public enum FlashWindowFlags
{
	/// <summary>
	/// Flash the taskbar button
	/// </summary>
	FlashTray = 0x00000002,
	/// <summary>
	/// Flash the window caption
	/// </summary>
	FlashCaption = 0x00000001,
	/// <summary>
	/// Flash both the window caption and taskbar button
	/// </summary>
	FlashBoth = FlashTray | FlashCaption,
}


/// <summary>
/// Holds all Windows native methods.
/// </summary>
public class NativeMethods
{

	#region SendMessage methods


	[DllImport("user32.dll")]
	private static extern Int32 SendMessage(IntPtr hwnd, Int32 msg, Int32 wparam, Int32 lparam);
	

	/// <summary>
	/// 
	/// </summary>
	/// <param name="control"></param>
	/// <param name="msg"></param>
	/// <param name="wparam"></param>
	/// <param name="lparam"></param>
	/// <returns></returns>
	public static Int32 SendMessage(WinForms.Control control, Int32 msg, Int32 wparam, Int32 lparam)
	{
		return SendMessage(control.Handle, msg, wparam, lparam);
	}


	/// <summary>
	/// 
	/// </summary>
	/// <param name="control"></param>
	/// <param name="msg"></param>
	/// <param name="wparam"></param>
	/// <returns></returns>
	public static Int32 SendMessage(WinForms.Control control, Int32 msg, Int32 wparam)
	{
		return SendMessage(control, msg, wparam, 0);
	}


	/// <summary>
	/// 
	/// </summary>
	/// <param name="control"></param>
	/// <param name="msg"></param>
	/// <returns></returns>
	public static Int32 SendMessage(WinForms.Control control, Int32 msg)
	{
		return SendMessage(control, msg, 0);
	}


	#endregion


	#region PlaySound methods


	[DllImport("winmm.dll")]
	private static extern bool PlaySound(string fileName, IntPtr hmodule, Int32 flags);


	/// <summary>
	/// 
	/// </summary>
	/// <param name="fileName"></param>
	/// <param name="flags"></param>
	/// <returns></returns>
	public static bool PlaySoundFromFile(string fileName, PlaySoundFlags flags)
	{
		return PlaySound(fileName, (IntPtr)0, 0x20000 | (int)flags);
	}


	/// <summary>
	/// 
	/// </summary>
	/// <param name="resourceName"></param>
	/// <param name="module"></param>
	/// <param name="flags"></param>
	/// <returns></returns>
	public static bool PlaySoundFromResource(string resourceName, IntPtr module, PlaySoundFlags flags)
	{
		return PlaySound(resourceName, module, 0x40004 | (int)flags);
	}


	/// <summary>
	/// 
	/// </summary>
	/// <param name="memoryPtr"></param>
	/// <param name="flags"></param>
	/// <returns></returns>
	public static bool PlaySoundFromMemory(IntPtr memoryPtr, PlaySoundFlags flags)
	{
		return PlaySound(string.Empty, memoryPtr, 0x04 | (int)flags);
	}


	#endregion


	#region Flash window methods


	[DllImport("user32.dll")]
	private static extern bool FlashWindowEx(ref FlashWindowInfo info);

	[StructLayout(LayoutKind.Sequential)]
		private struct FlashWindowInfo
	{
		public UInt32  cbSize;
		public IntPtr  hwnd;
		public UInt32 dwFlags;
		public UInt32  uCount;
		public UInt32 dwTimeout;
	}


	/// <summary>
	/// 
	/// </summary>
	/// <param name="form"></param>
	/// <param name="count"></param>
	/// <param name="flags"></param>
	/// <returns></returns>
	public static bool FlashWindow(WinForms.Form form, uint count, FlashWindowFlags flags)
	{
		return FlashWindow(form, count, flags, 0);
	}


	/// <summary>
	/// 
	/// </summary>
	/// <param name="form"></param>
	/// <param name="count"></param>
	/// <param name="flags"></param>
	/// <param name="timeout"></param>
	/// <returns></returns>
	public static bool FlashWindow(WinForms.Form form, uint count,
		FlashWindowFlags flags, uint timeout)
	{
		FlashWindowInfo info = new FlashWindowInfo();

		info.cbSize = 20;
		info.dwFlags = 0x0000000C | (uint)flags; // FLASHW_TIMERNOFG
		info.dwTimeout = timeout;
		info.hwnd = form.Handle;
		info.uCount = count;

		return FlashWindowEx(ref info);
	}


	/// <summary>
	/// 
	/// </summary>
	/// <param name="form"></param>
	/// <param name="count"></param>
	/// <param name="flags"></param>
	/// <returns></returns>
	public static bool FlashWindowUntilStop(WinForms.Form form, uint count, FlashWindowFlags flags)
	{
		return FlashWindowUntilStop(form, count, flags, 0);
	}


	/// <summary>
	/// 
	/// </summary>
	/// <param name="form"></param>
	/// <param name="count"></param>
	/// <param name="flags"></param>
	/// <param name="timeout"></param>
	/// <returns></returns>
	public static bool FlashWindowUntilStop(WinForms.Form form, uint count,
		FlashWindowFlags flags, uint timeout)
	{
		if (flags == 0)
			throw new ArgumentException("Flags cannot be zero", "flags");

		FlashWindowInfo info = new FlashWindowInfo();

		info.cbSize = 20;
		info.dwFlags = 0x00000004 | (uint)flags; // FLASHW_TIMER
		info.dwTimeout = timeout;
		info.hwnd = form.Handle;
		info.uCount = count;

		return FlashWindowEx(ref info);
	}


	/// <summary>
	/// 
	/// </summary>
	/// <param name="form"></param>
	/// <returns></returns>
	public static bool FlashWindowStop(WinForms.Form form)
	{
		FlashWindowInfo info = new FlashWindowInfo();

		info.cbSize = 20;
		info.dwFlags = 0; // FLASHW_STOP
		info.dwTimeout = 0;
		info.hwnd = form.Handle;
		info.uCount = 0;

		return FlashWindowEx(ref info);
	}


	#endregion



	/// <summary>
	/// 
	/// </summary>
	/// <param name="control"></param>
	public static void VerticalScrollToBottom(WinForms.Control control)
	{
		if (control.Created)
		{
			SendMessage(control.Handle, 0x0115 /* WM_VSCROLL */, 7 /* VB_BOTTOM */, 0);
		}
	}


	private NativeMethods()
	{}
}
