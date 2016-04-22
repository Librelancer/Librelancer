/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
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
	}
}

