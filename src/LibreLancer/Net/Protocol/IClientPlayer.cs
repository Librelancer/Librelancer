using System;
using System.Numerics;
using LibreLancer.Missions;
using LibreLancer.World;

namespace LibreLancer.Net.Protocol;

[RPCInterface]
public interface IClientPlayer
{
    void UpdateBaselinePrices(BaselinePriceBundle prices);
    void CallThorn(string script, ObjNetId mainObject);
    void ListPlayers(bool isAdmin);
    void UpdateWeaponGroups(NetWeaponGroup[] wg);

    void SpawnPlayer(int id, string system, CrcIdMap[] crcMap, NetObjective objective, Vector3 position, Quaternion orientation, uint tick);
    void UpdateEffects(ObjNetId id, SpawnedEffect[] effects);
    void UpdateAttitude(ObjNetId id, RepAttitude attitude);
    void SpawnProjectiles(ProjectileSpawn[] projectiles);
    void UpdateAnimations(ObjNetId id, NetCmpAnimation[] animations);
    void UpdateStatistics(NetPlayerStatistics stats);
    void UpdateReputations(NetReputation[] reps);
    void UpdateInventory(PlayerInventoryDiff diff);
    void UpdateSlotCount(int slot, int count);
    void DeleteSlot(int slot);
    void UpdateLootObject(ObjNetId id, NetBasicCargo[] items);
    void SpawnObjects(ObjectSpawnInfo[] objects);
    void OnConsoleMessage(string text);
    void SpawnMissile(int id, bool playSound, uint equip, Vector3 position, Quaternion orientation);
    void DestroyMissile(int id, bool explode);
    void BaseEnter(string _base, NetObjective objective, NetThnInfo thns, NewsArticle[] news, SoldGood[] goods, NetSoldShip[] ships);
    void UpdateThns(NetThnInfo thns);
    void SetObjective(NetObjective objective, bool history);
    void Killed();
    void DespawnObject(int id, bool explode);
    void PlaySound(string sound);
    void PlayMusic(string music, float fade);
    void DestroyPart(ObjNetId id, uint part);
    void RunMissionDialog(NetDlgLine[] lines);
    void StartJumpTunnel();
    void StartTradelane();
    void TradelaneDisrupted();
    void EndTradelane();

    void StartTractor(ObjNetId ship, ObjNetId target);
    void EndTractor(ObjNetId ship, ObjNetId target);
    void TractorFailed();
    void UpdateFormation(NetFormation formation);
    void TradelaneActivate(uint id, bool left);
    void TradelaneDeactivate(uint id, bool left);
    void MarkImportant(int objId, bool important);
    [Channel(1)]
    void ReceiveChatMessage(ChatCategory category, BinaryChatMessage player, BinaryChatMessage message);
    void PopupOpen(int title, int contents, string id);
    [Channel(1)]
    void OnPlayerJoin(int id, string name);
    [Channel(1)]
    void OnPlayerLeave(int id, string name);

    [Channel(1)] //Low prio
    void VisitObject(uint hash, byte flags);
    void UpdateVisits(VisitBundle visits);
    void StopShip();

    void UpdateAllowedDocking(AllowedDocking allowed);

    void UpdateCharacterProgress(int level, long nextNetWorth);
    void UndockFrom(ObjNetId id, int index);
    void RunDirectives(MissionDirective[] directives);
    void UpdatePlayTime(double time, DateTime startTime);
    void ClearScan();
    void UpdateScan(ObjNetId id, NetLoadoutDiff diff);

    void StoryMissionFailed(int failedIds);

    // SINGLEPLAYER ONLY
    void SPSetAutosave(string path);
}
