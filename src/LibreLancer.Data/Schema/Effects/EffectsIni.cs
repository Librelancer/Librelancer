// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Effects;

[ParsedIni]
public partial class EffectsIni
{
    [Section("viseffect")]
    public List<VisEffect> VisEffects = [];
    [Section("beamspear")]
    public List<BeamSpear> BeamSpears = [];
    [Section("beambolt")]
    public List<BeamBolt> BeamBolts = [];
    [Section("effect")]
    public List<Effect> Effects = [];
    [Section("effecttype")]
    public List<EffectType> EffectTypes = [];
    [Section("effectlod")]
    public List<EffectLOD> EffectLODs = [];

    public void AddIni(string ini, FileSystem vfs, IniStringPool? stringPool = null) => ParseIni(ini, vfs, stringPool);
}
