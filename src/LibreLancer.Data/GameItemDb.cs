// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.Archetypes;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Data.GameData.Market;
using LibreLancer.Data.GameData.RandomMissions;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema;
using LibreLancer.Data.Schema.Audio;
using LibreLancer.Data.Schema.Effects;
using LibreLancer.Data.Schema.Equipment;
using LibreLancer.Data.Schema.Fuses;
using LibreLancer.Data.Schema.Goods;
using LibreLancer.Data.Schema.MBases;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Data.Schema.Pilots;
using LibreLancer.Data.Schema.Solar;
using LibreLancer.Data.Schema.Universe;
using Archetype = LibreLancer.Data.GameData.Archetype;
using Asteroid = LibreLancer.Data.GameData.Asteroid;
using AsteroidField = LibreLancer.Data.GameData.World.AsteroidField;
using Base = LibreLancer.Data.GameData.World.Base;
using DockSphere = LibreLancer.Data.GameData.World.DockSphere;
using DynamicAsteroid = LibreLancer.Data.GameData.DynamicAsteroid;
using Explosion = LibreLancer.Data.GameData.Explosion;
using FileSystem = LibreLancer.Data.IO.FileSystem;
using LightSource = LibreLancer.Data.GameData.World.LightSource;
using Nebula = LibreLancer.Data.GameData.World.Nebula;
using NewsItem = LibreLancer.Data.GameData.NewsItem;
using Pilot = LibreLancer.Data.GameData.Pilot;
using Spine = LibreLancer.Data.GameData.World.Spine;
using StarSystem = LibreLancer.Data.GameData.World.StarSystem;
using SystemObject = LibreLancer.Data.GameData.World.SystemObject;
using Zone = LibreLancer.Data.GameData.World.Zone;

namespace LibreLancer.Data
{
    public class GameItemDb
    {
        public ReadFileCallback ThornReadCallback;
        public FileSystem VFS;
        public FreelancerData Ini => flData;

        public string DataVersion => flData.DataVersion;

        // Database
        public GameItemCollection<Accessory> Accessories = new();
        public GameItemCollection<Archetype> Archetypes = new();
        public GameItemCollection<Asteroid> Asteroids = new();
        public GameItemCollection<Base> Bases = new();
        public GameItemCollection<Bodypart> Bodyparts = new();
        public GameItemCollection<DebrisInfo> Debris = new();
        public GameItemCollection<DynamicAsteroid> DynamicAsteroids = new();
        public GameItemCollection<FuseResources> Fuses = new();
        public GameItemCollection<ResolvedFx> Effects;
        public GameItemCollection<Equipment> Equipment = new();
        public GameItemCollection<Explosion> Explosions = new();
        public GameItemCollection<Faction> Factions = new();
        public GameItemCollection<ResolvedGood> Goods;
        public List<IntroScene> IntroScenes;
        public IEnumerable<ObjectLoadout> Loadouts => _loadouts.Values;
        public NewsCollection News;
        public GameItemCollection<Ship> Ships = new();
        public GameItemCollection<SimpleObject> SimpleObjects = new();
        public List<StoryIndex> Story;
        public GameItemCollection<Sun> Stars;
        public GameItemCollection<StarSystem> Systems = new();
        public GameItemCollection<ResolvedFx> VisEffects;
        public VignetteTree VignetteTree;

        // Backing Fields
        FreelancerData flData;

        Dictionary<string, ObjectLoadout> _loadouts =
            new Dictionary<string, ObjectLoadout>(StringComparer.OrdinalIgnoreCase);

        // Other dictionaries
        private Dictionary<string, Pilot> pilots = new Dictionary<string, Pilot>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, Vector3> quadratics = new Dictionary<string, Vector3>();
        private Dictionary<uint, ShipPackage> shipPackageByCRC = new Dictionary<uint, ShipPackage>();
        Dictionary<string, ShipPackage> shipPackages = new Dictionary<string, ShipPackage>();
        Dictionary<string, long> shipPrices = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        private Dictionary<string, string> shipToIcon =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        Dictionary<string, ResolvedTexturePanels> tpanels =
            new Dictionary<string, ResolvedTexturePanels>(StringComparer.OrdinalIgnoreCase);

        private object tPanelsLock = new object();

        private EffectStorage fxdata;


        public GameItemDb(FileSystem vfs)
        {
            VFS = vfs;
            var flini = new FreelancerIni(VFS);
            flData = new FreelancerData(flini, VFS);
            ThornReadCallback = (file) => VFS.ReadAllBytes("EXE/" + file);
        }

        public string DataPath(string input)
        {
            if (input == null) return null;
            var path = flData.Freelancer.DataPath + input;
            if (!VFS.FileExists(path))
            {
                FLLog.Error("GameData", $"File {flData.Freelancer.DataPath}{input} not found");
                return null;
            }

            return path;
        }

        bool TryResolveThn(string path, out ResolvedThn r)
        {
            r = null;
            if (path == null) return false;
            var resolved = DataPath(path);
            if (VFS.FileExists(resolved))
            {
                r = new ResolvedThn()
                    { SourcePath = path, VFS = VFS, DataPath = resolved, ReadCallback = ThornReadCallback };
                return true;
            }

            return false;
        }

        ResolvedThn ResolveThn(string path)
        {
            if (path == null) return null;
            return new() { SourcePath = path, VFS = VFS, DataPath = DataPath(path), ReadCallback = ThornReadCallback };
        }

        void InitEffects()
        {
            fxdata = new EffectStorage();
            foreach (var fx in flData.Effects.VisEffects)
                fxdata.VisFx[fx.Nickname] = fx;
            foreach (var fx in flData.Effects.BeamSpears)
                fxdata.BeamSpears[fx.Nickname] = fx;
            foreach (var fx in flData.Effects.BeamBolts)
                fxdata.BeamBolts[fx.Nickname] = fx;
            Effects = new GameItemCollection<ResolvedFx>();
            VisEffects = new GameItemCollection<ResolvedFx>();
            foreach (var fx in flData.Effects.VisEffects)
            {
                string alepath = null;
                if (!string.IsNullOrWhiteSpace(fx.AlchemyPath))
                    alepath = DataPath(fx.AlchemyPath);
                var lib = fx.Textures.Select(DataPath).Where(x => x != null).ToArray();
                VisEffects.Add(new ResolvedFx()
                {
                    AlePath = alepath,
                    VisFxCrc = (uint)fx.EffectCrc,
                    LibraryFiles = lib,
                    CRC = FLHash.CreateID(fx.Nickname),
                    Nickname = fx.Nickname
                });
            }

            foreach (var effect in flData.Effects.Effects)
            {
                VisEffect visfx = null;
                BeamSpear spear = null;
                BeamBolt bolt = null;
                AudioEntry sound = null;

                if (!string.IsNullOrWhiteSpace(effect.VisEffect))
                    fxdata.VisFx.TryGetValue(effect.VisEffect, out visfx);

                if (!string.IsNullOrWhiteSpace(effect.VisBeam))
                {
                    fxdata.BeamSpears.TryGetValue(effect.VisBeam, out spear);
                    fxdata.BeamBolts.TryGetValue(effect.VisBeam, out bolt);
                }

                if (!string.IsNullOrWhiteSpace(effect.SndEffect))
                {
                    sound = flData.Audio.Entries.FirstOrDefault(x =>
                        x.Nickname.Equals(effect.SndEffect, StringComparison.OrdinalIgnoreCase));
                }

                string alepath = null;
                if (!string.IsNullOrWhiteSpace(visfx?.AlchemyPath))
                {
                    alepath = DataPath(visfx.AlchemyPath);
                }

                var lib = visfx?.Textures.Select(DataPath).Where(x => x != null).ToArray();
                Effects.Add(new ResolvedFx()
                {
                    AlePath = alepath,
                    VisFxCrc = (uint)(visfx?.EffectCrc ?? 0),
                    LibraryFiles = lib,
                    Spear = spear,
                    Bolt = bolt,
                    CRC = FLHash.CreateID(effect.Nickname),
                    Nickname = effect.Nickname,
                    Sound = sound
                });
            }
        }

