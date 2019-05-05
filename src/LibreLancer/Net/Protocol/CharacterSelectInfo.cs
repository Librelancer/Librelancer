// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
namespace LibreLancer
{
	public class CharacterSelectInfo
	{
        public string ServerName;
        public string ServerDescription;
		public string ServerNews;
        public List<SelectableCharacter> Characters;
	}
    public class SelectableCharacter
    {
        public string Name;
        public int Rank;
        public long Funds;
        public string Ship;
        public string Location;
    }
}
