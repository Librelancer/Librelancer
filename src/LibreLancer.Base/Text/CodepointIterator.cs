// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer
{
	//Helper class for Font. Iterates over UTF32 codepoints in a string - shouldn't generate garbage
	struct CodepointIterator
	{
		//Backing String
		public string str;
		//index within the UTF16 string
		int strIndex;
		//index within the UTF32 codepoints
		public int Index;
		public int Count;
		public uint Codepoint;
		//Have a peek at the next codepoint
		public uint PeekNext()
		{
			if (Index >= Count - 1)
				return 0;
			return (uint)char.ConvertToUtf32 (str, strIndex);
		}
		//Move to the next codepoint in the string
		public bool Iterate()
		{
			if (Index >= Count)
				return false;
			Codepoint = (uint)char.ConvertToUtf32 (str, strIndex);
			if (char.IsHighSurrogate (str, strIndex))
				strIndex++;
			strIndex++;
			Index++;
			return true;
		}
		//Create an iterator
		public CodepointIterator(string str)
		{
			this.str = str;
			//Count the amount of UTF32 codepoints
			Count = 0;
			for(int i = 0; i < str.Length; ++i)
			{
				Count++;
				if(char.IsHighSurrogate(str, i))
					i++;
			}
			//C# complains if we don't set these fields
			Index = 0;
			Codepoint = 0;
			strIndex = 0;
		}
        public CodepointIterator(string str, int start, int end)
        {
            this.str = str;
            //Count the amount of UTF32 codepoints
            Count = 0;
            for (int i = start; i < end; ++i)
            {
                Count++;
                if (char.IsHighSurrogate(str, i))
                    i++;
            }
            //C# complains if we don't set these fields
            Index = 0;
            Codepoint = 0;
            strIndex = start;
        }
    }
}

