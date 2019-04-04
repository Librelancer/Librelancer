// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
    
using System;
using System.Collections.Generic;
using LibreLancer.Ini;
namespace LibreLancer.Data.Audio
{
    public class VoiceIni : IniFile
    {
        public List<Voice> Voices = new List<Voice>();
        public VoiceIni()
        {
        }
        public void AddVoiceIni(string path)
        {

        }
    }
    public class Voice
    {
        [Entry("nickname")]
        public string Nickname;
        [Entry("extend")]
        public string Extend;
    }
}
