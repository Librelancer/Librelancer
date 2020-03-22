// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Threading;
using Lidgren.Network;

namespace LibreLancer
{
	public class GameListener
    {
        private GameServer game;
		static readonly object TagConnecting = new object();

        public int Port = NetConstants.DEFAULT_PORT;
		public string AppIdentifier = NetConstants.DEFAULT_APP_IDENT;
        
        private bool running = false;
		Thread netThread;
		Thread gameThread;
		public NetServer NetServer;

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
            var netconf = new NetPeerConfiguration(AppIdentifier);
            netconf.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
            netconf.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            netconf.EnableMessageType(NetIncomingMessageType.UnconnectedData);
            netconf.DualStack = true;
            netconf.Port = Port;
            netconf.MaximumConnections = 200;
            NetServer = new NetServer(netconf);
            NetServer.Start();
            FLLog.Info("Server", "Listening on port " + Port);
            NetIncomingMessage im;
            while (running)
            {
                while ((im = NetServer.ReadMessage()) != null)
                {
                    switch (im.MessageType)
                    {
                        case NetIncomingMessageType.DebugMessage:
                        case NetIncomingMessageType.ErrorMessage:
                        case NetIncomingMessageType.WarningMessage:
                        case NetIncomingMessageType.VerboseDebugMessage:
                            FLLog.Info("Lidgren", im.ReadString());
                            NetServer.Recycle(im);
                            break;
                        case NetIncomingMessageType.ConnectionApproval:
                            //Ban IP?
                            im.SenderConnection.Approve();
                            NetServer.Recycle(im);
                            break;
                        case NetIncomingMessageType.DiscoveryRequest:
                            NetOutgoingMessage dresp = NetServer.CreateMessage();
                            //Include Server Data
                            dresp.Write(game.ServerName);
                            dresp.Write(game.ServerDescription);
                            dresp.Write(game.GameData.DataVersion);
                            dresp.Write(NetServer.ConnectionsCount);
                            dresp.Write(NetServer.Configuration.MaximumConnections);
                            //Send off
                            NetServer.SendDiscoveryResponse(dresp, im.SenderEndPoint);
                            NetServer.Recycle(im);
                            break;
                        case NetIncomingMessageType.UnconnectedData:
                            //Respond to pings
                            try
                            {
                                if (im.ReadUInt32() == NetConstants.PING_MAGIC)
                                {
                                    var om = NetServer.CreateMessage();
                                    om.Write(NetConstants.PING_MAGIC);
                                    NetServer.SendUnconnectedMessage(om, im.SenderEndPoint);
                                }
                            }
                            finally
                            {
                                NetServer.Recycle(im);
                            }
                            break;
                        case NetIncomingMessageType.StatusChanged:
                            NetConnectionStatus status = (NetConnectionStatus) im.ReadByte();
                            string reason = im.ReadString();
                            FLLog.Info("Lidgren",
                                NetUtility.ToHexString(im.SenderConnection.RemoteUniqueIdentifier) + " " + status +
                                ": " + reason);
                            if (status == NetConnectionStatus.Connected)
                            {
                                FLLog.Info("Lidgren",
                                    "Remote hail: " + im.SenderConnection.RemoteHailMessage.ReadString());
                                BeginAuthentication(NetServer, im.SenderConnection);
                            }
                            else if (status == NetConnectionStatus.Disconnected)
                            {
                                FLLog.Info("Lidgren", im.SenderEndPoint.ToString() + " disconnected");
                                if (im.SenderConnection.Tag is Player player)
                                {
                                    player.Disconnected();
                                    lock (game.ConnectedPlayers)
                                    {
                                        game.ConnectedPlayers.Remove(player);
                                    }
                                }
                            }
                            NetServer.Recycle(im);
                            break;
                        case NetIncomingMessageType.Data:
                            IPacket pkt;
                            try
                            {
                                pkt = im.ReadPacket();
                            }
                            catch (Exception)
                            {
                                pkt = null;
                                im.SenderConnection.Disconnect("Malformed Packet");
                                if (im.SenderConnection.Tag is Player)
                                    ((Player) im.SenderConnection.Tag).Disconnected();
                            }
                            if (pkt != null)
                            {
                                if (im.SenderConnection.Tag == TagConnecting)
                                {
                                    if (pkt is AuthenticationReplyPacket)
                                    {
                                        var auth = (AuthenticationReplyPacket) pkt;
                                        var p = new Player(new RemotePacketClient(im.SenderConnection, NetServer),
                                            game, auth.Guid);
                                        im.SenderConnection.Tag = p;
                                        AsyncManager.RunTask(() => p.DoAuthSuccess());
                                        lock (game.ConnectedPlayers)
                                        {
                                            game.ConnectedPlayers.Add(p);
                                        }
                                    }
                                    else
                                    {
                                        im.SenderConnection.Disconnect("Invalid Packet");
                                    }
                                    NetServer.Recycle(im);
                                }
                                else
                                {
                                    var player = (Player) im.SenderConnection.Tag;
                                    AsyncManager.RunTask(() => player.ProcessPacket(pkt));
                                    NetServer.Recycle(im);
                                }
                            }
                            break;
                    }
                }
                Thread.Sleep(0); //Reduce CPU load
            }
            NetServer.Shutdown("Shutdown");
        }

        void BeginAuthentication(NetServer server, NetConnection connection)
		{
			var msg = server.CreateMessage();
            msg.Write(new AuthenticationPacket()
            {
                Type = AuthenticationKind.GUID
            });
			server.SendMessage(msg, connection, NetDeliveryMethod.ReliableOrdered);
			connection.Tag = TagConnecting;
		}

		public void Stop()
		{
			running = false;
			netThread.Join();
			gameThread.Join();
		}
	}
}
