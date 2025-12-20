using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LibreLancer.Data.Dll;

namespace LibreLancer.ContentEdit;

public static class DllWriter
{
    // blank dll template
    private const string PE_TEMPLATE =
        "TVqQAAMAAAAEAAAA//8AALgAAAAAAAAAQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAsAAAAA4fug4AtAnNIbgBTM0hVGhpcyBwcm9ncmFtIGNhbm5vdCBiZSBydW4gaW4gRE9TIG1v" +
        "ZGUuDQ0KJAAAAAAAAADrIDXbr0FbiK9BW4ivQVuIaEddiK5BW4hSaWNor0FbiAAAAAAAAAAAAAAA" +
        "AAAAAABQRQAATAEBAC9np08AAAAAAAAAAOAADiELAQYAAAAAAAACAAAAAAAAAAAAAAAQAAAAEAAA" +
        "AAAOBwAQAAAAAgAABAAAAAAAAAAEAAAAAAAAAAAgAAAAAgAAAAAAAAIAAAAAABAAABAAAAAAEAAA" +
        "EAAAAAAAABAAAAAAAAAAAAAAAAAAAAAAAAAAABAAABAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAC5yc3JjAAAAEAAAAAAQAAAAAgAAAAIAAAAAAAAAAAAA" +
        "AAAAAEAAAEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";


    // Version Info resource so we always have something valid to put into the rsrc section
    private const string VERSION_INFO =
        "FAE0AAAAVgBTAF8AVgBFAFIAUwBJAE8ATgBfAEkATgBGAE8AAAAAAL0E7/4AAAEAAAABAAAAAAAA" +
        "AAEAAAAAAD8AAAAAAAAABAAEAAEAAAAAAAAAAAAAAAAAAAByAAAAAQBTAHQAcgBpAG4AZwBGAGkA" +
        "bABlAEkAbgBmAG8AAABOAAAAAQAwADQAMAA5ADAANABCADAAAAA2AAkAAQBQAHIAbwBkAHUAYwB0" +
        "AFYAZQByAHMAaQBvAG4AAAB2ADEALgAwAC4AMAAuADAAAAAAAEQAAAABAFYAYQByAEYAaQBsAGUA" +
        "SQBuAGYAbwAAAAAAJAAEAAAAVAByAGEAbgBzAGwAYQB0AGkAbwBuAAAAAAAJBLAE";

    private const int RSRC_VIRTUAL_OFFSET = 0x1000;

    static int Align(int value, int alignment) => (value + (alignment - 1)) & ~(alignment - 1);

    private const uint RT_MENU = 4;
    private const uint RT_DIALOG = 5;
    private const uint RT_STRING = 6;
    private const uint RT_VERSION = 16;
    private const uint RT_RCDATA = 23;

    static uint DirOffset(uint value) => value | 0x80000000;

