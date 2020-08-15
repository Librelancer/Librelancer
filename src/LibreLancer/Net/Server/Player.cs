// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using LibreLancer.GameData.Items;
using LibreLancer.Entities.Character;

namespace LibreLancer
{
    public class Player
    {
        //ID
        public int ID = 0;
        static int _gid = 0;
        //Reference
        public IPacketClient Client;
        GameServer game;
        public ServerWorld World;
        private MissionRuntime msnRuntime;
        //State
        public NetCharacter Character;
        public string Name = "Player";
        public string System;
        public string Base;
        public Vector3 Position;
        public Quaternion Orientation;
        //Store so we can choose the correct character from the index
        public List<SelectableCharacter> CharacterList;
       
        Guid playerGuid; //:)
        
        public Player(IPacketClient client, GameServer game, Guid playerGuid)
        {
            this.Client = client;
            this.game = game;
            this.playerGuid = playerGuid;
            ID = Interlocked.Increment(ref _gid);
        }

        public void UpdateMissionRuntime(TimeSpan elapsed)
        {
            msnRuntime?.Update(elapsed);
            if (World != null)
            {
                while (worldActions.Count > 0)
                    worldActions.Dequeue()();
            }
        }

        List<string> rtcs = new List<string>();
        public void AddRTC(string rtc)
        {
            lock (rtcs)
            {
                rtcs.Add(rtc);
                Client.SendPacket(new UpdateRTCPacket() { RTCs = rtcs.ToArray()}, PacketDeliveryMethod.ReliableOrdered);
            }
        }

        public void RemoveRTC(string rtc)
        {
            lock (rtcs)
            {
                rtcs.Remove(rtc);
                Client.SendPacket(new UpdateRTCPacket() { RTCs = rtcs.ToArray()}, PacketDeliveryMethod.ReliableOrdered);
            }
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
                lock (rtcs)
                {
                    Client.SendPacket(new BaseEnterPacket()
                    {
                        Base = Base,
                        Ship = Character.EncodeLoadout(),
                        RTCs = rtcs.ToArray()
                    }, PacketDeliveryMethod.ReliableOrdered);
                }
                InitStory(sg);
            }
            else
            {
                var sys = game.GameData.GetSystem(System);
                game.RequestWorld(sys, (world) =>
                {
                    World = world; 
                    Client.SendPacket(new SpawnPlayerPacket()
                    {
                        System = System,
                        Position = Position,
                        Orientation = Orientation,
                        Ship = Character.EncodeLoadout()
                    }, PacketDeliveryMethod.ReliableOrdered);
                    world.SpawnPlayer(this, Position, Orientation);
                    //work around race condition where world spawns after player has been sent to a base
                    InitStory(sg);
                });
            }
            
        }

        void InitStory(Data.Save.SaveGame sg)
        {
            var missionNum = sg.StoryInfo?.MissionNum ?? 0;
            if (game.GameData.Ini.ContentDll.AlwaysMission13) missionNum = 14;
            if (missionNum != 0 && (missionNum - 1) < game.GameData.Ini.Missions.Count)
            {
                msnRuntime = new MissionRuntime(game.GameData.Ini.Missions[missionNum - 1], this);
                msnRuntime.Update(TimeSpan.Zero);
            }
        }

        private Queue<Action> worldActions = new Queue<Action>();
        public void WorldAction(Action a)
        {
            worldActions.Enqueue(a);
        }

