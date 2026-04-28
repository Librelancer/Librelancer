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
        private Dictionary<int,string> strings = new();
        private byte[] stringBlock;

        public StringBlock(byte[] block)
        {
            stringBlock = block;
        }
        public string GetString(int nameOffset)
        {
            if (!strings.TryGetValue(nameOffset, out var str))
            {
                var len = stringBlock.AsSpan(nameOffset).IndexOf((byte)0);
                str = Encoding.ASCII.GetString(stringBlock, nameOffset, len);
                strings.Add(nameOffset, str);
            }
            return str;
        }
    }
}
