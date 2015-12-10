using System;
using System.Runtime.InteropServices;
namespace LibreLancer.Dll.Structs
{
	[StructLayout(LayoutKind.Sequential)]
	struct IMAGE_RESOURCE_DIRECTORY
	{
		public uint Characteristics;
		public uint TimeDateStamp;
		public ushort MajorVersion;
		public ushort MinorVersion;
		public ushort NumberOfNamedEntries;
		public ushort NumberOfIdEntries;
	}
}

