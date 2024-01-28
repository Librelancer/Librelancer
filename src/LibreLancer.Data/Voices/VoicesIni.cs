// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.IO;
using LibreLancer.Ini;

namespace LibreLancer.Data.Voices
{
    public class VoicesIni : IniFile
    {
        public Dictionary<string, Voice> Voices = new Dictionary<string, Voice>(StringComparer.OrdinalIgnoreCase);
        class VoiceSection
        {
            [Entry("nickname")] public string Nickname;
            [Entry("extend")] public string Extend;
            [Entry("script", Multiline = true)] public List<string> Scripts = new List<string>();
        }

        public void AddVoicesIni(string path, FileSystem vfs)
        {
            Voice currentVoice = null;
            foreach (var section in ParseFile(path, vfs))
            {
                switch (section.Name.ToLowerInvariant())
                {
                    case "voice":
                        var s = FromSection<VoiceSection>(section);
                        var name = s.Extend ?? s.Nickname;
                        if (!Voices.TryGetValue(name, out currentVoice))
                        {
                            currentVoice = new Voice() { Nickname = name };
                            Voices.Add(name, currentVoice);
                        }
                        currentVoice.Scripts.AddRange(s.Scripts);
                        break;
                    case "sound":
                        if (currentVoice == null)
                        {
                            FLLog.Error("Ini",
                                string.Format("{0}:{1} [Sound] section without matching [Voice]", section.File, section.Line));
                        } else
                            currentVoice.Messages.Add(FromSection<VoiceMessage>(section));
                        break;
                }
            }
        }
    }

    public class Voice
    {
        public string Nickname;
        public List<string> Scripts = new List<string>();
        public List<VoiceMessage> Messages = new List<VoiceMessage>();
    }

    public class VoiceMessage
    {
        [Entry("msg")] public string Message;
        [Entry("attenuation")] public float Attenuation;
        [Entry("duration")] public float Duration;
        [Entry("priority")] public string Priority;
    }
}