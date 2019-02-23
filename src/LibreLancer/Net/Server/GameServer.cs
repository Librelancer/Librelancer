// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Lidgren.Network;
namespace LibreLancer
{
	public class GameServer
	{
		static readonly object TagConnecting = new object();

		public string ServerName = "Librelancer Server";
		public string ServerDescription = "Description of the server is here.";
		public string ServerNews = "News of the server goes here";

		public int Port = NetConstants.DEFAULT_PORT;
		public string AppIdentifier = NetConstants.DEFAULT_APP_IDENT;
		public string DbConnectionString;
		public GameDataManager GameData;
		public ServerDatabase Database;

		volatile bool running = false;
		Thread netThread;
		Thread gameThread;
		public NetServer NetServer;

		public GameServer(string fldir)
		{
			GameData = new GameDataManager(fldir, null);	
		}

		public void Start()
		{
			running = true;
			gameThread = new Thread(GameThread);
            gameThread.Name = "Game";
            gameThread.Start();
			netThread = new Thread(NetThread);
            netThread.Name = "NetServer";
            netThread.Start();
		}

		void GameThread()
		{
			Stopwatch sw = Stopwatch.StartNew();
			double lastTime = 0;
			while (running)
			{
				//Start Loop
				var time = sw.Elapsed.TotalMilliseconds;
				var elapsed = (time - lastTime) / 1000f;
				//Update

				//Sleep
				var endTime = sw.Elapsed.TotalMilliseconds;
				var sleepTime = (int)((1 / 60f * 1000) - (endTime - time));
				if (sleepTime > 0)
					Thread.Sleep(sleepTime);
				lastTime = endTime;
			}
		}

		void NetThread()
		{
			FLLog.Info("Server","Loading Game Data...");
			GameData.LoadData();
			FLLog.Info("Server","Finished Loading Game Data");
			Database = new ServerDatabase(DbConnectionString, GameData);
			var netconf = new NetPeerConfiguration(AppIdentifier);
			netconf.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
			netconf.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
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
							dresp.Write(ServerName);
							dresp.Write(ServerDescription);
                            dresp.Write(GameData.DataVersion);
							dresp.Write(NetServer.ConnectionsCount);
							dresp.Write(NetServer.Configuration.MaximumConnections);
							//Send off
							NetServer.SendDiscoveryResponse(dresp, im.SenderEndPoint);
							NetServer.Recycle(im);
							break;
						case NetIncomingMessageType.StatusChanged:
							NetConnectionStatus status = (NetConnectionStatus)im.ReadByte();

							string reason = im.ReadString();
							FLLog.Info("Lidgren", NetUtility.ToHexString(im.SenderConnection.RemoteUniqueIdentifier) + " " + status + ": " + reason);

							if (status == NetConnectionStatus.Connected)
							{
								FLLog.Info("Lidgren", "Remote hail: " + im.SenderConnection.RemoteHailMessage.ReadString());
								BeginAuthentication(NetServer, im.SenderConnection);
							}
							NetServer.Recycle(im);
							break;
						case NetIncomingMessageType.Data:
							var kind = (PacketKind)im.ReadByte();
							if (im.SenderConnection.Tag == TagConnecting)
							{
								if (kind == PacketKind.Authentication)
								{
                                    im.SenderConnection.Disconnect("boilerplate reason from server");
                                    /*
									var authkind = (AuthenticationKind)im.ReadByte();
									var guid = new Guid(im.ReadBytes(16));
									if (guid == Guid.Empty) im.SenderConnection.Disconnect("Invalid UUID");
									FLLog.Info("Lidgren", "GUID for " + im.SenderEndPoint + " = " + guid.ToString());
									var p = new NetPlayer(im.SenderConnection, this, guid);
									im.SenderConnection.Tag = p;
									AsyncManager.RunTask(() => p.DoAuthSuccess());*/
								}
								else
								{
									im.SenderConnection.Disconnect("Invalid Packet");
								}
							}
							else
							{
								var player = (NetPlayer)im.SenderConnection.Tag;
								AsyncManager.RunTask(() => player.ProcessPacket(im, kind));
							}
 							break;
					}
				}
				Thread.Sleep(1); //Reduce CPU load
			}
			Database.Dispose();
		}

        void UpdatePlayerPosition()
        {
            
        }
		void BeginAuthentication(NetServer server, NetConnection connection)
		{
			var msg = server.CreateMessage();
			msg.Write((byte)PacketKind.Authentication);
			msg.Write((byte)AuthenticationKind.GUID);
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
