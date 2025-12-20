// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LibreLancer.Data;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.IO;
using LibreLancer.Data.Schema.Save;
using LibreLancer.Data.Schema.Save;
using LibreLancer.Database;
using LibreLancer.Net;
using LibreLancer.Net.Protocol;
using LibreLancer.Physics;
using LibreLancer.Resources;
using Microsoft.EntityFrameworkCore.Design;

namespace LibreLancer.Server
{
    public class GameServer
    {
        public string ServerName = "Librelancer Server";
        public string ServerDescription = "Description of the server is here.";
        public string ServerNews = "News of the server goes here";
        public string LoginUrl = null;

        public bool SendDebugInfo = false;
        public string DebugInfo { get; private set; }

        public string ScriptsFolder { get; set; }

        public IDesignTimeDbContextFactory<LibreLancerContext> DbContextFactory;
        public GameDataManager GameData;
        public ServerDatabase Database;
        public ResourceManager Resources;
        public WorldProvider Worlds;
        public ServerPerformance PerformanceStats;

        public BaselinePriceBundle BaselineGoodPrices;

        volatile bool running = false;

        public GameListener Listener;
        private Thread gameThread;

        public List<Player> ConnectedPlayers = new List<Player>();
        public Player LocalPlayer;

        public ConcurrentHashSet<long> CharactersInUse = new ConcurrentHashSet<long>();

        private bool needLoadData = true;


        private string debugInfoForFrame = "";
        public void ReportDebugInfo(string info)
        {
            debugInfoForFrame = info;
        }

        public GameServer(FileSystem vfs)
        {
            Resources = new ServerResourceManager(null, vfs);
            GameData = new GameDataManager(new GameItemDb(vfs), Resources);
            Listener = new GameListener(this);
        }

        public GameServer(GameDataManager gameData, ConvexMeshCollection convexCollection)
        {
            Resources = new ServerResourceManager(convexCollection, gameData.VFS);
            GameData = gameData;
            needLoadData = false;
        }

        public SaveGame NewCharacter(string name, int factionIndex)
        {
            var fac = GameData.Items.Ini.NewCharDB.Factions[factionIndex];
            var pilot = GameData.Items.Ini.NewCharDB.Pilots.First(x =>
                x.Nickname.Equals(fac.Pilot, StringComparison.OrdinalIgnoreCase));
            var package = GameData.Items.Ini.NewCharDB.Packages.First(x =>
                x.Nickname.Equals(fac.Package, StringComparison.OrdinalIgnoreCase));
            //TODO: initial_rep = %%FACTION%%
            //does this have any effect in FL?

            var src = new StringBuilder(
                Encoding.UTF8.GetString(FlCodec.DecodeBytes(GameData.VFS.ReadAllBytes("EXE\\mpnewcharacter.fl"))));

            src.Replace("%%NAME%%", SavePlayer.EncodeName(name));
            src.Replace("%%BASE_COSTUME%%", pilot.Body);
            src.Replace("%%COMM_COSTUME%%", pilot.Comm);
            //Changing voice breaks in vanilla (commented out in mpnewcharacter)
            src.Replace("%%VOICE%%", pilot.Voice);
            //TODO: pilot comm_anim (not in vanilla mpnewcharacter)
            //TODO: pilot body_anim (not in vanilla mpnewcharacter)
            src.Replace("%%MONEY%%", package.Money.ToString());
            src.Replace("%%HOME_SYSTEM%%", GameData.Items.Bases.Get(fac.Base).System);
            src.Replace("%%HOME_BASE%%", fac.Base);

            var pkgStr = new StringBuilder();
            pkgStr.Append("ship_archetype = ").AppendLine(package.Ship);
            var loadout = GameData.Items.Ini.Loadouts.Loadouts.First(x =>
                x.Nickname.Equals(package.Loadout, StringComparison.OrdinalIgnoreCase));
            //do loadout
            foreach (var x in loadout.Equip)
            {
                pkgStr.AppendLine(new PlayerEquipment()
                {
                    Item = new HashValue(x.Nickname),
                    Hardpoint = x.Hardpoint ?? ""
                }.ToString());
            }

            foreach (var x in loadout.Cargo)
            {
                pkgStr.AppendLine(new PlayerCargo()
                {
                    Item = new HashValue(x.Nickname),
                    Count = x.Count
                }.ToString());
            }

            //append
            src.Replace("%%PACKAGE%%", pkgStr.ToString());
            var initext = src.ToString();
            return SaveGame.FromString($"mpnewcharacter: {fac.Nickname}", initext);
        }

        public void Start()
        {
            running = true;
            gameThread = new Thread(GameThread);
            gameThread.Name = "Game Server";
            gameThread.Start();
        }

        public void Stop()
        {
            running = false;
            gameThread.Join();
        }

        public void AdminChanged(long id, bool isAdmin)
        {
            foreach (var p in GetPlayers())
            {
                if (p.Character?.ID == id)
                {
                    p.Character.Admin = isAdmin;
                    break;
                }
            }
        }


