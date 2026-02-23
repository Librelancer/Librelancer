using System;
using System.Collections.Generic;
using LibreLancer.Data.Schema.Voices;

namespace LibreLancer.Data.GameData;

public class Voice : IdentifiableItem
{
    public FLGender Gender = FLGender.unset;
    public string[] Scripts = [];
    public Dictionary<string, VoiceLineInfo> Lines = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<uint, VoiceLineInfo> LinesByHash = new();
}

public record struct VoiceLineInfo(float Attenuation);
