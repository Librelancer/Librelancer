// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using LibreLancer.Interface;

namespace LibreLancer.Net.Protocol
{
    [WattleScript.Interpreter.WattleScriptUserData]
	public class CharacterSelectInfo : ITableData
	{
        public string ServerName;
        public string ServerDescription;
		public string ServerNews;
        public List<SelectableCharacter> Characters;
        public int Count => Characters.Count;
        public int Selected { get; set; } = -1;
        public string GetContentString(int row, string column)
        {
            if (row >= Count || row < 0) return null;
            switch (column.ToLowerInvariant())
            {
                case "name":
                    return Characters[row].Name;
                case "rank":
                    return Characters[row].Rank.ToString();
                case "funds":
                    return Characters[row].Funds.ToString();
                case "ship":
                    return Characters[row].Ship;
                case "location":
                    return Characters[row].Location;
                default:
                    return null;
            }
        }
        
        public bool ValidSelection()
        {
            return (Selected >= 0 && Selected < Count);
        }
    }
    [WattleScript.Interpreter.WattleScriptUserData]
    public class SelectableCharacter
    {
        public string Name;
        public int Rank;
        public long Funds;
        public string Ship;
        public string Location;
        public long Id; //Serverside only
    }
}
