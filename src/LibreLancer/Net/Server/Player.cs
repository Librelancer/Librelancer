// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LibreLancer.GameData.Items;
using Lidgren.Network;
namespace LibreLancer
{
    public class Player
    {
        private IPacketClient client;
        GameServer game;
        Guid playerGuid;
        public NetCharacter Character;
        public PlayerAccount Account;
        public string Name = "Player";
        public string System;
        public string Base;
        public Vector3 Position;
        public Quaternion Orientation;
        public ServerWorld World;
        private MissionRuntime msnRuntime;
        public int ID = 0;
        static int _gid = 0;

        public Player(IPacketClient client, GameServer game, Guid playerGuid)
        {
            this.client = client;
            this.game = game;
            this.playerGuid = playerGuid;
            ID = Interlocked.Increment(ref _gid);
        }

        public void UpdateMissionRuntime(TimeSpan elapsed)
        {
            msnRuntime?.Update(elapsed);
        }
        public void OpenSaveGame(Data.Save.SaveGame sg)
        {
            Orientation = Quaternion.Identity;
            Position = sg.Player.Position;
            Base = sg.Player.Base;
            System = sg.Player.System;
            Character = new NetCharacter();
            Character.Credits = sg.Player.Money;
            string ps;
            if (sg.Player.ShipArchetype != null)
                ps = sg.Player.ShipArchetype;
            else
                ps = game.GameData.GetShip(sg.Player.ShipArchetypeCrc).Nickname;
            Character.Ship = game.GameData.GetShip(ps);
            Character.Equipment = new List<NetEquipment>();
            foreach (var eq in sg.Player.Equip)
            {
                var hp = eq.Hardpoint;
                Equipment equip;
                if (eq.EquipName != null) equip = game.GameData.GetEquipment(eq.EquipName);
                else equip = game.GameData.GetEquipment(eq.EquipHash);
                if (equip != null)
                {
                    Character.Equipment.Add(new NetEquipment()
                    {
                        Equipment = equip, Hardpoint = hp, Health = 1
                    });
                }
            }
            if (Base != null)
            {
                client.SendPacket(new BaseEnterPacket()
                {
                    Base = Base,
                    Ship = Character.EncodeLoadout()
                }, NetDeliveryMethod.ReliableOrdered);
            }
            else
            {
                var sys = game.GameData.GetSystem(System);
                game.RequestWorld(sys, (world) =>
                {
                    World = world; 
                    client.SendPacket(new SpawnPlayerPacket()
                    {
                        System = System,
                        Position = Position,
                        Orientation = Orientation,
                        Ship = Character.EncodeLoadout()
                    }, NetDeliveryMethod.ReliableOrdered);
                    world.SpawnPlayer(this, Position, Orientation);
                });
            }
            var missionNum = sg.StoryInfo?.MissionNum ?? 0;
            if (game.GameData.Ini.ContentDll.AlwaysMission13) missionNum = 14;
            if (missionNum != 0 && (missionNum - 1) < game.GameData.Ini.Missions.Count)
            {
                msnRuntime = new MissionRuntime(game.GameData.Ini.Missions[missionNum - 1], this);
                msnRuntime.Update(TimeSpan.Zero);
            }
        }

        public void DoAuthSuccess()
        {
            try
            {
                client.SendPacket(new LoginSuccessPacket(), NetDeliveryMethod.ReliableOrdered);
                Account = new PlayerAccount();
                client.SendPacket(new OpenCharacterListPacket()
                {
                    Info = new CharacterSelectInfo()
                    {
                        ServerName = game.ServerName,
                        ServerDescription = game.ServerDescription,
                        ServerNews = game.ServerNews,
                        Characters = new List<SelectableCharacter>()
                    }
                }, NetDeliveryMethod.ReliableOrdered);
            }
            catch (Exception ex)
            {
                FLLog.Error("Player",ex.Message);
                FLLog.Error("Player",
                    ex.StackTrace);
            }
        }

