using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema;

namespace LibreLancer.Data.Schema.InitialWorld;

[ParsedSection]
public partial class FlGroup
{
    [Entry("nickname", Required = true)] public string Nickname = null!;
    [Entry("ids_name")] public int IdsName;
    [Entry("ids_info")] public int IdsInfo;
    [Entry("ids_short_name")] public int IdsShortName;

    public List<GroupReputation> Rep = [];

    [EntryHandler("rep", MinComponents = 2, Multiline = true)]
    private void HandleRep(Entry e) =>
        Rep.Add(new GroupReputation(e[0].ToSingle(), e[1].ToString()));
}