        IEnumerable<Schema.Universe.Base> InitBases(LoadingTasks tasks)
        {
            FLLog.Info("Game", "Initing " + flData.Universe.Bases.Count + " bases");
            Dictionary<string, MBase> mbases = new(StringComparer.OrdinalIgnoreCase);
            foreach (var mbase in flData.MBases.MBases)
            {
                mbases[mbase.Nickname] = mbase;
            }

            foreach (var inibase in flData.Universe.Bases)
            {
                if (inibase.Nickname.StartsWith("intro", StringComparison.InvariantCultureIgnoreCase))
                    yield return inibase;
                Dictionary<string, MRoom> mrooms = new(StringComparer.OrdinalIgnoreCase);
                if (mbases.TryGetValue(inibase.Nickname, out var mbase))
                {
                    foreach (var r in mbase.Rooms)
                    {
                        mrooms[r.Nickname] = r;
                    }
                }

                var b = new Base();
                b.Nickname = inibase.Nickname;
                b.CRC = FLHash.CreateID(b.Nickname);
                b.SourceFile = inibase.File;
                b.IdsName = inibase.IdsName;
                b.BaseRunBy = inibase.BGCSBaseRunBy;
                b.AutosaveForbidden = inibase.AutosaveForbidden ?? false;
                b.System = inibase.System;
                b.TerrainTiny = inibase.TerrainTiny;
                b.TerrainSml = inibase.TerrainSml;
                b.TerrainMdm = inibase.TerrainMdm;
                b.TerrainLrg = inibase.TerrainLrg;
                b.TerrainDyna1 = inibase.TerrainDyna1;
                b.TerrainDyna2 = inibase.TerrainDyna2;
                if (mbase != null)
                {
                    b.MsgIdPrefix = mbase.MsgIdPrefix;
                    b.Diff = mbase.Diff;
                    b.LocalFaction = Factions.Get(mbase.LocalFaction);
                    if (mbase.MVendor != null)
                    {
                        b.MinMissionOffers = (int)mbase.MVendor.NumOffers.X;
                        b.MaxMissionOffers = (int)mbase.MVendor.NumOffers.Y;
                    }

                    foreach (var npc in mbase.Npcs)
                    {
                        b.Npcs.Add(new BaseNpc
                        {
                            Nickname = npc.Nickname,
                            BaseAppr = npc.BaseAppr,
                            Body = npc.Body,
                            Head = npc.Head,
                            LeftHand = npc.LeftHand,
                            RightHand = npc.RightHand,
                            Accessory = npc.Accessory,
                            IndividualName = npc.IndividualName,
                            Affiliation = Factions.Get(npc.Affiliation),
                            Voice = npc.Voice,
                            Room = npc.Room,
                            Know = npc.Know,
                            Rumors = npc.Rumors,
                            Bribes = npc.Bribes,
                            Mission = npc.Mission,
                        });
                    }
                }

                foreach (var room in inibase.Rooms)
                {
                    var nr = new BaseRoom();
                    nr.SourceFile = room.File;
                    nr.Music = room.RoomSound?.Music;
                    nr.MusicOneShot = room.RoomSound?.MusicOneShot ?? false;
                    nr.SceneScripts = new List<SceneScript>();
                    nr.PlayerShipPlacement = room.PlayerShipPlacement?.Name;
                    nr.ForSaleShipPlacements = room.ForSaleShipPlacements.Select(x => x.Name).ToList();
                    tasks.Begin(() =>
                    {
                        nr.SetScript = ResolveThn(room.RoomInfo?.SetScript);
                        if (room.RoomInfo?.SceneScripts != null)
                            foreach (var e in room.RoomInfo.SceneScripts)
                                nr.SceneScripts.Add(
                                    new SceneScript(e.AmbientAll, e.TrafficPriority, ResolveThn(e.Path)));
                        nr.LandScript = ResolveThn(room.PlayerShipPlacement?.LandingScript);
                        nr.LaunchScript = ResolveThn(room.PlayerShipPlacement?.LaunchingScript);
                        nr.StartScript = ResolveThn(room.CharacterPlacement?.StartScript);

                        nr.GoodscartScript = ResolveThn(room.RoomInfo?.GoodscartScript);
                    });
                    nr.Hotspots = new List<BaseHotspot>();
                    foreach (var hp in room.Hotspots)
                        nr.Hotspots.Add(new BaseHotspot()
                        {
                            Name = hp.Name,
                            Behavior = hp.Behavior,
                            Room = hp.RoomSwitch,
                            SetVirtualRoom = hp.SetVirtualRoom,
                            VirtualRoom = hp.VirtualRoom
                        });
                    nr.Nickname = room.Nickname;
                    nr.CRC = FLHash.CreateLocationID(b.Nickname, nr.Nickname);
                    if (room.Nickname.Equals(inibase.BaseInfo?.StartRoom, StringComparison.OrdinalIgnoreCase))
                        b.StartRoom = nr;
                    nr.Camera = room.Camera?.Name;
                    nr.FixedNpcs = new List<BaseFixedNpc>();
                    if (mrooms.TryGetValue(room.Nickname, out var mroom))
                    {
                        foreach (var npc in mroom.NPCs)
                        {
                            nr.FixedNpcs.Add(new BaseFixedNpc
                            {
                                Placement = npc.StandMarker,
                                FidgetScript = ResolveThn(npc.Script),
                                Action = npc.Action,
                                Npc = b.Npcs.Find(n => n.Nickname == npc.Npc)
                            });
                        }
                    }

                    b.Rooms.Add(nr);
                }

                Bases.Add(b);
            }

            flData.MBases = null; //Free memory
        }

        public string GetShipIcon(Ship ship)
        {
            shipToIcon.TryGetValue(ship.Nickname, out string icon);
            return icon;
        }

        public long GetShipPrice(Ship ship)
        {
            shipPrices.TryGetValue(ship.Nickname, out long price);
            return price;
        }

        void InitGoods()
        {
            FLLog.Info("Game", "Initing " + flData.Goods.Goods.Count + " goods");
            Goods = new GameItemCollection<ResolvedGood>();
            Dictionary<string, Good> hulls =
                new Dictionary<string, Good>(256, StringComparer.OrdinalIgnoreCase);
            List<Good> ships = new List<Good>();
            foreach (var g in flData.Goods.Goods)
            {
                switch (g.Category)
                {
                    case GoodCategory.ShipHull:
                        hulls[g.Nickname] = g;
                        shipToIcon[g.Ship] = g.ItemIcon;
                        shipPrices[g.Ship] = g.Price;
                        break;
                    case GoodCategory.Ship:
                        ships.Add(g);
                        break;
                    case GoodCategory.Equipment:
                    case GoodCategory.Commodity:
                        if (Equipment.TryGetValue(g.Nickname, out var equip))
                        {
                            var good = new ResolvedGood()
                            {
                                Nickname = g.Nickname, Equipment = equip, Ini = g, CRC = CrcTool.FLModelCrc(g.Nickname)
                            };
                            equip.Good = good;
                            Goods.Add(good);
                        }

                        break;
                }
            }

            foreach (var g in ships)
            {
                Good hull = hulls[g.Hull];
                var sp = new ShipPackage();
                sp.Ship = hull.Ship;
                sp.Nickname = g.Nickname;
                sp.CRC = FLHash.CreateID(sp.Nickname);
                sp.BasePrice = hull.Price;
                foreach (var addon in g.Addons)
                {
                    if (Equipment.TryGetValue(addon.Equipment, out var equip))
                    {
                        sp.Addons.Add(new PackageAddon()
                        {
                            Equipment = equip,
                            Hardpoint = addon.Hardpoint,
                            Amount = addon.Amount
                        });
                    }
                }

                shipPackages.Add(g.Nickname, sp);
                shipPackageByCRC.Add(sp.CRC, sp);
            }

            flData.Goods = null; //Free memory
        }

        void InitMarkets()
        {
            FLLog.Info("Game", "Initing " + flData.Markets.BaseGoods.Count + " shops");
            foreach (var m in flData.Markets.BaseGoods)
            {
                Base b;
                if (!Bases.TryGetValue(m.Base, out b))
                {
                    //This is allowed by demo at least
                    FLLog.Warning("Market", "BaseGoods references nonexistent base " + m.Base);
                    continue;
                }

                foreach (var gd in m.MarketGoods)
                {
                    ShipPackage sp;
                    if (shipPackages.TryGetValue(gd.Good, out sp))
                    {
                        if (gd.Min != 0 || gd.Max != 0) //Vanilla adds disabled ships ??? (why)
                            b.SoldShips.Add(new SoldShip() { Package = sp });
                    }
                    else if (Goods.TryGetValue(gd.Good, out var good))
                    {
                        b.SoldGoods.Add(new BaseSoldGood()
                        {
                            Rep = gd.Rep,
                            Rank = gd.Rank,
                            Good = good,
                            Price = (ulong)((double)good.Ini.Price * gd.Multiplier),
                            ForSale = gd.Max > 0
                        });
                    }
                }
            }

            flData.Markets = null; //Free memory
        }

        public FormationDef GetFormation(string form) =>
            string.IsNullOrWhiteSpace(form)
                ? null
                : flData.Formations.Formations.FirstOrDefault(x =>
                    form.Equals(x.Nickname, StringComparison.OrdinalIgnoreCase));

        public ShipPackage GetShipPackage(uint crc)
        {
            shipPackageByCRC.TryGetValue(crc, out var pkg);
            return pkg;
        }

        void InitFactions()
        {
            FLLog.Info("Factions", $"Initing {flData.InitialWorld.Groups.Count} factions");
            foreach (var f in flData.InitialWorld.Groups)
            {
                var fac = new Faction()
                {
                    Nickname = f.Nickname,
                    IdsInfo = f.IdsInfo,
                    IdsName = f.IdsName,
                    IdsShortName = f.IdsShortName,
                    Properties = flData.FactionProps.FactionProps.FirstOrDefault(x =>
                        x.Affiliation.Equals(f.Nickname, StringComparison.OrdinalIgnoreCase))
                };
                fac.Hidden = flData.Freelancer.HiddenFactions.Contains(fac.Nickname, StringComparer.OrdinalIgnoreCase);
                fac.CRC = CrcTool.FLModelCrc(fac.Nickname);
                Factions.Add(fac);
            }

            foreach (var f in flData.InitialWorld.Groups)
            {
                var us = Factions.Get(f.Nickname);
                foreach (var rep in f.Rep)
                {
                    if (Factions.TryGetValue(rep.Name, out var other))
                    {
                        us.Reputations[other] = rep.Rep;
                    }
                    else
                    {
                        FLLog.Warning("InitialWorld", $"Reputation for non-existing faction {rep.Name}");
                    }
                }

                var emp = flData.Empathy.RepChangeEffects.FirstOrDefault(x => x.Group.Equals(us.Nickname));
                if (emp != null)
                {
                    us.ObjectDestroyRepChange =
                        emp.Events.LastOrDefault(x => x.Type == EmpathyEventType.ObjectDestruction).ChangeAmount;
                    us.MissionSucceedRepChange =
                        emp.Events.LastOrDefault(x => x.Type == EmpathyEventType.RandomMissionSuccess).ChangeAmount;
                    us.MissionFailRepChange =
                        emp.Events.LastOrDefault(x => x.Type == EmpathyEventType.RandomMissionFailure).ChangeAmount;
                    us.MissionAbortRepChange =
                        emp.Events.LastOrDefault(x => x.Type == EmpathyEventType.RandomMissionAbort).ChangeAmount;
                    us.FactionEmpathy = emp.EmpathyRate
                        .Where(x => x.Rep != 0 && (Factions.Get(x.Name) != null))
                        .Select(x => new Empathy(Factions.Get(x.Name), x.Rep))
                        .ToArray();
                }
                else
                {
                    us.FactionEmpathy = Array.Empty<Empathy>();
                }
            }
        }

