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
using System.Threading;
using Lidgren.Network;
namespace LibreLancer
{
	public class GameClient : IDisposable
	{
		bool running = false;
		IUIThread mainThread;
		Thread networkThread;
		NetClient client;
		public event Action<LocalServerInfo> ServerFound;

		public GameClient(IUIThread mainThread)
		{
			this.mainThread = mainThread;
		}

		public void Start()
		{
			running = true;
			networkThread = new Thread(NetworkThread);
			networkThread.Start();
		}

		public void Stop()
		{
			running = false;
			networkThread.Join();
		}

		public void Dispose()
		{
			if (running) Stop();
		}

		public void DiscoverLocalPeers()
		{
			if (running)
			{
				while (client == null) Thread.Sleep(0);
				client.DiscoverLocalPeers(NetConstants.DEFAULT_PORT);
			}
		}

		public void DiscoverGlobalPeers()
		{
			//HTTP?
		}


		void NetworkThread()
		{
			var conf = new NetPeerConfiguration(NetConstants.DEFAULT_APP_IDENT);
			client = new NetClient(conf);
			client.Start();
			NetIncomingMessage im;
			while (running)
			{
				while ((im = client.ReadMessage()) != null)
				{
					try
					{
						switch (im.MessageType)
						{
							case NetIncomingMessageType.DebugMessage:
							case NetIncomingMessageType.ErrorMessage:
							case NetIncomingMessageType.WarningMessage:
							case NetIncomingMessageType.VerboseDebugMessage:
								FLLog.Info("Lidgren", im.ReadString());
								break;
							case NetIncomingMessageType.DiscoveryResponse:
								if (ServerFound != null)
								{
									var info = new LocalServerInfo();
									info.EndPoint = im.SenderEndPoint;
									info.Name = im.ReadString();
									info.CurrentPlayers = im.ReadInt32();
									info.MaxPlayers = im.ReadInt32();
									mainThread.QueueUIThread(() => ServerFound(info));
								}
								break;
						}
					}
					catch (Exception)
					{
						FLLog.Error("Net", "Error reading message of type " + im.MessageType.ToString());
					}

				}
				Thread.Sleep(0);
			}
			FLLog.Info("Lidgren", "Client shutdown");
			client.Shutdown("Shutdown");
		}
	}
}
