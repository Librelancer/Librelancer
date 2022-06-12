// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using LibreLancer.Data.Save;
using LibreLancer.GameData.Items;
using LibreLancer.Entities.Character;
using LibreLancer.GameData.Market;
using LibreLancer.Net;

namespace LibreLancer
{
    public class Player : IServerPlayer, INetResponder
    {
        //ID
        public int ID = 0;
        static int _gid = 0;

        //Reference
        public IPacketClient Client;
        public GameServer Game;
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
        //Respawn?
        public bool Dead = false;
       
        Guid playerGuid; //:)

        public NetResponseHandler ResponseHandler;

        private RemoteClientPlayer rpcClient;

        public RemoteClientPlayer RemoteClient => rpcClient;
        
        public Player(IPacketClient client, GameServer game, Guid playerGuid)
        {
            this.Client = client;
            this.Game = game;
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

        public bool InTradelane;
        public void StartTradelane()
        {
            rpcClient.StartTradelane();
            InTradelane = true;
        }

        public void EndTradelane()
        {
            rpcClient.EndTradelane();
            InTradelane = false;
        }

        public MissionRuntime MissionRuntime => msnRuntime;

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
                msnRuntime?.FinishRTC(rtc);
            }
        }

        void IServerPlayer.StoryNPCSelect(string name, string room, string _base)
        {
            msnRuntime?.StoryNPCSelect(name,room,_base);
        }
        