        void InitVignetteTree()
        {
            VignetteTree = VignetteTree.FromIni(Ini.VignetteParams);
            Ini.VignetteParams = null;
        }

        void InitNews()
        {
            Story = Ini.Storyline.Items.Select((x, y) => new StoryIndex(y, x)).ToList();
            News = new();
            foreach (var n in Ini.News.NewsItems)
            {
                var ni = new NewsItem()
                {
                    Icon = n.Icon,
                    Logo = n.Logo,
                    Headline = n.Headline,
                    Text = n.Text,
                    Autoselect = n.Autoselect,
                    Audio = n.Audio
                };
                if (n.Rank is { Length: >= 2 })
                {
                    ni.From = Story.FirstOrDefault(x =>
                        x.Item.Nickname.Equals(n.Rank[0], StringComparison.OrdinalIgnoreCase));
                    ni.To = Story.FirstOrDefault(x =>
                        x.Item.Nickname.Equals(n.Rank[1], StringComparison.OrdinalIgnoreCase));
                }

                News.AddNewsItem(ni);
                foreach (var b in n.Base)
                {
                    if (Bases.TryGetValue(b, out var loc))
                    {
                        News.AddToBase(ni, loc);
                    }
                }
            }
        }

        public void LoadData(Action onIniLoaded = null)
        {
            flData.LoadData();
            var tasks = new LoadingTasks();
            if (onIniLoaded != null)
                tasks.Begin(onIniLoaded);
            /*if (glResource != null && ui != null)
            {
                glResource.AddPreload(
                    fldata.EffectShapes.Files.Select(txmfile => DataPath(txmfile))
                );
                foreach (var shape in fldata.EffectShapes.Shapes)
                {
                    var s = new RenderShape()
                    {
                        Texture = shape.Value.TextureName,
                        Dimensions = shape.Value.Dimensions
                    };
                    glResource.AddShape(shape.Key, s);
                }
                ui.QueueUIThread(() => glResource.Preload());
            }*/
            //
            var pilotTask = tasks.Begin(InitPilots);
            var effectsTask = tasks.Begin(InitEffects);
            var fusesTask = tasks.Begin(InitFuses, effectsTask);
            var explosionTask = tasks.Begin(InitExplosions, effectsTask);
            var debrisTask = tasks.Begin(InitDebris);
            var shipsTask = tasks.Begin(InitShips, explosionTask, fusesTask, debrisTask);
            List<Schema.Universe.Base> introbases = new List<Schema.Universe.Base>();
            var baseTask = tasks.Begin(() => introbases.AddRange(InitBases(tasks)));
            tasks.Begin(() =>
            {
                FLLog.Info("Game", "Loading intro scenes");
                IntroScenes = new List<IntroScene>();
                foreach (var b in introbases)
                {
                    foreach (var room in b.Rooms)
                    {
                        if (room.Nickname == b.BaseInfo?.StartRoom)
                        {
                            var isc = new IntroScene();
                            isc.Scripts = new List<ResolvedThn>();
                            if (room.RoomInfo != null)
                            {
                                foreach (var p in room.RoomInfo.SceneScripts)
                                {
                                    if (TryResolveThn(p.Path, out var thn))
                                        isc.Scripts.Add(thn);
                                    else
                                        FLLog.Error("Thn", $"Could not find intro script {p.Path}");
                                }

                                isc.Music = room.RoomSound?.Music;
                                IntroScenes.Add(isc);
                            }
                        }
                    }
                }
            }, baseTask);
            var factionsTask = tasks.Begin(InitFactions);
            var equipmentTask = tasks.Begin(InitEquipment, effectsTask);
            var goodsTask = tasks.Begin(InitGoods, equipmentTask);
            var loadoutsTask = tasks.Begin(InitLoadouts, equipmentTask);
            var archetypesTask = tasks.Begin(InitArchetypes, loadoutsTask, debrisTask);
            var starsTask = tasks.Begin(InitStars);
            var astsTask = tasks.Begin(InitAsteroids);
            tasks.Begin(InitMarkets, baseTask, goodsTask, archetypesTask);
            tasks.Begin(InitBodyParts);
            tasks.Begin(InitNews, baseTask);
            tasks.Begin(() => InitSystems(tasks),
                baseTask,
                archetypesTask,
                equipmentTask,
                shipsTask,
                factionsTask,
                loadoutsTask,
                pilotTask,
                astsTask,
                starsTask
            );
            tasks.Begin(InitVignetteTree);
            tasks.WaitAll();
            flData.Universe = null; //Free universe ini!
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(); //We produced a crapload of garbage
        }

        void InitBodyParts()
        {
            foreach (var p in flData.Bodyparts.Bodyparts)
            {
                var b = new Bodypart();
                b.Nickname = p.Nickname;
                b.CRC = FLHash.CreateID(b.Nickname);
                b.Path = DataPath(p.Mesh);
                b.Sex = p.Sex;
                Bodyparts.Add(b);
            }

            foreach (var src in flData.Bodyparts.Accessories)
            {
                var a = new Accessory();
                a.Nickname = src.Nickname;
                a.CRC = FLHash.CreateID(src.Nickname);
                a.BodyHardpoint = src.BodyHardpoint;
                a.Hardpoint = src.Hardpoint;
                a.ModelFile = ResolveDrawable(src.Mesh);
                Accessories.Add(a);
            }
        }

        void InitEquipment()
        {
            FLLog.Info("Game", "Initing " + flData.Equipment.Equip.Count + " equipments");
            Dictionary<string, LightInheritHelper> lights =
                new Dictionary<string, LightInheritHelper>(StringComparer.OrdinalIgnoreCase);

            void SetCommonFields(Equipment equip, AbstractEquipment val)
            {
                equip.Nickname = val.Nickname;
                equip.CRC = FLHash.CreateID(equip.Nickname);
                equip.HPChild = val.HPChild;
                equip.LODRanges = val.LODRanges;
                equip.IdsName = val.IdsName;
                equip.IdsInfo = val.IdsInfo;
                equip.Volume = val.Volume;
            }

            //Process munitions first
            foreach (var mn in flData.Equipment.Munitions)
            {
                Equipment equip;
                if (!string.IsNullOrEmpty(mn.Motor))
                {
                    var mequip = new MissileEquip()
                    {
                        Def = mn,
                        ModelFile = ResolveDrawable(mn.MaterialLibrary, mn.DaArchetype),
                        Motor = flData.Equipment.Motors.FirstOrDefault(x =>
                            x.Nickname.Equals(mn.Motor, StringComparison.OrdinalIgnoreCase)),
                        Explosion = flData.Equipment.Explosions.FirstOrDefault(x =>
                            x.Nickname.Equals(mn.ExplosionArch, StringComparison.OrdinalIgnoreCase))
                    };
                    if (mequip.Explosion != null &&
                        !string.IsNullOrEmpty(mequip.Explosion.Effect))
                    {
                        mequip.ExplodeFx = Effects.Get(mequip.Explosion.Effect);
                    }

                    equip = mequip;
                }
                else
                {
                    var effect = Effects.Get(mn.ConstEffect);
                    var mequip = new MunitionEquip()
                    {
                        Def = mn,
                        ConstEffect_Spear = effect?.Spear,
                        ConstEffect_Bolt = effect?.Bolt,
                    };
                    equip = mequip;
                }

                SetCommonFields(equip, mn);
                Equipment.Add(equip);
            }

            //Then all equipment
            foreach (var val in flData.Equipment.Equip)
            {
                Equipment equip = null;
                if (val is Light l)
                {
                    lights.Add(val.Nickname, new LightInheritHelper(l));
                }
                else if (val is InternalFx)
                {
                    var eq = new AnimationEquipment();
                    eq.Animation = ((InternalFx)val).UseAnimation;
                    equip = eq;
                }

                if (val is AttachedFx)
                {
                    equip = GetAttachedFx((AttachedFx)val);
                }

                if (val is PowerCore pc)
                {
                    var eqp = new PowerEquipment();
                    eqp.Def = pc;
                    eqp.ModelFile = ResolveDrawable(pc.MaterialLibrary, pc.DaArchetype);
                    equip = eqp;
                }

                if (val is CountermeasureDropper cms)
                {
                    var eqp = new CountermeasureEquipment()
                    {
                        HpType = "hp_countermeasure_dropper"
                    };
                    eqp.ModelFile = ResolveDrawable(cms.MaterialLibrary, cms.DaArchetype);
                    equip = eqp;
                }

                if (val is ShieldBattery bat)
                {
                    var eqp = new ShieldBatteryEquipment();
                    eqp.Def = bat;
                    equip = eqp;
                }

                if (val is RepairKit rep)
                {
                    var eqp = new RepairKitEquipment();
                    eqp.Def = rep;
                    equip = eqp;
                }
                else if (val is Gun gn)
                {
                    Equipment.TryGetValue(gn.ProjectileArchetype, out Equipment mnEquip);
                    if (mnEquip is MunitionEquip mn)
                    {
                        var eqp = new GunEquipment()
                        {
                            HpType = gn.HpGunType,
                            Munition = mn,
                            Def = gn
                        };
                        eqp.FlashEffect = Effects.Get(gn.FlashParticleName);
                        equip = eqp;
                        equip.ModelFile = ResolveDrawable(gn.MaterialLibrary, gn.DaArchetype);
                    }
                    else if (mnEquip is MissileEquip me)
                    {
                        var eqp = new MissileLauncherEquipment()
                        {
                            HpType = gn.HpGunType,
                            Munition = me,
                            Def = gn
                        };
                        equip = eqp;
                        equip.ModelFile = ResolveDrawable(gn.MaterialLibrary, gn.DaArchetype);
                    }
                    else
                    {
                        FLLog.Error("Equipment", $"Munition {gn.ProjectileArchetype} not found (Gun {gn.Nickname})");
                        continue;
                    }
                }

                if (val is Thruster th)
                {
                    var eqp = new ThrusterEquipment()
                    {
                        Drain = th.PowerUsage,
                        Force = th.MaxForce,
                        HpParticles = th.HpParticles,
                        HpType = "hp_thruster"
                    };
                    equip = eqp;
                    eqp.Particles = Effects.Get(th.Particles);
                    equip.ModelFile = ResolveDrawable(th.MaterialLibrary, th.DaArchetype);
                }

                if (val is ShieldGenerator sh)
                {
                    var eqp = new ShieldEquipment()
                    {
                        HpType = sh.HpType,
                        Def = sh
                    };
                    eqp.ModelFile = ResolveDrawable(sh.MaterialLibrary, sh.DaArchetype);
                    equip = eqp;
                }

                if (val is Scanner sc)
                {
                    var eq = new ScannerEquipment
                    {
                        Def = sc
                    };
                    equip = eq;
                }

                if (val is Tractor tc)
                {
                    var eq = new TractorEquipment
                    {
                        Def = tc
                    };
                    equip = eq;
                }

                if (val is LootCrate lc)
                {
                    var eq = new LootCrateEquipment();
                    eq.ModelFile = ResolveDrawable(lc.MaterialLibrary, lc.DaArchetype);
                    eq.Mass = lc.Mass;
                    eq.Hitpoints = lc.Hitpoints;
                    equip = eq;
                }

                if (val is Engine deng)
                {
                    var engequip = new EngineEquipment() { Def = deng };
                    if (deng.CruiseSpeed > 0)
                        engequip.CruiseSpeed = deng.CruiseSpeed;
                    equip = engequip;
                }

                if (val is Tradelane tl)
                {
                    var tlequip = new TradelaneEquipment();
                    tlequip.RingActive = Effects.Get(tl.TlRingActive);
                    equip = tlequip;
                }

                if (val is Commodity cm)
                    equip = new CommodityEquipment();
                if (equip == null)
                    continue;
                SetCommonFields(equip, val);
                Equipment.Add(equip);
            }

            //Resolve light inheritance
            foreach (var lt in lights.Values)
            {
                if (!string.IsNullOrWhiteSpace(lt.InheritName))
                {
                    if (!lights.TryGetValue(lt.InheritName, out lt.Inherit))
                        FLLog.Error("Light", $"Light not found {lt.InheritName}");
                }
            }

            foreach (var lt in lights.Values)
            {
                var eq = GetLight(lt);
                eq.Nickname = lt.Nickname;
                eq.CRC = FLHash.CreateID(eq.Nickname);
                Equipment.Add(eq);
            }

            // LootCrateEquipment references
            foreach (var val in flData.Equipment.Equip)
            {
                var eq = Equipment.Get(val.Nickname);
                if (eq == null)
                    continue;
                eq.LootAppearance = Equipment.Get(val.LootAppearance) as LootCrateEquipment;
            }

            flData.Equipment = null; //Free memory
        }

