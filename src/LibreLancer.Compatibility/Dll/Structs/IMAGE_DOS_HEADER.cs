// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;
namespace LibreLancer.Dll.Structs
{
	[StructLayout(LayoutKind.Sequential)]
	struct IMAGE_DOS_HEADER
	{
		public ushort e_magic;              // Magic number **IMPORTANT**
		public ushort e_cblp;               // Bytes on last page of file
		public ushort e_cp;                 // Pages in file
		public ushort e_crlc;               // Relocations
		public ushort e_cparhdr;            // Size of header in paragraphs
		public ushort e_minalloc;           // Minimum extra paragraphs needed
		public ushort e_maxalloc;           // Maximum extra paragraphs needed
		public ushort e_ss;                 // Initial (relative) SS value
		public ushort e_sp;                 // Initial SP value
		public ushort e_csum;               // Checksum
		public ushort e_ip;                 // Initial IP value
		public ushort e_cs;                 // Initial (relative) CS value
		public ushort e_lfarlc;             // File address of relocation table
		public ushort e_ovno;               // Overlay number
		public ushort e_res_0;              // Reserved 
		public ushort e_res_1;              // Reserved 
		public ushort e_res_2;              // Reserved 
		public ushort e_res_3;              // Reserved 
		public ushort e_oemid;              // OEM identifier (for e_oeminfo)
		public ushort e_oeminfo;            // OEM information; e_oemid specific
		public ushort e_res2_0;             // Reserved 
		public ushort e_res2_1;             // Reserved 
		public ushort e_res2_2;             // Reserved 
		public ushort e_res2_3;             // Reserved 
		public ushort e_res2_4;             // Reserved 
		public ushort e_res2_5;             // Reserved 
		public ushort e_res2_6;             // Reserved 
		public ushort e_res2_7;             // Reserved 
		public ushort e_res2_8;             // Reserved 
		public ushort e_res2_9;             // Reserved 
		public uint e_lfanew;               // File address of the PE header **IMPORTANT**
	}
}

