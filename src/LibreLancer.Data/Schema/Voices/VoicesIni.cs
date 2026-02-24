// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Voices;

[ParsedSection]
internal partial class VoiceSection
{
    [Entry("nickname")] public string? Nickname;
    [Entry("extend")] public string? Extend;
    [Entry("script", Multiline = true)] public List<string> Scripts = [];
}

public class VoicesIni
{
    public Dictionary<string, Voice> Voices = new(StringComparer.OrdinalIgnoreCase);
    public List<VoiceProp> VoiceProps = new();

    public void AddVoicesIni(string path, FileSystem vfs, IniStringPool? stringPool = null)
    {
        Voice? currentVoice = null;
        foreach (var section in IniFile.ParseFile(path, vfs, false, stringPool))
        {
            switch (section.Name.ToLowerInvariant())
            {
                case "voice":
                    if (VoiceSection.TryParse(section, out var s))
                    {
                        var name = s.Extend ?? s.Nickname;
                        if (name == null)
                        {
                            FLLog.Error("Ini", $"{section.File}:{section.Line} [Voice] missing nickname or extend.");
                            continue;
                        }
                        if (!Voices.TryGetValue(name, out currentVoice))
                        {
                            currentVoice = new Voice() { Nickname = name };
                            Voices.Add(name, currentVoice);
                        }

                        currentVoice.Scripts.AddRange(s.Scripts);
                    }

                    break;
                case "sound":
                    if (currentVoice == null)
                    {
                        FLLog.Error("Ini", $"{section.File}:{section.Line} [Sound] section without matching [Voice]");
                    }
                    else if (VoiceMessage.TryParse(section, out var msg))
                    {
                        currentVoice.Messages.Add(msg);
                    }
                    break;
                case "mvoiceprop":
                    if (VoiceProp.TryParse(section, out var prop))
                    {
                        VoiceProps.Add(prop);
                    }
                    break;
            }
        }
    }
}

public class Voice
{
    public required string Nickname;
    public List<string> Scripts = [];
    public List<VoiceMessage> Messages = [];
}

[ParsedSection]
public partial class VoiceMessage
{
    [Entry("msg", Required = true)] public string Message = null!;
    [Entry("attenuation")] public float Attenuation;
    [Entry("duration")] public float Duration;
    [Entry("priority")] public string? Priority;
}

[ParsedSection]
public partial class VoiceProp
{
    [Entry("Voice", Required = true)]
    public string Voice = "";
    [Entry("supports_roles")]
    public string[] SupportsRoles = [];
    [Entry("gender")]
    public FLGender Gender;
}
