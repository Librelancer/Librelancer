// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using LibreLancer.Net.Protocol;
using LibreLancer.Resources;
using LibreLancer.Server;

namespace LibreLancer.Net
{
    public class EmbeddedServer : IPacketConnection
    {
        public GameServer Server;
        public LocalPacketClient Client;

        //Hardcoded delay for single player.
        public uint EstimateTickDelay() => 2;

        public int MaxSequencedSize => int.MaxValue;

        public EmbeddedServer(GameDataManager gameData, GameResourceManager resources, string saveFolder)
        {
            Client = new LocalPacketClient();
            Server = new GameServer(gameData, resources.ConvexCollection);
            Server.ScriptsFolder = Path.Combine(saveFolder, "scripts");
            Server.LocalPlayer = new Player(Client, Server, Guid.Empty);
            Server.ConnectedPlayers.Add(Server.LocalPlayer);
            Server.LocalPlayer.SaveFolder = saveFolder;
        }

        public void StartFromSave(string path, byte[] save)
        {
            var sg = Data.Schema.Save.SaveGame.FromBytes(path, save);
            //This starts the simulation + packet sending
            Server.Start();
            Server.LoadSaveGame(sg);
        }

        public string Save(string description, bool autosave)
        {
            var t = Server.LocalPlayer.SaveSP(description, autosave ? 1628 : 0, autosave, DateTime.Now);
            t.Wait();
            return t.Result;
        }

        public void SendPacket(IPacket packet, PacketDeliveryMethod method)
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
