// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer;
using LibreLancer.Data;
using LibreLancer.Data.Voices;
namespace LancerEdit
{
    public class IDTable
    {
        public Dictionary<uint, string> UtfNicknameTable;

        public IDTable(string fldir)
        {
            UtfNicknameTable = new Dictionary<uint, string>();
            VFS.Init(fldir);
            var flini = new FreelancerIni();
            var voices = new VoicesIni();
            foreach(var path in flini.VoicePaths)
                voices.AddVoicesIni(path);
            foreach (var voice in voices.Voices.Values)
            {
                foreach (var msg in voice.Messages)
                {
                    UtfNicknameTable[FLHash.CreateID(msg.Message)] = msg.Message;
                }
            }
        }
    }
}