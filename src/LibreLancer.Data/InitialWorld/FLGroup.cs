using System.Collections.Generic;
using LibreLancer.Ini;

namespace LibreLancer.Data.InitialWorld
{
    public class FlGroup : ICustomEntryHandler
    {
        [Entry("nickname", Required = true)] public string Nickname;
        [Entry("ids_name")] public int IdsName;
        [Entry("ids_info")] public int IdsInfo;
        [Entry("ids_short_name")] public int IdsShortName;

        public List<GroupReputation> Rep = new List<GroupReputation>();
        
        private static readonly CustomEntry[] _custom = new CustomEntry[]
        {
            new("rep", (h, e) =>
            {
                if (e.Count < 2)
                    FLLog.Warning("InitialWorld", $"Invalid rep entry at {e.File}:{e.Line}");
                else
                    ((FlGroup)h).Rep.Add(new GroupReputation(e[0].ToSingle(), e[1].ToString()));
            })
        };

        IEnumerable<CustomEntry> ICustomEntryHandler.CustomEntries => _custom;
    }

    public struct GroupReputation
    {
        public float Rep;
        public string Name;
        public GroupReputation(float rep, string name)
        {
            Rep = rep;
            Name = name;
        }
        
    }
    
}