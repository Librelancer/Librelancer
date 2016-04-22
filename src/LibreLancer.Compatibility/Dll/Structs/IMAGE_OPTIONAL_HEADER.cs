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
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	struct IMAGE_OPTIONAL_HEADER
	{
		public ushort Magic;
		public byte MajorLinkerVersion;
		public byte MinorLinkerVersion;
		public uint SizeOfCode;
		public uint SizeOfInitializedData;
		public uint SizeOfUninitializedData;
		public uint AddressOfEntryPoint;
		public uint BaseOfCode;
		public uint BaseOfData;
		public uint ImageBase;
		public uint SectionAlignment;
		public uint FileAlignment;
		public ushort MajorOperatingSystemVersion;
		public ushort MinorOperatingSystemVersion;
		public ushort MajorImageVersion;
		public ushort MinorImageVersion;
		public ushort MajorSubsystemVersion;
		public ushort MinorSubsystemVersion;
		public uint Win32VersionValue;
		public uint SizeOfImage;
		public uint SizeOfHeaders;
		public uint CheckSum;
		public ushort Subsystem;
		public ushort DllCharacteristics;
		public uint SizeOfStackReserve;
		public uint SizeOfStackCommit;
		public uint SizeOfHeapReserve;
		public uint SizeOfHeapCommit;
		public uint LoaderFlags;
		public uint NumberOfRvaAndSizes;

		public IMAGE_DATA_DIRECTORY ExportTable;
		public IMAGE_DATA_DIRECTORY ImportTable;
		public IMAGE_DATA_DIRECTORY ResourceTable;
		public IMAGE_DATA_DIRECTORY ExceptionTable;
		public IMAGE_DATA_DIRECTORY CertificateTable;
		public IMAGE_DATA_DIRECTORY BaseRelocationTable;
		public IMAGE_DATA_DIRECTORY Debug;
		public IMAGE_DATA_DIRECTORY Architecture;
		public IMAGE_DATA_DIRECTORY GlobalPtr;
		public IMAGE_DATA_DIRECTORY TLSTable;
		public IMAGE_DATA_DIRECTORY LoadConfigTable;
		public IMAGE_DATA_DIRECTORY BoundImport;
		public IMAGE_DATA_DIRECTORY IAT;
		public IMAGE_DATA_DIRECTORY DelayImportDescriptor;
		public IMAGE_DATA_DIRECTORY CLRRuntimeHeader;
		public IMAGE_DATA_DIRECTORY Reserved;
	}
}

