// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using LibreLancer.Net;
using LiteNetLib;
using LiteNetLib.Utils;

namespace LibreLancer
{
	public class GameListener
    {
        private GameServer game;
		static readonly object TagConnecting = new object();

        public int Port = LNetConst.DEFAULT_PORT;
        public int MaxConnections = 200;
        public string AppIdentifier = LNetConst.DEFAULT_APP_IDENT;
        
        private bool running = false;
		Thread netThread;
		public NetManager Server;

		public GameListener(GameServer srv)
        {
            this.game = srv;
        }

		public void Start()
		{
			running = true;
            netThread = new Thread(NetThread);
            netThread.Name = "Server Listener";
            netThread.Start();
		}

        void NetThread()
        {
            EventBasedNetListener listener = new EventBasedNetListener();
            int unique = Environment.TickCount;
            listener.ConnectionRequestEvent += request =>
            {
                if (Server.ConnectedPeersCount > MaxConnections) request.Reject();
                else request.AcceptIfKey(AppIdentifier + GeneratedProtocol.PROTOCOL_HASH);
            };
            listener.PeerConnectedEvent += peer =>
            {
                FLLog.Info("Server", $"Connected: {peer.EndPoint}");
                BeginAuthentication(peer);
            };
            listener.PeerDisconnectedEvent += (peer, info) =>
            {
                FLLog.Info("Server", $"Disconnected: {peer.EndPoint}");
                if (peer.Tag is Player player)
                {
                    player.Disconnected();
                    lock (game.ConnectedPlayers)
                    {
                        game.ConnectedPlayers.Remove(player);
                    }
                }
            };
            listener.NetworkReceiveUnconnectedEvent += (point, reader, type) =>
            {
                try
                {
                    if (type == UnconnectedMessageType.Broadcast)
                    {
                        reader.TryGetULong(out ulong key);
                        if (key != LNetConst.BROADCAST_KEY) return;
                        var dw = new NetDataWriter();
                        dw.Put((int) 1);
                        dw.Put(unique);
                        dw.PutStringPacked(game.ServerName);
                        dw.PutStringPacked(game.ServerDescription);
                        dw.PutStringPacked(game.GameData.DataVersion);
                        dw.Put(Server.ConnectedPeersCount);
                        dw.Put(MaxConnections);
                        Server.SendUnconnectedMessage(dw, point);

                    } else if (type == UnconnectedMessageType.BasicMessage)
                    {
                        if (!reader.TryGetUInt(out uint magic)) return;
                        if (magic != LNetConst.PING_MAGIC) return;
                        var dw = new NetDataWriter();
                        dw.Put((int) 0);
                        Server.SendUnconnectedMessage(dw, point);
                    }
                }
                finally
                {
                    reader.Recycle();
                }
            };
            listener.NetworkReceiveEvent += (peer, reader, channel, method) =>
            {
                try
                {
                    var pkt = Packets.Read(reader);
                    if (peer.Tag == TagConnecting)
                    {
                        if (pkt is AuthenticationReplyPacket)
                        {
                            var auth = (AuthenticationReplyPacket) pkt;
                            if (auth.Guid == Guid.Empty)
                            {
                                var dw = new NetDataWriter();
                                dw.Put("bad GUID");
                                peer.Disconnect(dw);
                            }
                            else
                            {
                                var p = new Player(new RemotePacketClient(peer),
                                    game, auth.Guid);
                                peer.Tag = p;
                                Task.Run(() => p.DoAuthSuccess());
                                lock (game.ConnectedPlayers)
                                {
                                    game.ConnectedPlayers.Add(p);
                                }
                            }
                        }
                        else
                        {
                            var dw = new NetDataWriter();
                            dw.Put("Invalid packet");
                            peer.Disconnect(dw);
                        }
                    }
                    else
                    {
                        var player = (Player) peer.Tag;
                        player.EnqueuePacket(pkt);
                    }
                }
                #if !DEBUG
                catch (Exception e)
                {
                    FLLog.Warning("Server", $"Error when reading packet {e}");
                    var dw = new NetDataWriter();
                    dw.Put("Packet processing error");
                    peer.Disconnect(dw);
                    if (peer.Tag is Player p)
                        p.Disconnected();
                }
                #endif
                finally
                {
                    reader.Recycle();
                }
            };
            listener.DeliveryEvent += (peer, data) =>
            {
                if (data is Action onAck)
                    onAck();
            };
            Server = new NetManager(listener);
            Server.IPv6Mode = IPv6Mode.SeparateSocket;
            Server.UnconnectedMessagesEnabled = true;
            Server.BroadcastReceiveEnabled = true;
            Server.ChannelsCount = 3;
            Server.UnsyncedEvents = true;
            Server.Start(Port);
            FLLog.Info("Server", "Listening on port " + Port);
            var sw = Stopwatch.StartNew();
            var last = 0.0;
            ServerLoop sendLoop = null;
            sendLoop = new ServerLoop((time,totalTime) =>
            {
                foreach (var p in Server.ConnectedPeerList)
                {
                    if (p.Tag is Player player)
                    {
                        player.ProcessPacketQueue();
                        (player.Client as RemotePacketClient)?.Update(time.TotalSeconds);
                    }
                }

                if (!running) sendLoop.Stop();
            });
            sendLoop.Start();
            Server.Stop();
        }
        
        void BeginAuthentication(NetPeer peer)
        {
            var msg = new NetDataWriter();
            msg.Put((byte)1);
            Packets.Write(msg, new AuthenticationPacket()
            {
                Type = AuthenticationKind.GUID
            });
            peer.Send(msg, DeliveryMethod.ReliableOrdered);
			peer.Tag = TagConnecting;
		}

		public void Stop()
		{
			running = false;
			netThread.Join();
		}
	}
}
