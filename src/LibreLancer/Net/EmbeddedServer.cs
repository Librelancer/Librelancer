// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LiteNetLib;

namespace LibreLancer
{
    public class EmbeddedServer : IPacketConnection
    {
        public GameServer Server;
        public LocalPacketClient Client;

        public EmbeddedServer(GameDataManager gameData)
        {
            Client = new LocalPacketClient();
            Server = new GameServer(gameData);
            Server.LocalPlayer = new Player(Client, Server, Guid.Empty);
        }
        
        public void StartFromSave(string path)
        {
            var sg = Data.Save.SaveGame.FromFile(path);
            //This starts the simulation + packet sending
            Server.Start();
            Server.LocalPlayer.OpenSaveGame(sg);
        }
        
        public void SendPacket(IPacket packet, DeliveryMethod method)
        {
            #if DEBUG
            Packets.CheckRegistered(packet);
            #endif
            Server.OnLocalPacket(packet);
        }

        public void Shutdown()
        {
            Server.Stop();
        }

        public bool PollPacket(out IPacket packet)
        {
            if (!Client.Packets.IsEmpty)
            {
                return Client.Packets.TryDequeue(out packet);
            }
            packet = null;
            return false;
        }
    }
}