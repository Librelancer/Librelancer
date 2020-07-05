// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Collections.Generic;
using System.Text;

namespace LibreLancer.Utf
{
    public class StringBlock
    {
        Dictionary<int,string> strings = new Dictionary<int, string>();
        private byte[] stringBlock;
        private string asciiSource;
        private bool lenPrefixed;
        
        public StringBlock(byte[] block, bool lenPrefixed)
        {
            stringBlock = block;
            this.lenPrefixed = lenPrefixed;
            if (!lenPrefixed) asciiSource = Encoding.ASCII.GetString(block);
        }
        public string GetString(int nameOffset)
        {
            string str;
            if (!strings.TryGetValue(nameOffset, out str))
            {
                if (lenPrefixed)
                {
                    var length = BitConverter.ToUInt16(stringBlock, nameOffset);
                    str = Encoding.UTF8.GetString(stringBlock, nameOffset + 2, length);
                    strings.Add(nameOffset, str);
                }
                else
                {
                    str = asciiSource.Substring(nameOffset, asciiSource.IndexOf('\0', nameOffset) - nameOffset);
                    strings.Add(nameOffset, str);
                }
            }
            return str;
        }
    }
}