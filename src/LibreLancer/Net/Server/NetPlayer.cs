// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
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

        public string Name = "Player";
        public string System;
        public string Base;
        public Vector3 Position;
        public Quaternion Orientation;
        public int ID = 0;

        static int _gid = 0;

		public NetPlayer(NetConnection conn, GameServer server, Guid playerGuid)
		{
			connection = conn;
			this.server = server;
			this.playerGuid = playerGuid;
            ID = _gid++;
		}

		public void DoAuthSuccess()
		{
			try
			{
                Base = "li01_01_base";
                var m = server.NetServer.CreateMessage();
                m.Write(new BaseEnterPacket() { Base = Base });
                server.NetServer.SendMessage(m, connection, NetDeliveryMethod.ReliableOrdered);
			
			}
			catch (Exception ex)
			{
				FLLog.Error("NetPlayer",ex.Message);
				FLLog.Error("NetPlayer",
				            ex.StackTrace);
			}

		}

        public void UpdatePosition(NetPlayer p)
        {
            var m = server.NetServer.CreateMessage();
            m.Write(new ObjectUpdatePacket()
            {
                ID = p.ID,
                Position = p.Position,
                Orientation = p.Orientation
            });
            server.NetServer.SendMessage(m, connection, NetDeliveryMethod.UnreliableSequenced);
        }

        public void SpawnPlayer(NetPlayer p)
        {
            var m = server.NetServer.CreateMessage();
            m.Write(new SpawnObjectPacket()
            {
                ID = p.ID,
                Name = p.Name,
                Position = p.Position,
                Orientation = p.Orientation
            });
            server.NetServer.SendMessage(m, connection, NetDeliveryMethod.ReliableOrdered);
        }


        public void ProcessPacket(IPacket packet)
		{
            switch(packet)
            {
                case LaunchPacket l:
                    Launch();
                    break;
                case PositionUpdatePacket p:
                    Position = p.Position;
                    Orientation = p.Orientation;
                    foreach (var n in GetPlayers())
                        n.UpdatePosition(this);
                    break;
            }
        }

        public void Despawn(NetPlayer player)
        {
            var m = server.NetServer.CreateMessage();
            m.Write(new DespawnObjectPacket()
            {
                ID = player.ID
            });
        }

        public void Disconnected()
        {
            foreach (var p in GetPlayers())
                p.Despawn(this);
        }

        IEnumerable<NetPlayer> GetPlayers() {
            return server.NetServer.Connections.Where(
                x => x.Tag != this &&
                (x.Tag is NetPlayer) &&
                ((NetPlayer)x.Tag).System.Equals(System, StringComparison.OrdinalIgnoreCase))
                .Select(x => (NetPlayer)x.Tag);
        }

        void Launch()
        {
            var b = server.GameData.GetBase(Base);
            var sys = server.GameData.GetSystem(b.System);
            var obj = sys.Objects.FirstOrDefault((o) =>
            {
                return (o.Dock != null &&
                    o.Dock.Kind == DockKinds.Base &&
                    o.Dock.Target.Equals(Base, StringComparison.OrdinalIgnoreCase));
            });
            System = b.System;
            Orientation = Quaternion.Identity;
            Position = Vector3.Zero;
            if (obj == null)
            {
                FLLog.Error("Base", "Can't find object in " + sys + " docking to " + b);
            }
            else
            {
                Position = obj.Position;
                Orientation = (obj.Rotation == null ? Matrix3.Identity : new Matrix3(obj.Rotation.Value)).ExtractRotation();
                Position = Vector3.Transform(new Vector3(0, 0, 500), Orientation) + obj.Position; //TODO: This is bad
            }

            var msg = server.NetServer.CreateMessage();
            msg.Write(new SpawnPlayerPacket()
            {
                System = System,
                Position = Position,
                Orientation = Orientation
            });
            server.NetServer.SendMessage(msg, connection, NetDeliveryMethod.ReliableOrdered);
            foreach(var p in GetPlayers())
            {
                SpawnPlayer(p);
                p.SpawnPlayer(this);
            }

        }

    }
}
