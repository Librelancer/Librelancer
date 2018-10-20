// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

//beware! ugly code beyond here
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using LibreLancer.Dll.Structs;
namespace LibreLancer.Dll
{
	class ManagedDllProvider
	{
		private const uint RT_RCDATA = 23;
		private const uint RT_STRING = 6;
		private const ushort IMAGE_FILE_32BIT_MACHINE = 256;

		public Dictionary<int, string> Strings;
		public Dictionary<int, string> Infocards;

		public ManagedDllProvider (Stream stream)
		{
            using (BinaryReader binaryReader = new BinaryReader(stream))
            {
                var dosHeader = binaryReader.ReadStruct<IMAGE_DOS_HEADER>();
                binaryReader.BaseStream.Seek(dosHeader.e_lfanew, SeekOrigin.Begin);
                byte[] array = binaryReader.ReadBytes(4);
                if (Encoding.ASCII.GetString(array) != "PE\0\0")
                {
                    throw new Exception("Not a PE File");
                }
                var fileHeader = binaryReader.ReadStruct<IMAGE_FILE_HEADER>();
                if ((IMAGE_FILE_32BIT_MACHINE & fileHeader.Characteristics) != IMAGE_FILE_32BIT_MACHINE)
                {
                    throw new Exception("Not a 32-bit PE File");
                }
                binaryReader.ReadStruct<IMAGE_OPTIONAL_HEADER>();
                var sectionHeaders = new IMAGE_SECTION_HEADER[(int)fileHeader.NumberOfSections];
                IMAGE_SECTION_HEADER? rsrcSection = default(IMAGE_SECTION_HEADER?);
                for (int i = 0; i < (int)fileHeader.NumberOfSections; i++)
                {
                    sectionHeaders[i] = binaryReader.ReadStruct<IMAGE_SECTION_HEADER>();
                    if (sectionHeaders[i].Section == ".rsrc")
                    {
                        rsrcSection = new IMAGE_SECTION_HEADER?(sectionHeaders[i]);
                    }
                }
                if (!rsrcSection.HasValue)
                {
                    throw new Exception("No resources");
                }
                binaryReader.BaseStream.Seek((long)((ulong)rsrcSection.Value.PointerToRawData), SeekOrigin.Begin);
                var rootDirectory = binaryReader.ReadStruct<IMAGE_RESOURCE_DIRECTORY>();
                int num = (int)(rootDirectory.NumberOfIdEntries + rootDirectory.NumberOfNamedEntries);
                var entries = new IMAGE_RESOURCE_DIRECTORY_ENTRY[num];
                for (int j = 0; j < num; j++)
                {
                    entries[j] = binaryReader.ReadStruct<IMAGE_RESOURCE_DIRECTORY_ENTRY>();
                }
                Strings = new Dictionary<int, string>();
                Infocards = new Dictionary<int, string>();
                for (int k = 0; k < num; k++)
                {
                    if (entries[k].Name == RT_STRING)
                    {
                        ReadStringTable(binaryReader, entries[k].OffsetToData, rsrcSection.Value.PointerToRawData);
                    }
                    if (entries[k].Name == RT_RCDATA)
                    {
                        ReadXML(binaryReader, entries[k].OffsetToData, rsrcSection.Value.PointerToRawData);
                    }
                }
                binaryReader.Close();
            }
		}

