// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer;
using LibreLancer.Data;
using LibreLancer.Data.IO;
using LibreLancer.Data.Schema;
using LibreLancer.Data.Schema.Voices;

namespace LancerEdit
{
    public class IDTable
    {
        public Dictionary<uint, string> UtfNicknameTable;

        public IDTable(string fldir)
        {
            UtfNicknameTable = new Dictionary<uint, string>();
            var fs = FileSystem.FromPath(fldir, true);
            var flini = new FreelancerIni(fs);
            var voices = new VoicesIni();
            foreach(var path in flini.VoicePaths)
                voices.AddVoicesIni(path, fs);
            foreach (var voice in voices.Voices.Values)
            {
                foreach (var msg in voice.Messages)
                {
                    if(msg.Message != null)
                        UtfNicknameTable[FLHash.CreateID(msg.Message)] = msg.Message;
                }
            }
        }
    }
}
