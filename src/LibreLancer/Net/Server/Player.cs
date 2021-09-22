// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LibreLancer.Data.Save;
using LibreLancer.GameData.Items;
using LibreLancer.Entities.Character;
using LibreLancer.Net;
using Microsoft.EntityFrameworkCore.Storage;

namespace LibreLancer
{
    public class Player : IServerPlayer, INetResponder
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
        public GameData.Base BaseData;
        public Vector3 Position;
        public Quaternion Orientation;
        //Store so we can choose the correct character from the index
        public List<SelectableCharacter> CharacterList;
       
        Guid playerGuid; //:)

        public NetResponseHandler ResponseHandler;

        private RemoteClientPlayer rpcClient;

        public RemoteClientPlayer RemoteClient => rpcClient;
        
        public Player(IPacketClient client, GameServer game, Guid playerGuid)
        {
            this.Client = client;
            this.game = game;
            this.playerGuid = playerGuid;
            ID = Interlocked.Increment(ref _gid);
            ResponseHandler = new NetResponseHandler();
            rpcClient = new RemoteClientPlayer(this);
        }

        public void UpdateMissionRuntime(double elapsed)
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
                rpcClient.UpdateRTCs(rtcs.ToArray());
            }
        }

        void IServerPlayer.RTCComplete(string rtc)
        {
            lock (rtcs)
            {
                rtcs.Remove(rtc);
                rpcClient.UpdateRTCs(rtcs.ToArray());
            }
        }

        void IServerPlayer.LineSpoken(uint hash)
        {
            msnRuntime?.LineFinished(hash);
        }

        void IServerPlayer.OnLocationEnter(string _base, string room)
        {
            msnRuntime?.EnterLocation(room, _base);
        }

        void IServerPlayer.FireProjectiles(ProjectileSpawn[] projectiles)
        {
            World?.FireProjectiles(projectiles, this);
        }

        public void OpenSaveGame(SaveGame sg)
        {
            Orientation = Quaternion.Identity;
            Position = sg.Player.Position;
            Base = sg.Player.Base;
            System = sg.Player.System;
            Character = new NetCharacter();
            Character.UpdateCredits(sg.Player.Money);
            string ps;
            if (sg.Player.ShipArchetype != null)
                ps = sg.Player.ShipArchetype;
            else
                ps = game.GameData.GetShip(sg.Player.ShipArchetypeCrc).Nickname;
            Character.Ship = game.GameData.GetShip(ps);
            Character.Equipment = new List<NetEquipment>();
            Character.Cargo = new List<NetCargo>();
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
            foreach (var cg in sg.Player.Cargo)
            {
                Equipment equip;
                if (cg.CargoName != null) equip = game.GameData.GetEquipment(cg.CargoName);
                else equip = game.GameData.GetEquipment(cg.CargoHash);
                if (equip != null)
                {
                    Character.Cargo.Add(new NetCargo()
                    {
                        Equipment = equip, Count = cg.Count
                    });
                }
            }
            if (Base != null)
            {
                PlayerEnterBase();
                InitStory(sg);
            }
            else
            {
                SpaceInitialSpawn(sg);
            }
            
        }

        void SpaceInitialSpawn(SaveGame sg)
        {
            var sys = game.GameData.GetSystem(System);
            game.RequestWorld(sys, (world) =>
            {
                World = world; 
                rpcClient.SpawnPlayer(System, world.TotalTime, Position, Orientation, Character.Credits, Character.EncodeLoadout());
                world.SpawnPlayer(this, Position, Orientation);
                //work around race condition where world spawns after player has been sent to a base
                if(sg != null) InitStory(sg);
            });
        }
        bool NewsFind(LibreLancer.Data.Missions.NewsItem ni)
        {
            if (ni.Rank[1] != "mission_end")
                return false;
            foreach(var x in ni.Base)
                if (x.Equals(Base, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
        
        void PlayerEnterBase()
        {
            //fetch news articles
            List<NewsArticle> news = new List<NewsArticle>();
            foreach (var x in game.GameData.Ini.News.NewsItems.Where(NewsFind))
            {
                news.Add(new NewsArticle()
                {
                    Icon = x.Icon, Category = x.Category, Headline =  x.Headline,
                    Logo = x.Logo, Text = x.Text
                });
            }
            //load base
            BaseData = game.GameData.GetBase(Base);
            //update
            Character.UpdatePosition(Base, System, Position);
            //send to player
            lock (rtcs)
            {
                rpcClient.BaseEnter(Base, Character.Credits, Character.EncodeLoadout(), rtcs.ToArray(), news.ToArray(), BaseData.SoldGoods.Select(x => new SoldGood()
                {
                    GoodCRC = CrcTool.FLModelCrc(x.Good.Ini.Nickname),
                    Price = x.Price,
                    Rank = x.Rank,
                    Rep = x.Rep,
                    ForSale = x.ForSale
                }).ToArray());
            }
        }

        async Task<bool> IServerPlayer.PurchaseGood(string item, int count)
        {
            if (BaseData == null) return false;
            var g = BaseData.SoldGoods.FirstOrDefault(x =>
                x.Good.Equipment.Nickname.Equals(item, StringComparison.OrdinalIgnoreCase));
            if (g == null) return false;
            var cost = (long) (g.Price * (ulong)count);
            if (Character.Credits >= cost)
            {
                Character.UpdateCredits(Character.Credits - cost);
                Character.AddCargo(g.Good.Equipment, count);
                rpcClient.UpdateInventory(Character.Credits, Character.EncodeLoadout());
                return true;
            }
            return false;
        }

        async Task<bool> IServerPlayer.SellGood(int id, int count)
        {
            if (BaseData == null)
            {
                FLLog.Error("Player", $"{Name} tried to sell good while in space");
                return false;
            }
            var slot = Character.Cargo.FirstOrDefault(x => x.ID == id);
            if (slot == null)
            {
                FLLog.Error("Player", $"{Name} tried to sell unknown slot {id}");
                return false;
            }
            if (slot.Count < count)
            {
                FLLog.Error("Player", $"{Name} tried to oversell slot");
                return false;
            }
            var g = BaseData.SoldGoods.FirstOrDefault(x =>
                x.Good.Equipment.Nickname.Equals(slot.Equipment.Nickname, StringComparison.OrdinalIgnoreCase));
            ulong unitPrice;
            if (g != null) {
                unitPrice = g.Price;
            }
            else {
                unitPrice = (ulong)slot.Equipment.Good.Ini.Price;
            }

            Character.RemoveCargo(slot, count);
            Character.UpdateCredits(Character.Credits + (long) ((ulong) count * unitPrice));
            rpcClient.UpdateInventory(Character.Credits, Character.EncodeLoadout());
            return true;
        }
        
        void InitStory(Data.Save.SaveGame sg)
        {
            var missionNum = sg.StoryInfo?.MissionNum ?? 0;
            if (game.GameData.Ini.ContentDll.AlwaysMission13) missionNum = 14;
            if (missionNum != 0 && (missionNum - 1) < game.GameData.Ini.Missions.Count)
            {
                msnRuntime = new MissionRuntime(game.GameData.Ini.Missions[missionNum - 1], this);
                msnRuntime.Update(0.0);
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
            var lO = p.Character.EncodeLoadout();
            lO.Health = p.Character.Ship.Hitpoints;
            rpcClient.SpawnObject(p.ID, p.Name, p.Position, p.Orientation, lO);
        }

        public void SendSolars(Dictionary<string, GameObject> solars)
        {
            var si = new List<SolarInfo>();
            foreach (var solar in solars)
            {
                var tr = solar.Value.WorldTransform;
                si.Add(new SolarInfo()
                {
                    ID = solar.Value.NetID,
                    Archetype = solar.Value.ArchetypeName,
                    Position = Vector3.Transform(Vector3.Zero, tr),
                    Orientation = tr.ExtractRotation()
                });
            }
            rpcClient.SpawnSolar(si.ToArray());
        }
        
        public void SendDestroyPart(int id, string part)
        {
            rpcClient.DestroyPart(0, id, part);
        }
        
        public void ProcessPacket(IPacket packet)
        {
            if(ResponseHandler.HandlePacket(packet))
                return;
            try
            {
                var hsp = GeneratedProtocol.HandleServerPacket(packet, this, this);
                hsp.Wait();
                if (hsp.Result)
                    return;
            }
            catch (Exception e)
            {
                FLLog.Exception("Player", e);
                throw;
            }
            try
            {
                switch(packet)
                {
                    case PositionUpdatePacket p:
                        //TODO: Error handling
                        World?.PositionUpdate(this, p.Position, p.Orientation, p.Speed);
                        break;
                }
            }
            catch (Exception e)
            {
                FLLog.Exception("Player", e);
                throw;
            }
          
        }

        class ConsoleCommand
        {
            public string Cmd;
            public Action<string> Action;
            public ConsoleCommand(string cmd, Action<string> act)
            {
                this.Cmd = cmd;
                this.Action = act;
            }
        }

        private ConsoleCommand[] Commands;

        void InitCommands()
        {
            Commands = new ConsoleCommand[]
            {
                new("base", (arg) =>
                {
                    if(game.GameData.BaseExists(arg))
                        ForceLand(arg);
                    else 
                        rpcClient.OnConsoleMessage($"Base does not exist `{arg}`");
                }),
                new("credits", (x) => rpcClient.OnConsoleMessage($"You have ${Character.Credits}")),
                new("sethealth", (arg) =>
                {
                    if (int.TryParse(arg, out var h))
                    {
                        if (Client is LocalPacketClient)
                        {
                            if (World != null) {
                                World.EnqueueAction(() => {
                                    World.Players[this].GetComponent<HealthComponent>().CurrentHealth = h;
                                    rpcClient.OnConsoleMessage("OK");
                                });
                            }
                        }
                        else
                        {
                            rpcClient.OnConsoleMessage("Permission denied");
                        }
                    }
                    else
                    {
                        rpcClient.OnConsoleMessage("Invalid argument");
                    }
                })
            };
        }

        void IServerPlayer.ConsoleCommand(string cmd)
        {
            if (Commands == null) InitCommands();
            foreach (var x in Commands)
            {
                if (cmd.StartsWith(x.Cmd, StringComparison.OrdinalIgnoreCase))
                {
                    var arg = cmd.Substring(x.Cmd.Length).Trim();
                    x.Action(arg);
                    return;
                }
            }
            rpcClient.OnConsoleMessage("Unrecognised command");
        }

        void IServerPlayer.RequestCharacterDB()
        {
            Client.SendPacket(new NewCharacterDBPacket()
            {
                Factions = game.GameData.Ini.NewCharDB.Factions,
                Packages = game.GameData.Ini.NewCharDB.Packages,
                Pilots = game.GameData.Ini.NewCharDB.Pilots
            }, PacketDeliveryMethod.ReliableOrdered);
        }

        async Task<bool> IServerPlayer.SelectCharacter(int index)
        {
            if (index >= 0 && index < CharacterList.Count)
            {
                var sc = CharacterList[index];
                FLLog.Info("Server", $"opening id {sc.Id}");
                Character = NetCharacter.FromDb(sc.Id, game);
                Name = Character.Name;
                rpcClient.UpdateBaselinePrices(game.BaselineGoodPrices);
                Base = Character.Base;
                System = Character.System;
                Position = Character.Position;
                if (Base != null) {
                    PlayerEnterBase();
                } else {
                    SpaceInitialSpawn(null);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        async Task<bool> IServerPlayer.DeleteCharacter(int index)
        {
            if (index < 0 || index >= CharacterList.Count)
                return false;
            var sc = CharacterList[index];
            game.Database.DeleteCharacter(sc.Id);
            CharacterList.Remove(sc);
            return true;
        }

        async Task<bool> IServerPlayer.CreateNewCharacter(string name, int index)
        {
            if (!game.Database.NameInUse(name))
            {
                Character ch = null;
                game.Database.AddCharacter(playerGuid, (db) =>
                {
                    ch = db;
                    var sg = game.NewCharacter(name, index);
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
                return true;
            } else {
                return false;
            }
        }

        public void SpawnDebris(int id, string archetype, string part, Matrix4x4 tr, float mass)
        {
            rpcClient.SpawnDebris(id, archetype, part, Vector3.Transform(Vector3.Zero, tr), tr.ExtractRotation(), mass);
        }
        
        public void ForceLand(string target)
        {
            World?.RemovePlayer(this);
            World = null;
            Base = target;
            PlayerEnterBase();
        }
        
        public void Despawn(int objId)
        {
            rpcClient.DespawnObject(objId);
        }
        
        public void Disconnected()
        {
            Character?.UpdatePosition(Base, System, Position);
            World?.RemovePlayer(this);
            Character?.Dispose();
        }
        
        public void PlaySound(string sound)
        {
            rpcClient.PlaySound(sound);
        }

        public void PlayMusic(string music)
        {
            rpcClient.PlayMusic(music);
        }

        public void PlayDialog(NetDlgLine[] dialog)
        {
            rpcClient.RunMissionDialog(dialog);
        }
        public void CallThorn(string thorn)
        {
            rpcClient.CallThorn(thorn);
        }

        public void JumpTo(string system, string target)
        {
            rpcClient.StartJumpTunnel();
            if(World != null) World.RemovePlayer(this);
            
            var sys = game.GameData.GetSystem(system);
            game.RequestWorld(sys, (world) =>
            {
                this.World = world;
                var obj = sys.Objects.FirstOrDefault((o) =>
                {
                    return o.Nickname.Equals(target, StringComparison.OrdinalIgnoreCase);
                });
                System = system;
                Base = null;
                Position = Vector3.Zero;
                if (obj == null) {
                    FLLog.Error("Server", $"Can't find target {target} to spawn player in {system}");
                }
                else {
                    Position = obj.Position;
                    Orientation = (obj.Rotation ?? Matrix4x4.Identity).ExtractRotation();
                    Position = Vector3.Transform(new Vector3(0, 0, 500), Orientation) + obj.Position; //TODO: This is bad
                }
                BaseData = null;
                Base = null;
                rpcClient.SpawnPlayer(System, world.TotalTime, Position, Orientation, Character.Credits, Character.EncodeLoadout());
                world.SpawnPlayer(this, Position, Orientation);
                msnRuntime?.EnteredSpace();
            });
        }

        void IServerPlayer.RequestDock(string nickname)
        {
            World.RequestDock(this, nickname);
        }

        void IServerPlayer.Launch()
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
                    FLLog.Error("Base", "Can't find object in " + sys.Nickname + " docking to " + b.Nickname);
                }
                else
                {
                    Position = obj.Position;
                    Orientation = (obj.Rotation ?? Matrix4x4.Identity).ExtractRotation();
                    Position = Vector3.Transform(new Vector3(0, 0, 500), Orientation) + obj.Position; //TODO: This is bad
                }
                BaseData = null;
                Base = null;
                rpcClient.SpawnPlayer(System, world.TotalTime, Position, Orientation, Character.Credits, Character.EncodeLoadout());
                world.SpawnPlayer(this, Position, Orientation);
                msnRuntime?.EnteredSpace();
            });
        }

        void INetResponder.SendResponse(IPacket packet)
        {
            Client.SendPacket(packet, PacketDeliveryMethod.ReliableOrdered);
        }
    }
}