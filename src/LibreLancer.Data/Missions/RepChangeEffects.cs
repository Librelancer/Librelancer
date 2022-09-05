using System.Collections.Generic;
using LibreLancer.Ini;

namespace LibreLancer.Data.Missions
{
    public class RepChangeEffects : ICustomEntryHandler
    {
        [Entry("group", Required = true)]
        public string Group;

        public List<EmpathyEvent> Events = new List<EmpathyEvent>();
        public List<GroupReputation> EmpathyRate = new List<GroupReputation>();
        
        private static readonly CustomEntry[] _custom = new CustomEntry[]
        {
            new("empathy_rate", (h, e) =>
            {
                if (e.Count < 2)
                    FLLog.Warning("Ini", $"Invalid empathy_rate entry at {e.File}:{e.Line}");
                else
                    ((RepChangeEffects)h).EmpathyRate.Add(new GroupReputation(e[1].ToSingle(), e[0].ToString()));
            }),
            new("event", (h, e) =>
            {
                if (e.Count < 2)
                    FLLog.Warning("Ini", $"Invalid event entry at {e.File}:{e.Line}");
                else
                    ((RepChangeEffects) h).Events.Add(new EmpathyEvent(e));
            })
        };

        IEnumerable<CustomEntry> ICustomEntryHandler.CustomEntries => _custom;
    }
}