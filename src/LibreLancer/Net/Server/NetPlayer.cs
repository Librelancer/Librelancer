// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using Lidgren.Network;
namespace LibreLancer
{
	public class NetPlayer
	{
		NetConnection connection;
		GameServer server;
		Guid playerGuid;
		PlayerAccount account;

        public Vector3 Position;
        public Quaternion Orientation;
        public int ID = 0;

		public NetPlayer(NetConnection conn, GameServer server, Guid playerGuid)
		{
			connection = conn;
			this.server = server;
			this.playerGuid = playerGuid;
		}

		public void DoAuthSuccess()
		{
			try
			{
				//Basically start up everything
				if ((account = server.Database.GetAccount(playerGuid)) != null)
				{
					server.Database.AccountAccessed(account);
				}
				else
				{
					account = new PlayerAccount();
					account.GUID = playerGuid;
					account.LastVisit = DateTime.Now;
					server.Database.CreateAccount(account);
				}

				//Get character list to send

				//Continue on
				var om = server.NetServer.CreateMessage();
				om.Write((byte)PacketKind.AuthenticationSuccess);
				om.Write(server.ServerNews);
				var ls = server.Database.GetOwnedCharacters(account).ToList();
				om.Write((int)ls.Count);
				foreach (var character in ls)
				{
					om.Write(character.ID);
					om.Write(character.Name);
					om.Write(character.Location);
					om.Write(character.Credits);
				}
				server.NetServer.SendMessage(om, connection, NetDeliveryMethod.ReliableOrdered);
			}
			catch (Exception ex)
			{
				FLLog.Error("NetPlayer",ex.Message);
				FLLog.Error("NetPlayer",
				            ex.StackTrace);
			}

		}

		public void ProcessPacket(NetIncomingMessage im, PacketKind kind)
		{
			switch (kind)
			{
				case PacketKind.NewCharacter:
					var om = server.NetServer.CreateMessage();
					om.Write((byte)PacketKind.NewCharacter);
					om.Write((byte)1); //allowed (1 byte as Lidgren packs bools)
					om.Write(1000); //Credits
					server.NetServer.SendMessage(om, connection, NetDeliveryMethod.ReliableOrdered);
					break;
			}
			server.NetServer.Recycle(im);
		}
	}
}
