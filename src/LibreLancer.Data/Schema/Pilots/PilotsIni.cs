// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Pilots
{
    [ParsedIni]
    public partial class PilotsIni
    {
        [Section("BuzzHeadTowardBlock")]
        public List<BuzzHeadTowardBlock> BuzzHeadTowardBlocks = new List<BuzzHeadTowardBlock>();
        [Section("BuzzPassByBlock")] public List<BuzzPassByBlock> BuzzPassByBlocks = new List<BuzzPassByBlock>();
        [Section("DamageReactionBlock")]
        public List<DamageReactionBlock> DamageReactionBlocks = new List<DamageReactionBlock>();
        [Section("EvadeBreakBlock")] public List<EvadeBreakBlock> EvadeBreakBlocks = new List<EvadeBreakBlock>();
        [Section("EvadeDodgeBlock")] public List<EvadeDodgeBlock> EvadeDodgeBlocks = new List<EvadeDodgeBlock>();
        [Section("CountermeasureBlock")]
        public List<CountermeasureBlock> CountermeasureBlocks = new List<CountermeasureBlock>();
        [Section("FormationBlock")] public List<FormationBlock> FormationBlocks = new List<FormationBlock>();
        [Section("GunBlock")] public List<GunBlock> GunBlocks = new List<GunBlock>();
        [Section("JobBlock")] public List<JobBlock> JobBlocks = new List<JobBlock>();
        [Section("MissileBlock")] public List<MissileBlock> MissileBlocks = new List<MissileBlock>();
        [Section("MissileReactionBlock")]
        public List<MissileReactionBlock> MissileReactionBlocks = new List<MissileReactionBlock>();
        [Section("MineBlock")] public List<MineBlock> MineBlocks = new List<MineBlock>();
        [Section("RepairBlock")] public List<RepairBlock> RepairBlocks = new List<RepairBlock>();
        [Section("EngineKillBlock")] public List<EngineKillBlock> EngineKillBlocks = new List<EngineKillBlock>();
        [Section("StrafeBlock")] public List<StrafeBlock> StrafeBlocks = new List<StrafeBlock>();
        [Section("TrailBlock")] public List<TrailBlock> TrailBlocks = new List<TrailBlock>();
        [Section("Pilot")] public List<Pilot> Pilots = new List<Pilot>();

        public void AddFile(string file, FileSystem vfs, IniStringPool stringPool = null) =>
            ParseIni(file, vfs, stringPool);
    }
}
