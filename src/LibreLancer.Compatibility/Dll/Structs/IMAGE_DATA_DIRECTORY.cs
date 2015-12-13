using System;
using System.Runtime.InteropServices;
namespace LibreLancer.Dll.Structs
{
	[StructLayout(LayoutKind.Sequential)]
	struct IMAGE_DATA_DIRECTORY 
	{
		public uint VirtualAddress;
		public uint Size;
	}
}