        Dictionary<StarSystem, ServerWorld> worlds = new Dictionary<StarSystem, ServerWorld>();
        ConcurrentQueue<Action> worldRequests = new ConcurrentQueue<Action>();
        ConcurrentQueue<IPacket> localPackets = new ConcurrentQueue<IPacket>();

        public void OnLocalPacket(IPacket pkt)
        {
            localPackets.Enqueue(pkt);
        }

        public void WorldReady(ServerWorld world)
        {
            worldRequests.Enqueue(() =>
            {
                var sysName = this.GameData.GetString(world.System.IdsName);
                FLLog.Info("Server", "Spun up " + world.System.Nickname + " (" + sysName + ")");
                worlds.Add(world.System, world);
            });
        }

        void InitBaselinePrices()
        {
            var bp = new List<BaselinePrice>();
            foreach (var good in GameData.Items.Goods)
            {
                bp.Add(new BaselinePrice()
                {
                    GoodCRC = CrcTool.FLModelCrc(good.Ini.Nickname),
                    Price = (ulong) good.Ini.Price
                });
            }

            if (Listener == null)
            {
                BaselineGoodPrices = new BaselinePriceBundle() { Prices = bp.ToArray() };
            }
            else
            {
                BaselineGoodPrices = BaselinePriceBundle.Compress(bp.ToArray());
            }
        }

        public void SystemChatMessage(Player source, BinaryChatMessage message)
        {
            var s = source.System;
            foreach (var p in GetPlayers())
            {
                if (p.System.Equals(s, StringComparison.OrdinalIgnoreCase))
                    p.RpcClient.ReceiveChatMessage(ChatCategory.System, BinaryChatMessage.PlainText(source.Name+ ": "), message);
            }
        }

        IEnumerable<Player> GetPlayers()
        {
            lock (ConnectedPlayers)
            {
                return ConnectedPlayers.ToArray();
            }
        }

        public IEnumerable<Player> AllPlayers => GetPlayers();

        public Player GetConnectedPlayer(string name) =>
            GetPlayers().FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        public void LoadSaveGame(SaveGame sg) => worldRequests.Enqueue(() =>
        {
            LocalPlayer.OpenSaveGame(sg);
        });


        private FixedTimestepLoop processingLoop;

        public double TotalTime => processingLoop.TotalTime.TotalSeconds;

        public uint CurrentTick { get; private set; }

        void Process(TimeSpan time, TimeSpan totalTime, uint currentTick)
        {
            CurrentTick = currentTick;
            var startTime = serverTiming.Elapsed;
            while (!localPackets.IsEmpty && localPackets.TryDequeue(out var local))
                LocalPlayer.ProcessPacketDirect(local);
            Action a;
            if (worldRequests.Count > 0 && worldRequests.TryDequeue(out a))
                a();
            //Update
            if (!(LocalPlayer?.Space?.World?.Paused ?? false))
            {
                LocalPlayer?.UpdateMissionRuntime(time.TotalSeconds);
            }
            LocalPlayer?.RunSave();
            debugInfoForFrame = "";
            var toSpinDown = new StarSystem[worlds.Count];
            int spinDownCount = -1;
            foreach (var w in worlds)
            {
                if (!w.Value.Update(time.TotalSeconds, totalTime.TotalSeconds, currentTick))
                    toSpinDown[Interlocked.Increment(ref spinDownCount)] = w.Key;
            }

            DebugInfo = debugInfoForFrame;
            Listener?.Server?.TriggerUpdate(); //Send packets asap
            //Remove
            for (int i = 0; i <= spinDownCount; i++)
            {
                var w = toSpinDown[i];
                if (worlds[w].PlayerCount <= 0)
                {
                    Worlds.RemoveWorld(w);
                    worlds[w].Finish();
                    worlds.Remove(w);
                    var wName = GameData.GetString(w.IdsName);
                    FLLog.Info("Server", $"Shut down world {w.Nickname} ({wName})");
                }
            }
            var updateDuration = serverTiming.Elapsed - startTime;
            PerformanceStats?.AddEntry((float)updateDuration.TotalMilliseconds);
            if (updateDuration > TimeSpan.FromTicks(166667))
            {
                FLLog.Warning("Server", $"Running slow: update took {updateDuration.TotalMilliseconds:F2}ms");
            }
            if (!running) processingLoop.Stop();
        }

        private Stopwatch serverTiming;

        void GameThread()
        {
            if (needLoadData)
            {
                LuaHardwire_LibreLancer.Initialize();
                FLLog.Info("Server", "Loading Game Data...");
                GameData.LoadData(null);
                FLLog.Info("Server", "Finished Loading Game Data");
                GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
            }
            InitBaselinePrices();
            Worlds = new WorldProvider(this);
            serverTiming = Stopwatch.StartNew();
            Database = new ServerDatabase(this);
            Listener?.Start();
            double lastTime = 0;
            processingLoop = new FixedTimestepLoop(Process);
            processingLoop.Start();
            Listener?.Stop();
            Database.Dispose();
            Database = null;
        }
    }
}
