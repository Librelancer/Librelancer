using System;
using System.Runtime.InteropServices;
namespace LibreLancer.Platforms
{
	class MacPlatform : IPlatform
	{
		const int ATTR_VOL_CAPABILITIES = 0x20000;
		const int VOL_CAP_FMT_CASE_SENSITIVE = 256;

		struct attrlist
		{
			public ushort bitmapcount;
			public ushort reserved;
			public uint commonattr;
			public uint volattr;
			public uint dirattr;
			public uint fileattr;
			public uint forkattr;
		}
		//typedef u_int32_t vol_capabilities_set_t[4]
		unsafe struct vol_capabilities_attr
		{
			public fixed uint capabilities[4];
			//vol_capabilities_set_t
			public fixed uint valid[4];
		}

		[DllImport ("libc")]
		static extern unsafe int getattrlist (string path, attrlist*_attrlist, void*attrbuf, IntPtr attrbufsize, IntPtr options);

		public bool IsDirCaseSensitive (string directory)
		{
			var alist = new attrlist ();
			alist.volattr = ATTR_VOL_CAPABILITIES;
			int bufsize = Marshal.SizeOf (typeof(vol_capabilities_attr)) + IntPtr.Size;
			byte* buf = stackalloc byte[bufsize];
			bool success = (getattrlist (directory, &alist, (void*)buf, (IntPtr)bufsize, IntPtr.Zero)) == 0;
			if (success && (alist.volattr & ATTR_VOL_CAPABILITIES) != 0) {
				var vcaps = (vol_capabilities_attr*)buf;
				if ((vcaps->capabilities [0] & VOL_CAP_FMT_CASE_SENSITIVE) != 0) {
					return true;
				}
			}
			return false;
		}
	}
}