        public void SendUpdate(ObjectUpdatePacket update)
        {
            client.SendPacket(update, NetDeliveryMethod.UnreliableSequenced);
        }

        public void SpawnPlayer(Player p)
        {
            client.SendPacket(new SpawnObjectPacket()
            {
                ID = p.ID,
                Name = p.Name,
                Position = p.Position,
                Orientation = p.Orientation,
                Loadout = p.Character.EncodeLoadout()
            }, NetDeliveryMethod.ReliableOrdered);
        }


        public void ProcessPacket(IPacket packet)
        {
            FLLog.Info("Server", "Got packet of type " + packet.GetType());
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
                case LineSpokenPacket lp:
                    msnRuntime?.LineFinished(lp.Hash);
                    break;
            }
        }

        void ListAction(CharacterListActionPacket pkt)
        {
            switch(pkt.Action)
            {
                case CharacterListAction.RequestCharacterDB:
                {
                    client.SendPacket(new NewCharacterDBPacket()
                    {
                        Factions = game.GameData.Ini.NewCharDB.Factions,
                        Packages = game.GameData.Ini.NewCharDB.Packages,
                        Pilots = game.GameData.Ini.NewCharDB.Pilots
                    }, NetDeliveryMethod.ReliableOrdered);
                    break;
                }
                case CharacterListAction.SelectCharacter:
                {
                    var sc = Account.Characters[pkt.IntArg];
                    Character = NetCharacter.FromDb(sc, game.GameData);
                    Base = Character.Base;
                    client.SendPacket(new BaseEnterPacket()
                    {
                        Base = Character.Base,
                        Ship = Character.EncodeLoadout()
                    }, NetDeliveryMethod.ReliableOrdered);
                    break;
                }
                case CharacterListAction.CreateNewCharacter:
                {
                    var ac = new ServerCharacter()
                    {
                        Name = pkt.StringArg,
                        Base = "li01_01_base",
                        Credits = 2000,
                        ID = 0,
                        Ship = "ge_fighter"
                    };
                    Account.Characters.Add(ac);
                    client.SendPacket(new AddCharacterPacket()
                    {
                        Character = NetCharacter.FromDb(ac, game.GameData).ToSelectable()
                    }, NetDeliveryMethod.ReliableOrdered);
                    break;
                }
            }
        }

        public void ForceLand(string target)
        {
            World?.RemovePlayer(this);
            Base = target;
            client.SendPacket(new BaseEnterPacket()
            {
                Base = Base,
                Ship = Character.EncodeLoadout()
            }, NetDeliveryMethod.ReliableOrdered);
        }
        public void Despawn(Player player)
        {
            client.SendPacket(new DespawnObjectPacket() { ID = player.ID }, NetDeliveryMethod.ReliableOrdered);
        }
        
        public void Disconnected()
        {
            World?.RemovePlayer(this);
        }
        
        public void PlaySound(string sound)
        {
            client.SendPacket(new PlaySoundPacket() { Sound = sound }, NetDeliveryMethod.ReliableOrdered);
        }

        public void PlayMusic(string music)
        {
            client.SendPacket(new PlayMusicPacket() {Music = music }, NetDeliveryMethod.ReliableOrdered);
        }

        public void PlayDialog(NetDlgLine[] dialog)
        {
            client.SendPacket(new MsnDialogPacket() { Lines = dialog }, NetDeliveryMethod.ReliableOrdered);
        }
        public void CallThorn(string thorn)
        {
            client.SendPacket(new CallThornPacket() { Thorn = thorn }, NetDeliveryMethod.ReliableOrdered);
        }
        
        void Launch()
        {
            var b = game.GameData.GetBase(Base);
            var sys = game.GameData.GetSystem(b.System);
            game.RequestWorld(sys, (world) =>
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
                client.SendPacket(new SpawnPlayerPacket()
                {
                    System = System,
                    Position = Position,
                    Orientation = Orientation,
                    Ship = Character.EncodeLoadout()
                }, NetDeliveryMethod.ReliableOrdered);
                world.SpawnPlayer(this, Position, Orientation);
            });
        }
    }
}