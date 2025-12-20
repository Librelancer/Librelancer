using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Save;

[ParsedSection]
public partial class MissionDebugState
{
    [Entry("MissionStateNum")]
    public int MissionStateNum;
}
