// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LibreLancer.Data.Save;
using LibreLancer.Database;
using LibreLancer.GameData;
using LibreLancer.Net;
using Microsoft.EntityFrameworkCore.Design;

namespace LibreLancer
{
    public class GameServer
    {
        public string ServerName = "Librelancer Server";
        public string ServerDescription = "Description of the server is here.";
        public string ServerNews = "News of the server goes here";
        
        public IDesignTimeDbContextFactory<LibreLancerContext> DbContextFactory;
        public GameDataManager GameData;
        public ServerDatabase Database;
        public ResourceManager Resources;
        
        public BaselinePrice[] BaselineGoodPrices;

        //TODO: This should be set in the database, not as a config string
        public List<string> AdminCharacters = new List<string>();


        volatile bool running = false;

        public GameListener Listener;
        private Thread gameThread;
        
        public List<Player> ConnectedPlayers = new List<Player>();
        public Player LocalPlayer;

        private bool needLoadData = true;
        public GameServer(string fldir)
        {
            Resources = new ServerResourceManager();
            GameData = new GameDataManager(fldir, Resources);	
            Listener = new GameListener(this);
        }

        public GameServer(GameDataManager gameData)
        {
            Resources = new ServerResourceManager();
            GameData = gameData;
            needLoadData = false;
        }

