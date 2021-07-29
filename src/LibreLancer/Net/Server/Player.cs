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
                PlayerEnterBase();
                InitStory(sg);
            }
            else
            {
                var sys = game.GameData.GetSystem(System);
                game.RequestWorld(sys, (world) =>
                {
                    World = world; 
                    rpcClient.SpawnPlayer(System, Position, Orientation, Character.EncodeLoadout());
                    world.SpawnPlayer(this, Position, Orientation);
                    //work around race condition where world spawns after player has been sent to a base
                    InitStory(sg);
                });
            }
            
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
            //send to player
            lock (rtcs)
            {
                rpcClient.BaseEnter(Base, Character.EncodeLoadout(), rtcs.ToArray(), news.ToArray(), BaseData.SoldGoods.Select(x => new SoldGood()
                {
                    GoodCRC = CrcTool.FLModelCrc(x.Good.Ini.Nickname),
                    Price = x.Price,
                    Rank = x.Rank,
                    Rep = x.Rep
                }).ToArray());
            }
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
            rpcClient.SpawnObject(p.ID, p.Name, p.Position, p.Orientation, p.Character.EncodeLoadout());
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
                        World?.PositionUpdate(this, p.Position, p.Orientation);
                        break;
                }
            }
            catch (Exception e)
            {
                FLLog.Exception("Player", e);
                throw;
            }
          
        }

        void IServerPlayer.ConsoleCommand(string cmd)
        {
            if (cmd.StartsWith("base", StringComparison.OrdinalIgnoreCase)) {
                ForceLand(cmd.Substring(4).Trim());
            }
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
                rpcClient.UpdateBaselinePrices(game.BaselineGoodPrices);
                Base = Character.Base;
                PlayerEnterBase();
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
            World?.RemovePlayer(this);
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
                    FLLog.Error("Base", "Can't find object in " + sys + " docking to " + b);
                }
                else
                {
                    Position = obj.Position;
                    Orientation = (obj.Rotation ?? Matrix4x4.Identity).ExtractRotation();
                    Position = Vector3.Transform(new Vector3(0, 0, 500), Orientation) + obj.Position; //TODO: This is bad
                }
                rpcClient.SpawnPlayer(System, Position, Orientation, Character.EncodeLoadout());
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