        Vector3 GetQuadratic(string attenCurve)
        {
            Vector3 q;
            if (!quadratics.TryGetValue(attenCurve, out q))
            {
                q = ApproximateCurve.GetQuadraticFunction(flData.Graphs.FindFloatGraph(attenCurve).Points.ToArray());
                quadratics.Add(attenCurve, q);
            }

            return q;
        }

        void InitLoadouts()
        {
            _loadouts = new Dictionary<string, ObjectLoadout>(StringComparer.OrdinalIgnoreCase);
            foreach (var l in flData.Loadouts.Loadouts)
            {
                var ld = new ObjectLoadout() { Nickname = l.Nickname, Archetype = l.Archetype };
                foreach (var eq in l.Equip)
                {
                    Equipment equip = Equipment.Get(eq.Nickname);
                    if (equip != null)
                    {
                        ld.Items.Add(new LoadoutItem(eq.Hardpoint, equip));
                    }
                }

                foreach (var c in l.Cargo)
                {
                    Equipment equip = Equipment.Get(c.Nickname);
                    if (equip != null)
                        ld.Cargo.Add(new BasicCargo(equip, c.Count));
                }

                _loadouts[l.Nickname] = ld;
            }
        }

        void InitSystems(LoadingTasks tasks)
        {
            FLLog.Info("Game", "Initing " + flData.Universe.Systems.Count + " systems");
            foreach (var inisys in flData.Universe.Systems)
            {
                if (inisys.MultiUniverse) continue; //Skip multiuniverse for now
                FLLog.Info("System", inisys.Nickname);
                var sys = new StarSystem();
                sys.LocalFaction = Factions.Get(inisys.Info?.LocalFaction);
                sys.UniversePosition = inisys.Pos ?? Vector2.Zero;
                sys.AmbientColor = inisys.Ambient?.Color ?? Color3f.Black;
                sys.IdsName = inisys.IdsName;
                sys.IdsInfo = inisys.IdsInfo;
                sys.Nickname = inisys.Nickname;
                sys.Visit = (VisitFlags)inisys.Visit;
                sys.CRC = CrcTool.FLModelCrc(sys.Nickname);
                sys.MsgIdPrefix = inisys.MsgIdPrefix;
                sys.BackgroundColor = inisys.Info?.SpaceColor ?? Color4.Black;
                sys.MusicSpace = inisys.Music?.Space;
                sys.MusicBattle = inisys.Music?.Battle;
                sys.MusicDanger = inisys.Music?.Danger;
                sys.Spacedust = inisys.Dust?.Spacedust;
                sys.SpacedustMaxParticles = inisys.Dust?.SpacedustMaxParticles ?? 0;
                sys.FarClip = inisys.Info?.SpaceFarClip ?? 20000f;
                sys.NavMapScale = inisys.NavMapScale;
                sys.SourceFile = inisys.File;
                foreach (var ec in inisys.EncounterParameters)
                {
                    sys.EncounterParameters.Add(new EncounterParameters()
                    {
                        Nickname = ec.Nickname,
                        SourceFile = ec.Filename
                    });
                }

                var p = new List<PreloadObject>();
                foreach (var a in inisys.Preloads.SelectMany(x => x.ArchetypeShip))
                    p.Add(new PreloadObject(PreloadType.Ship, a));
                foreach (var a in inisys.Preloads.SelectMany(x => x.ArchetypeSimple))
                    p.Add(new PreloadObject(PreloadType.Simple, a));
                foreach (var a in inisys.Preloads.SelectMany(x => x.ArchetypeEquipment))
                    p.Add(new PreloadObject(PreloadType.Equipment, a));
                foreach (var a in inisys.Preloads.SelectMany(x => x.ArchetypeSnd))
                    p.Add(new PreloadObject(PreloadType.Sound, a));
                foreach (var a in inisys.Preloads.SelectMany(x => x.ArchetypeSolar))
                    p.Add(new PreloadObject(PreloadType.Solar, a));
                foreach (var a in inisys.Preloads.SelectMany(x => x.ArchetypeVoice))
                    p.Add(new PreloadObject(PreloadType.Voice, a.Select(x => new HashValue(x)).ToArray()));
                sys.Preloads = p.ToArray();

                if (inisys.TexturePanels != null)
                    sys.TexturePanelsFiles.AddRange(inisys.TexturePanels.Files);

                sys.StarsBasic = ResolveDrawable(inisys.Background?.BasicStarsPath);
                sys.StarsComplex = ResolveDrawable(inisys.Background?.ComplexStarsPath);
                sys.StarsNebula = ResolveDrawable(inisys.Background?.NebulaePath);
                if (inisys.LightSources != null)
                {
                    foreach (var src in inisys.LightSources)
                    {
                        var lt = new RenderLight();
                        if (src.Color.HasValue)
                        {
                            var srcCol = src.Color.Value;
                            lt.Color = new Color3f(srcCol.R, srcCol.G, srcCol.B);
                        }
                        else
                        {
                            lt.Color = Color3f.White;
                            FLLog.Warning("Light", $"{inisys.Nickname}: Light Source {src.Nickname} missing color");
                        }

                        if (src.Pos.HasValue)
                            lt.Position = src.Pos.Value;
                        else
                            FLLog.Warning("Light", $"{inisys.Nickname}: Light Source {src.Nickname} missing position");
                        if (src.Range.HasValue)
                            lt.Range = src.Range.Value;
                        else
                        {
                            lt.Range = 200000;
                            FLLog.Warning("Light", $"{inisys.Nickname}: Light Source {src.Nickname} missing range");
                        }

                        lt.Direction = src.Direction ?? new Vector3(0, 0, 1);
                        lt.Kind = ((src.Type ?? LightType.Point) ==
                                   LightType.Point)
                            ? LightKind.Point
                            : LightKind.Directional;
                        lt.Attenuation = src.Attenuation ?? Vector3.UnitY;
                        if (src.AttenCurve != null)
                        {
                            lt.Kind = LightKind.PointAttenCurve;
                            lt.Attenuation = GetQuadratic(src.AttenCurve);
                        }

                        sys.LightSources.Add(new LightSource()
                            { Light = lt, AttenuationCurveName = src.AttenCurve, Nickname = src.Nickname });
                    }
                }

                var objDict =
                    new Dictionary<string, Schema.Universe.SystemObject>(StringComparer
                        .OrdinalIgnoreCase);
                foreach (var obj in inisys.Objects)
                {
                    var o = GetSystemObject(inisys.Nickname, obj);
                    if (o == null)
                        continue;
                    objDict[o.Nickname] = obj;
                    sys.Objects.Add(o);
                }

                //fill tradelane names right
                foreach (var obj in inisys.Objects.Where(x => x.NextRing != null && x.TradelaneSpaceName != 0))
                {
                    var spaceName = obj.TradelaneSpaceName;
                    var oNext = obj;
                    var start = sys.Objects.FirstOrDefault(x =>
                        x.Nickname.Equals(obj.Nickname, StringComparison.OrdinalIgnoreCase));
                    if (start != null) start.IdsRight = spaceName;
                    int i = 0;
                    while (oNext.NextRing != null &&
                           objDict.TryGetValue(oNext.NextRing, out oNext))
                    {
                        var go = sys.Objects.FirstOrDefault(x =>
                            x.Nickname.Equals(oNext.Nickname, StringComparison.OrdinalIgnoreCase));
                        go.IdsRight = spaceName;
                        if (i++ > 5000)
                        {
                            FLLog.Warning("System", $"Loop detected in tradelane {oNext.Nickname}");
                            break; //Infinite loop
                        }
                    }
                }

                //fill tradelane names left
                foreach (var obj in inisys.Objects.Where(x => x.PrevRing != null && x.TradelaneSpaceName != 0))
                {
                    var spaceName = obj.TradelaneSpaceName;
                    var oNext = obj;
                    var start = sys.Objects.FirstOrDefault(x =>
                        x.Nickname.Equals(obj.Nickname, StringComparison.OrdinalIgnoreCase));
                    if (start != null) start.IdsLeft = spaceName;
                    int i = 0;
                    while (oNext.PrevRing != null &&
                           objDict.TryGetValue(oNext.PrevRing, out oNext))
                    {
                        var go = sys.Objects.FirstOrDefault(x =>
                            x.Nickname.Equals(oNext.Nickname, StringComparison.OrdinalIgnoreCase));
                        go.IdsLeft = spaceName;
                        if (i++ > 5000)
                        {
                            FLLog.Warning("System", $"Loop detected in tradelane {oNext.Nickname}");
                            break; //Infinite loop
                        }
                    }
                }

                if (inisys.Zones != null)
                    foreach (var zne in inisys.Zones)
                    {
                        var z = new Zone();
                        z.Nickname = zne.Nickname;
                        z.IdsName = zne.IdsName;
                        z.IdsInfo = zne.IdsInfo;
                        z.EdgeFraction = zne.EdgeFraction ?? 0.25f;
                        z.PropertyFlags = (ZonePropFlags)zne.PropertyFlags;
                        z.PropertyFogColor = zne.PropertyFogColor;
                        z.VisitFlags = (VisitFlags)(zne.Visit ?? 0);
                        z.Position = zne.Pos ?? Vector3.Zero;
                        z.Sort = zne.Sort ?? 0;
                        //
                        z.Music = zne.Music;
                        z.Spacedust = zne.Spacedust;
                        z.SpacedustMaxParticles = zne.SpacedustMaxParticles;
                        z.Interference = zne.Interference;
                        z.PowerModifier = zne.PowerModifier;
                        z.DragModifier = zne.DragModifier;
                        z.Comment = CommentEscaping.Unescape(zne.Comment);
                        z.LaneId = zne.LaneId;
                        z.TradelaneAttack = zne.TradelaneAttack;
                        z.TradelaneDown = zne.TradelaneDown;
                        z.Damage = zne.Damage;
                        z.Toughness = zne.Toughness;
                        z.Density = zne.Density;
                        z.PopulationAdditive = zne.PopulationAdditive;
                        z.MissionEligible = zne.MissionEligible;
                        z.MaxBattleSize = zne.MaxBattleSize;
                        z.PopType = zne.PopType;
                        z.ReliefTime = zne.ReliefTime;
                        z.RepopTime = zne.RepopTime;
                        z.AttackIds = zne.AttackIds;
                        z.MissionType = zne.MissionType;
                        z.PathLabel = zne.PathLabel;
                        z.Usage = zne.Usage;
                        z.VignetteType = zne.VignetteType;
                        z.Encounters = zne.Encounters.ToArray();
                        z.DensityRestrictions = zne.DensityRestrictions.ToArray();
                        //
                        if (zne.Pos == null)
                            FLLog.Warning("Zone", $"Zone {zne.Nickname} in {inisys.Nickname} has no position");
                        if (zne.Rotate != null)
                        {
                            var r = zne.Rotate.Value;
                            z.RotationMatrix = MathHelper.MatrixFromEulerDegrees(r);
                            z.RotationAngles = new Vector3(
                                MathHelper.DegreesToRadians(r.X),
                                MathHelper.DegreesToRadians(r.Y),
                                MathHelper.DegreesToRadians(r.Z)
                            );
                        }
                        else
                        {
                            z.RotationMatrix = Matrix4x4.Identity;
                            z.RotationAngles = Vector3.Zero;
                        }

                        z.Shape = (ShapeKind)(int)(zne.Shape ?? ZoneShape.SPHERE);
                        var sz = zne.Size ?? Vector3.One;
                        if (z.Shape == ShapeKind.Ring)
                            z.Size = new Vector3(sz.X, sz.Z, sz.Y); //outer, height, inner
                        else
                            z.Size = sz;
                        sys.Zones.Add(z);
                        sys.ZoneDict[z.Nickname] = z;
                    }

                tasks.Begin(() =>
                {
                    if (inisys.Asteroids != null)
                    {
                        foreach (var ast in inisys.Asteroids)
                        {
                            try
                            {
                                var a = GetAsteroidField(sys, ast);
                                if (a != null)
                                    sys.AsteroidFields.Add(a);
                                else
                                {
                                    FLLog.Error("System", $"{sys.Nickname} failed to add asteroid field.");
                                }
                            }
                            catch (Exception e)
                            {
                                FLLog.Error("System", $"{sys.Nickname} failed to add asteroid field.\n{e}");
                            }
                        }
                    }

                    if (inisys.Nebulae != null)
                    {
                        foreach (var nbl in inisys.Nebulae)
                        {
                            if (sys.ZoneDict.ContainsKey(nbl.ZoneName))
                            {
                                try
                                {
                                    sys.Nebulae.Add(GetNebula(sys, nbl));
                                }
                                catch (Exception e)
                                {
                                    FLLog.Error("System", $"{sys.Nickname} failed to add nebula.\n{e}");
                                }
                            }
                            else
                            {
                                FLLog.Error("System", $"{sys.Nickname} Nebula references missing zone {nbl.ZoneName}");
                            }
                        }
                    }
                });
                Systems.Add(sys);
            }

            FLLog.Info("Game", "Calculating shortest paths");
            ShortestPaths.CalculateShortestPaths(this);
            FLLog.Info("Game", "Shortest paths calculated");
        }