        void IServerPlayer.ClosedPopup(string id)
        {
            msnRuntime?.ClosePopup(id);
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

        string FirstAvailableHardpoint(string hptype)
        {
            if(string.IsNullOrWhiteSpace(hptype)) return null;
            if (!Character.Ship.PossibleHardpoints.TryGetValue(hptype, out var candidates))
                return null;
            foreach (var possible in candidates)
            {
                if(!Character.Items.Any(x => possible.Equals(x.Hardpoint, StringComparison.OrdinalIgnoreCase)))
                    return possible;
            }
            return null;
        }

        ulong GetShipWorth()
        {
            if(Character.Ship == null)
                return 0;
            return (ulong) (Game.GameData.GetShipPrice(Character.Ship) * TradeConstants.SHIP_RESALE_MULTIPLIER);
        }
        
        
        Task<bool> IServerPlayer.Unmount(string hardpoint)
        {
            if (BaseData == null) {
                FLLog.Error("Player", $"{Name} tried to unmount good while in space");
                return Task.FromResult(false);
            }
            var equip = Character.Items.FirstOrDefault(x =>
                hardpoint.Equals(x.Hardpoint, StringComparison.OrdinalIgnoreCase));
            if (equip == null) {
                FLLog.Error("Player", $"{Name} tried to unmount empty hardpoint");
                return Task.FromResult(false);
            }

            equip.Hardpoint = null;
            rpcClient.UpdateInventory(Character.Credits, GetShipWorth(), Character.EncodeLoadout());
            return Task.FromResult(true);
        }

        Task<bool> IServerPlayer.Mount(int id)
        {
            if (BaseData == null) {
                FLLog.Error("Player", $"{Name} tried to mount good while in space");
                return Task.FromResult(false);
            }
            var slot = Character.Items.FirstOrDefault(x => x.ID == id);
            if (slot == null) {
                FLLog.Error("Player", $"{Name} tried to mount unknown slot {id}");
                return Task.FromResult(false);
            }
            if (!string.IsNullOrEmpty(slot.Hardpoint))
            {
                FLLog.Error("Player", $"{Name} tried to mount already mounted item {id}");
                return Task.FromResult(false);
            }
            string hp = FirstAvailableHardpoint(slot.Equipment.HpType);
            if (hp == null) {
                FLLog.Error("Player", $"{Name} has no hp available to mount {slot.Equipment.Nickname} ({slot.Equipment.HpType})");
                return Task.FromResult(false);
            }
            slot.Hardpoint = hp;
            Character.ItemModified(slot);
            rpcClient.UpdateInventory(Character.Credits, GetShipWorth(), Character.EncodeLoadout());
            return Task.FromResult(true);
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
                ps = Game.GameData.GetShip(sg.Player.ShipArchetypeCrc).Nickname;
            Character.Ship = Game.GameData.GetShip(ps);
            Character.Items = new List<NetCargo>();
            foreach (var eq in sg.Player.Equip)
            {
                var hp = eq.Hardpoint;
                if (string.IsNullOrEmpty(hp)) hp = "internal";
                Equipment equip;
                if (eq.EquipName != null) equip = Game.GameData.GetEquipment(eq.EquipName);
                else equip = Game.GameData.GetEquipment(eq.EquipHash);
                if (equip != null)
                {
                    Character.Items.Add(new NetCargo()
                    {
                        Equipment = equip, Hardpoint = hp, Health = 1, Count = 1
                    });
                }
            }
            foreach (var cg in sg.Player.Cargo)
            {
                Equipment equip;
                if (cg.CargoName != null) equip = Game.GameData.GetEquipment(cg.CargoName);
                else equip = Game.GameData.GetEquipment(cg.CargoHash);
                if (equip != null)
                {
                    Character.Items.Add(new NetCargo()
                    {
                        Equipment = equip, Count = cg.Count
                    });
                }
            }
            rpcClient.UpdateInventory(Character.Credits, GetShipWorth(), Character.EncodeLoadout());
            if (Base != null)
            {
                InitStory(sg);
                PlayerEnterBase();
            }
            else
            {
                InitStory(sg);
                SpaceInitialSpawn(sg);
            }
            
        }

        void SpaceInitialSpawn(SaveGame sg)
        {
            var sys = Game.GameData.GetSystem(System);
            Game.RequestWorld(sys, (world) =>
            {
                World = world;
                world.EnqueueAction(() =>
                {
                    world.SpawnPlayer(this, Position, Orientation);
                    rpcClient.SpawnPlayer(System, 0, Position, Orientation);
                    msnRuntime?.EnteredSpace();
                });
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

        IEnumerable<NetSoldShip> GetSoldShips()
        {
            var b = Game.GameData.GetBase(Base);
            foreach (var s in b.SoldShips)
            {
                ulong goodsPrice = 0;
                foreach (var eq in s.Package.Addons)
                {
                    goodsPrice += (ulong) ((long)GetUnitPrice(eq.Equipment) * eq.Amount);
                }
                yield return new NetSoldShip()
                {
                    ShipCRC = (int) FLHash.CreateID(s.Package.Ship),
                    PackageCRC = (int)FLHash.CreateID(s.Package.Nickname),
                    HullPrice = (ulong) s.Package.BasePrice,
                    PackagePrice = (ulong)s.Package.BasePrice + goodsPrice
                };
            }
        }
        
        void PlayerEnterBase()
        {
            //fetch news articles
            List<NewsArticle> news = new List<NewsArticle>();
            foreach (var x in Game.GameData.Ini.News.NewsItems.Where(NewsFind))
            {
                news.Add(new NewsArticle()
                {
                    Icon = x.Icon, Category = x.Category, Headline =  x.Headline,
                    Logo = x.Logo, Text = x.Text
                });
            }
            //load base
            BaseData = Game.GameData.GetBase(Base);
            //update
            Character.UpdatePosition(Base, System, Position);
            //send to player
            lock (rtcs)
            {
                rpcClient.BaseEnter(Base, rtcs.ToArray(), news.ToArray(), BaseData.SoldGoods.Select(x => new SoldGood()
                {
                    GoodCRC = CrcTool.FLModelCrc(x.Good.Ini.Nickname),
                    Price = x.Price,
                    Rank = x.Rank,
                    Rep = x.Rep,
                    ForSale = x.ForSale
                }).ToArray(), GetSoldShips().ToArray());
            }
        }

        ulong GetUnitPrice(Equipment eq)
        {
            var g = BaseData.SoldGoods.FirstOrDefault(x =>
                x.Good.Equipment.Nickname.Equals(eq.Nickname, StringComparison.OrdinalIgnoreCase));
            if (g == null) {
                return (ulong) (eq.Good?.Ini?.Price ?? 0);
            }
            return g.Price;
        }

        Task<bool> IServerPlayer.PurchaseGood(string item, int count)
        {
            if (BaseData == null) return Task.FromResult(false);
            var g = BaseData.SoldGoods.FirstOrDefault(x =>
                x.Good.Equipment.Nickname.Equals(item, StringComparison.OrdinalIgnoreCase));
            if (g == null) return Task.FromResult(false);
            var cost = (long) (g.Price * (ulong)count);
            if (Character.Credits >= cost)
            {
                string hp;
                if (count == 1 && (hp = FirstAvailableHardpoint(g.Good.Equipment.HpType)) != null) {
                    Character.AddCargo(g.Good.Equipment, hp, 1);
                }
                else {
                    Character.AddCargo(g.Good.Equipment, null, count);
                }
                Character.UpdateCredits(Character.Credits - cost);
                rpcClient.UpdateInventory(Character.Credits, GetShipWorth(), Character.EncodeLoadout());
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        Task<bool> IServerPlayer.SellGood(int id, int count)
        {
            if (BaseData == null) {
                FLLog.Error("Player", $"{Name} tried to sell good while in space");
                return Task.FromResult(false);
            }
            var slot = Character.Items.FirstOrDefault(x => x.ID == id);
            if (slot == null)
            {
                FLLog.Error("Player", $"{Name} tried to sell unknown slot {id}");
                return Task.FromResult(false);
            }
            if (slot.Count < count)
            {
                FLLog.Error("Player", $"{Name} tried to oversell slot");
                return Task.FromResult(false);
            }
            ulong unitPrice = GetUnitPrice(slot.Equipment);
            if (slot.Equipment is not CommodityEquipment)
                unitPrice = (ulong) (unitPrice * TradeConstants.EQUIP_RESALE_MULTIPLIER);
            Character.RemoveCargo(slot, count);
            Character.UpdateCredits(Character.Credits + (long) ((ulong) count * unitPrice));
            rpcClient.UpdateInventory(Character.Credits, GetShipWorth(), Character.EncodeLoadout());
            return Task.FromResult(true);
        }

        private static readonly uint InternalCRC = CrcTool.FLModelCrc("internal");

        public Task<ShipPackageInfo> GetShipPackage(int package)
        {
            var resolved = Game.GameData.GetShipPackage((uint) package);
            if (resolved == null)
            {
                return Task.FromResult<ShipPackageInfo>(null);
            }
            var spi = new ShipPackageInfo();
            spi.Included = resolved.Addons.Select(x =>
            {
                return new IncludedGood()
                {
                    EquipCRC = x.Equipment.CRC,
                    HardpointCRC = string.IsNullOrWhiteSpace(x.Hardpoint) ? InternalCRC : CrcTool.FLModelCrc(x.Hardpoint),
                    Amount = x.Amount
                };
            }).ToArray();
            return Task.FromResult(spi);
        }

        public Task<ShipPurchaseStatus> PurchaseShip(int package, MountId[] mountedPlayer, MountId[] mountedPackage, SellCount[] sellPlayer,
            SellCount[] sellPackage)
        {
            var b = Game.GameData.GetBase(Base);
            var resolved = Game.GameData.GetShipPackage((uint) package);
            if (resolved == null) return Task.FromResult(ShipPurchaseStatus.Fail);
            if (b.SoldShips.All(x => x.Package != resolved)) {
                FLLog.Error("Player", $"{Name} tried to purchase ship package not available on base");
                return Task.FromResult(ShipPurchaseStatus.Fail);
            }
            var included = new List<PackageAddon>();
            foreach (var a in resolved.Addons)
                included.Add(new PackageAddon() {Equipment = a.Equipment, Amount = a.Amount});
            long shipPrice = resolved.BasePrice;
            //Sell included Items
            foreach (var item in sellPackage)
            {
                var a = included[item.ID];
                if (a == null) return Task.FromResult(ShipPurchaseStatus.Fail);
                if (item.Count > a.Amount) return Task.FromResult(ShipPurchaseStatus.Fail);
                var price = GetUnitPrice(a.Equipment);
                shipPrice -= (long)price * item.Count;
                a.Amount -= item.Count;
                if (a.Amount <= 0)
                    included[item.ID] = null;
            }
            if (shipPrice < 0) shipPrice = 0;
            //Deduct ship worth
            shipPrice -= (long)GetShipWorth();
            //Add price of rest of items
            foreach (var a in included)
            {
                if (a == null) continue;
                var price = GetUnitPrice(a.Equipment);
                shipPrice += (long)price * a.Amount;
            }
            Dictionary<int, int> counts = new Dictionary<int, int>();
            //Calculate player items price
            foreach (var item in sellPlayer)
            {
                var slot = Character.Items.FirstOrDefault(x => x.ID == item.ID);
                if (slot == null) {
                    FLLog.Error("Player", $"{Name} tried to sell unknown slot {item.ID}");
                    return Task.FromResult(ShipPurchaseStatus.Fail);
                }
                if (!counts.TryGetValue(slot.ID, out int count))
                    counts[slot.ID] = slot.Count;
                if (count < item.Count) {
                    FLLog.Error("Player", $"{Name} tried to oversell slot");
                    return Task.FromResult(ShipPurchaseStatus.Fail);
                }
                var price = GetUnitPrice(slot.Equipment);
                if (slot.Equipment is not CommodityEquipment)
                    price = (ulong) (price * TradeConstants.EQUIP_RESALE_MULTIPLIER);
                shipPrice -= (long)price * item.Count;
                counts[slot.ID] = (count - item.Count);
            }
            //Check if we have credits
            if (shipPrice > Character.Credits) {
                FLLog.Error("Player", $"{Name} does not have enough credits");
                return Task.FromResult(ShipPurchaseStatus.Fail);
            }
            //Check that all mounts are valid
            HashSet<int> mountedP = new HashSet<int>();
            HashSet<int> mountedInc = new HashSet<int>();
            HashSet<string> usedHardpoints = new HashSet<string>();
            foreach (var item in mountedPackage)
            {
                if (included[item.ID] == null) {
                    FLLog.Error("Player", $"{Name} tried to mount sold item");
                    return Task.FromResult(ShipPurchaseStatus.Fail);
                }
                var hp = item.Hardpoint.ToLowerInvariant();
                if (mountedInc.Contains(item.ID)) {
                    FLLog.Error("Player", $"{Name} tried to mount from package twice");
                    mountedInc.Add(item.ID);
                    return Task.FromResult(ShipPurchaseStatus.Fail);
                }
                if (hp != "internal" && usedHardpoints.Contains(hp)) {
                    FLLog.Error("Player", $"{Name} tried to mount to hardpoint {hp} twice");
                    return Task.FromResult(ShipPurchaseStatus.Fail);
                }  
                if (hp != "internal") {
                    usedHardpoints.Add(hp);
                }
            }
            foreach (var item in mountedPlayer)
            {
                var slot = Character.Items.FirstOrDefault(x => x.ID == item.ID);
                if (slot == null) {
                    FLLog.Error("Player", $"{Name} tried to mount non-existant item");
                    return Task.FromResult(ShipPurchaseStatus.Fail);
                }
                if (counts.TryGetValue(item.ID, out var nc) && nc == 0) {
                    FLLog.Error("Player", $"{Name} tried to mount sold item");
                    return Task.FromResult(ShipPurchaseStatus.Fail);
                }
                var hp = item.Hardpoint.ToLowerInvariant();
                if (mountedP.Contains(item.ID)) {
                    FLLog.Error("Player", $"{Name} tried to mount item twice");
                    mountedP.Add(item.ID);
                    return Task.FromResult(ShipPurchaseStatus.Fail);
                }
                if (hp != "internal" && usedHardpoints.Contains(hp)) {
                    FLLog.Error("Player", $"{Name} tried to mount to hardpoint {hp} twice");
                    return Task.FromResult(ShipPurchaseStatus.Fail);
                }  
                if (hp != "internal") {
                    usedHardpoints.Add(hp);
                }
            }
            //Remove sold items
            foreach (var item in counts) {
                var slot = Character.Items.FirstOrDefault(x => x.ID == item.Key);
                Character.RemoveCargo(slot, slot.Count - item.Value);
            }
            //Unmount items and remove items without a good
            List<NetCargo> toRemove = new List<NetCargo>();
            foreach (var item in Character.Items) {
                item.Hardpoint = null;
                if (item.DbItem != null) item.DbItem.Hardpoint = null;
                if(item.Equipment.Good == null)
                    toRemove.Add(item);
            }
            foreach(var item in toRemove)
                Character.RemoveCargo(item, item.Count);
            //Set Ship
            Character.SetShip(Game.GameData.GetShip(resolved.Ship));
            //Install new cargo and mount
            foreach (var item in mountedPlayer)
            {
                var slot = Character.Items.FirstOrDefault(x => x.ID == item.ID);
                slot.Hardpoint = item.Hardpoint;
                if (slot.DbItem != null) slot.DbItem.Hardpoint = item.Hardpoint;
            }
            foreach (var item in mountedPackage)
            {
                var inc = included[item.ID];
                Character.AddCargo(inc.Equipment, item.Hardpoint, inc.Amount);
                included[item.ID] = null;
            }
            foreach (var item in included)
            {
                if (item == null) continue;
                Character.AddCargo(item.Equipment, item.Equipment.Good == null ? item.Hardpoint : null, item.Amount);
            }
            Character.UpdateCredits(Character.Credits - shipPrice);
            rpcClient.UpdateInventory(Character.Credits, GetShipWorth(), Character.EncodeLoadout());
            //Success
            return Task.FromResult(shipPrice < 0 ? ShipPurchaseStatus.SuccessGainCredits : ShipPurchaseStatus.Success);
        }

        private Dictionary<string, int> missionNumbers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "mission_01a", 1 },
            { "mission_01b", 2 },
            { "mission_02", 3 },
            { "mission_03", 4 },
            { "mission_04", 5 },
            { "mission_05", 6 },
            { "mission_06", 7 },
            { "mission_07", 8 },
            { "mission_08", 9 },
            { "mission_09", 10 }, 
            { "mission_10", 11 },
            { "mission_11", 12 },
            { "mission_12", 13 },
            { "mission_13", 14 }
        };

        void MissionNumber(string str, ref int num)
        {
            if (!string.IsNullOrEmpty(str) && missionNumbers.TryGetValue(str, out var n))
                num = n;
        }
        void InitStory(Data.Save.SaveGame sg)
        {
            var missionNum = sg.StoryInfo?.MissionNum ?? 0;
            MissionNumber(sg.StoryInfo?.Mission, ref missionNum);
            if (Game.GameData.Ini.ContentDll.AlwaysMission13) missionNum = 14;
            if (missionNum != 0 && (missionNum - 1) < Game.GameData.Ini.Missions.Count)
            {
                msnRuntime = new MissionRuntime(Game.GameData.Ini.Missions[missionNum - 1], this);
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
                CharacterList = Game.Database.PlayerLogin(playerGuid);
                Client.SendPacket(new OpenCharacterListPacket()
                {
                    Info = new CharacterSelectInfo()
                    {
                        ServerName = Game.ServerName,
                        ServerDescription = Game.ServerDescription,
                        ServerNews = Game.ServerNews,
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

        private ConcurrentQueue<IPacket> inputPackets = new ConcurrentQueue<IPacket>();
        public void EnqueuePacket(IPacket packet)
        {
            inputPackets.Enqueue(packet);
        }

        public void ProcessPacketQueue()
        {
            while(inputPackets.TryDequeue(out var pkt)) ProcessPacketDirect(pkt);
        }
        
        public void ProcessPacketDirect(IPacket packet)
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
                    case InputUpdatePacket p:
                        //TODO: Error handling
                        World?.InputsUpdate(this, p);
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
                    if(Game.GameData.BaseExists(arg))
                        ForceLand(arg);
                    else 
                        rpcClient.OnConsoleMessage($"Base does not exist `{arg}`");
                }),
                new("warp", (arg) =>
                {
                    var argSplit = arg.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (argSplit.Length == 3)
                    {
                        if (PermissionCheck() && World != null)
                        {
                            if (float.TryParse(argSplit[0], out var x) &&
                                float.TryParse(argSplit[1], out var y) &&
                                float.TryParse(argSplit[2], out var z))
                            {
                                World.EnqueueAction(() =>
                                {
                                    var obj = World.Players[this];
                                    obj.SetLocalTransform(Matrix4x4.CreateTranslation(x,y,z));
                                });
                            } else
                                rpcClient.OnConsoleMessage("Invalid argument");
                        }
                    } else rpcClient.OnConsoleMessage("Invalid argument");
                }),
                new("credits", (x) => rpcClient.OnConsoleMessage($"You have ${Character.Credits}")),
                new("sethealth", (arg) =>
                {
                    if (PermissionCheck())
                    {
                        if (int.TryParse(arg, out var h))
                        {
                            if (World != null) {
                                World.EnqueueAction(() => {
                                    World.Players[this].GetComponent<SHealthComponent>().CurrentHealth = h;
                                    rpcClient.OnConsoleMessage("OK");
                                });
                            }
                        }
                        else
                        {
                            rpcClient.OnConsoleMessage("Invalid argument");
                        }
                    }
                }),
                new("npcspawn", (arg) =>
                {
                    if (PermissionCheck() && World != null)
                    {
                        if (World.Server.GameData.TryGetLoadout(arg.Trim(), out var ld))
                        {
                            World.NPCs.SpawnNPC(ld, Position + new Vector3(0, 0, 200)).ContinueWith(x =>
                            {
                                rpcClient.OnConsoleMessage($"ID = {x.Result}");
                            });
                        }
                        else
                        {
                            rpcClient.OnConsoleMessage($"Unknown loadout '{arg}'");
                        }
                    }
                }),
                new ("npcdock", (arg) =>
                {
                    var x = arg.Split(',');
                    if (PermissionCheck() && x.Length == 2 && int.TryParse(x[0].Trim(), out int netid) && World != null)
                    {
                        World.NPCs.DockWith(netid, x[1].Trim());
                    }
                }),
                new ("npcattack", (arg) =>
                {
                    var x = arg.Split(',');
                    if (PermissionCheck() && x.Length == 2 && int.TryParse(x[0].Trim(), out int netid) && World != null)
                    {
                        World.NPCs.Attack(netid, x[1].Trim());
                    }
                })
            };
        }

        bool PermissionCheck()
        {
            if (Client is LocalPacketClient) return true;
            else
            {
                rpcClient.OnConsoleMessage("Permission denied");
                return false;
            }
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
            rpcClient.OnConsoleMessage($"Unrecognised command '{cmd}'");
        }

        void IServerPlayer.RTCMissionAccepted()
        {
            msnRuntime?.MissionAccepted();
        }

        void IServerPlayer.RequestCharacterDB()
        {
            Client.SendPacket(new NewCharacterDBPacket()
            {
                Factions = Game.GameData.Ini.NewCharDB.Factions,
                Packages = Game.GameData.Ini.NewCharDB.Packages,
                Pilots = Game.GameData.Ini.NewCharDB.Pilots
            }, PacketDeliveryMethod.ReliableOrdered);
        }

        Task<bool> IServerPlayer.SelectCharacter(int index)
        {
            if (index >= 0 && index < CharacterList.Count)
            {
                var sc = CharacterList[index];
                FLLog.Info("Server", $"opening id {sc.Id}");
                Character = NetCharacter.FromDb(sc.Id, Game);
                Name = Character.Name;
                rpcClient.UpdateBaselinePrices(Game.BaselineGoodPrices);
                rpcClient.UpdateInventory(Character.Credits, GetShipWorth(), Character.EncodeLoadout());
                Base = Character.Base;
                System = Character.System;
                Position = Character.Position;
                if (Base != null) {
                    PlayerEnterBase();
                } else {
                    SpaceInitialSpawn(null);
                }
                return Task.FromResult(false);
            }
            else
            {
                return Task.FromResult(true);
            }
        }

        Task<bool> IServerPlayer.DeleteCharacter(int index)
        {
            if (index < 0 || index >= CharacterList.Count)
                return Task.FromResult(false);
            var sc = CharacterList[index];
            Game.Database.DeleteCharacter(sc.Id);
            CharacterList.Remove(sc);
            return Task.FromResult(true);
        }

        Task<bool> IServerPlayer.CreateNewCharacter(string name, int index)
        {
            FLLog.Info("Player", $"New char {name}");
            if (!Game.Database.NameInUse(name))
            {
                Character ch = null;
                Game.Database.AddCharacter(playerGuid, (db) =>
                {
                    ch = db;
                    var sg = Game.NewCharacter(name, index);
                    db.Name = sg.Player.Name;
                    db.Base = sg.Player.Base;
                    db.System = sg.Player.System;
                    db.Rank = 1;
                    db.Costume = sg.Player.Costume;
                    db.ComCostume = sg.Player.ComCostume;
                    db.Money = sg.Player.Money;
                    db.Ship = sg.Player.ShipArchetype;
                    db.Items = new List<CargoItem>();
                    foreach (var eq in sg.Player.Equip)
                    {
                        db.Items.Add(new CargoItem()
                        {
                            ItemName = eq.EquipName,
                            Hardpoint = string.IsNullOrEmpty(eq.Hardpoint) ? "internal" : eq.Hardpoint,
                            ItemCount = 1
                        });
                    }
                    foreach (var cg in sg.Player.Cargo)
                    {
                        db.Items.Add(new CargoItem()
                        {
                            ItemName = cg.CargoName,
                            ItemCount = cg.Count
                        });
                    }
                });
                var sel = NetCharacter.FromDb(ch.Id, Game).ToSelectable();
                CharacterList.Add(sel);
                Client.SendPacket(new AddCharacterPacket()
                {
                    Character = sel
                }, PacketDeliveryMethod.ReliableOrdered);
                return Task.FromResult(true);
            } else {
                return Task.FromResult(false);
            }
        }

        public void SpawnDebris(int id, GameObjectKind kind, string archetype, string part, Matrix4x4 tr, float mass)
        {
            rpcClient.SpawnDebris(id, kind, archetype, part, Vector3.Transform(Vector3.Zero, tr), tr.ExtractRotation(), mass);
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

        public void Killed()
        {
            World?.RemovePlayer(this);
            World = null;
            Dead = true;
            rpcClient.Killed();
            Base = Character.Base;
            System = Character.System;
            Position = Character.Position;
        }

        void IServerPlayer.Respawn()
        {
            if (Dead)
            {
                Dead = false;
                if (Base != null) {
                    PlayerEnterBase();
                } else {
                    SpaceInitialSpawn(null);
                }
            }
        }
        
        public void OnSPSave()
        {
            Character?.UpdatePosition(Base, System, Position);
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
        public void CallThorn(string thorn, int mainObject)
        {
            rpcClient.CallThorn(thorn, mainObject);
        }

        public void JumpTo(string system, string target)
        {
            rpcClient.StartJumpTunnel();
            if(World != null) World.RemovePlayer(this);
            
            var sys = Game.GameData.GetSystem(system);
            Game.RequestWorld(sys, (world) =>
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
                world.EnqueueAction(() =>
                {
                    world.SpawnPlayer(this, Position, Orientation);
                    rpcClient.SpawnPlayer(System, 0, Position, Orientation);
                    msnRuntime?.EnteredSpace();
                });
            });
        }

        void IServerPlayer.RequestDock(string nickname)
        {
            World.RequestDock(this, nickname);
        }

        void IServerPlayer.Launch()
        {
            var b = Game.GameData.GetBase(Base);
            var sys = Game.GameData.GetSystem(b.System);
            Game.RequestWorld(sys, (world) =>
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
                world.EnqueueAction(() =>
                {
                    world.SpawnPlayer(this, Position, Orientation);
                    rpcClient.SpawnPlayer(System, 0, Position, Orientation);
                    msnRuntime?.EnteredSpace();
                });
            });
        }

        void INetResponder.SendResponse(IPacket packet)
        {
            Client.SendPacket(packet, PacketDeliveryMethod.ReliableOrdered);
        }
    }
}