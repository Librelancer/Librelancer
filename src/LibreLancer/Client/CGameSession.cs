// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Interface;
using LibreLancer.Missions;
using LibreLancer.Net;
using LibreLancer.Net.Protocol;
using LibreLancer.Net.Protocol.RpcPackets;
using LibreLancer.Server;
using LibreLancer.World;

namespace LibreLancer.Client;

public partial class CGameSession : IClientPlayer
{
    private static readonly int NewPlayerIds = 393298;
    private static readonly int DepartingPlayerIds = 393299;

    public List<StoryCutsceneIni> ActiveCutscenes = [];
    public bool Admin;
    private AllowedDocking? allowedDocking;
    private readonly Queue<Action> audioActions = new();

    public Dictionary<uint, ulong> BaselinePrices = new();
    public ChatSource Chats = new();
    private readonly IPacketConnection connection;
    public long Credits;
    public NetObjective CurrentObjective;
    public int CurrentRank;
    private string? departingPlayerStr;

    private int enterCount;

    public Action<IPacket>? ExtraPackets;
    public FreelancerGame Game;
    private readonly Queue<Action> gameplayActions = new();


    public SoldGood[] Goods = null!;

    private bool hasChanged;

    private bool inTradelane;
    public List<NetCargo> Items = [];

    private PlayerInventory lastInventory = new();
    public long NetWorth;

    private string? newPlayerStr;
    public NewsArticle[] News = [];
    public long NextLevelWorth;
    public Action? ObjectiveUpdated;

    public Action? OnUpdateInventory;
    public Action? OnUpdatePlayerShip;
    private bool paused;
    public string? PlayerBase;
    public int PlayerNetID;
    public Quaternion PlayerOrientation;
    public Vector3 PlayerPosition;
    public ReputationCollection PlayerReputations = new();
    private DateTime playerSessionStart = DateTime.UtcNow;
    public Ship? PlayerShip;

    public string PlayerSystem = null!;
    private double playerTotalTime;

    public ConcurrentQueue<Popup> Popups = new();

    public NetResponseHandler ResponseHandler;
    private ObjNetId? scanId;
    private NetLoadout? scanLoadout;
    private UIInventoryItem[] scannedInventory = [];
    public NetSoldShip[]? Ships;
    public ulong ShipWorth;
    private SpaceGameplay? spaceGameplay;

    // Use only for Single Player
    // Works because the data is already loaded,
    // and this is really only waiting for the embedded server to start
    private bool started;
    public PlayerStats Statistics = new();
    public DynamicThn Thns = new();
    private readonly Queue<Action> uiActions = new();
    public Dictionary<uint, VisitFlags> Visits = new();

    public uint WorldTick = 0;

    public CGameSession(FreelancerGame g, IPacketConnection connection)
    {
        Game = g;
        this.connection = connection;
        ResponseHandler = new NetResponseHandler();
        RpcServer = new RemoteServerPlayer(connection, ResponseHandler);
        SpaceRpc = new RemoteSpacePlayer(connection, ResponseHandler);
        BaseRpc = new RemoteBasesidePlayer(connection, ResponseHandler);
    }

    public IServerPlayer RpcServer { get; }

    public IBasesidePlayer BaseRpc { get; }

    public ISpacePlayer SpaceRpc { get; }

    public double WorldTime => WorldTick * (1 / 60.0f);

    public bool Multiplayer => connection is GameNetClient;


    public EmbeddedServer? EmbeddedServer => connection as EmbeddedServer;

    public string? AutoSavePath { get; private set; }


    public double CharacterPlayTime => playerTotalTime + (DateTime.UtcNow - playerSessionStart).TotalSeconds;

    void IClientPlayer.SPSetAutosave(string path)
    {
        AutoSavePath = path;
    }

    void IClientPlayer.UpdateStatistics(NetPlayerStatistics stats)
    {
        Statistics.TotalMissions = stats.TotalMissions;
        Statistics.TotalKills = stats.FightersKilled + stats.FreightersKilled + stats.TransportsKilled +
                                stats.BattleshipsKilled;
        Statistics.SystemsVisited = stats.SystemsVisited;
        Statistics.BasesVisited = stats.BasesVisited;
        Statistics.JumpHolesFound = stats.JumpHolesFound;
        Statistics.FightersKilled = stats.FightersKilled;
        Statistics.FreightersKilled = stats.FreightersKilled;
        Statistics.TransportsKilled = stats.TransportsKilled;
        Statistics.BattleshipsKilled = stats.BattleshipsKilled;
    }

