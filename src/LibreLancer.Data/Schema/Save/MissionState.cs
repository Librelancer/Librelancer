using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Save;

public record struct MissionRtc(string Script, bool Repeatable);
public record struct MissionAmbient(string Script, HashValue Room, HashValue Base);

[ParsedSection]
public partial class MissionState : IWriteSection
{
    [Entry("mission_accepted")] public int MissionAccepted;
    [Entry("att_clamp")] public bool AttClamp;
    [Entry("tradelane_attacks")] public bool TradelaneAttacks;
    [Entry("scan_clamp")] public bool ScanClamp;
    [Entry("gcs_clamp")] public bool GcsClamp;
    [Entry("hostile_clamp")] public bool HostileClamp;
    [Entry("random_pop")] public bool RandomPop;
    [Entry("msn_offer")] public int MsnOffer;
    [Entry("msn_title")] public int MsnTitle;
    public List<MissionRtc> Rtcs = new List<MissionRtc>();
    public List<MissionAmbient> Ambients = new List<MissionAmbient>();
    [Entry("story_cue")] public int StoryCue;

    [EntryHandler("rtc", Multiline = true, MinComponents = 2)]
    void HandleRtc(Entry e) =>
        Rtcs.Add(new(e[0].ToString(), e[1].ToBoolean()));

    [EntryHandler("ambi_scene", Multiline = true, MinComponents = 3)]
    void HandleAmbiScene(Entry e) =>
        Ambients.Add(new(e[0].ToString(), new HashValue(e[1]), new HashValue(e[2])));

    public void WriteTo(IniBuilder builder)
    {
        var s = builder.Section("MissionState")
            .Entry("mission_accepted", MissionAccepted) //int
            .Entry("att_clamp", AttClamp)
            .Entry("tradelane_attacks", TradelaneAttacks)
            .Entry("scan_clamp", ScanClamp)
            .Entry("gcs_clamp", GcsClamp)
            .Entry("hostile_clamp", HostileClamp)
            .Entry("random_pop", RandomPop)
            .Entry("msn_offer", MsnOffer)
            .Entry("msn_title", MsnTitle);
        foreach (var r in Rtcs)
            s.Entry("rtc", r.Script, r.Repeatable);
        foreach (var a in Ambients)
            s.Entry("ambi_scene", a.Script, (uint)a.Room, (uint)a.Base);
        s.Entry("story_cue", StoryCue);
    }
}