        public SaveGame NewCharacter(string name, int factionIndex)
        {
            var fac = GameData.Ini.NewCharDB.Factions[factionIndex];
            var pilot = GameData.Ini.NewCharDB.Pilots.First(x =>
                x.Nickname.Equals(fac.Pilot, StringComparison.OrdinalIgnoreCase));
            var package = GameData.Ini.NewCharDB.Packages.First(x =>
                x.Nickname.Equals(fac.Package, StringComparison.OrdinalIgnoreCase));
            //TODO: initial_rep = %%FACTION%%
            //does this have any effect in FL?
            
            var src = new StringBuilder(Encoding.UTF8.GetString(FlCodec.ReadFile(GameData.VFS.Resolve("EXE\\mpnewcharacter.fl"))));
            
            src.Replace("%%NAME%%", SavePlayer.EncodeName(name));
            src.Replace("%%BASE_COSTUME%%", pilot.Body);
            src.Replace("%%COMM_COSTUME%%", pilot.Comm);
            //Changing voice breaks in vanilla (commented out in mpnewcharacter)
            src.Replace("%%VOICE%%", pilot.Voice);
            //TODO: pilot comm_anim (not in vanilla mpnewcharacter)
            //TODO: pilot body_anim (not in vanilla mpnewcharacter)
            src.Replace("%%MONEY%%", package.Money.ToString());
            src.Replace("%%HOME_SYSTEM%%", GameData.GetBase(fac.Base).System);
            src.Replace("%%HOME_BASE%%", fac.Base);

            var pkgStr = new StringBuilder();
            pkgStr.Append("ship_archetype = ").AppendLine(package.Ship);
            var loadout = GameData.Ini.Loadouts.Loadouts.First(x =>
                x.Nickname.Equals(package.Loadout, StringComparison.OrdinalIgnoreCase));
            //do loadout
            foreach (var x in loadout.Equip)
            {
                pkgStr.AppendLine(new PlayerEquipment()
                {
                    EquipName = x.Nickname,
                    Hardpoint = x.Hardpoint ?? ""
                }.ToString());
            }

            foreach (var x in loadout.Cargo)
            {
                pkgStr.AppendLine(new PlayerCargo()
                {
                    CargoName = x.Nickname,
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
        
        
        Dictionary<GameData.StarSystem, ServerWorld> worlds = new Dictionary<GameData.StarSystem, ServerWorld>();
        List<GameData.StarSystem> availableWorlds = new List<GameData.StarSystem>();
        ConcurrentQueue<Action> worldRequests = new ConcurrentQueue<Action>();
        ConcurrentQueue<IPacket> localPackets = new ConcurrentQueue<IPacket>();
        
        public void OnLocalPacket(IPacket pkt)
        {
            localPackets.Enqueue(pkt);
        }
        public void RequestWorld(GameData.StarSystem system, Action<ServerWorld> spunUp)
        {
            lock(availableWorlds)
            {
                if (availableWorlds.Contains(system)) { spunUp(worlds[system]); return; }
            }
            worldRequests.Enqueue(() =>
            {
                var world = new ServerWorld(system, this);
                FLLog.Info("Server", "Spun up " + system.Nickname + " (" + system.Name + ")");
                worlds.Add(system, world);
                lock (availableWorlds)
                {
                    availableWorlds.Add(system);
                }
                spunUp(world);
            });
        }

        void InitBaselinePrices()
        {
            var bp = new List<BaselinePrice>();
            foreach (var good in GameData.AllGoods)
            {
                bp.Add(new BaselinePrice()
                {
                    GoodCRC = CrcTool.FLModelCrc(good.Ini.Nickname),
                    Price = (ulong)good.Ini.Price
                });
            }
            BaselineGoodPrices = bp.ToArray();
        }

        public void SystemChatMessage(Player source, string message)
        {
            var s = source.System;
            foreach (var p in GetPlayers())
            {
                if(p.System.Equals(s, StringComparison.OrdinalIgnoreCase))
                    p.RemoteClient.ReceiveChatMessage(ChatCategory.System, source.Name, message);
            }
        }

        IEnumerable<Player> GetPlayers()
        {
            lock (ConnectedPlayers)
            {
                return ConnectedPlayers.ToArray();
            }
        }

        public Player GetConnectedPlayer(string name) =>
            GetPlayers().FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        

        private ServerLoop processingLoop;

        public double TotalTime => processingLoop.TotalTime.TotalSeconds;

        //FromSeconds creates an inaccurate timespan
        static readonly TimeSpan RATE_60 = TimeSpan.FromTicks(166667);
        static readonly TimeSpan RATE_30 = TimeSpan.FromTicks(333333);

        void Process(TimeSpan time, TimeSpan totalTime)
        {
            while (!localPackets.IsEmpty && localPackets.TryDequeue(out var local))
                LocalPlayer.ProcessPacketDirect(local);
            Action a;
            if (worldRequests.Count > 0 && worldRequests.TryDequeue(out a))
                a();
            //Update
            if (!(LocalPlayer?.World?.Paused ?? false))
            {
                LocalPlayer?.UpdateMissionRuntime(time.TotalSeconds);
            }
            ConcurrentBag<StarSystem> toSpinDown = new ConcurrentBag<StarSystem>();
            Parallel.ForEach(worlds, (world) =>
            {
                if(!world.Value.Update(time.TotalSeconds, totalTime.TotalSeconds))
                    toSpinDown.Add(world.Key);
            });
            //Remove
            if (toSpinDown.Count > 0)
            {
                lock (availableWorlds) 
                {
                    foreach (var w in toSpinDown)
                    {
                        if (worlds[w].PlayerCount <= 0)
                        {
                            worlds[w].Finish();
                            availableWorlds.Remove(w);
                            worlds.Remove(w);
                            FLLog.Info("Server", $"Shut down world {w.Nickname} ({w.Name})");
                        }
                    }
                }
            }
            processingLoop.TimeStep = worlds.Count > 0 ? RATE_60 : RATE_30;
            if (!running) processingLoop.Stop();
        }

        void GameThread()
        {
            if (needLoadData)
            {
                FLLog.Info("Server", "Loading Game Data...");
                GameData.LoadData(null);
                FLLog.Info("Server", "Finished Loading Game Data");
            }
            InitBaselinePrices();
            Database = new ServerDatabase(this);
            Listener?.Start();
            double lastTime = 0;
            processingLoop = new ServerLoop(Process);
            processingLoop.Start();
            Listener?.Stop();
        }
    }
}