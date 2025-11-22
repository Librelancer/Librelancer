// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;

namespace LibreLancer.Data.Ini
{
    //Avoid repeated allocations in BINI loading
    public class BiniStringBlock
    {
        private string block;
        Dictionary<int,string> strings = new Dictionary<int, string>();
        private IniStringPool stringPool = null;

        public BiniStringBlock(string block, IniStringPool stringPool = null)
        {
            this.block = block;
            this.stringPool = stringPool;
        }

        public string Get(int strOffset)
        {
            if (!strings.TryGetValue(strOffset, out string s))
            {
                if (stringPool != null)
                {
                    s = stringPool.FromSpan(block.AsSpan(strOffset, block.IndexOf('\0', strOffset) - strOffset));
                }
                else
                {
                    s = block.Substring(strOffset, block.IndexOf('\0', strOffset) - strOffset);
                }
                strings.Add(strOffset, s);
            }
            return s;
        }
    }
}
