// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Data.Schema.Pilots;

namespace LibreLancer.Data.GameData
{
    public class Pilot
    {
        public string Nickname;
        public BuzzHeadTowardBlock BuzzHeadToward;
        public BuzzPassByBlock BuzzPassBy;
        public CountermeasureBlock Countermeasure;
        public DamageReactionBlock DamageReaction;
        public EngineKillBlock EngineKill;
        public EvadeBreakBlock EvadeBreak;
        public EvadeDodgeBlock EvadeDodge;
        public FormationBlock Formation;
        public GunBlock Gun;
        public JobBlock Job;
        public MineBlock Mine;
        public MissileBlock Missile;
        public MissileReactionBlock MissileReactionBlock;
        public RepairBlock Repair;
        public StrafeBlock Strafe;
        public TrailBlock Trail;
    }
}