		private void ReadStringTable (BinaryReader reader, uint offset, uint rsrcStart)
		{
			reader.BaseStream.Seek ((long)((offset & 0x7FFFFFFF) + rsrcStart), SeekOrigin.Begin);
			IMAGE_RESOURCE_DIRECTORY rootDir = reader.ReadStruct<IMAGE_RESOURCE_DIRECTORY> ();
			int num = (int)(rootDir.NumberOfIdEntries + rootDir.NumberOfNamedEntries);
			IMAGE_RESOURCE_DIRECTORY_ENTRY[] entries = new IMAGE_RESOURCE_DIRECTORY_ENTRY[num];
			for (int i = 0; i < num; i++)
			{
				entries [i] = reader.ReadStruct<IMAGE_RESOURCE_DIRECTORY_ENTRY> ();
			}
			for (int i = 0; i < num; i++)
			{
				uint dirOffset = entries [i].OffsetToData & 0x7FFFFFFF;
				reader.BaseStream.Seek ((long)(dirOffset + rsrcStart), SeekOrigin.Begin);
				reader.ReadStruct<IMAGE_RESOURCE_DIRECTORY> ();
				IMAGE_RESOURCE_DIRECTORY_ENTRY languageEntry = reader.ReadStruct<IMAGE_RESOURCE_DIRECTORY_ENTRY> ();
				reader.BaseStream.Seek ((long)(languageEntry.OffsetToData + rsrcStart), SeekOrigin.Begin);
				IMAGE_RESOURCE_DATA_ENTRY dataEntry = reader.ReadStruct<IMAGE_RESOURCE_DATA_ENTRY> ();
				reader.BaseStream.Seek ((long)dataEntry.OffsetToData, SeekOrigin.Begin);
				int blockId = (int)((entries [i].Name - 1u) * 16);

				for (int j = 0; j < 16; j++)
				{
					int length = (int)(reader.ReadUInt16 () * 2);
					if (length != 0)
					{
						byte[] bytes = reader.ReadBytes (length);
						string str = Encoding.Unicode.GetString (bytes);
						Strings.Add (blockId + j, str);
					}
				}
			}
		}

		private void ReadXML (BinaryReader reader, uint offset, uint rsrcStart)
		{
			reader.BaseStream.Seek ((long)((offset & 0x7FFFFFFF) + rsrcStart), SeekOrigin.Begin);
			IMAGE_RESOURCE_DIRECTORY rootDir = reader.ReadStruct<IMAGE_RESOURCE_DIRECTORY> ();
			int num = (int)(rootDir.NumberOfIdEntries + rootDir.NumberOfNamedEntries);
			IMAGE_RESOURCE_DIRECTORY_ENTRY[] entries = new IMAGE_RESOURCE_DIRECTORY_ENTRY[num];
			for (int i = 0; i < num; i++)
			{
				entries [i] = reader.ReadStruct<IMAGE_RESOURCE_DIRECTORY_ENTRY> ();
			}
			for (int j = 0; j < num; j++)
			{
				uint dirOffset = entries [j].OffsetToData & 0x7FFFFFFF;
				reader.BaseStream.Seek ((long)(dirOffset + rsrcStart), SeekOrigin.Begin);
				reader.ReadStruct<IMAGE_RESOURCE_DIRECTORY> ();
				IMAGE_RESOURCE_DIRECTORY_ENTRY languageEntry = reader.ReadStruct<IMAGE_RESOURCE_DIRECTORY_ENTRY> ();
				reader.BaseStream.Seek ((long)(languageEntry.OffsetToData + rsrcStart), SeekOrigin.Begin);
				IMAGE_RESOURCE_DATA_ENTRY dataEntry = reader.ReadStruct<IMAGE_RESOURCE_DATA_ENTRY> ();
				reader.BaseStream.Seek ((long)dataEntry.OffsetToData, SeekOrigin.Begin);
				byte[] xmlBytes = reader.ReadBytes ((int)dataEntry.Size);
				int idx = 0;
				int count = xmlBytes.Length;
				if (xmlBytes[0] == 0xFF && xmlBytes[1] == 0xFE) //Skip BOM
				{
					idx = 2;
					count -= 2;
				}
				try {
					Infocards.Add ((int)entries [j].Name, Encoding.Unicode.GetString(xmlBytes,idx,count));
				} catch (Exception) {
					FLLog.Error ("Infocards", "Infocard Corrupt: " + entries[j].Name);
				}

			}
		}

		public string GetXml (ushort resourceId)
		{

			try {
				return Infocards[(int)resourceId];
			} catch (Exception) {
				FLLog.Warning ("Infocards","Not Found: " + resourceId);
				return null;
			}

		}

		public string GetString (ushort resourceId)
		{
			try {
				return Strings[(int)resourceId];
			} catch (Exception) {
				FLLog.Warning ("Infocards","Not Found: " + resourceId);
				return "";
			}
		}
	}
}
