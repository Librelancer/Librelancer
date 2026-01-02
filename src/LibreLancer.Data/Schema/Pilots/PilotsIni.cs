// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Pilots;

[ParsedIni]
public partial class PilotsIni
{
    [Section("BuzzHeadTowardBlock")]
    public List<BuzzHeadTowardBlock> BuzzHeadTowardBlocks = [];
    [Section("BuzzPassByBlock")] public List<BuzzPassByBlock> BuzzPassByBlocks = [];
    [Section("DamageReactionBlock")]
    public List<DamageReactionBlock> DamageReactionBlocks = [];
    [Section("EvadeBreakBlock")] public List<EvadeBreakBlock> EvadeBreakBlocks = [];
    [Section("EvadeDodgeBlock")] public List<EvadeDodgeBlock> EvadeDodgeBlocks = [];
    [Section("CountermeasureBlock")]
    public List<CountermeasureBlock> CountermeasureBlocks = [];
    [Section("FormationBlock")] public List<FormationBlock> FormationBlocks = [];
    [Section("GunBlock")] public List<GunBlock> GunBlocks = [];
    [Section("JobBlock")] public List<JobBlock> JobBlocks = [];
    [Section("MissileBlock")] public List<MissileBlock> MissileBlocks = [];
    [Section("MissileReactionBlock")]
    public List<MissileReactionBlock> MissileReactionBlocks = [];
    [Section("MineBlock")] public List<MineBlock> MineBlocks = [];
    [Section("RepairBlock")] public List<RepairBlock> RepairBlocks = [];
    [Section("EngineKillBlock")] public List<EngineKillBlock> EngineKillBlocks = [];
    [Section("StrafeBlock")] public List<StrafeBlock> StrafeBlocks = [];
    [Section("TrailBlock")] public List<TrailBlock> TrailBlocks = [];
    [Section("Pilot")] public List<Pilot> Pilots = [];

    public void AddFile(string file, FileSystem vfs, IniStringPool? stringPool = null) =>
        ParseIni(file, vfs, stringPool);
}
