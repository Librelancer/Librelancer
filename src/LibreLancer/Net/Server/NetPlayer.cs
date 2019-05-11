// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using Lidgren.Network;
namespace LibreLancer
{
    public class NetCharacter
    {
        public string Name;
        public string Base;
        public long Credits;
        public GameData.Ship Ship;
        public List<NetEquipment> Equipment;

        GameDataManager gData;
        public static NetCharacter FromDb(ServerCharacter character, GameDataManager gameData)
        {
            var nc = new NetCharacter();
            nc.Name = character.Name;
            nc.gData = gameData;
            nc.Base = character.Base;
            nc.Ship = gameData.GetShip(character.Ship);
            nc.Credits = character.Credits;
            nc.Equipment = new List<NetEquipment>(character.Equipment.Count);
            foreach(var equip in character.Equipment)
            {
                nc.Equipment.Add(new NetEquipment()
                {
                    Hardpoint = equip.Hardpoint,
                    Equipment = gameData.GetEquipment(equip.Equipment),
                    Health = equip.Health
                });
            }
            return nc;
        }

        public NetShipLoadout EncodeLoadout()
        {
            var sl = new NetShipLoadout();
            sl.ShipCRC = Ship.CRC;
            sl.Equipment = new List<NetShipEquip>(Equipment.Count);
            foreach(var equip in Equipment) {
                sl.Equipment.Add(new NetShipEquip(
                CrcTool.FLModelCrc(equip.Hardpoint),
                    equip.Equipment.CRC,
                (byte)(equip.Health * 255f))); 
            }
            return sl;
        }

        public SelectableCharacter ToSelectable()
        {
            var selectable = new SelectableCharacter();
            selectable.Rank = 1;
            selectable.Ship = Ship.Nickname;
            selectable.Name = Name;
            selectable.Funds = Credits;
            selectable.Location = gData.GetBase(Base).System;
            return selectable;
        }

    }

    public class NetEquipment
    {
        public string Hardpoint;
        public GameData.Items.Equipment Equipment;
        public float Health;
    }

    public class NetPlayer
	{
		NetConnection connection;
		GameServer server;
		Guid playerGuid;
		public NetCharacter Character;
        public PlayerAccount Account;
        public string Name = "Player";
        public string System;
        public string Base;
        public Vector3 Position;
        public Quaternion Orientation;
        public ServerWorld World;
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
                Account = new PlayerAccount();
                var m = server.NetServer.CreateMessage();
                m.Write(new OpenCharacterListPacket()
                {
                    Info = new CharacterSelectInfo()
                    {
                        ServerName = server.ServerName,
                        ServerDescription = server.ServerDescription,
                        ServerNews = server.ServerNews,
                        Characters = new List<SelectableCharacter>()
                    }
                });
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
                Orientation = p.Orientation,
                Loadout = p.Character.EncodeLoadout()
            });
            server.NetServer.SendMessage(m, connection, NetDeliveryMethod.ReliableOrdered);
        }


        public void ProcessPacket(IPacket packet)
		{
            switch(packet)
            {
                case CharacterListActionPacket c:
                    ListAction(c);
                    break;
                case LaunchPacket l:
                    Launch();
                    break;
                case PositionUpdatePacket p:
                    World.PositionUpdate(this, p.Position, p.Orientation);
                    break;
            }
        }

        void ListAction(CharacterListActionPacket pkt)
        {
            switch(pkt.Action)
            {
                case CharacterListAction.RequestCharacterDB:
                    {
                        var m = server.NetServer.CreateMessage();
                        m.Write(new NewCharacterDBPacket()
                        {
                            Factions = server.GameData.Ini.NewCharDB.Factions,
                            Packages = server.GameData.Ini.NewCharDB.Packages,
                            Pilots = server.GameData.Ini.NewCharDB.Pilots
                        });
                        server.NetServer.SendMessage(m, connection, NetDeliveryMethod.ReliableOrdered);
                        break;
                    }
                case CharacterListAction.SelectCharacter:
                    {
                        var m = server.NetServer.CreateMessage();
                        var sc = Account.Characters[pkt.IntArg];
                        Character = NetCharacter.FromDb(sc, server.GameData);
                        Base = Character.Base;
                        m.Write(new BaseEnterPacket()
                        {
                            Base = Character.Base,
                            Ship = Character.EncodeLoadout()
                        });
                        server.NetServer.SendMessage(m, connection, NetDeliveryMethod.ReliableOrdered);
                        break;
                    }
                case CharacterListAction.CreateNewCharacter:
                    {
                        var m = server.NetServer.CreateMessage();
                        var ac = new ServerCharacter()
                        {
                            Name = pkt.StringArg,
                            Base = "li01_01_base",
                            Credits = 2000,
                            ID = 0,
                            Ship = "ge_fighter"
                        };
                        Account.Characters.Add(ac);
                        m.Write(new AddCharacterPacket()
                        {
                            Character = NetCharacter.FromDb(ac, server.GameData).ToSelectable()
                        });
                        server.NetServer.SendMessage(m, connection, NetDeliveryMethod.ReliableOrdered);
                        break;
                    }
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
            if (World != null) World.RemovePlayer(this);
        }

        void Launch()
        {
            var b = server.GameData.GetBase(Base);
            var sys = server.GameData.GetSystem(b.System);
            server.RequestWorld(sys, (world) =>
            {
                this.World = world;
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
                    Orientation = Orientation,
                    Ship = Character.EncodeLoadout()
                });
                server.NetServer.SendMessage(msg, connection, NetDeliveryMethod.ReliableOrdered);
                world.SpawnPlayer(this, Position, Orientation);
            });
        }

    }
}
