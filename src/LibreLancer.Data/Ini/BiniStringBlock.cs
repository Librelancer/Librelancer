// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System.Collections.Generic;
namespace LibreLancer.Ini
{
    //Avoid repeated allocations in BINI loading
    public class BiniStringBlock
    {
        private string block;
        Dictionary<int,string> strings = new Dictionary<int, string>();
        public BiniStringBlock(string block)
        {
            this.block = block;
        }

        public string Get(int strOffset)
        {
            if (!strings.TryGetValue(strOffset, out string s))
            {
                s = block.Substring(strOffset, block.IndexOf('\0', strOffset) - strOffset);
                strings.Add(strOffset, s);
            }
            return s;
        }
    }
}