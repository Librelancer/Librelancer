using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Universe;

[ParsedSection]
public partial class SystemPreloads
{
    [Entry("ship", Multiline = true)]
    public List<string> ArchetypeShip = [];
    [Entry("simple", Multiline =  true)]
    public List<string> ArchetypeSimple = [];
    [Entry("solar", Multiline = true)]
    public List<string> ArchetypeSolar = [];
    [Entry("equipment", Multiline = true)]
    public List<string> ArchetypeEquipment = [];
    [Entry("snd", Multiline = true)]
    public List<string> ArchetypeSnd = [];
    [Entry("voice", Multiline = true)]
    public List<string[]> ArchetypeVoice = [];
}