        ResolvedTexturePanels TexturePanelFile(string f, string srcPath)
        {
            lock (tPanelsLock)
            {
                ResolvedTexturePanels pnl;
                if (!tpanels.TryGetValue(f, out pnl))
                {
                    if (!VFS.FileExists(f))
                    {
                        return null;
                    }

                    var pf = new TexturePanels(f, VFS);
                    pnl = new ResolvedTexturePanels() { SourcePath = srcPath };
                    foreach (var s in pf.Shapes)
                        pnl.Shapes[s.Key] = s.Value;
                    pnl.TextureShapes = pf.TextureShapes;
                    pnl.LibraryFiles = pf.Files.Select(DataPath).ToArray();
                    tpanels.Add(f, pnl);
                }

                return pnl;
            }
        }


        AsteroidField GetAsteroidField(StarSystem sys, Schema.Universe.AsteroidField ast)
        {
            var a = new AsteroidField();
            if (!sys.ZoneDict.ContainsKey(ast.ZoneName))
            {
                FLLog.Error("System", $"{sys.Nickname}: {ast.ZoneName} zone missing in Asteroid ref");
                return null;
            }

            a.SourceFile = ast.IniFile;
            a.Zone = sys.ZoneDict[ast.ZoneName];
            if (ast.TexturePanels != null)
            {
                foreach (var f in ast.TexturePanels.Files)
                {
                    a.TexturePanels.Add(TexturePanelFile(DataPath(f), f));
                }
            }

            if (ast.Properties != null)
            {
                foreach (var prop in ast.Properties.Flag)
                {
                    if (FieldFlagUtils.TryParse(prop, out var f))
                    {
                        a.Flags |= f;
                    }
                }
            }

            if (ast.Band != null)
            {
                a.Band = new AsteroidBand();
                a.Band.RenderParts = ast.Band.RenderParts.Value;
                a.Band.Height = ast.Band.Height.Value;
                a.Band.Shape = ast.Band.Shape;
                a.Band.Fade = new Vector4(ast.Band.Fade[0], ast.Band.Fade[1], ast.Band.Fade[2], ast.Band.Fade[3]);
                var cs = ast.Band.ColorShift ?? Vector3.One;
                a.Band.ColorShift = new Color4(cs.X, cs.Y, cs.Z, 1f);
                a.Band.TextureAspect = ast.Band.TextureAspect ?? 1f;
                a.Band.OffsetDistance = ast.Band.OffsetDist ?? 0f;
            }

            a.Cube = new List<StaticAsteroid>();
            if (ast.Field != null)
            {
                a.CubeRotation = new AsteroidCubeRotation();
                a.CubeRotation.AxisX = ast.Cube?.RotationX ?? AsteroidCubeRotation.Default_AxisX;
                a.CubeRotation.AxisY = ast.Cube?.RotationY ?? AsteroidCubeRotation.Default_AxisY;
                a.CubeRotation.AxisZ = ast.Cube?.RotationZ ?? AsteroidCubeRotation.Default_AxisZ;
                a.CubeSize = ast.Field.CubeSize ?? 100; //HACK: Actually handle null cube correctly
                a.FillDist = ast.Field.FillDist.Value;
                a.EmptyCubeFrequency = ast.Field.EmptyCubeFrequency ?? 0f;
                if (ast.Field.TintField.HasValue)
                {
                    a.DiffuseColor = ast.Field.TintField.Value;
                    a.AmbientColor = ast.Field.TintField.Value;
                }
                else
                {
                    a.DiffuseColor = ast.Field.DiffuseColor;
                    a.AmbientColor = ast.Field.AmbientColor ?? Color4.White;
                }

                a.AmbientIncrease = ast.Field.AmbientIncrease;
                if (ast.Cube?.Cube != null)
                {
                    foreach (var c in ast.Cube.Cube)
                    {
                        var sta = new StaticAsteroid()
                        {
                            Position = c.Position,
                            Info = c.Info,
                            Archetype = Asteroids.Get(c.Name)
                        };
                        sta.Rotation = MathHelper.QuatFromEulerDegrees(c.Rotation);
                        a.Cube.Add(sta);
                    }
                }
            }

            foreach (var dyn in ast.DynamicAsteroids)
            {
                var da = DynamicAsteroids.Get(dyn.Asteroid);
                if (da == null)
                {
                    FLLog.Error("Asteroids", $"Dynamic asteroid arch '{dyn.Asteroid}' not found");
                    continue;
                }

                a.DynamicAsteroids.Add(new()
                {
                    Asteroid = da,
                    ColorShift = dyn.ColorShift,
                    Count = dyn.Count,
                    MaxAngularVelocity = dyn.MaxAngularVelocity,
                    MaxVelocity = dyn.MaxVelocity,
                    PlacementOffset = dyn.PlacementOffset,
                    PlacementRadius = dyn.PlacementRadius
                });
            }

            foreach (var lz in ast.LootableZones)
            {
                var lc = Equipment.Get(lz.DynamicLootCommodity);
                var cont = Equipment.Get(lz.DynamicLootContainer);
                var z = new DynamicLootZone()
                {
                    LootCommodity = lc,
                    LootContainer = cont as LootCrateEquipment,
                    LootDifficulty = lz.DynamicLootDifficulty,
                    LootCount = lz.DynamicLootCount
                };
                if (string.IsNullOrWhiteSpace(lz.Zone))
                {
                    a.FieldLoot = z;
                }
                else
                {
                    if (!sys.ZoneDict.TryGetValue(lz.Zone, out z.Zone))
                    {
                        FLLog.Error("System", "Loot zone " + lz.Zone + " zone does not exist in " + sys.Nickname);
                    }
                }
            }

            a.ExclusionZones = new List<AsteroidExclusionZone>();
            if (ast.ExclusionZones != null)
            {
                foreach (var excz in ast.ExclusionZones)
                {
                    Zone zone;
                    if (!sys.ZoneDict.TryGetValue(excz.ZoneName, out zone))
                    {
                        FLLog.Error("System",
                            "Exclusion zone " + excz.ZoneName + " zone does not exist in " + sys.Nickname);
                        continue;
                    }

                    a.ExclusionZones.Add(new AsteroidExclusionZone()
                    {
                        Zone = zone,
                        BillboardCount = excz.BillboardCount,
                        EmptyCubeFrequency = excz.EmptyCubeFrequency,
                        ExcludeBillboards = excz.ExcludeBillboards,
                        ExcludeDynamicAsteroids = excz.ExcludeDynamicAsteroids,
                    });
                }
            }

            a.BillboardCount = ast.AsteroidBillboards == null ? -1 : ast.AsteroidBillboards.Count.Value;
            if (a.BillboardCount != -1)
            {
                a.BillboardDistance = ast.AsteroidBillboards.StartDist.Value;
                a.BillboardFadePercentage = ast.AsteroidBillboards.FadeDistPercent.Value;
                a.BillboardShape = ast.AsteroidBillboards.Shape;
                a.BillboardSize = ast.AsteroidBillboards.Size.Value;
                a.BillboardTint = new Color3f(ast.AsteroidBillboards.ColorShift ?? Vector3.One);
            }

            return a;
        }

