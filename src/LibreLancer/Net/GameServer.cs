/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2017
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Diagnostics;
using System.Threading;
using Lidgren.Network;
namespace LibreLancer
{
	public class GameServer
	{
		public string ServerName = "Librelancer Server";
		public int Port = NetConstants.DEFAULT_PORT;
		public string AppIdentifier = NetConstants.DEFAULT_APP_IDENT;
		public LegacyGameData GameData;

		volatile bool running = false;
		Thread netThread;
		Thread gameThread;
		NetServer srv;

		public GameServer(string fldir)
		{
			GameData = new LegacyGameData(fldir, null);	
		}

		public void Start()
		{
			running = true;
			gameThread = new Thread(GameThread);
			gameThread.Start();
			netThread = new Thread(NetThread);
			netThread.Start();
		}

		void GameThread()
		{
			Stopwatch sw = Stopwatch.StartNew();
			double lastTime = 0;
			int i = 0;
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
			var netconf = new NetPeerConfiguration(AppIdentifier);
			netconf.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
			netconf.Port = Port;
			netconf.MaximumConnections = 200;
			srv = new NetServer(netconf);
			srv.Start();
			FLLog.Info("Server", "Listening on port " + Port);
			NetIncomingMessage im;
			while (running)
			{
				while ((im = srv.ReadMessage()) != null)
				{
					switch (im.MessageType)
					{
						case NetIncomingMessageType.DebugMessage:
						case NetIncomingMessageType.ErrorMessage:
						case NetIncomingMessageType.WarningMessage:
						case NetIncomingMessageType.VerboseDebugMessage:
							FLLog.Info("Lidgren", im.ReadString());
							srv.Recycle(im);
							break;
						case NetIncomingMessageType.DiscoveryRequest:
							NetOutgoingMessage dresp = srv.CreateMessage();
							//Include Server Data
							dresp.Write(ServerName);
							dresp.Write(srv.ConnectionsCount);
							dresp.Write(srv.Configuration.MaximumConnections);
							//Send off
							srv.SendDiscoveryResponse(dresp, im.SenderEndPoint);
							srv.Recycle(im);
							break;
						case NetIncomingMessageType.StatusChanged:
							NetConnectionStatus status = (NetConnectionStatus)im.ReadByte();

							string reason = im.ReadString();
							FLLog.Info("Lidgren", NetUtility.ToHexString(im.SenderConnection.RemoteUniqueIdentifier) + " " + status + ": " + reason);

							if (status == NetConnectionStatus.Connected)
								FLLog.Info("Lidgren", "Remote hail: " + im.SenderConnection.RemoteHailMessage.ReadString());
							srv.Recycle(im);
							break;
					}
				}
				Thread.Sleep(0); //Reduce CPU load
			}
		}

		public void Stop()
		{
			running = false;
			netThread.Join();
			gameThread.Join();
		}
	}
}
