// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Collections.Generic;
namespace LibreLancer.Utf
{
    public class StringBlock
    {
        Dictionary<int,string> strings = new Dictionary<int, string>();
        private string stringBlock;
        public StringBlock(string block)
        {
            stringBlock = block;
        }
        public string GetString(int nameOffset)
        {
            string str;
            if (!strings.TryGetValue(nameOffset, out str))
            {
                str = stringBlock.Substring(nameOffset, stringBlock.IndexOf('\0', nameOffset) - nameOffset);
                strings.Add(nameOffset, str);
            }
            return str;
        }
    }
}