using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Missions
{
    [ParsedSection]
    public partial class RepChangeEffects
    {
        [Entry("group", Required = true)]
        public string Group;

        public List<EmpathyEvent> Events = new List<EmpathyEvent>();
        public List<GroupReputation> EmpathyRate = new List<GroupReputation>();

        [EntryHandler("empathy_rate", MinComponents = 2, Multiline = true)]
        void HandleEmpathyRate(Entry e) => EmpathyRate.Add(new GroupReputation(e[1].ToSingle(), e[0].ToString()));

        [EntryHandler("event", MinComponents = 2, Multiline = true)]
        void HandleEvent(Entry e) => Events.Add(new EmpathyEvent(e));
    }
}
