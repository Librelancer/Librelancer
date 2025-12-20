// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Voices
{
    [ParsedSection]
    partial class VoiceSection
    {
        [Entry("nickname")] public string Nickname;
        [Entry("extend")] public string Extend;
        [Entry("script", Multiline = true)] public List<string> Scripts = new List<string>();
    }

    public class VoicesIni
    {
        public Dictionary<string, Voice> Voices = new Dictionary<string, Voice>(StringComparer.OrdinalIgnoreCase);

        public void AddVoicesIni(string path, FileSystem vfs, IniStringPool stringPool = null)
        {
            Voice currentVoice = null;
            foreach (var section in IniFile.ParseFile(path, vfs, false, stringPool))
            {
                switch (section.Name.ToLowerInvariant())
                {
                    case "voice":
                        VoiceSection.TryParse(section, out var s);
                        var name = s!.Extend ?? s.Nickname;
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
                        }
                        else if (VoiceMessage.TryParse(section, out var msg))
                        {
                            currentVoice.Messages.Add(msg);
                        }
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

    [ParsedSection]
    public partial class VoiceMessage
    {
        [Entry("msg", Required = true)] public string Message;
        [Entry("attenuation")] public float Attenuation;
        [Entry("duration")] public float Duration;
        [Entry("priority")] public string Priority;
    }
}