    void IClientPlayer.StartTradelane()
    {
        inTradelane = true;
        RunSync(spaceGameplay!.StartTradelane);
    }

    void IClientPlayer.UpdateVisits(VisitBundle bundle)
    {
        Visits = new Dictionary<uint, VisitFlags>();

        foreach (var b in bundle.Visits)
            Visits[b.Obj.Hash] = (VisitFlags)b.Visit;
    }

    void IClientPlayer.VisitObject(uint hash, byte flags)
    {
        Visits[hash] = (VisitFlags)flags;
    }

    void IClientPlayer.PopupOpen(int title, int contents, string id)
    {
        FLLog.Debug("CGameSession", "Enqueuing popup");
        Popups.Enqueue(new Popup { Title = title, Contents = contents, ID = id });
    }

    void IClientPlayer.StartJumpTunnel()
    {
        FLLog.Warning("Client", "Jump tunnel unimplemented");
    }

    void IClientPlayer.SetObjective(NetObjective objective, bool history)
    {
        CurrentObjective = objective;

        if (!history)
            ObjectiveUpdated?.Invoke();
    }

    void IClientPlayer.OnConsoleMessage(string text)
    {
        FLLog.Info("Console", text);
        var msg = BinaryChatMessage.PlainText(text);

        if (text.Length > 200)
            msg.Segments[0].Size = ChatMessageSize.Small;

        Chats.Append(null, msg, Color4.LimeGreen, "Arial");
    }

    void IClientPlayer.UpdateReputations(NetReputation[] reps)
    {
        foreach (var r in reps)
        {
            var f = Game.GameData.Items.Factions.Get(r.FactionHash);

            if (f != null)
                PlayerReputations.Reputations[f] = r.Reputation;
        }
    }

    void IClientPlayer.UpdateInventory(PlayerInventoryDiff diff)
    {
        lastInventory = diff.Apply(lastInventory);
        Credits = lastInventory.Credits;
        ShipWorth = lastInventory.ShipWorth;
        NetWorth = (long)lastInventory.NetWorth;
        SetSelfLoadout(lastInventory.Loadout);

        if (OnUpdateInventory == null)
            return;

        uiActions.Enqueue(OnUpdateInventory);

        if (spaceGameplay == null && OnUpdatePlayerShip != null)
            uiActions.Enqueue(OnUpdatePlayerShip);
    }

    void IClientPlayer.UpdateCharacterProgress(int rank, long nextNetWorth)
    {
        CurrentRank = rank;
        NextLevelWorth = nextNetWorth;
    }

    public void UpdateSlotCount(int slot, int count)
    {
        var cargo = Items.FirstOrDefault(x => x.ID == slot);
        cargo?.Count = count;

        if (OnUpdateInventory != null)
            uiActions.Enqueue(OnUpdateInventory);
    }

    public void DeleteSlot(int slot)
    {
        var cargo = Items.FirstOrDefault(x => x.ID == slot);

        if (cargo != null)
            Items.Remove(cargo);

        if (OnUpdateInventory != null)
            uiActions.Enqueue(OnUpdateInventory);
    }

    public void UpdateWeaponGroups(NetWeaponGroup[] wg)
    {
    }


    void IClientPlayer.BaseEnter(string _base, NetObjective objective, NetThnInfo thns, NewsArticle[] news,
        SoldGood[] goods, NetSoldShip[] ships)
    {
        if (enterCount > 0 && connection is EmbeddedServer es)
        {
            var path = Game.GetSaveFolder();
            Directory.CreateDirectory(path);
            es.Save(null, true);
        }

        CurrentObjective = objective;
        enterCount++;
        PlayerBase = _base;
        News = news;
        Goods = goods;
        Ships = ships;
        SceneChangeRequired();
        CutsceneUpdate(thns);
    }

    void IClientPlayer.UpdateBaselinePrices(BaselinePriceBundle prices)
    {
        foreach (var p in prices.Prices)
            BaselinePrices[p.GoodCRC] = p.Price;
    }

