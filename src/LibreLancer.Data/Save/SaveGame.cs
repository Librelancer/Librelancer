﻿// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Ini;
using System.IO;
using System.Text;

namespace LibreLancer.Data.Save
{
    public class SaveGame : IniFile
    {
        [Section("player")]
        public SavePlayer Player;

        [Section("mplayer")]
        public MPlayer MPlayer;

        [Section("storyinfo")]
        public StoryInfo StoryInfo;

        [Section("TriggerSave")]
        public List<TriggerSave> TriggerSave = new List<TriggerSave>();

        [Section("time")]
        public SaveTime Time;

        [Section("group")]
        public List<SaveGroup> Groups = new List<SaveGroup>();

        [Section("locked_gates")]
        public LockedGates LockedGates;

        [Section("nnobjective")]
        public List<SavedObjective> Objectives = new List<SavedObjective>();

        public List<Section> ToIni()
        {
            var builder = new IniBuilder();
            Player?.WriteTo(builder);
            MPlayer?.WriteTo(builder);
            foreach(var ts in TriggerSave) ts.WriteTo(builder);
            StoryInfo?.WriteTo(builder);
            Time?.WriteTo(builder);
            foreach(var g in Groups) g.WriteTo(builder);
            LockedGates?.WriteTo(builder);
            return builder.Sections;
        }

        public static SaveGame FromString(string name, string str)
        {
            var sg = new SaveGame();
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(str)))
            {
                sg.ParseAndFill(name, stream, false);
            }
            return sg;
        }
        public static SaveGame FromBytes(string path, byte[] bytes)
        {
            var sg = new SaveGame();
            using (var stream = new MemoryStream(FlCodec.DecodeBytes(bytes)))
            {
                sg.ParseAndFill(path, stream, false);
            }
            return sg;
        }
    }
}
