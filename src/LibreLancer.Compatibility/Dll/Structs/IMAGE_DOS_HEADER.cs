/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
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

