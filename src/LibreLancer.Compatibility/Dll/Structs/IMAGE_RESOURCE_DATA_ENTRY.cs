using System;
using System.Runtime.InteropServices;
namespace LibreLancer.Dll.Structs
{
	[StructLayout(LayoutKind.Sequential)]
	struct IMAGE_RESOURCE_DATA_ENTRY
	{
		public uint OffsetToData;
		public uint Size;
		public uint CodePage;
		public uint Reserved;
	}
}