        public Nebula GetNebula(StarSystem sys, Schema.Universe.Nebula nbl)
        {
            var n = new Nebula();
            n.SourceFile = nbl.IniFile;
            n.Zone = sys.ZoneDict[nbl.ZoneName];
            foreach (var f in nbl.TexturePanels.Files)
            {
                n.TexturePanels.Add(TexturePanelFile(DataPath(f), f));
            }

            n.ExteriorFill = nbl.Exterior.FillShape;
            n.ExteriorColor = nbl.Exterior.Color ?? Color4.White;
            n.FogColor = nbl.Fog.Color;
            n.FogEnabled = (nbl.Fog.Enabled != 0);
            n.FogRange = new Vector2(nbl.Fog.Near, nbl.Fog.Distance);
            n.SunBurnthroughScale = n.SunBurnthroughIntensity = 1f;
            if (nbl.NebulaLights != null && nbl.NebulaLights.Count > 0)
            {
                n.AmbientColor = nbl.NebulaLights[0].Ambient;
                n.SunBurnthroughScale = nbl.NebulaLights[0].SunBurnthroughScaler ?? 1f;
                n.SunBurnthroughIntensity = nbl.NebulaLights[0].SunBurnthroughIntensity ?? 1f;
            }

            if (nbl.Clouds.Count > 0)
            {
                var clds = nbl.Clouds[0];
                if (clds.PuffShape.Count > 0)
                {
                    n.HasInteriorClouds = true;
                    n.InteriorCloudShapes = new WeightedRandomCollection<string>(
                        clds.PuffShape.ToArray(),
                        clds.PuffWeights
                    );
                    n.InteriorCloudColorA = clds.PuffColorA.Value;
                    n.InteriorCloudColorB = clds.PuffColorB.Value;
                    n.InteriorCloudRadius = clds.PuffRadius.Value;
                    n.InteriorCloudCount = clds.PuffCount.Value;
                    n.InteriorCloudMaxDistance = clds.MaxDistance.Value;
                    n.InteriorCloudMaxAlpha = clds.PuffMaxAlpha ?? 1f;
                    n.InteriorCloudFadeDistance = clds.NearFadeDistance.Value;
                    n.InteriorCloudDrift = clds.PuffDrift.Value;
                }
            }

            if (nbl.Exterior != null && nbl.Exterior.Shape is { Count : > 0 })
            {
                n.HasExteriorBits = true;
                n.ExteriorCloudShapes = new WeightedRandomCollection<string>(
                    nbl.Exterior.Shape.ToArray(),
                    nbl.Exterior.ShapeWeights
                );
                n.ExteriorMinBits = nbl.Exterior.MinBits.Value;
                n.ExteriorMaxBits = nbl.Exterior.MaxBits.Value;
                n.ExteriorBitRadius = nbl.Exterior.BitRadius.Value;
                n.ExteriorBitRandomVariation = nbl.Exterior.BitRadiusRandomVariation ?? 0;
                n.ExteriorMoveBitPercent = nbl.Exterior.MoveBitPercent ?? 0;
            }

            if (nbl.ExclusionZones != null)
            {
                n.ExclusionZones = new List<NebulaExclusionZone>();
                foreach (var excz in nbl.ExclusionZones)
                {
                    Zone zone;
                    if (!sys.ZoneDict.TryGetValue(excz.ZoneName, out zone))
                    {
                        FLLog.Error("System",
                            "Exclusion zone " + excz.ZoneName + " zone does not exist in " + sys.Nickname);
                        continue;
                    }

                    var e = new NebulaExclusionZone();
                    e.Zone = zone;
                    e.FogFar = excz.FogFar ?? n.FogRange.Y;
                    if (excz.ZoneShellPath != null)
                    {
                        e.ShellPath = excz.ZoneShellPath;
                        e.ShellTint = excz.Tint ?? Color3f.White;
                        e.ShellScalar = excz.ShellScalar ?? 1f;
                        e.ShellMaxAlpha = excz.MaxAlpha ?? 1f;
                    }

                    n.ExclusionZones.Add(e);
                }
            }

            if (nbl.BackgroundLightning != null)
            {
                n.BackgroundLightning = true;
                n.BackgroundLightningDuration = nbl.BackgroundLightning.Duration;
                n.BackgroundLightningColor = nbl.BackgroundLightning.Color;
                n.BackgroundLightningGap = nbl.BackgroundLightning.Gap;
            }

            if (nbl.DynamicLightning != null)
            {
                n.DynamicLightning = true;
                n.DynamicLightningGap = nbl.DynamicLightning.Gap;
                n.DynamicLightningColor = nbl.DynamicLightning.Color;
                n.DynamicLightningDuration = nbl.DynamicLightning.Duration;
            }

            if (nbl.Clouds.Count > 0 && nbl.Clouds[0].LightningDuration != null)
            {
                n.CloudLightning = true;
                n.CloudLightningDuration = nbl.Clouds[0].LightningDuration.Value;
                n.CloudLightningColor = nbl.Clouds[0].LightningColor.Value;
                n.CloudLightningGap = nbl.Clouds[0].LightningGap.Value;
                n.CloudLightningIntensity = nbl.Clouds[0].LightningIntensity.Value;
            }

            foreach (var ex in n.ExclusionZones)
            {
                if (ex.ShellPath != null) ex.Shell = ResolveDrawable(ex.ShellPath);
            }

            return n;
        }


        ResolvedModel ResolveDrawable(string file) => ResolveDrawable((IEnumerable<string>)null, file);

        ResolvedModel ResolveDrawable(string libs, string file) => ResolveDrawable(new[] { libs }, file);

        ResolvedModel ResolveDrawable(IEnumerable<string> libs, string file)
        {
            if (string.IsNullOrWhiteSpace(file)) return null;
            var mdl = new ResolvedModel()
            {
                ModelFile = DataPath(file),
                SourcePath = file,
            };
            if (libs != null)
                mdl.LibraryFiles = libs.Select(x => DataPath(x)).Where(x => x != null).ToArray();
            return mdl;
        }


