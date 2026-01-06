// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Save;

[ParsedIni(Preparse = false)]
public partial class SaveGame
{
    [Section("player")]
    public SavePlayer? Player;

    [Section("mplayer")]
    public MPlayer? MPlayer;

    [Section("missionstate")]
    public MissionState? MissionState;

    [Section("TriggerSave")]
    public List<TriggerSave> TriggerSave = [];

    [Section("mission01asave")] //Unknown, matching vanilla
    public MissionDebugState? Mission01aSave;
    [Section("mission01bsave")] //Unknown, matching vanilla
    public MissionDebugState? Mission01bSave;
    [Section("bstorymissiondone")] //Unknown, matching vanilla
    public EmptySection? BStoryMissionDone;

    [Section("storyinfo")]
    public StoryInfo? StoryInfo;

    [Section("time")]
    public SaveTime? Time;

    [Section("group")]
    public List<SaveGroup> Groups = [];

    [Section("locked_gates")]
    public LockedGates? LockedGates;

    [Section("nnobjective")]
    public List<SavedObjective> Objectives = [];

    public List<Section> ToIni()
    {
        var builder = new IniBuilder();
        Player?.WriteTo(builder);
        MPlayer?.WriteTo(builder);
        MissionState?.WriteTo(builder);
        foreach(var ts in TriggerSave) ts.WriteTo(builder);
        builder.Section("BStoryMissionDone");
        if (Mission01aSave != null)
            builder.Section("Mission01aSave")
                .Entry("MissionStateNum", Mission01aSave.MissionStateNum);
        if (Mission01bSave != null)
            builder.Section("Mission01bSave")
                .Entry("MissionStateNum", Mission01bSave.MissionStateNum);
        StoryInfo?.WriteTo(builder);
        Time?.WriteTo(builder);
        foreach(var g in Groups) g.WriteTo(builder);
        LockedGates?.WriteTo(builder);
        return builder.Sections;
    }

    public static SaveGame FromString(string name, string str)
    {
        var sg = new SaveGame();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(str));

        sg.ParseIni(stream, name);

        return sg;
    }

    public static SaveGame FromBytes(string path, byte[] bytes)
    {
        var sg = new SaveGame();
        using var stream = new MemoryStream(FlCodec.DecodeBytes(bytes));

        sg.ParseIni(stream, path);

        return sg;
    }
}
