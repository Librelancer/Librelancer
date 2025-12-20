using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Universe;

[ParsedSection]
public partial class SystemPreloads
{
    [Entry("ship", Multiline = true)]
    public List<string> ArchetypeShip = new List<string>();
    [Entry("simple", Multiline =  true)]
    public List<string> ArchetypeSimple = new List<string>();
    [Entry("solar", Multiline = true)]
    public List<string> ArchetypeSolar = new List<string>();
    [Entry("equipment", Multiline = true)]
    public List<string> ArchetypeEquipment = new List<string>();
    [Entry("snd", Multiline = true)]
    public List<string> ArchetypeSnd = new List<string>();
    [Entry("voice", Multiline = true)]
    public List<string[]> ArchetypeVoice = new List<string[]>();
}