    void IClientPlayer.UpdateThns(NetThnInfo thns)
    {
        CutsceneUpdate(thns);
    }

    void IClientPlayer.ListPlayers(bool isAdmin)
    {
        Admin = isAdmin;
    }

    void IClientPlayer.ReceiveChatMessage(ChatCategory category, BinaryChatMessage player,
        BinaryChatMessage message)
    {
        Chats.Append(player, message, category.GetColor(), "Arial");
    }

    void IClientPlayer.OnPlayerJoin(int id, string name)
    {
        if (newPlayerStr == null)
            newPlayerStr = Game.GameData.GetInfocardText(NewPlayerIds, Game.Fonts)!.TrimEnd('\n');

        Chats.Append(null, BinaryChatMessage.PlainText($"{newPlayerStr}{name}"), Color4.DarkRed, "Arial");
    }

    void IClientPlayer.OnPlayerLeave(int id, string name)
    {
        if (departingPlayerStr == null)
            departingPlayerStr = Game.GameData.GetInfocardText(DepartingPlayerIds, Game.Fonts)!.TrimEnd('\n');

        Chats.Append(null, BinaryChatMessage.PlainText($"{departingPlayerStr}{name}"), Color4.DarkRed, "Arial");
    }

    void IClientPlayer.UpdatePlayTime(double time, DateTime startTime)
    {
        playerSessionStart = startTime;
        playerTotalTime = time;
    }

    public void Pause()
    {
        if (connection is not EmbeddedServer es)
            return;

        es.Server.LocalPlayer?.Space?.World?.Pause();
        paused = true;
    }

    public void Resume()
    {
        if (connection is not EmbeddedServer es)
            return;

        es.Server.LocalPlayer?.Space?.World?.Resume();
        paused = false;
    }

    public void Save(string description)
    {
        if (connection is EmbeddedServer es)
            Game.Saves.AddFile(es.Save(description, false));
    }

    public void CutsceneUpdate(NetThnInfo info)
    {
        Thns.Unpack(info, Game.GameData);
        ActiveCutscenes = [];

        foreach (var path in Thns.Rtcs)
        {
            var rtc = new StoryCutsceneIni(Game.GameData.Items.Ini.Freelancer.DataPath + path.Script,
                Game.GameData.VFS)
            {
                RefPath = path.Script!
            };

            ActiveCutscenes.Add(rtc);
        }
    }

    public void FinishCutscene(StoryCutsceneIni cutscene)
    {
        ActiveCutscenes.Remove(cutscene);
        RpcServer.RTCComplete(cutscene.RefPath);
    }

    public void RoomEntered(string room, string bse)
    {
        RpcServer.OnLocationEnter(bse, room);
    }

    private void SceneChangeRequired()
    {
        gameplayActions.Clear();

        if (PlayerBase != null)
        {
            Game.ChangeState(new RoomGameplay(Game, this, PlayerBase));
        }
        else
        {
            Acks = default;
            processUpdatePackets = false;
            spaceGameplay = new SpaceGameplay(Game, this);
            Game.ChangeState(spaceGameplay);
        }

        hasChanged = true;
    }

    public bool Update()
    {
        hasChanged = false;
        UpdatePackets();
        UIUpdate();
        return hasChanged;
    }

    private void UpdateAudio()
    {
        while (audioActions.TryDequeue(out var act))
            act();
    }

    private void UIUpdate()
    {
        while (uiActions.TryDequeue(out var act))
            act();
    }

    public DisplayFaction[] GetUIRelations()
    {
        return PlayerReputations.Reputations
            .Where(x => !x.Key.Hidden)
            .Select(x => new DisplayFaction(x.Key.IdsName, x.Value))
            .OrderBy(x => x.Relationship)
            .ToArray();
    }

    private NetCargo ResolveCargo(NetShipCargo cg)
    {
        var equip = Game.GameData.Items.Equipment.Get(cg.EquipCRC)!;
        return new NetCargo(cg.ID)
        {
            Equipment = equip,
            Hardpoint = cg.Hardpoint,
            Health = cg.Health / 255f,
            Count = cg.Count
        };
    }

