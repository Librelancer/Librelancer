// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Data.Schema.Audio;
using LibreLancer.Data.Schema.Effects;

namespace LibreLancer.Data.GameData;

public class ResolvedFx : IdentifiableItem
{
    public required string[] LibraryFiles;
    public uint VisFxCrc;
    public string? AlePath;
    public BeamSpear? Spear;
    public BeamBolt? Bolt;
    public AudioEntry? Sound;
}
