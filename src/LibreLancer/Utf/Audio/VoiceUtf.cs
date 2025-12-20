// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using LibreLancer.Data;

namespace LibreLancer.Utf.Audio
{
    public class VoiceUtf : UtfFile
    {
        public Dictionary<uint, byte[]> AudioFiles = new Dictionary<uint, byte[]>();
        public VoiceUtf(string path, Stream stream)
        {
            foreach(var child in parseFile(path, stream))
            {
                if(!(child is LeafNode))
                {
                    FLLog.Error("Utf", "Invalid audio intermediate node in " + path);
                    continue;
                }
                var leaf = (LeafNode)child;
                uint hash;
                if(!child.Name.StartsWith("0x",StringComparison.OrdinalIgnoreCase))
                {
                    hash = FLHash.CreateID(child.Name);
                }
                else
                {
                    hash = uint.Parse(child.Name.Substring(2), NumberStyles.HexNumber);
                }
                AudioFiles.Add(hash, leaf.ByteArrayData);
            }
        }
    }
}
