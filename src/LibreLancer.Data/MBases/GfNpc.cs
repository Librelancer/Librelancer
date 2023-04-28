// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Ini;
namespace LibreLancer.Data
{
	public class GfNpc : ICustomEntryHandler
	{
        [Entry("nickname")]
		public string Nickname;
        [Entry("base_appr")] 
        public string BaseAppr;
        [Entry("body")]
		public string Body;
        [Entry("head")]
		public string Head;
        [Entry("lefthand")]
		public string LeftHand;
        [Entry("righthand")]
		public string RightHand;
        [Entry("accessory")] 
        public string Accessory;
        [Entry("individual_name")] 
        public int IndividualName;
        [Entry("affiliation")] 
        public string Affiliation;
        [Entry("voice")] 
        public string Voice;
        [Entry("room")] 
        public string Room;

        public List<NpcKnow> Know = new List<NpcKnow>();
        public List<NpcRumor> Rumors = new List<NpcRumor>();
        public List<NpcBribe> Bribes = new List<NpcBribe>();
        public NpcMission Mission;
        
        private static CustomEntry[] entries = {
            new("know", (n,e) => ((GfNpc)n).Know.Add(
                new NpcKnow(e[0].ToInt32(), e[1].ToInt32(), e[2].ToInt32(), e[3].ToInt32())
                )),
            new("rumor", (n, e) => ((GfNpc)n).Rumors.Add(
                    new NpcRumor(e[0].ToString(), e[1].ToString(), e[2].ToInt32(), e[3].ToInt32(), false)
                )),
            new("rumor_type2", (n, e) => ((GfNpc)n).Rumors.Add(
                new NpcRumor(e[0].ToString(), e[1].ToString(), e[2].ToInt32(), e[3].ToInt32(), true)
            )),
            new("bribe", (n, e) => ((GfNpc)n).Bribes.Add(
                new NpcBribe(e[0].ToString(), e[1].ToInt32(), e[2].ToInt32())
            )),
            new ("misn", (n,e ) => ((GfNpc)n).Mission = new NpcMission(e[0].ToString(), e[1].ToSingle(), e[2].ToSingle())),
            new ("rumordb", RumorKnowDb),
            new ("knowdb", KnowDb)
        };

        static void RumorKnowDb(ICustomEntryHandler self, Entry knowdb)
        {
            var gf = (GfNpc) self;
            if (gf.Rumors.Count == 0)
            {
                IniWarning.Warn("rumorknowdb without rumor", knowdb);
            }
            else
            {
                gf.Rumors[^1].Objects = knowdb.Select(x => x.ToString()).ToArray();
            }
        }
        
        static void KnowDb(ICustomEntryHandler self, Entry knowdb)
        {
            var gf = (GfNpc) self;
            if (gf.Know.Count == 0)
            {
                IniWarning.Warn("knowdb without know", knowdb);
            }
            else
            {
                gf.Know[^1].Objects = knowdb.Select(x => x.ToString()).ToArray();
            }
        }
        IEnumerable<CustomEntry> ICustomEntryHandler.CustomEntries => entries;
    }

    public record NpcMission(string Kind, float Min, float Max);

    public class NpcKnow
    {
        public int Ids1;
        public int Ids2;
        public int Price;
        public int Unknown;

        public string[] Objects;
        
        public NpcKnow()
        {
        }

        public NpcKnow(int ids1, int ids2, int price, int unknown)
        {
            Ids1 = ids1;
            Ids2 = ids2;
            Price = price;
            Unknown = unknown;
        }
    }

    public record NpcBribe(string Faction, int Ids1, int Ids2);

    public class NpcRumor
    {
        public string Start;
        public string End;
        public int Unknown;
        public int Ids;

        public bool Type2;

        public string[] Objects;

        public NpcRumor()
        {
        }

        public NpcRumor(string start, string end, int unknown, int ids, bool type2)
        {
            Start = start;
            End = end;
            Unknown = unknown;
            Ids = ids;
        }
    }

}
