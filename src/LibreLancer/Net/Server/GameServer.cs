// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using LibreLancer.GameData;

namespace LibreLancer
{
    public class GameServer
    {
        public string ServerName = "Librelancer Server";
        public string ServerDescription = "Description of the server is here.";
        public string ServerNews = "News of the server goes here";
        
        public string DbConnectionString;
        public GameDataManager GameData;
        public ServerDatabase Database;
        public ResourceManager Resources;
        
        
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

        void GameThread()
        {
            if (needLoadData)
            {
                FLLog.Info("Server", "Loading Game Data...");
                GameData.LoadData();
                FLLog.Info("Server", "Finished Loading Game Data");
            }
            if(!string.IsNullOrWhiteSpace(DbConnectionString))
                Database = new ServerDatabase(DbConnectionString);
            Listener?.Start();
            Stopwatch sw = Stopwatch.StartNew();
            double lastTime = 0;
            while (running)
            {
                while (!localPackets.IsEmpty && localPackets.TryDequeue(out var local))
                    LocalPlayer.ProcessPacket(local);
                Action a;
                if (worldRequests.Count > 0 && worldRequests.TryDequeue(out a))
                    a();
                //Start Loop
                var time = sw.Elapsed.TotalMilliseconds;
                var elapsed = (time - lastTime);
                if (elapsed < 2) continue;
                elapsed /= 1000f;
                lastTime = time;
                //Update
                LocalPlayer?.UpdateMissionRuntime(TimeSpan.FromSeconds(elapsed));
                ConcurrentBag<StarSystem> toSpinDown = new ConcurrentBag<StarSystem>();
                Parallel.ForEach(worlds, (world) =>
                {
                    if(!world.Value.Update(TimeSpan.FromSeconds(elapsed)))
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
                //Sleep
                Thread.Sleep(0);
            }
            Listener?.Stop();
        }
    }
}