        public void DoAuthSuccess()
        {
            try
            {
                FLLog.Info("Server", "Account logged in");
                Client.SendPacket(new LoginSuccessPacket(), PacketDeliveryMethod.ReliableOrdered);
                CharacterList = game.Database.PlayerLogin(playerGuid);
                Client.SendPacket(new OpenCharacterListPacket()
                {
                    Info = new CharacterSelectInfo()
                    {
                        ServerName = game.ServerName,
                        ServerDescription = game.ServerDescription,
                        ServerNews = game.ServerNews,
                        Characters = CharacterList,
                    }
                }, PacketDeliveryMethod.ReliableOrdered);
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
            Client.SendPacket(update, PacketDeliveryMethod.SequenceA);
        }

        public void SpawnPlayer(Player p)
        {
            Client.SendPacket(new SpawnObjectPacket()
            {
                ID = p.ID,
                Name = p.Name,
                Position = p.Position,
                Orientation = p.Orientation,
                Loadout = p.Character.EncodeLoadout()
            }, PacketDeliveryMethod.ReliableOrdered);
        }

        public void SendSolars(Dictionary<string, GameObject> solars)
        {
            var pkt = new SpawnSolarPacket() {Solars = new List<SolarInfo>()};
            foreach (var solar in solars)
            {
                var tr = solar.Value.GetTransform();
                pkt.Solars.Add(new SolarInfo()
                {
                    ID = solar.Value.NetID,
                    Archetype = solar.Value.ArchetypeName,
                    Position = Vector3.Transform(Vector3.Zero, tr),
                    Orientation = tr.ExtractRotation()
                });
            }
            Client.SendPacket(pkt, PacketDeliveryMethod.ReliableOrdered);
        }
        
        public void SendDestroyPart(int id, string part)
        {
            Client.SendPacket(new DestroyPartPacket()
            {
                ID = id,
                PartName = part
            }, PacketDeliveryMethod.ReliableOrdered);
        }
        
        public void ProcessPacket(IPacket packet)
        {
            try
            {
                switch(packet)
                {
                    case CharacterListActionPacket c:
                        ListAction(c);
                        break;
                    case LaunchPacket l:
                        Launch();
                        break;
                    case EnterLocationPacket lc:
                        msnRuntime?.EnterLocation(lc.Room, lc.Base);
                        break;
                    case PositionUpdatePacket p:
                        World.PositionUpdate(this, p.Position, p.Orientation);
                        break;
                    case RTCCompletePacket cp:
                        RemoveRTC(cp.RTC);
                        break;
                    case LineSpokenPacket lp:
                        msnRuntime?.LineFinished(lp.Hash);
                        break;
                    case ConsoleCommandPacket cmd:
                        HandleConsoleCommand(cmd.Command);
                        break;
                }
            }
            catch (Exception e)
            {
                FLLog.Exception("Player", e);
                throw;
            }
          
        }

        public void HandleConsoleCommand(string cmd)
        {
            if (cmd.StartsWith("base", StringComparison.OrdinalIgnoreCase)) {
                ForceLand(cmd.Substring(4).Trim());
            }
        }
        void ListAction(CharacterListActionPacket pkt)
        {
            switch(pkt.Action)
            {
                case CharacterListAction.RequestCharacterDB:
                {
                    Client.SendPacket(new NewCharacterDBPacket()
                    {
                        Factions = game.GameData.Ini.NewCharDB.Factions,
                        Packages = game.GameData.Ini.NewCharDB.Packages,
                        Pilots = game.GameData.Ini.NewCharDB.Pilots
                    }, PacketDeliveryMethod.ReliableOrdered);
                    break;
                }
                case CharacterListAction.SelectCharacter:
                {
                    if (pkt.IntArg > 0 && pkt.IntArg < CharacterList.Count)
                    {
                        var sc = CharacterList[pkt.IntArg];
                        FLLog.Info("Server", $"opening id {sc.Id}");
                        Character = NetCharacter.FromDb(sc.Id, game);
                        FLLog.Info("Server", $"sending packet");
                        Base = Character.Base;
                        Client.SendPacket(new BaseEnterPacket()
                        {
                            Base = Character.Base,
                            Ship = Character.EncodeLoadout()
                        }, PacketDeliveryMethod.ReliableOrdered);
                    }
                    else
                    {
                        Client.SendPacket(new CharacterListActionResponsePacket()
                        {
                            Action = CharacterListAction.SelectCharacter,
                            Status = CharacterListStatus.ErrBadIndex
                        }, PacketDeliveryMethod.ReliableOrdered);
                    }
                    break;
                }
                case CharacterListAction.DeleteCharacter:
                {
                    var sc = CharacterList[pkt.IntArg];
                    game.Database.DeleteCharacter(sc.Id);
                    CharacterList.Remove(sc);
                    Client.SendPacket(new CharacterListActionResponsePacket()
                    {
                        Action = CharacterListAction.DeleteCharacter,
                        Status = CharacterListStatus.OK
                    }, PacketDeliveryMethod.ReliableOrdered);
                    break;
                }
                case CharacterListAction.CreateNewCharacter:
                {
                    if (!game.Database.NameInUse(pkt.StringArg))
                    {
                        Character ch = null;
                        game.Database.AddCharacter(playerGuid, (db) =>
                        {
                            ch = db;
                            var sg = game.NewCharacter(pkt.StringArg, pkt.IntArg);
                            db.Name = sg.Player.Name;
                            db.Base = sg.Player.Base;
                            db.System = sg.Player.System;
                            db.Rank = 1;
                            db.Costume = sg.Player.Costume;
                            db.ComCostume = sg.Player.ComCostume;
                            db.Money = sg.Player.Money;
                            db.Ship = sg.Player.ShipArchetype;
                            db.Equipment = new HashSet<EquipmentEntity>();
                            db.Cargo = new HashSet<CargoItem>();
                            foreach (var eq in sg.Player.Equip)
                            {
                                db.Equipment.Add(new EquipmentEntity()
                                {
                                    EquipmentNickname = eq.EquipName,
                                    EquipmentHardpoint = eq.Hardpoint
                                });
                            }
                            foreach (var cg in sg.Player.Cargo)
                            {
                                db.Cargo.Add(new CargoItem()
                                {
                                    ItemName = cg.CargoName,
                                    ItemCount = cg.Count
                                });
                            }
                        });
                        var sel = NetCharacter.FromDb(ch.Id, game).ToSelectable();
                        CharacterList.Add(sel);
                        Client.SendPacket(new AddCharacterPacket()
                        {
                            Character = sel
                        }, PacketDeliveryMethod.ReliableOrdered);
                    } else
                    {
                        Client.SendPacket(new CharacterListActionResponsePacket()
                        {
                            Action = CharacterListAction.CreateNewCharacter,
                            Status = CharacterListStatus.ErrUnknown
                        }, PacketDeliveryMethod.ReliableOrdered);
                    }
                    break;
                }
            }
        }

        public void SpawnDebris(int id, string archetype, string part, Matrix4x4 tr, float mass)
        {
            Client.SendPacket(new SpawnDebrisPacket()
            {
                ID = id,
                Archetype =  archetype,
                Part = part,
                Mass = mass,
                Orientation =  tr.ExtractRotation(),
                Position = Vector3.Transform(Vector3.Zero, tr)
            }, PacketDeliveryMethod.ReliableOrdered);
        }
        public void ForceLand(string target)
        {
            World?.RemovePlayer(this);
            World = null;
            Base = target;
            Client.SendPacket(new BaseEnterPacket()
            {
                Base = Base,
                Ship = Character.EncodeLoadout(),
                RTCs = rtcs.ToArray()
            }, PacketDeliveryMethod.ReliableOrdered);
        }
        
        public void Despawn(int objId)
        {
            Client.SendPacket(new DespawnObjectPacket() { ID = objId }, PacketDeliveryMethod.ReliableOrdered);
        }
        
        public void Disconnected()
        {
            World?.RemovePlayer(this);
        }
        
        public void PlaySound(string sound)
        {
            Client.SendPacket(new PlaySoundPacket() { Sound = sound }, PacketDeliveryMethod.ReliableOrdered);
        }

        public void PlayMusic(string music)
        {
            Client.SendPacket(new PlayMusicPacket() {Music = music }, PacketDeliveryMethod.ReliableOrdered);
        }

        public void PlayDialog(NetDlgLine[] dialog)
        {
            Client.SendPacket(new MsnDialogPacket() { Lines = dialog }, PacketDeliveryMethod.ReliableOrdered);
        }
        public void CallThorn(string thorn)
        {
            Client.SendPacket(new CallThornPacket() { Thorn = thorn }, PacketDeliveryMethod.ReliableOrdered);
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
                    Orientation = (obj.Rotation ?? Matrix4x4.Identity).ExtractRotation();
                    Position = Vector3.Transform(new Vector3(0, 0, 500), Orientation) + obj.Position; //TODO: This is bad
                }
                Client.SendPacket(new SpawnPlayerPacket()
                {
                    System = System,
                    Position = Position,
                    Orientation = Orientation,
                    Ship = Character.EncodeLoadout()
                }, PacketDeliveryMethod.ReliableOrdered);
                world.SpawnPlayer(this, Position, Orientation);
                msnRuntime?.EnteredSpace();
            });
        }
    }
}