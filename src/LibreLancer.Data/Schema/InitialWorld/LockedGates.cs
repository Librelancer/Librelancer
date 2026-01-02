using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.InitialWorld;

[ParsedSection]
public partial class LockedGates
{
    [Entry("locked_gate", Multiline = true)]
    public List<int> Locked = [];

    [Entry("npc_locked_gate", Multiline = true)]
    public List<int> NpcLockedGates = [];
}
