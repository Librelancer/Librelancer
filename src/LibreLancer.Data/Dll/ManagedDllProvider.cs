// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;

namespace LibreLancer.Dll
{
    class ManagedDllProvider
    {
        [StructLayout(LayoutKind.Sequential)]
        struct IMAGE_RESOURCE_DIRECTORY //Size: 16
        {
            public uint Characteristics;
            public uint TimeDateStamp;
            public ushort MajorVersion;
            public ushort MinorVersion;
            public ushort NumberOfNamedEntries;
            public ushort NumberOfIdEntries;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)] //Size: 8
        struct IMAGE_RESOURCE_DIRECTORY_ENTRY
        {
            public uint Name;
            public uint OffsetToData; //Relative to rsrc
        }
        [StructLayout(LayoutKind.Sequential)]
        struct IMAGE_RESOURCE_DATA_ENTRY
        {
            public uint OffsetToData; //Relative to start of dll
            public uint Size;
            public uint CodePage;
            public uint Reserved;
        }

        class ResourceTable
        {
            public uint Type;
            public List<Resource> Resources = new List<Resource>();
        }
        class Resource
        {
            public uint Name;
            public List<ResourceData> Locales = new List<ResourceData>();
        }

        class ResourceData
        {
            public uint Locale;
            public ArraySegment<byte> Data;
        }
        
        const uint RT_RCDATA = 23;
        const uint RT_STRING = 6;
        const uint IMAGE_RESOURCE_NAME_IS_STRING = 0x80000000;
        const uint IMAGE_RESOURCE_DATA_IS_DIRECTORY = 0x80000000;

        public Dictionary<int, string> Strings = new Dictionary<int, string>();
        public Dictionary<int, string> Infocards = new Dictionary<int, string>();
        
        public ManagedDllProvider(Stream stream, string name)
        {
            var (rsrcOffset, rsrc) = GetRsrcSection(stream);
            var directory = Struct<IMAGE_RESOURCE_DIRECTORY>(rsrc, 0);
            List<ResourceTable> resources = new List<ResourceTable>();
            for (int i = 0; i < directory.NumberOfNamedEntries + directory.NumberOfIdEntries; i++)
            {
                var off = 16 + (i * 8);
                var entry = Struct <IMAGE_RESOURCE_DIRECTORY_ENTRY>(rsrc, off);
                if ((IMAGE_RESOURCE_NAME_IS_STRING & entry.Name) == IMAGE_RESOURCE_NAME_IS_STRING) continue;
                resources.Add(ReadResourceTable(rsrcOffset, DirOffset(entry.OffsetToData), rsrc, entry.Name));
            }

            foreach (var table in resources) {
                if (table.Type == RT_RCDATA)
                {
                    foreach (var res in table.Resources)
                    {
                        int idx = res.Locales[0].Data.Offset;
                        int count = res.Locales[0].Data.Count;
                        if (res.Locales[0].Data.Count > 2)
                        {
                            if (res.Locales[0].Data[0] == 0xFF && res.Locales[0].Data[1] == 0xFE)
                            {
                                //skip BOM
                                idx += 2;
                                count -= 2;
                            }
                        }
                        try {
                            Infocards.Add ((int)res.Name, Encoding.Unicode.GetString(res.Locales[0].Data.Array,idx,count));
                        } catch (Exception) {
                            FLLog.Error ("Infocards", $"{name}: Infocard Corrupt: {res.Name}");
                        }
                    }
                }
                else if (table.Type == RT_STRING)
                {
                    foreach (var res in table.Resources)
                    {
                        int blockId = (int)((res.Name - 1u) * 16);
                        var seg = res.Locales[0].Data;
                        using (var reader = new BinaryReader(new MemoryStream(seg.Array, seg.Offset, seg.Count)))
                        {
                            for (int j = 0; j < 16; j++)
                            {
                                int length = (int) (reader.ReadUInt16() * 2);
                                if (length != 0)
                                {
                                    byte[] bytes = reader.ReadBytes(length);
                                    string str = Encoding.Unicode.GetString(bytes);
                                    Strings.Add(blockId + j, str);
                                }
                            }
                        }
                    }
                }
            }
        }

        static int DirOffset(uint a) => (int)(a & 0x7FFFFFFF);

        static ResourceTable ReadResourceTable(int rsrcOffset, int offset, byte[] rsrc, uint type)
        {
            var directory = Struct<IMAGE_RESOURCE_DIRECTORY>(rsrc, offset);
            bool hasLanguage = false;
            var table = new ResourceTable() {Type = type};
            for (int i = 0; i < directory.NumberOfNamedEntries + directory.NumberOfIdEntries; i++)
            {
                var off = offset + 16 + (i * 8);
                var entry = Struct<IMAGE_RESOURCE_DIRECTORY_ENTRY>(rsrc, off);
                var res = new Resource() { Name = entry.Name };
                if ((IMAGE_RESOURCE_DATA_IS_DIRECTORY & entry.OffsetToData) == IMAGE_RESOURCE_DATA_IS_DIRECTORY)
                {
                    var langDirectory = Struct<IMAGE_RESOURCE_DIRECTORY>(rsrc, DirOffset(entry.OffsetToData));
                    for (int j = 0; j < langDirectory.NumberOfIdEntries + langDirectory.NumberOfNamedEntries; j++)
                    {
                        var langOff = DirOffset(entry.OffsetToData) + 16 + (j * 8);
                        var langEntry = Struct<IMAGE_RESOURCE_DIRECTORY_ENTRY>(rsrc, langOff);
                        if((IMAGE_RESOURCE_DATA_IS_DIRECTORY & langEntry.OffsetToData) == IMAGE_RESOURCE_DATA_IS_DIRECTORY)
                            throw new Exception("Malformed .rsrc section");
                        var dataEntry = Struct<IMAGE_RESOURCE_DATA_ENTRY>(rsrc, (int) langEntry.OffsetToData);
                        var dat = new ArraySegment<byte>(rsrc, (int)dataEntry.OffsetToData - rsrcOffset, (int)dataEntry.Size);
                        res.Locales.Add(new ResourceData() {Locale = langEntry.Name, Data = dat});
                    }
                }
                else
                    throw new Exception("Malformed .rsrc section");
                table.Resources.Add(res);
            }
            return table;
        }

        static T Struct<T>(byte[] bytes, int offset) where T : unmanaged
        {
            var sz = Marshal.SizeOf(typeof(T));
            if(offset + sz >= bytes.Length || offset < 0)
                throw new IndexOutOfRangeException();
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                return (T) Marshal.PtrToStructure(handle.AddrOfPinnedObject() + offset, typeof(T));
            }
            finally {
                handle.Free();
            }
        }
        
        static (int, byte[]) GetRsrcSection(Stream stream)
        {
            using (var pe = new PEReader(stream))
            {
                var section = pe.GetSectionData(".rsrc");
                int offset = 0;
                for (int i = 0; i < pe.PEHeaders.SectionHeaders.Length; i++)
                {
                    var h = pe.PEHeaders.SectionHeaders[i];
                    if (h.Name == ".rsrc") {
                        offset = h.VirtualAddress;
                    }
                }
                var array = new byte[section.Length];
                section.GetContent().CopyTo(array);
                return (offset, array);
            }
        }
    }
}