        void FillBlock<T>(string nick, string blockId, List<T> source, ref T dest)
            where T : PilotBlock
        {
            if (!string.IsNullOrEmpty(blockId))
            {
                var obj = source.FirstOrDefault(x => x.Nickname.Equals(blockId, StringComparison.OrdinalIgnoreCase));
                if (obj == null)
                {
                    FLLog.Warning("Pilot", $"{nick}: Unable to find {typeof(T).Name} '{blockId}'");
                }
                else
                {
                    dest = obj;
                }
            }
        }

        void FillPilot(Pilot pilot, Schema.Pilots.Pilot src)
        {
            if (src.Inherit != null)
            {
                var parent = flData.Pilots.Pilots.FirstOrDefault(x =>
                    x.Nickname.Equals(src.Inherit, StringComparison.OrdinalIgnoreCase));
                if (parent == null)
                    FLLog.Error("Data", $"Pilot {src.Nickname} references missing inherit {src.Inherit}");
                else
                    FillPilot(pilot, parent);
            }

            string n = src.Nickname;
            FillBlock(n, src.BuzzHeadTowardId, flData.Pilots.BuzzHeadTowardBlocks, ref pilot.BuzzHeadToward);
            FillBlock(n, src.BuzzPassById, flData.Pilots.BuzzPassByBlocks, ref pilot.BuzzPassBy);
            FillBlock(n, src.CountermeasureId, flData.Pilots.CountermeasureBlocks, ref pilot.Countermeasure);
            FillBlock(n, src.DamageReactionId, flData.Pilots.DamageReactionBlocks, ref pilot.DamageReaction);
            FillBlock(n, src.EngineKillId, flData.Pilots.EngineKillBlocks, ref pilot.EngineKill);
            FillBlock(n, src.EvadeBreakId, flData.Pilots.EvadeBreakBlocks, ref pilot.EvadeBreak);
            FillBlock(n, src.EvadeDodgeId, flData.Pilots.EvadeDodgeBlocks, ref pilot.EvadeDodge);
            FillBlock(n, src.FormationId, flData.Pilots.FormationBlocks, ref pilot.Formation);
            FillBlock(n, src.GunId, flData.Pilots.GunBlocks, ref pilot.Gun);
            FillBlock(n, src.JobId, flData.Pilots.JobBlocks, ref pilot.Job);
            FillBlock(n, src.MineId, flData.Pilots.MineBlocks, ref pilot.Mine);
            FillBlock(n, src.MissileId, flData.Pilots.MissileBlocks, ref pilot.Missile);
            FillBlock(n, src.MissileReactionId, flData.Pilots.MissileReactionBlocks, ref pilot.MissileReactionBlock);
            FillBlock(n, src.RepairId, flData.Pilots.RepairBlocks, ref pilot.Repair);
            FillBlock(n, src.StrafeId, flData.Pilots.StrafeBlocks, ref pilot.Strafe);
            FillBlock(n, src.TrailId, flData.Pilots.TrailBlocks, ref pilot.Trail);
        }

        public Pilot GetPilot(string nickname)
        {
            if (string.IsNullOrEmpty(nickname)) return null;
            pilots.TryGetValue(nickname, out var p);
            return p;
        }

        void InitPilots()
        {
            FLLog.Info("Game", "Initing Pilots");
            foreach (var orig in flData.Pilots.Pilots)
            {
                var p = new Pilot() { Nickname = orig.Nickname };
                FillPilot(p, orig);
                pilots[p.Nickname] = p;
            }
        }

        void InitDebris()
        {
            FLLog.Info("Game", "Initing Debris");
            foreach (var orig in flData.Ships.Simples
                         .Concat(flData.Solar.Simples)
                         .Concat(flData.Explosions.Simples))

            {
                var db = new SimpleObject();
                db.Nickname = orig.Nickname;
                db.CRC = FLHash.CreateID(orig.Nickname);
                db.Model = ResolveDrawable(orig.MaterialLibrary, orig.DaArchetypeName);
                SimpleObjects.Add(db);
            }

            foreach (var orig in flData.Explosions.Debris)
            {
                var db = new DebrisInfo();
                db.Nickname = orig.Nickname;
                db.CRC = FLHash.CreateID(orig.Nickname);
                db.Lifetime = orig.Lifetime;
                Debris.Add(db);
            }

            flData.Ships.Simples = null;
            flData.Solar.Simples = null;
            flData.Explosions.Simples = null;
            flData.Explosions.Debris = null;
        }

        SeparablePart FromCollisionGroup(CollisionGroup cg)
        {
            var sp = new SeparablePart();
            sp.Part = cg.obj;
            sp.ChildDamageCapHardpoint = cg.GroupDmgHp;
            sp.ChildDamageCap = SimpleObjects.Get(cg.GroupDmgObj);
            sp.ParentDamageCapHardpoint = cg.DmgHp;
            sp.ParentDamageCap = SimpleObjects.Get(cg.DmgObj);
            sp.Mass = cg.Mass <= 0 ? 1 : cg.Mass;
            sp.ChildImpulse = cg.ChildImpulse;
            sp.DebrisType = Debris.Get(cg.DebrisType);
            return sp;
        }

        void InitExplosions()
        {
            FLLog.Info("Game", "Initing Explosions");
            foreach (var orig in flData.Explosions.Explosions)
            {
                var ex = new Explosion() { Nickname = orig.Nickname };
                ex.CRC = CrcTool.FLModelCrc(ex.Nickname);
                if (orig.Effects.Count > 0)
                    ex.Effect = Effects.Get(orig.Effects[0].Name);
                Explosions.Add(ex);
            }
        }

        void InitShips()
        {
            FLLog.Info("Game", "Initing " + flData.Ships.Ships.Count + " ships");
            foreach (var orig in flData.Ships.Ships)
            {
                var ship = new Ship();
                ship.ModelFile = ResolveDrawable(orig.MaterialLibraries, orig.DaArchetypeName);
                ship.LODRanges = orig.LodRanges;
                ship.HoldSize = orig.HoldSize;
                ship.Mass = orig.Mass;
                ship.Class = orig.ShipClass;
                ship.AngularDrag = orig.AngularDrag;
                ship.RotationInertia = orig.RotationInertia;
                ship.SteeringTorque = orig.SteeringTorque;
                ship.Hitpoints = orig.Hitpoints;
                ship.StrafeForce = orig.StrafeForce;
                ship.MaxBankAngle = orig.MaxBankAngle;
                ship.ChaseOffset = orig.CameraOffset;
                ship.CameraHorizontalTurnAngle = orig.CameraHorizontalTurnAngle;
                ship.CameraVerticalTurnUpAngle = orig.CameraVerticalTurnUpAngle;
                ship.CameraVerticalTurnDownAngle = orig.CameraVerticalTurnDownAngle;
                ship.Nickname = orig.Nickname;
                ship.NameIds = orig.IdsName;
                ship.Infocard = orig.IdsInfo;
                ship.IdsInfo = [orig.IdsInfo1, orig.IdsInfo2, orig.IdsInfo3];
                ship.ShipType = orig.Type;
                ship.Explosion = Explosions.Get(orig.ExplosionArch);
                ship.CRC = FLHash.CreateID(ship.Nickname);
                ship.MaxShieldBatteries = orig.ShieldBatteryLimit;
                ship.MaxRepairKits = orig.NanobotLimit;
                ship.ShieldLinkHull = orig.ShieldLink?.HardpointMount;
                ship.ShieldLinkSource = orig.ShieldLink?.HardpointShield;
                ship.TractorSource = orig.HpTractorSource;
                ship.SeparableParts = orig.CollisionGroups.Select(FromCollisionGroup).ToList();
                foreach (var fuse in orig.Fuses)
                {
                    ship.Fuses.Add(new DamageFuse()
                    {
                        Fuse = Fuses.Get(fuse.Fuse),
                        Threshold = fuse.Threshold
                    });
                }

                foreach (var hp in orig.HardpointTypes)
                {
                    if (!flData.HpTypes.Types.TryGetValue(hp.Type, out var typedef))
                    {
                        FLLog.Error("Ship", $"Unrecognised hp_type {hp.Type} in {ship.Nickname}");
                        continue;
                    }

                    if (!ship.PossibleHardpoints.TryGetValue(hp.Type, out var possible))
                    {
                        possible = new List<string>();
                        ship.PossibleHardpoints.Add(hp.Type, possible);
                    }

                    foreach (var tgt in hp.Hardpoints)
                    {
                        if (!ship.HardpointTypes.TryGetValue(tgt, out var types))
                        {
                            types = new List<HpType>();
                            ship.HardpointTypes.Add(tgt, types);
                        }

                        types.Add(typedef);
                        possible.Add(tgt);
                    }
                }

                Ships.Add(ship);
            }

            flData.Ships = null; //free memory
        }

        void InitAsteroids()
        {
            FLLog.Info("Game", "Initing " + flData.Asteroids.Asteroids.Count + "asteroids");
            foreach (var ast in flData.Asteroids.Asteroids)
            {
                var asteroid = new Asteroid();
                asteroid.Nickname = ast.Nickname;
                asteroid.ModelFile = ResolveDrawable(ast.MaterialLibrary, ast.DaArchetype);
                asteroid.CRC = CrcTool.FLModelCrc(asteroid.Nickname);
                Asteroids.Add(asteroid);
            }

            foreach (var dynast in flData.Asteroids.DynamicAsteroids)
            {
                var dyn = new DynamicAsteroid();
                dyn.Nickname = dynast.Nickname;
                dyn.ModelFile = ResolveDrawable(dynast.MaterialLibrary, dynast.DaArchetype);
                dyn.CRC = CrcTool.FLModelCrc(dyn.Nickname);
                DynamicAsteroids.Add(dyn);
            }
        }

