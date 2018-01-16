using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ImGuiNET;
namespace LancerEdit
{
	public class ImGuiExt
	{
		static Dictionary<string, IntPtr> strings = new Dictionary<string, IntPtr>();

		static IntPtr GetPtr(string s)
		{
			IntPtr ptr;
			if (!strings.TryGetValue(s, out ptr))
			{
				var bytes = System.Text.Encoding.UTF8.GetBytes(s);
				ptr = Marshal.AllocHGlobal(bytes.Length + 1);
				Marshal.Copy(bytes, 0, ptr, bytes.Length);
				Marshal.WriteByte(ptr, bytes.Length, 0);
				strings.Add(s, ptr);
			}
			return ptr;
		}

		[DllImport("cimgui", EntryPoint = "igShutdownDock")]
		public static extern void ShutdownDock();

		[DllImport("cimgui", EntryPoint = "igEndDock")]
		public static extern void EndDock();

		[DllImport("cimgui", EntryPoint = "igSetDockActive")]
		public static extern void SetDockActive();

		[DllImport("cimgui", EntryPoint = "igRootDock")]
		public static extern void RootDock(float posx, float posy, float sizew, float sizeh);

		[DllImport("cimgui")]
		static extern bool igBeginDock([MarshalAs(UnmanagedType.LPStr)]string label, IntPtr opened, int extraflags);

		public static unsafe bool BeginDock(string label, ref bool opened, WindowFlags extraflags)
		{
			fixed(bool *ptr = &opened)
			{
				return igBeginDock(label, (IntPtr)ptr, (int)extraflags);
			}
		}

		public static bool BeginDock(string label)
		{
			return igBeginDock(label, IntPtr.Zero, 0);
		}

		[DllImport("cimgui", EntryPoint = "igSaveDock")]
		public static extern void SaveDock();
	}
}