    private void SetSelfLoadout(NetLoadout ld)
    {
        var sh = ld.ArchetypeCrc == 0 ? null : Game.GameData.Items.Ships.Get(ld.ArchetypeCrc);
        PlayerShip = sh;

        Items = new List<NetCargo>(ld.Items.Count);

        if (sh != null)
            foreach (var cg in ld.Items)
                Items.Add(ResolveCargo(cg));
    }

    public bool IsVisited(uint hash)
    {
        if (!Visits.TryGetValue(hash, out var visit))
            return false;

        return (visit & VisitFlags.Hidden) != VisitFlags.Hidden &&
               (visit & VisitFlags.Visited) == VisitFlags.Visited;
    }

    public void WaitStart()
    {
        if (!started)
            while (connection.PollPacket(out var packet))
            {
                HandlePacket(packet);

                if (packet is IClientPlayer_BaseEnterPacket || packet is IClientPlayer_SpawnPlayerPacket)
                    started = true;
            }
    }

    private void RunSync(Action gp)
    {
        gameplayActions.Enqueue(gp);
    }

    public void EnqueueAction(Action a)
    {
        uiActions.Enqueue(a);
    }


    private void UpdatePackets()
    {
        while (connection.PollPacket(out var packet))
            HandlePacket(packet);
    }


    public void SetDebug(bool on)
    {
        if (connection is EmbeddedServer es)
            es.Server.SendDebugInfo = on;
    }

    public string? GetSelectedDebugInfo()
    {
        if (connection is EmbeddedServer es)
            return es.Server.DebugInfo;

        return null;
    }

    public MissionRuntime.TriggerInfo[]? GetTriggerInfo()
    {
        if (connection is EmbeddedServer es)
            return es.Server.LocalPlayer?.MissionRuntime?.ActiveTriggersInfo;

        return null;
    }

    public void HandlePacket(IPacket pkt)
    {
        if (ResponseHandler.HandlePacket(pkt))
            return;

        var hcp = GeneratedProtocol.HandleIClientPlayer(pkt, this, connection);
        hcp.Wait();

        if (hcp.Result)
            return;

        if (pkt is not SPUpdatePacket && pkt is not PackedUpdatePacket)
            FLLog.Debug("Client", "Got packet of type " + pkt.GetType());

        switch (pkt)
        {
            case SPUpdatePacket:
            case PackedUpdatePacket:
                if (processUpdatePackets)
                    updatePackets.Enqueue(pkt);

                break;
            default:
                if (ExtraPackets != null)
                    ExtraPackets(pkt);
                else
                    FLLog.Error("Network", "Unknown packet type " + pkt.GetType());

                break;
        }
    }


    public void Launch()
    {
        RpcServer.Launch();
    }

    private void AppendBlue(string text)
    {
        Chats.Append(null, BinaryChatMessage.PlainText(text), Color4.CornflowerBlue, "Arial");
    }

    public void OnChat(ChatCategory category, string str)
    {
        if (str.TrimEnd() == "/ping")
        {
            if (connection is GameNetClient nc)
            {
                var stats = $"Ping: {nc.Ping}, Loss {nc.LossPercent}%";
                AppendBlue(stats);
                AppendBlue(
                    $"Sent: {DebugDrawing.SizeSuffix(nc.BytesSent)}, Received: {DebugDrawing.SizeSuffix(nc.BytesReceived)}");
            }
            else
            {
                AppendBlue("Offline");
            }
        }
        else if (str.TrimEnd() == "/debug")
        {
            Game.Debug.Enabled = !Game.Debug.Enabled;
        }
        else if (str.TrimEnd() == "/pos")
        {
            ((IClientPlayer)this).OnConsoleMessage(spaceGameplay != null
                ? spaceGameplay.player.LocalTransform.Position.ToString()
                : "null");
        }
        else
        {
            BinaryChatMessage msg;

            if (str[0] == '/' || !Admin)
                msg = BinaryChatMessage.PlainText(str);
            else
                msg = BinaryChatMessage.ParseBbCode(str);

            RpcServer.ChatMessage(category, msg);
        }
    }

    public void Disconnected()
    {
        Game.ChangeState(new LuaMenu(Game));
    }

    public void QuitToMenu()
    {
        connection.Shutdown();
        Game.ChangeState(new LuaMenu(Game));
    }

    public void OnExit()
    {
        connection.Shutdown();
    }


    public class Popup
    {
        public int Contents;
        public required string ID;
        public int Title;
    }
}
