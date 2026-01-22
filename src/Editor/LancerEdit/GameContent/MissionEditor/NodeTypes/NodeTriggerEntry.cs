using System;
using LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;
using LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;
using LibreLancer;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes;

public abstract class NodeTriggerEntry : Node
{
    protected NodeTriggerEntry(VertexDiffuse? color = null) : base(color)
    {
    }

    public abstract void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder);

    public override void Render(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodeLookups lookups)
    {
        throw new InvalidOperationException("Trigger items should be rendered from MissionTriggerNode");
    }

    public abstract void RenderContent(GameDataContext gameData, PopupManager popup, EditorUndoBuffer undoBuffer,
        ref NodePopups nodePopups,
        ref NodeLookups lookups);

    public static NodeTriggerEntry ConditionToNode(TriggerConditions condition, Entry entry)
    {
        return condition switch
        {
            TriggerConditions.Cnd_WatchVibe => new CndWatchVibe(entry),
            TriggerConditions.Cnd_WatchTrigger => new CndWatchNodeTrigger(entry),
            TriggerConditions.Cnd_True => new CndTrue(entry),
            TriggerConditions.Cnd_TLExited => new CndTradeLaneExit(entry),
            TriggerConditions.Cnd_TLEntered => new CndTradeLaneEnter(entry),
            TriggerConditions.Cnd_Timer => new CndTimer(entry),
            TriggerConditions.Cnd_TetherBroke => new CndTetherBreak(entry),
            TriggerConditions.Cnd_SystemExit => new CndSystemExit(entry),
            TriggerConditions.Cnd_SystemEnter => new CndSystemEnter(entry),
            TriggerConditions.Cnd_SpaceExit => new CndSpaceExit(entry),
            TriggerConditions.Cnd_SpaceEnter => new CndSpaceEnter(entry),
            TriggerConditions.Cnd_RumorHeard => new CndRumourHeard(entry),
            TriggerConditions.Cnd_RTCDone => new CndRtcComplete(entry),
            TriggerConditions.Cnd_ProjHitShipToLbl => new CndProjectileHitShipToLabel(entry),
            TriggerConditions.Cnd_ProjHit => new CndProjectileHit(entry),
            TriggerConditions.Cnd_PopUpDialog => new CndPopUpDialog(entry),
            TriggerConditions.Cnd_PlayerManeuver => new CndPlayerManeuver(entry),
            TriggerConditions.Cnd_PlayerLaunch => new CndPlayerLaunch(entry),
            TriggerConditions.Cnd_NPCSystemExit => new CndNpcSystemExit(entry),
            TriggerConditions.Cnd_NPCSystemEnter => new CndNpcSystemEnter(entry),
            TriggerConditions.Cnd_MsnResponse => new CndMissionResponse(entry),
            TriggerConditions.Cnd_LootAcquired => new CndLootAcquired(entry),
            TriggerConditions.Cnd_LocExit => new CndLocationExit(entry),
            TriggerConditions.Cnd_LocEnter => new CndLocationEnter(entry),
            TriggerConditions.Cnd_LaunchComplete => new CndLaunchComplete(entry),
            TriggerConditions.Cnd_JumpInComplete => new CndJumpInComplete(entry),
            //TriggerConditions.Cnd_JumpgateAct => // need examples of what this one looks like
            TriggerConditions.Cnd_InZone => new CndInZone(entry),
            TriggerConditions.Cnd_InTradelane => new CndInTradeLane(entry),
            TriggerConditions.Cnd_InSpace => new CndInSpace(entry),
            TriggerConditions.Cnd_HealthDec => new CndHealthDecreased(entry),
            TriggerConditions.Cnd_HasMsn => new CndHasMission(entry),
            TriggerConditions.Cnd_EncLaunched => new CndEncounterLaunched(entry),
            TriggerConditions.Cnd_DistVecLbl => new CndShipDistanceVectorLabel(entry),
            TriggerConditions.Cnd_DistVec => new CndShipDistanceVector(entry),
            TriggerConditions.Cnd_DistShip => new CndShipDistance(entry),
            TriggerConditions.Cnd_DistCircle => new CndShipDistanceCircle(entry),
            TriggerConditions.Cnd_Destroyed => new CndDestroyed(entry),
            //TriggerConditions.Cnd_CmpToPlane => need examples of this one too
            TriggerConditions.Cnd_CommComplete => new CndCommComplete(entry),
            TriggerConditions.Cnd_CharSelect => new CndCharacterSelect(entry),
            TriggerConditions.Cnd_CargoScanned => new CndCargoScanned(entry),
            TriggerConditions.Cnd_BaseExit => new CndBaseExit(entry),
            TriggerConditions.Cnd_BaseEnter => new CndBaseEnter(entry),
            _ => throw new NotImplementedException($"{condition} is not implemented")
        };
    }

    public static NodeTriggerEntry ActionToNode(TriggerActions type, MissionAction action)
    {
        return type switch
        {
            TriggerActions.Act_PlaySoundEffect => new ActPlaySound(action),
            TriggerActions.Act_Invulnerable => new ActInvulnerable(action),
            TriggerActions.Act_PlayMusic => new ActPlayMusic(action),
            TriggerActions.Act_SetShipAndLoadout => new ActSetShipAndLoadout(action),
            TriggerActions.Act_RemoveAmbient => new ActRemoveAmbient(action),
            TriggerActions.Act_AddAmbient => new ActAddAmbient(action),
            TriggerActions.Act_RemoveRTC => new ActRemoveRtc(action),
            TriggerActions.Act_AddRTC => new ActAddRtc(action),
            TriggerActions.Act_AdjAcct => new ActAdjustAccount(action),
            TriggerActions.Act_DeactTrig => new ActDeactivateNodeTrigger(action),
            TriggerActions.Act_ActTrig => new ActActivateNodeTrigger(action),
            TriggerActions.Act_SetNNObj => new ActSetNnObject(action),
            TriggerActions.Act_ForceLand => new ActForceLand(action),
            TriggerActions.Act_LightFuse => new ActLightFuse(action),
            TriggerActions.Act_PopUpDialog => new ActPopupDialog(action),
            TriggerActions.Act_ChangeState => new ActChangeState(action),
            TriggerActions.Act_RevertCam => new ActRevertCamera(action),
            TriggerActions.Act_CallThorn => new ActCallThorn(action),
            TriggerActions.Act_MovePlayer => new ActMovePlayer(action),
            TriggerActions.Act_Cloak => new ActCloak(action),
            TriggerActions.Act_PobjIdle => new ActPObjectIdle(action),
            TriggerActions.Act_SetInitialPlayerPos => new ActSetInitialPlayerPos(action),
            TriggerActions.Act_RelocateShip => new ActRelocateShip(action),
            TriggerActions.Act_StartDialog => new ActStartDialog(action),
            TriggerActions.Act_SendComm => new ActSendComm(action),
            TriggerActions.Act_DebugMsg => new ActDebugMsg(action),
            TriggerActions.Act_EtherComm => new ActEtherComm(action),
            TriggerActions.Act_SetVibe => new ActSetVibe(action),
            TriggerActions.Act_SetVibeLbl => new ActSetVibeLabel(action),
            TriggerActions.Act_SetVibeShipToLbl => new ActSetVibeShipToLabel(action),
            TriggerActions.Act_SetVibeLblToShip => new ActSetVibeLabelToShip(action),
            TriggerActions.Act_SpawnSolar => new ActSpawnSolar(action),
            TriggerActions.Act_SpawnShip => new ActSpawnShip(action),
            TriggerActions.Act_SpawnFormation => new ActSpawnFormation(action),
            TriggerActions.Act_MarkObj => new ActMarkObject(action),
            TriggerActions.Act_Destroy => new ActDestroy(action),
            TriggerActions.Act_StaticCam => new ActStaticCamera(action),
            TriggerActions.Act_SpawnLoot => new ActSpawnLoot(action),
            TriggerActions.Act_SetVibeOfferBaseHack => new ActSetVibeOfferBaseHack(action),
            TriggerActions.Act_SetTitle => new ActSetTitle(action),
            TriggerActions.Act_SetRep => new ActSetRep(action),
            TriggerActions.Act_SetOrient => new ActSetOrientation(action),
            TriggerActions.Act_SetOffer => new ActSetOffer(action),
            TriggerActions.Act_SetNNState => new ActSetNnState(action),
            TriggerActions.Act_SetNNHidden => new ActSetNnHidden(action),
            TriggerActions.Act_SetLifeTime => new ActSetLifetime(action),
            TriggerActions.Act_Save => new ActSave(action),
            TriggerActions.Act_RpopTLAttacksEnabled => new ActRPopAttacksEnabled(action),
            TriggerActions.Act_RpopAttClamp => new ActRPopClamp(action),
            TriggerActions.Act_RemoveCargo => new ActRemoveCargo(action),
            TriggerActions.Act_RandomPopSphere => new ActRandomPopSphere(action),
            TriggerActions.Act_RandomPop => new ActRandomPop(action),
            TriggerActions.Act_SetPriority => new ActSetPriority(action),
            TriggerActions.Act_PlayerEnemyClamp => new ActPlayerEnemyClamp(action),
            TriggerActions.Act_PlayerCanTradelane => new ActCanTradeLane(action),
            TriggerActions.Act_PlayerCanDock => new ActCanDock(action),
            TriggerActions.Act_NNIds => new ActNnIds(action),
            TriggerActions.Act_NNPath => new ActNnPath(action),
            TriggerActions.Act_NagOff => new ActNagOff(action),
            TriggerActions.Act_NagGreet => new ActNagGreet(action),
            TriggerActions.Act_NagDistTowards => new ActNagDistTowards(action),
            TriggerActions.Act_NagDistLeaving => new ActNagDistLeaving(action),
            TriggerActions.Act_NagClamp => new ActNagClamp(action),
            TriggerActions.Act_LockManeuvers => new ActLockManeuvers(action),
            TriggerActions.Act_LockDock => new ActLockDock(action),
            TriggerActions.Act_Jumper => new ActJumper(action),
            TriggerActions.Act_HostileClamp => new ActHostileClamp(action),
            TriggerActions.Act_GiveObjList => new ActGiveObjectList(action),
            TriggerActions.Act_GiveNNObjs => new ActGiveNnObjectives(action),
            TriggerActions.Act_GCSClamp => new ActGcsClamp(action),
            TriggerActions.Act_EnableManeuver => new ActEnableManeuver(action),
            TriggerActions.Act_EnableEnc => new ActEnableEncounter(action),
            TriggerActions.Act_DockRequest => new ActDockRequest(action),
            TriggerActions.Act_DisableTradelane => new ActDisableTradelane(action),
            TriggerActions.Act_DisableFriendlyFire => new ActDisableFriendlyFire(action),
            TriggerActions.Act_DisableEnc => new ActDisableEncounter(action),
            TriggerActions.Act_AdjHealth => new ActAdjustHealth(action),
            _ => throw new NotImplementedException($"Unable to render node for action type: {action.Type}"),
        };
    }
}