        void InitStars()
        {
            FLLog.Info("Game", "Initing " + flData.Stars.Stars.Count + " stars");
            var glows = new Dictionary<string, StarGlow>(StringComparer.OrdinalIgnoreCase);
            var spines = new Dictionary<string, Spines>(StringComparer.OrdinalIgnoreCase);

            StarGlow GetGlow(string g)
            {
                if (string.IsNullOrWhiteSpace(g)) return null;
                glows.TryGetValue(g, out var glow);
                return glow;
            }

            Spines GetSpines(string id)
            {
                if (string.IsNullOrWhiteSpace(id)) return null;
                spines.TryGetValue(id, out var sp);
                return sp;
            }

            foreach (var glow in flData.Stars.StarGlows)
            {
                glows[glow.Nickname] = glow;
            }

            foreach (var sp in flData.Stars.Spines)
            {
                spines[sp.Nickname] = sp;
            }

            Stars = new GameItemCollection<Sun>();
            foreach (var star in flData.Stars.Stars)
            {
                var s = new Sun()
                {
                    Nickname = star.Nickname,
                    CRC = FLHash.CreateID(star.Nickname),
                    Radius = star.Radius
                };
                //glow
                var starglow = GetGlow(star.StarGlow);
                s.GlowSprite = starglow.Shape;
                s.GlowColorInner = starglow.InnerColor;
                s.GlowColorOuter = starglow.OuterColor;
                s.GlowScale = starglow.Scale;
                //center
                var centerglow = GetGlow(star.StarCenter);
                if (centerglow != null)
                {
                    s.CenterSprite = centerglow.Shape;
                    s.CenterColorInner = centerglow.InnerColor;
                    s.CenterColorOuter = centerglow.OuterColor;
                    s.CenterScale = centerglow.Scale;
                }

                var sp = GetSpines(star.Spines);
                if (sp != null)
                {
                    s.SpinesSprite = sp.Shape;
                    s.SpinesScale = sp.RadiusScale;
                    s.Spines = new List<Spine>(sp.Items.Count);
                    foreach (var it in sp.Items)
                        s.Spines.Add(new Spine(it.LengthScale, it.WidthScale, it.InnerColor, it.OuterColor, it.Alpha));
                }

                Stars.Add(s);
            }
        }

        void InitArchetypes()
        {
            FLLog.Info("Game", "Initing " + flData.Solar.Solars.Count + " archetypes");
            foreach (var arch in flData.Solar.Solars)
            {
                var obj = new Archetype();
                obj.Type = arch.Type;
                obj.Loadout = GetLoadout(arch.LoadoutName);
                obj.NavmapIcon = arch.ShapeName;
                obj.SolarRadius = arch.SolarRadius ?? 0;
                foreach (var dockSphere in arch.DockingSpheres)
                {
                    obj.DockSpheres.Add(new DockSphere()
                    {
                        Type = dockSphere.Type,
                        Hardpoint = dockSphere.Hardpoint,
                        Radius = dockSphere.Radius,
                        Script = dockSphere.Script
                    });
                }

                if (arch.OpenAnim != null)
                {
                    foreach (var sph in obj.DockSpheres)
                        sph.Script = sph.Script ?? arch.OpenAnim;
                }

                if (arch.Type == ArchetypeType.tradelane_ring)
                {
                    obj.DockSpheres.Add(new DockSphere()
                    {
                        Type = DockSphereType.moor_large,
                        Hardpoint = "HpRightLane",
                        Radius = 30
                    });
                    obj.DockSpheres.Add(new DockSphere()
                    {
                        Type = DockSphereType.moor_large,
                        Hardpoint = "HpLeftLane",
                        Radius = 30
                    });
                }

                obj.SeparableParts = arch.CollisionGroups.Select(FromCollisionGroup).ToList();
                obj.Nickname = arch.Nickname;
                obj.CRC = FLHash.CreateID(obj.Nickname);
                obj.LODRanges = arch.LODRanges;
                obj.ModelFile = ResolveDrawable(arch.MaterialPaths, arch.DaArchetypeName);
                obj.Hitpoints = arch.Hitpoints ?? -1;
                if (!arch.Destructible ||
                    float.IsInfinity(obj.Hitpoints))
                {
                    obj.Hitpoints = -1;
                }

                Archetypes.Add(obj);
            }
        }

        ObjectLoadout GetLoadout(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;
            _loadouts.TryGetValue(key, out var ld);
            return ld;
        }

        public bool TryGetLoadout(string name, out ObjectLoadout l)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                l = null;
                return false;
            }

            return _loadouts.TryGetValue(name, out l);
        }

        public SystemObject GetSystemObject(string system, Schema.Universe.SystemObject o)
        {
            var obj = new SystemObject();
            obj.Nickname = o.Nickname;
            obj.Reputation = Factions.Get(o.Reputation);
            obj.Visit = (VisitFlags)(o.Visit ?? 0);
            obj.IdsName = o.IdsName;
            obj.Position = o.Pos ?? Vector3.Zero;
            obj.Parent = o.Parent;
            obj.Spin = o.Spin ?? Vector3.Zero;
            obj.IdsInfo = o.IdsInfo;
            obj.Base = Bases.Get(o.Base);
            obj.Faction = Factions.Get(o.Faction);
            obj.Pilot = GetPilot(o.Pilot);
            obj.Behavior = o.Behavior;
            obj.BurnColor = o.BurnColor;
            obj.AmbientColor = o.AmbientColor;
            obj.AtmosphereRange = o.AtmosphereRange ?? 0;
            obj.MsgIdPrefix = o.MsgIdPrefix;
            obj.DifficultyLevel = o.DifficultyLevel ?? 0;
            obj.Parent = o.Parent;
            obj.Voice = o.Voice;
            obj.SpaceCostume = o.SpaceCostume;
            obj.JumpEffect = o.JumpEffect;
            obj.RingZone = o.RingZone;
            obj.RingFile = o.RingFile;
            obj.Comment = CommentEscaping.Unescape(o.Comment);
            if (o.DockWith != null)
            {
                obj.Dock = new DockAction() { Kind = DockKinds.Base, Target = o.DockWith };
            }
            else if (o.Goto != null)
            {
                obj.Dock = new DockAction()
                    { Kind = DockKinds.Jump, Target = o.Goto.System, Exit = o.Goto.Exit, Tunnel = o.Goto.TunnelEffect };
            }

            if (o.Rotate != null)
            {
                obj.Rotation = MathHelper.MatrixFromEulerDegrees(o.Rotate.Value).ExtractRotation();
            }

            obj.Archetype = Archetypes.Get(o.Archetype);

            obj.TradelaneSpaceName = o.TradelaneSpaceName;
            if (o.NextRing != null && o.TradelaneSpaceName != 0)
            {
                obj.IdsLeft = o.TradelaneSpaceName;
            }
            else if (o.PrevRing != null && o.TradelaneSpaceName != 0)
            {
                obj.IdsRight = o.TradelaneSpaceName;
            }

            if (obj.Archetype?.Type == ArchetypeType.tradelane_ring)
            {
                obj.Dock = new DockAction()
                {
                    Kind = DockKinds.Tradelane,
                    Target = o.NextRing,
                    TargetLeft = o.PrevRing
                };
            }
            else if (obj.Archetype == null)
            {
                FLLog.Error("Systems",
                    $"Object {obj.Nickname} in {system} has bad archetype '{o.Archetype ?? "NULL"}'");
                return null;
            }

            obj.Star = Stars.Get(o.Star);
            obj.Loadout = GetLoadout(o.Loadout);
            return obj;
        }

        void InitFuses()
        {
            foreach (var fuse in flData.Fuses.Fuses)
            {
                var fr = new FuseResources() { Fuse = fuse };
                fr.Nickname = fuse.Name;
                fr.CRC = FLHash.CreateID(fuse.Name);
                foreach (var act in fuse.Actions)
                {
                    if (act is FuseStartEffect fza)
                    {
                        if (string.IsNullOrEmpty(fza.Effect)) continue;
                        if (!fr.Fx.ContainsKey(fza.Effect))
                        {
                            fr.Fx[fza.Effect] = Effects.Get(fza.Effect);
                        }
                    }
                }

                Fuses.Add(fr);
            }
        }

        EffectEquipment GetAttachedFx(AttachedFx fx)
        {
            var equip = new EffectEquipment()
            {
                Particles = Effects.Get(fx.Particles),
            };
            return equip;
        }

        LightEquipment GetLight(LightInheritHelper lt)
        {
            var equip = new LightEquipment();
            equip.Color = lt.Color ?? Color3f.White;
            equip.MinColor = lt.MinColor ?? Color3f.Black;
            equip.GlowColor = lt.GlowColor ?? equip.Color;
            equip.BulbSize = lt.BulbSize ?? 1f;
            equip.GlowSize = lt.GlowSize ?? 1f;
            equip.AlwaysOn = lt.AlwaysOn ?? true;
            equip.DockingLight = lt.DockingLight ?? false;
            equip.EmitRange = lt.EmitRange ?? 0;
            equip.EmitAttenuation = lt.EmitAttenuation ?? new Vector3(1, 0.01f, 0.000055f);
            if (lt.AvgDelay != null)
            {
                equip.Animated = true;
                equip.AvgDelay = lt.AvgDelay.Value;
                equip.BlinkDuration = lt.BlinkDuration.Value;
            }

            return equip;
        }

        class EffectStorage
        {
            public Dictionary<string, BeamBolt> BeamBolts =
                new Dictionary<string, BeamBolt>(StringComparer.OrdinalIgnoreCase);

            public Dictionary<string, BeamSpear> BeamSpears =
                new Dictionary<string, BeamSpear>(StringComparer.OrdinalIgnoreCase);

            public Dictionary<string, VisEffect> VisFx =
                new Dictionary<string, VisEffect>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
