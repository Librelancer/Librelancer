// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
namespace LibreLancer
{
    public class ServerWorld
    {
        public Dictionary<NetPlayer, GameObject> Players = new Dictionary<NetPlayer, GameObject>();
        ConcurrentQueue<Action> actions = new ConcurrentQueue<Action>();
        public GameWorld GameWorld;
        public GameServer Server;
        public GameData.StarSystem System;

        public ServerWorld(GameData.StarSystem system, GameServer server)
        {
            Server = server;
            System = system;
            GameWorld = new GameWorld(null);
            GameWorld.PhysicsUpdate += GameWorld_PhysicsUpdate;
            GameWorld.LoadSystem(system, server.Resources);
        }

        public void SpawnPlayer(NetPlayer player, Vector3 position, Quaternion orientation)
        {
            actions.Enqueue(() =>
            {
                foreach (var p in Players)
                {
                    player.SpawnPlayer(p.Key);
                    p.Key.SpawnPlayer(player);
                }
                var obj = new GameObject() { World = GameWorld };
                GameWorld.Objects.Add(obj);
                Players[player] = obj;
                Players[player].Transform = Matrix4.CreateFromQuaternion(orientation) * Matrix4.CreateTranslation(position);
            });
        }

        public void RemovePlayer(NetPlayer player)
        {
            actions.Enqueue(() =>
            {
                GameWorld.Objects.Remove(Players[player]);
                Players.Remove(player);
                foreach(var p in Players)
                {
                    p.Key.Despawn(player);
                }
            });
        }

        public void PositionUpdate(NetPlayer player, Vector3 position, Quaternion orientation)
        {
            actions.Enqueue(() =>
            {
                Players[player].Transform = Matrix4.CreateFromQuaternion(orientation) * Matrix4.CreateTranslation(position);
            });
        }

        public void Update(TimeSpan delta)
        {
            //Avoid locks during Update
            Action act;
            while(actions.Count > 0 && actions.TryDequeue(out act)){ act(); }
            //Update
            GameWorld.Update(delta);
        }

        const double UPDATE_RATE = 1 / 30.0;
        double current = 0;
        void GameWorld_PhysicsUpdate(TimeSpan delta)
        {
            current += delta.TotalSeconds;
            if (current >= UPDATE_RATE)
            {
                current = 0;
                SendPositionUpdates();
            }
        }

        //This could do with some work
        void SendPositionUpdates()
        {
            foreach(var player in Players)
            {
                var tr = player.Value.GetTransform();
                player.Key.Position = tr.Transform(Vector3.Zero);
                player.Key.Orientation = tr.ExtractRotation();
            }
            foreach (var player in Players)
            {
                List<PackedShipUpdate> ps = new List<PackedShipUpdate>();
                foreach (var otherPlayer in Players)
                {
                    if (otherPlayer.Key == player.Key) continue;
                    var update = new PackedShipUpdate();
                    update.ID = otherPlayer.Key.ID;
                    update.HasPosition = true;
                    update.Position = otherPlayer.Key.Position;
                    update.HasOrientation = true;
                    update.Orientation = otherPlayer.Key.Orientation;
                    ps.Add(update);
                }
                player.Key.SendUpdate(new ObjectUpdatePacket()
                {
                    Updates = ps.ToArray()
                });
            }
        }
    }
}