    public static unsafe void Write(ResourceDll resourceDll, Stream outfile)
    {
        bool hasStrings = resourceDll.Strings.Count > 0;
        bool hasInfocards = resourceDll.Infocards.Count > 0;
        bool hasDialogs = resourceDll.Dialogs.Count > 0;
        bool hasMenus = resourceDll.Menus.Count > 0;

        using var mem = new MemoryStream();
        //Set up PE template
        var writer = new BinaryWriter(mem);
        void PatchUint(int offset, uint value)
        {
            var saved = mem.Position;
            mem.Seek(offset, SeekOrigin.Begin);
            writer.Write(value);
            mem.Seek(saved, SeekOrigin.Begin);
        }
        writer.Write(Convert.FromBase64String(PE_TEMPLATE));
        //Write .rsrc section
        void ResourceDirectory(int entryCount)
        {
            writer.Write((uint)0); //characteristics
            writer.Write((uint)0); //timedatestamp
            writer.Write((ushort)4); //majorversion
            writer.Write((ushort)0); //minorversion
            writer.Write((ushort)0); //numberOfNamedEntries
            writer.Write((ushort)entryCount); //numberOfIdEntries
        }
        //root directory: IMAGE_RESOURCE_DIRECTORY, size: 16
        int typeCount = 1;
        if (hasStrings) typeCount++;
        if (hasInfocards) typeCount++;
        if (hasDialogs) typeCount++;
        if (hasMenus) typeCount++;
        ResourceDirectory(typeCount);
        int infocardWriteOffset = 0;
        int stringsWriteOffset = 0;
        int dialogsWriteOffset = 0;
        int menusWriteOffset = 0;
        // FindResourceW expects these entries to be ordered by ID number
        // API calls will fail otherwise
        //menus entry
        if (hasMenus) {
            writer.Write(RT_MENU);
            menusWriteOffset = (int) mem.Position;
            writer.Write((uint)0);
        }
        //dialogs entry
        if (hasDialogs) {
            writer.Write(RT_DIALOG);
            dialogsWriteOffset = (int) mem.Position;
            writer.Write((uint)0);
        }
        //strings entry
        if (hasStrings) {
            writer.Write(RT_STRING);
            stringsWriteOffset = (int) mem.Position;
            writer.Write((uint)0);
        }
        //version entry
        writer.Write(RT_VERSION);
        writer.Write(DirOffset((uint)(16 + typeCount * 8))); //offsetToData
        var versionData = resourceDll.VersionInfo?.Data ?? Convert.FromBase64String(VERSION_INFO);
        //infocards entry
        if (hasInfocards) {
            writer.Write(RT_RCDATA);
            infocardWriteOffset = (int)mem.Position;
            writer.Write((uint)0);
        }
        // version directory: IMAGE_RESOURCE_DIRECTORY, size: 16
        ResourceDirectory(1);
        //version language directory entry
        writer.Write((uint)1);
        writer.Write(DirOffset((uint)(mem.Position + 4 - 512))); //offsetToData
        //version language directory
        ResourceDirectory(1);
        //version language data entry
        writer.Write((uint)1033); //no language code
        writer.Write((uint)(mem.Position + 4 - 512)); //offset to data
        //write data
        writer.Write((uint)(mem.Position + 16 - 512 + RSRC_VIRTUAL_OFFSET));
        writer.Write((uint)versionData.Length);
        writer.Write((uint)0); //codepage
        writer.Write((uint)0); //reserved
        writer.Write(versionData);
        //strings
        if (hasStrings)
        {
            var maxKey = resourceDll.Strings.Keys.Max();
            int maxBlocks = Align(maxKey + 1, 16) / 16;
            List<int> blocks = new List<int>();
            for (int i = 0; i < maxBlocks; i++) {
                for (int j = 0; j < 16; j++) {
                    var idx = i * 16 + j;
                    if (resourceDll.Strings.TryGetValue(idx, out _)) {
                        blocks.Add(i);
                        break;
                    }
                }
            }
            int[] langDirectoryOffsets = new int[blocks.Count];
            PatchUint(stringsWriteOffset, DirOffset((uint) (mem.Position - 512)));
            //string directory: IMAGE_RESOURCE_DIRECTORY, size: 16
            ResourceDirectory(blocks.Count);
            //lang directory entries
            for (int i = 0; i < blocks.Count; i++) {
                writer.Write((uint)(blocks[i] + 1));
                langDirectoryOffsets[i] = (int) mem.Position;
                writer.Write((uint)0);
            }
            //write tables
            for (int i = 0; i < blocks.Count; i++) {
                PatchUint(langDirectoryOffsets[i], DirOffset((uint) (mem.Position - 512)));
                //string table language directory
                ResourceDirectory(1);
                //string table language data entry
                writer.Write((uint)1033); //english
                writer.Write((uint)(mem.Position + 4 - 512)); //offset to data
                //write data
                writer.Write((uint)(mem.Position + 16 - 512 + RSRC_VIRTUAL_OFFSET));
                int sizeOffset = (int) mem.Position;
                writer.Write((uint)0);
                writer.Write((uint)0); //codepage
                writer.Write((uint)0); //reserved
                int blockBegin = (int) mem.Position;
                //Write table
                var blockIdx = blocks[i];
                for (int j = 0; j < 16; j++)
                {
                    var idx = blockIdx * 16 + j;

                    if(resourceDll.Strings.TryGetValue(idx, out var str) && !string.IsNullOrEmpty(str))
                    {
                        var bytes = Encoding.Unicode.GetBytes(str);
                        if ((bytes.Length / 2) > ushort.MaxValue)
                            throw new Exception($"String {idx} is too big");
                        writer.Write((ushort)(bytes.Length / 2));
                        writer.Write(bytes);
                    }
                    else
                        writer.Write((ushort)0);
                }
                PatchUint(sizeOffset,(uint)(mem.Position - blockBegin));
            }
        }
        //infocards
        if (hasInfocards)
        {
            //Infocards must be written in order of ID or they will not be loaded correctly
            var infocards = resourceDll.Infocards.OrderBy(x => x.Key).ToArray();
            int[] langDirectoryOffsets = new int[infocards.Length];
            PatchUint(infocardWriteOffset, DirOffset((uint) (mem.Position - 512)));
            //string directory: IMAGE_RESOURCE_DIRECTORY, size: 16
            ResourceDirectory(infocards.Length);
            //lang directory entries
            for (int i = 0; i < infocards.Length; i++) {
                writer.Write((uint) (infocards[i].Key));
                langDirectoryOffsets[i] = (int) mem.Position;
                writer.Write((uint)0);
            }
            //write infocards
            for (int i = 0; i < infocards.Length; i++) {
                PatchUint(langDirectoryOffsets[i], DirOffset((uint) (mem.Position - 512)));
                //infocard language directory
                ResourceDirectory(1);
                //infocard language data entry
                writer.Write((uint)1033); //english
                writer.Write((uint)(mem.Position + 4 - 512)); //offset to data
                //write data
                var bytes = Encoding.Unicode.GetBytes(infocards[i].Value);
                writer.Write((uint)(mem.Position + 16 - 512 + RSRC_VIRTUAL_OFFSET));
                writer.Write(bytes.Length + 2);
                writer.Write((uint)0); //codepage
                writer.Write((uint)0); //reserved
                writer.Write((byte)0xFF); //utf-16 bom
                writer.Write((byte)0xFE);
                writer.Write(bytes);
            }
        }
        //dialogs
        if (hasDialogs)
        {
            int[] langDirectoryOffsets = new int[resourceDll.Dialogs.Count];
            PatchUint(dialogsWriteOffset, DirOffset((uint) (mem.Position - 512)));
            //dialogs directory: IMAGE_RESOURCE_DIRECTORY, size: 16
            ResourceDirectory(resourceDll.Dialogs.Count);
            //dialog directory entries
            for (int i = 0; i < resourceDll.Dialogs.Count; i++) {
                writer.Write((uint) (resourceDll.Dialogs[i].Name));
                langDirectoryOffsets[i] = (int) mem.Position;
                writer.Write((uint)0);
            }
            //write dialogs
            for (int i = 0; i < resourceDll.Dialogs.Count; i++) {
                PatchUint(langDirectoryOffsets[i], DirOffset((uint) (mem.Position - 512)));
                ResourceDirectory(1);
                //infocard language data entry
                writer.Write((uint)1033); //english
                writer.Write((uint)(mem.Position + 4 - 512)); //offset to data
                //write data
                writer.Write((uint)(mem.Position + 16 - 512 + RSRC_VIRTUAL_OFFSET));
                writer.Write(resourceDll.Dialogs[i].Data.Length);
                writer.Write((uint)0); //codepage
                writer.Write((uint)0); //reserved
                writer.Write(resourceDll.Dialogs[i].Data);
            }
        }
        //menus
        if (hasMenus)
        {
            int[] langDirectoryOffsets = new int[resourceDll.Menus.Count];
            PatchUint(menusWriteOffset, DirOffset((uint) (mem.Position - 512)));
            //menus directory: IMAGE_RESOURCE_DIRECTORY, size: 16
            ResourceDirectory(resourceDll.Menus.Count);
            //menu directory entries
            for (int i = 0; i < resourceDll.Menus.Count; i++) {
                writer.Write((uint) (resourceDll.Menus[i].Name));
                langDirectoryOffsets[i] = (int) mem.Position;
                writer.Write((uint)0);
            }
            //write menus
            for (int i = 0; i < resourceDll.Menus.Count; i++) {
                PatchUint(langDirectoryOffsets[i], DirOffset((uint) (mem.Position - 512)));
                ResourceDirectory(1);
                //infocard language data entry
                writer.Write((uint)1033); //english
                writer.Write((uint)(mem.Position + 4 - 512)); //offset to data
                //write data
                writer.Write((uint)(mem.Position + 16 - 512 + RSRC_VIRTUAL_OFFSET));
                writer.Write(resourceDll.Menus[i].Data.Length);
                writer.Write((uint)0); //codepage
                writer.Write((uint)0); //reserved
                writer.Write(resourceDll.Menus[i].Data);
            }
        }
        //Pad section to 512 byte alignment
        int rsrcLength = (int) (mem.Position - 512);
        var physAlignRsrc = Align(rsrcLength, 512);
        for(int i = rsrcLength; i < physAlignRsrc; i++)
            writer.Write(0);
        //Patch Created PE file
        PatchUint(0xD0, (uint)Align(rsrcLength, 1024)); //initialized data size
        PatchUint(0x100, (uint)Align(RSRC_VIRTUAL_OFFSET +  rsrcLength, 1024)); //image size (highest address)
        PatchUint(0x1B0, (uint)rsrcLength); //.rsrc actual size
        PatchUint(0x1B8, (uint)physAlignRsrc); //.rsrc physical size
        //Calculate and patch in checksum
        var data = mem.ToArray();
        fixed (byte* bp = data)
        {
            long checksum = 0;
            long top = 0xFFFFFFFF;
            for (int i = 0; i <= data.Length - 4; i += 4)
            {
                if (i == 0x108) continue; //Don't include checksum field in checksum
                checksum = (checksum & 0xffffffff) + *(uint*)(&bp[i]) + (checksum >> 32);
                if (checksum > top)
                    checksum = (checksum & 0xffffffff) + (checksum >> 32);
            }
            checksum = (checksum & 0xffff) + (checksum >> 16);
            checksum = (checksum) + (checksum >> 16);
            checksum = checksum & 0xffff;
            checksum += data.Length;
            *(uint*) (&bp[0x108]) = (uint) checksum;
        }
        //Write to output
        outfile.Write(data);
        outfile.Flush();
    }
}
