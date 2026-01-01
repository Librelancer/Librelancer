// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer.Data.Schema.Missions;

public enum TriggerConditions
{
    Cnd_WatchVibe,
    Cnd_WatchTrigger,
    Cnd_True,
    Cnd_TLExited,
    Cnd_TLEntered,
    Cnd_Timer,
    Cnd_TetherBroke,
    Cnd_SystemExit,
    Cnd_SystemEnter,
    Cnd_SpaceExit,
    Cnd_SpaceEnter,
    Cnd_RumorHeard,
    Cnd_RTCDone,
    Cnd_ProjHitShipToLbl,
    Cnd_ProjHit,
    Cnd_PopUpDialog,
    Cnd_PlayerManeuver,
    Cnd_PlayerLaunch,
    Cnd_NPCSystemExit,
    Cnd_NPCSystemEnter,
    Cnd_MsnResponse,
    Cnd_LootAcquired,
    Cnd_LocExit,
    Cnd_LocEnter,
    Cnd_LaunchComplete,
    Cnd_JumpInComplete,
    Cnd_JumpgateAct,
    Cnd_InZone,
    Cnd_InTradelane,
    Cnd_InSpace,
    Cnd_HealthDec,
    Cnd_HasMsn,
    Cnd_EncLaunched,
    Cnd_DistVecLbl,
    Cnd_DistVec,
    Cnd_DistShip,
    Cnd_DistCircle,
    Cnd_Destroyed,
    Cnd_CmpToPlane,
    Cnd_CommComplete,
    Cnd_CharSelect,
    Cnd_CargoScanned,
    Cnd_BaseExit,
    Cnd_BaseEnter
}