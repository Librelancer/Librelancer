// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security;
using LibreLancer.Data;
using LibreLancer.Data.Effects;
using LibreLancer.Data.Equipment;
using LibreLancer.Data.Fuses;
using LibreLancer.Data.Goods;
using LibreLancer.Data.Missions;
using LibreLancer.Data.Solar;
using LibreLancer.GameData;
using LibreLancer.GameData.Archetypes;
using LibreLancer.GameData.Items;
using LibreLancer.GameData.Market;
using LibreLancer.GameData.World;
using LibreLancer.Graphics;
using LibreLancer.Physics;
using LibreLancer.Render;
using LibreLancer.Thorn.VM;
using LibreLancer.Utf.Anm;
using Archetype = LibreLancer.GameData.Archetype;
using Asteroid = LibreLancer.GameData.Asteroid;
using DockSphere = LibreLancer.GameData.World.DockSphere;
using DynamicAsteroid = LibreLancer.GameData.DynamicAsteroid;
using DynamicAsteroids = LibreLancer.Data.Universe.DynamicAsteroids;
using FileSystem = LibreLancer.Data.IO.FileSystem;
using Spine = LibreLancer.GameData.World.Spine;

namespace LibreLancer
{
    public class GameDataManager
    {
        public ThornReadFile ThornReadCallback;
        public Data.FreelancerData Ini => fldata;
        Data.FreelancerData fldata;
        ResourceManager resource;
        GameResourceManager glResource;
        List<GameData.IntroScene> IntroScenes;
        public FileSystem VFS;
        public GameDataManager(FileSystem vfs, ResourceManager resman)
        {
            resource = resman;
            glResource = (resource as GameResourceManager);
            VFS = vfs;
            var flini = new Data.FreelancerIni(VFS);
            fldata = new Data.FreelancerData(flini, VFS);
            ThornReadCallback = (file) => VFS.ReadAllBytes("EXE/" + file);
        }
        public string DataVersion => fldata.DataVersion;

        public string DataPath(string input)
        {
            if (input == null) return null;
            var path = fldata.Freelancer.DataPath + input;
            if (!VFS.FileExists(path)) {
                FLLog.Error("GameData", $"File {fldata.Freelancer.DataPath}{input} not found");
                return null;
            }
            return path;
        }

        public Dictionary<string, string> GetBaseNavbarIcons()
        {
            return fldata.BaseNavBar.Navbar;
        }

        public List<string> GetIntroMovies()
        {
            var movies = new List<string>();
            foreach (var file in fldata.Freelancer.StartupMovies)
            {
                var path = DataPath(file);
                if (path != null)
                    movies.Add(path);
            }
            return movies;
        }

        private AnmFile characterAnimations;
        public AnmFile GetCharacterAnimations()
        {
            if (characterAnimations == null)
            {
                characterAnimations = new AnmFile();
                var stringTable = new StringDeduplication();
                foreach (var file in fldata.Bodyparts.Animations)
                {
                    var path = DataPath(file);
                    using var stream = VFS.Open(path);
                    AnmFile.ParseToTable(characterAnimations.Scripts, characterAnimations.Buffer, stringTable, stream, path);
                }
                characterAnimations.Buffer.Shrink();
            }
            return characterAnimations;
        }


        public bool GetCostume(string costume, out Bodypart body, out Bodypart head, out Bodypart leftHand, out Bodypart rightHand)
        {
            var cs = fldata.Costumes.FindCostume(costume);
            head = Bodyparts.Get(cs.Head);
            body = Bodyparts.Get(cs.Body);
            leftHand = Bodyparts.Get(cs.LeftHand);
            rightHand = Bodyparts.Get(cs.RightHand);
            return true;
        }

        public string GetCostumeForNPC(string npc)
        {
            return Ini.SpecificNPCs.Npcs.FirstOrDefault(x => x.Nickname.Equals(npc, StringComparison.OrdinalIgnoreCase))
                ?.BaseAppr;
        }

        bool TryResolveThn(string path, out ResolvedThn r)
        {
            r = null;
            if (path == null) return false;
            var resolved = DataPath(path);
            if (VFS.FileExists(resolved))
            {
                r = new ResolvedThn() {SourcePath = path, VFS = VFS, DataPath = resolved, ReadCallback = ThornReadCallback};
                return true;
            }
            return false;
        }

        ResolvedThn ResolveThn(string path)
        {
            if (path == null) return null;
            return new() {SourcePath = path, VFS = VFS, DataPath = DataPath(path), ReadCallback = ThornReadCallback};
        }

        class EffectStorage
        {
            public Dictionary<string, VisEffect> VisFx = new Dictionary<string, VisEffect>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, BeamSpear> BeamSpears = new Dictionary<string, BeamSpear>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, BeamBolt> BeamBolts = new Dictionary<string, BeamBolt>(StringComparer.OrdinalIgnoreCase);
        }
        private EffectStorage fxdata;
        public GameItemCollection<ResolvedFx> Effects;
        public GameItemCollection<ResolvedFx> VisEffects;
        void InitEffects()
        {
            fxdata = new EffectStorage();
            foreach (var fx in fldata.Effects.VisEffects)
                fxdata.VisFx[fx.Nickname] = fx;
            foreach (var fx in fldata.Effects.BeamSpears)
                fxdata.BeamSpears[fx.Nickname] = fx;
            foreach (var fx in fldata.Effects.BeamBolts)
                fxdata.BeamBolts[fx.Nickname] = fx;
            Effects = new GameItemCollection<ResolvedFx>();
            VisEffects = new GameItemCollection<ResolvedFx>();
            foreach (var fx in fldata.Effects.VisEffects)
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
            foreach (var effect in fldata.Effects.Effects)
            {
                VisEffect visfx = null;
                BeamSpear spear = null;
                BeamBolt bolt = null;
                if(!string.IsNullOrWhiteSpace(effect.VisEffect))
                    fxdata.VisFx.TryGetValue(effect.VisEffect, out visfx);
                if (!string.IsNullOrWhiteSpace(effect.VisBeam))
                {
                    fxdata.BeamSpears.TryGetValue(effect.VisBeam, out spear);
                    fxdata.BeamBolts.TryGetValue(effect.VisBeam, out bolt);
                }
                string alepath = null;
                if (!string.IsNullOrWhiteSpace(visfx?.AlchemyPath))
                {
                    alepath = DataPath(visfx.AlchemyPath);
                }
                var lib = visfx != null
                    ? visfx.Textures.Select(DataPath).Where(x => x != null).ToArray()
                    : null;
                Effects.Add(new ResolvedFx()
                {
                    AlePath = alepath,
                    VisFxCrc = (uint)(visfx?.EffectCrc ?? 0),
                    LibraryFiles = lib,
                    Spear = spear,
                    Bolt = bolt,
                    CRC = FLHash.CreateID(effect.Nickname),
                    Nickname = effect.Nickname
                });
            }
        }

        IEnumerable<Data.Universe.Base> InitBases(LoadingTasks tasks)
        {
            FLLog.Info("Game", "Initing " + fldata.Universe.Bases.Count + " bases");
            Dictionary<string, MBase> mbases = new(StringComparer.OrdinalIgnoreCase);
            foreach (var mbase in fldata.MBases.MBases)
            {
                mbases[mbase.Nickname] = mbase;
            }
            foreach (var inibase in fldata.Universe.Bases)
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
                    if (mbase.MVendor != null) {
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
                        if(room.RoomInfo?.SceneScripts != null)
                            foreach (var e in room.RoomInfo.SceneScripts)
                                nr.SceneScripts.Add(new SceneScript(e.AmbientAll, e.TrafficPriority, ResolveThn(e.Path)));
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
                    if (room.Nickname.Equals(inibase.BaseInfo?.StartRoom, StringComparison.OrdinalIgnoreCase)) b.StartRoom = nr;
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
            fldata.MBases = null; //Free memory
        }

        public GameItemCollection<ResolvedGood> Goods;
        Dictionary<string, long> shipPrices = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);


        private Dictionary<string, string> shipToIcon =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

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
            FLLog.Info("Game", "Initing " + fldata.Goods.Goods.Count + " goods");
            Goods = new GameItemCollection<ResolvedGood>();
            Dictionary<string, Data.Goods.Good> hulls = new Dictionary<string, Data.Goods.Good>(256, StringComparer.OrdinalIgnoreCase);
            List<Data.Goods.Good> ships = new List<Good>();
            foreach (var g in fldata.Goods.Goods)
            {
                switch (g.Category)
                {
                    case Data.Goods.GoodCategory.ShipHull:
                        hulls[g.Nickname] = g;
                        shipToIcon[g.Ship] = g.ItemIcon;
                        shipPrices[g.Ship] = g.Price;
                        break;
                    case Data.Goods.GoodCategory.Ship:
                        ships.Add(g);
                        break;
                    case Data.Goods.GoodCategory.Equipment:
                    case Data.Goods.GoodCategory.Commodity:
                        if (Equipment.TryGetValue(g.Nickname, out var equip))
                        {
                            var good = new ResolvedGood() {Nickname = g.Nickname, Equipment = equip, Ini = g, CRC = CrcTool.FLModelCrc(g.Nickname) };
                            equip.Good = good;
                            Goods.Add(good);
                        }
                        break;
                }
            }

            foreach (var g in ships)
            {
                Data.Goods.Good hull = hulls[g.Hull];
                var sp = new GameData.Market.ShipPackage();
                sp.Ship = hull.Ship;
                sp.Nickname = g.Nickname;
                sp.CRC = FLHash.CreateID(sp.Nickname);
                sp.BasePrice = hull.Price;
                foreach (var addon in g.Addons) {
                    if (Equipment.TryGetValue(addon.Equipment, out var equip))
                    {
                        sp.Addons.Add(new PackageAddon()
                        {
                            Equipment  = equip,
                            Hardpoint = addon.Hardpoint,
                            Amount = addon.Amount
                        });
                    }
                }
                shipPackages.Add(g.Nickname, sp);
                shipPackageByCRC.Add(sp.CRC, sp);
            }
            fldata.Goods = null; //Free memory
        }
        void InitMarkets()
        {
            FLLog.Info("Game", "Initing " + fldata.Markets.BaseGoods.Count + " shops");
            foreach (var m in fldata.Markets.BaseGoods)
            {
                Base b;
                if(!Bases.TryGetValue(m.Base, out b))
                {
                    //This is allowed by demo at least
                    FLLog.Warning("Market", "BaseGoods references nonexistent base " + m.Base);
                    continue;
                }
                foreach (var gd in m.MarketGoods)
                {
                    GameData.Market.ShipPackage sp;
                    if (shipPackages.TryGetValue(gd.Good, out sp))
                    {
                        if(gd.Min != 0 || gd.Max != 0) //Vanilla adds disabled ships ??? (why)
                            b.SoldShips.Add(new GameData.Market.SoldShip() { Package = sp });
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
            fldata.Markets = null; //Free memory
        }
        Dictionary<string, GameData.Market.ShipPackage> shipPackages = new Dictionary<string, GameData.Market.ShipPackage>();
        private Dictionary<uint, GameData.Market.ShipPackage> shipPackageByCRC = new Dictionary<uint, ShipPackage>();

        public FormationDef GetFormation(string form) =>
            string.IsNullOrWhiteSpace(form) ? null :
            fldata.Formations.Formations.FirstOrDefault(
                x => form.Equals(x.Nickname, StringComparison.OrdinalIgnoreCase));

        public ShipPackage GetShipPackage(uint crc)
        {
            shipPackageByCRC.TryGetValue(crc, out var pkg);
            return pkg;
        }

        void InitFactions()
        {
            FLLog.Info("Factions", $"Initing {fldata.InitialWorld.Groups.Count} factions");
            foreach (var f in fldata.InitialWorld.Groups)
            {
                var fac = new Faction() {
                    Nickname = f.Nickname,
                    IdsInfo = f.IdsInfo,
                    IdsName = f.IdsName,
                    IdsShortName = f.IdsShortName,
                    Properties = fldata.FactionProps.FactionProps.FirstOrDefault(x => x.Affiliation.Equals(f.Nickname, StringComparison.OrdinalIgnoreCase))
                };
                fac.Hidden = fldata.Freelancer.HiddenFactions.Contains(fac.Nickname, StringComparer.OrdinalIgnoreCase);
                fac.CRC = CrcTool.FLModelCrc(fac.Nickname);
                Factions.Add(fac);
            }

            foreach (var f in fldata.InitialWorld.Groups)
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
                var emp = fldata.Empathy.RepChangeEffects.FirstOrDefault(x => x.Group.Equals(us.Nickname));
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

        public void LoadData(IUIThread ui, Action onIniLoaded = null)
        {
            fldata.LoadData();
            if (glResource != null && ui != null)
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
            }
            if(onIniLoaded != null) ui.QueueUIThread(onIniLoaded);
            var tasks = new LoadingTasks();
            if(glResource != null)
                tasks.Begin(() => GetCharacterAnimations());
            var pilotTask = tasks.Begin(InitPilots);
            var effectsTask = tasks.Begin(InitEffects);
            var fusesTask = tasks.Begin(InitFuses, effectsTask);
            var explosionTask = tasks.Begin(InitExplosions, effectsTask);
            var debrisTask = tasks.Begin(InitDebris);
            var shipsTask = tasks.Begin(InitShips, explosionTask, fusesTask, debrisTask);
            List<Data.Universe.Base> introbases = new List<Data.Universe.Base>();
            var baseTask = tasks.Begin(() => introbases.AddRange(InitBases(tasks)));
            tasks.Begin(() =>
            {
                FLLog.Info("Game", "Loading intro scenes");
                IntroScenes = new List<GameData.IntroScene>();
                foreach (var b in introbases)
                {
                    foreach (var room in b.Rooms)
                    {
                        if (room.Nickname == b.BaseInfo?.StartRoom)
                        {
                            var isc = new GameData.IntroScene();
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
            tasks.WaitAll();
            fldata.Universe = null; //Free universe ini!
            GC.Collect(); //We produced a crapload of garbage
        }



        bool cursorsDone = false;
        public void PopulateCursors()
        {
            if (cursorsDone) return;
            cursorsDone = true;

            resource.LoadResourceFile(
                DataPath(fldata.Mouse.TxmFile)
            );
            foreach (var lc in fldata.Mouse.Cursors)
            {
                var shape = fldata.Mouse.Shapes.Where((arg) => arg.Name.Equals(lc.Shape, StringComparison.OrdinalIgnoreCase)).First();
                var cur = new Cursor();
                cur.Nickname = lc.Nickname;
                cur.Scale = lc.Scale;
                cur.Spin = lc.Spin;
                cur.Color = lc.Color;
                cur.Hotspot = lc.Hotspot;
                cur.Dimensions = shape.Dimensions;
                cur.Texture = fldata.Mouse.TextureName;
                glResource.AddCursor(cur, cur.Nickname);
            }
        }

        public IEnumerable<Data.Audio.AudioEntry> AllSounds => fldata.Audio.Entries;
        public Data.Audio.AudioEntry GetAudioEntry(string id)
        {
            return fldata.Audio.Entries.Where((arg) => arg.Nickname.ToLowerInvariant() == id.ToLowerInvariant()).First();
        }
        public Stream GetAudioStream(string id)
        {
            var audio = fldata.Audio.Entries.Where((arg) => arg.Nickname.ToLowerInvariant() == id.ToLowerInvariant()).First();
            if (VFS.FileExists(DataPath(audio.File)))
                return VFS.Open(DataPath(audio.File));
            return null;
        }
        public string GetVoicePath(string id)
        {
            return DataPath("AUDIO\\" + id + ".utf");
        }

        public string GetInfocardText(int id, FontManager fonts)
        {
            var res = fldata.Infocards.GetXmlResource(id);
            if (res == null) return null;
            return Infocards.RDLParse.Parse(res, fonts).ExtractText();
        }
        public Infocards.Infocard GetInfocard(int id, FontManager fonts)
        {
            return Infocards.RDLParse.Parse(fldata.Infocards.GetXmlResource(id), fonts);
        }

        public bool GetRelatedInfocard(int ogId, FontManager fonts, out Infocards.Infocard ic)
        {
            ic = null;
            if (fldata.InfocardMap.Map.TryGetValue(ogId, out int newId))
            {
                ic = GetInfocard(newId, fonts);
                return true;
            }
            return false;
        }
        public string GetString(int id)
        {
            return fldata.Infocards.GetStringResource(id);
        }
        public GameData.IntroScene GetIntroScene()
        {
            var rand = new Random();
            return IntroScenes[rand.Next(0, IntroScenes.Count)];
        }
#if DEBUG
        public GameData.IntroScene GetIntroSceneSpecific(int i)
        {
            if (i > IntroScenes.Count)
                return null;
            return IntroScenes[i];
        }
#endif

        public Texture2D GetSplashScreen()
        {
            if (!glResource.TextureExists("__startupscreen_1280.tga"))
            {
                if (VFS.FileExists(fldata.Freelancer.DataPath + "INTERFACE/INTRO/IMAGES/startupscreen_1280.tga"))
                {
                    glResource.AddTexture(
                        "__startupscreen_1280.tga",
                        DataPath("INTERFACE/INTRO/IMAGES/startupscreen_1280.tga")
                    );
                } else if (VFS.FileExists(fldata.Freelancer.DataPath + "INTERFACE/INTRO/IMAGES/startupscreen.tga"))
                {
                    glResource.AddTexture(
                        "__startupscreen_1280.tga",
                        DataPath("INTERFACE/INTRO/IMAGES/startupscreen.tga")
                    );
                }
                else
                {
                    FLLog.Error("Splash", "Splash screen not found");
                    return resource.WhiteTexture;
                }

            }
            return (Texture2D)resource.FindTexture("__startupscreen_1280.tga");
        }

        public IEnumerable<Maneuver> GetManeuvers()
        {
            foreach (var m in fldata.Hud.Maneuvers)
            {
                yield return new Maneuver()
                {
                    Action = m.Action,
                    InfocardA = fldata.Infocards.GetStringResource(m.InfocardA),
                    InfocardB = fldata.Infocards.GetStringResource(m.InfocardB),
                    ActiveModel = m.ActiveModel,
                    InactiveModel = m.InactiveModel,
                };
            }
        }

        void PreloadSur(IDrawable dr, ResourceManager res)
        {
            if (dr is not IRigidModelFile rm)
                return;
            var mdl = rm.CreateRigidModel(res is GameResourceManager, res);
            var surpath = Path.ChangeExtension(mdl.Path, ".sur");
            if (!File.Exists(surpath))
                return;
            var cvx = res.ConvexCollection.UseFile(surpath);
            if(mdl.Source == RigidModelSource.SinglePart)
                res.ConvexCollection.CreateShape(cvx, new ConvexMeshId(0,0));
            else
            {
                foreach(var p in mdl.AllParts)
                    res.ConvexCollection.CreateShape(cvx, new ConvexMeshId(0, CrcTool.FLModelCrc(p.Name)));
            }
        }

        public void PreloadObjects(PreloadObject[] objs, ResourceManager resources = null)
        {
            resources ??= resource;
            if (objs == null) return;
            foreach(var o in objs) {
                if (o.Type == PreloadType.Ship)
                {
                    foreach (var v in o.Values)
                    {
                        var sh = Ships.Get(v);
                        sh?.ModelFile?.LoadFile(resources);
                    }
                }
                else if (o.Type == PreloadType.Equipment)
                {
                    foreach (var v in o.Values)
                    {
                        var eq = Equipment.Get(v);
                        eq?.ModelFile?.LoadFile(resources);
                    }
                }
            }
        }

        void InitBodyParts()
        {
            foreach (var p in fldata.Bodyparts.Bodyparts)
            {
                var b = new Bodypart();
                b.Nickname = p.Nickname;
                b.CRC = FLHash.CreateID(b.Nickname);
                b.Path = DataPath(p.Mesh);
                b.Sex = p.Sex;
                Bodyparts.Add(b);
            }

            foreach (var src in fldata.Bodyparts.Accessories)
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

        public GameItemCollection<GameData.Accessory> Accessories = new GameItemCollection<Accessory>();

        public GameItemCollection<GameData.Bodypart> Bodyparts = new GameItemCollection<Bodypart>();

        public GameItemCollection<GameData.Explosion> Explosions = new GameItemCollection<GameData.Explosion>();

        public GameItemCollection<Equipment> Equipment = new GameItemCollection<Equipment>();

        public GameItemCollection<Faction> Factions = new GameItemCollection<Faction>();

        public GameItemCollection<Asteroid> Asteroids = new GameItemCollection<Asteroid>();
        public GameItemCollection<DynamicAsteroid> DynamicAsteroids = new();

        void InitEquipment()
        {
            FLLog.Info("Game", "Initing " + fldata.Equipment.Equip.Count + " equipments");
            Dictionary<string, LightInheritHelper> lights = new Dictionary<string, LightInheritHelper>(StringComparer.OrdinalIgnoreCase);

            void SetCommonFields(Equipment equip, Data.Equipment.AbstractEquipment val)
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
            foreach (var mn in fldata.Equipment.Munitions)
            {
                Equipment equip;
                if (!string.IsNullOrEmpty(mn.Motor))
                {
                    var mequip = new GameData.Items.MissileEquip()
                    {
                        Def = mn,
                        ModelFile = ResolveDrawable(mn.MaterialLibrary, mn.DaArchetype),
                        Motor = fldata.Equipment.Motors.FirstOrDefault(x => x.Nickname.Equals(mn.Motor, StringComparison.OrdinalIgnoreCase)),
                        Explosion = fldata.Equipment.Explosions.FirstOrDefault(x =>  x.Nickname.Equals(mn.ExplosionArch, StringComparison.OrdinalIgnoreCase))
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
                    var mequip = new GameData.Items.MunitionEquip()
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
            foreach (var val in fldata.Equipment.Equip)
            {
                GameData.Items.Equipment equip = null;
                if (val is Data.Equipment.Light l)
                {
                    lights.Add(val.Nickname, new LightInheritHelper(l));
                }
                else if (val is Data.Equipment.InternalFx)
                {
                    var eq = new GameData.Items.AnimationEquipment();
                    eq.Animation = ((Data.Equipment.InternalFx)val).UseAnimation;
                    equip = eq;
                }
                if (val is Data.Equipment.AttachedFx)
                {
                    equip = GetAttachedFx((Data.Equipment.AttachedFx)val);
                }
                if (val is Data.Equipment.PowerCore pc)
                {
                    var eqp = new GameData.Items.PowerEquipment();
                    eqp.Def = pc;
                    eqp.ModelFile = ResolveDrawable(pc.MaterialLibrary, pc.DaArchetype);
                    equip = eqp;
                }
                if (val is Data.Equipment.CountermeasureDropper cms)
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
                else if (val is Data.Equipment.Gun gn)
                {
                    Equipment.TryGetValue(gn.ProjectileArchetype, out Equipment mnEquip);
                    if (mnEquip is MunitionEquip mn)
                    {
                        var eqp = new GameData.Items.GunEquipment()
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
                        var eqp = new GameData.Items.MissileLauncherEquipment()
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
                if (val is Data.Equipment.Thruster th)
                {
                    var eqp = new GameData.Items.ThrusterEquipment()
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
                if (val is Data.Equipment.ShieldGenerator sh)
                {
                    var eqp = new GameData.Items.ShieldEquipment()
                    {
                        HpType = sh.HpType,
                        Def = sh
                    };
                    eqp.ModelFile = ResolveDrawable(sh.MaterialLibrary, sh.DaArchetype);
                    equip = eqp;
                }
                if (val is Data.Equipment.Scanner sc)
                {
                    var eq = new GameData.Items.ScannerEquipment
                    {
                        Def = sc
                    };
                    equip = eq;
                }
                if (val is Data.Equipment.Tractor tc)
                {
                    var eq = new GameData.Items.TractorEquipment
                    {
                        Def = tc
                    };
                    equip = eq;
                }

                if (val is Data.Equipment.LootCrate lc)
                {
                    var eq = new GameData.Items.LootCrateEquipment();
                    equip = eq;
                }

                if (val is Data.Equipment.Engine deng)
                {
                    var engequip = new EngineEquipment() {Def = deng};
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

                if (val is Data.Equipment.Commodity cm)
                    equip = new GameData.Items.CommodityEquipment();
                if(equip == null)
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
            fldata.Equipment = null; //Free memory
        }

        Dictionary<string, Vector3> quadratics = new Dictionary<string, Vector3>();
        Vector3 GetQuadratic(string attenCurve)
        {
            Vector3 q;
            if (!quadratics.TryGetValue(attenCurve, out q))
            {
                q = ApproximateCurve.GetQuadraticFunction(fldata.Graphs.FindFloatGraph(attenCurve).Points.ToArray());
                quadratics.Add(attenCurve, q);
            }
            return q;
        }

        public GameItemCollection<StarSystem> Systems = new GameItemCollection<StarSystem>();
        public GameItemCollection<Base> Bases = new GameItemCollection<Base>();

        void InitLoadouts()
        {
            _loadouts = new Dictionary<string, ObjectLoadout>(StringComparer.OrdinalIgnoreCase);
            foreach (var l in fldata.Loadouts.Loadouts)
            {
                var ld = new ObjectLoadout() { Nickname = l.Nickname, Archetype = l.Archetype };
                foreach (var eq in l.Equip)
                {
                    GameData.Items.Equipment equip = Equipment.Get(eq.Nickname);
                    if (equip != null)
                    {
                        ld.Items.Add(new LoadoutItem(eq.Hardpoint, equip));
                    }
                }
                foreach (var c in l.Cargo)
                {
                    GameData.Items.Equipment equip = Equipment.Get(c.Nickname);
                    if(equip != null)
                        ld.Cargo.Add(new BasicCargo(equip, c.Count));
                }
                _loadouts[l.Nickname] = ld;
            }
        }

        void InitSystems(LoadingTasks tasks)
        {
            FLLog.Info("Game", "Initing " + fldata.Universe.Systems.Count + " systems");
            foreach (var inisys in fldata.Universe.Systems)
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
                foreach(var a in inisys.Preloads.SelectMany(x => x.ArchetypeShip))
                    p.Add(new PreloadObject(PreloadType.Ship, a));
                foreach(var a in inisys.Preloads.SelectMany(x => x.ArchetypeSimple))
                    p.Add(new PreloadObject(PreloadType.Simple, a));
                foreach(var a in inisys.Preloads.SelectMany(x => x.ArchetypeEquipment))
                    p.Add(new PreloadObject(PreloadType.Equipment, a));
                foreach(var a in inisys.Preloads.SelectMany(x => x.ArchetypeSnd))
                    p.Add(new PreloadObject(PreloadType.Sound, a));
                foreach(var a in inisys.Preloads.SelectMany(x => x.ArchetypeSolar))
                    p.Add(new PreloadObject(PreloadType.Solar, a));
                foreach(var a in inisys.Preloads.SelectMany(x => x.ArchetypeVoice))
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
                        if(src.Pos.HasValue)
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
                        lt.Kind = ((src.Type ?? Data.Universe.LightType.Point) == Data.Universe.LightType.Point) ? LightKind.Point : LightKind.Directional;
                        lt.Attenuation = src.Attenuation ?? Vector3.UnitY;
                        if (src.AttenCurve != null)
                        {
                            lt.Kind = LightKind.PointAttenCurve;
                            lt.Attenuation = GetQuadratic(src.AttenCurve);
                        }
                        sys.LightSources.Add(new LightSource() {Light = lt, AttenuationCurveName = src.AttenCurve, Nickname = src.Nickname});
                    }
                }

                var objDict = new Dictionary<string, LibreLancer.Data.Universe.SystemObject>(StringComparer.OrdinalIgnoreCase);
                foreach (var obj in inisys.Objects)
                {
                    var o = GetSystemObject(inisys.Nickname, obj);
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
                        if (i++ > 5000) {
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
                        if (i++ > 5000) {
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
                        z.PropertyFlags = (ZonePropFlags) zne.PropertyFlags;
                        z.PropertyFogColor = zne.PropertyFogColor;
                        z.VisitFlags = (VisitFlags) (zne.Visit ?? 0);
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
                        if(zne.Pos == null) FLLog.Warning("Zone", $"Zone {zne.Nickname} in {inisys.Nickname} has no position");
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
                        z.Shape = (ShapeKind)(int)(zne.Shape ?? Data.Universe.ZoneShape.SPHERE);
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
        public IEnumerator<object> LoadSystemResources(StarSystem sys)
        {
            if (fldata.Stars != null)
            {
                foreach (var txmfile in fldata.Stars.TextureFiles
                             .SelectMany(x => x.Files))
                    resource.LoadResourceFile(DataPath(txmfile));
            }
            yield return null;
            sys.StarsBasic?.LoadFile(resource);
            sys.StarsComplex?.LoadFile(resource);
            sys.StarsNebula?.LoadFile(resource);
            yield return null;
            long a = 0;
            if (glResource != null)
            {
                foreach (var obj in sys.Objects)
                {
                    obj.Archetype.ModelFile?.LoadFile(glResource);
                    if (a % 3 == 0) yield return null;
                    a++;
                }
            }
            foreach (var resfile in sys.ResourceFiles)
            {
                resource.LoadResourceFile(resfile);
                if (a % 3 == 0) yield return null;
                a++;
            }
        }

        public void LoadAllSystem(StarSystem system)
        {
            var iterator = LoadSystemResources(system);
            while (iterator.MoveNext()) { }
        }


        Dictionary<string, ResolvedTexturePanels> tpanels = new Dictionary<string, ResolvedTexturePanels>(StringComparer.OrdinalIgnoreCase);
        private object tPanelsLock = new object();
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
                    var pf = new Data.Universe.TexturePanels(f, VFS);
                    pnl = new ResolvedTexturePanels() { SourcePath = srcPath };
                    foreach(var s in pf.Shapes)
                        pnl.Shapes.Add(s.Key, new RenderShape(s.Value.TextureName, s.Value.Dimensions));
                    pnl.TextureShapes = pf.TextureShapes;
                    pnl.LibraryFiles = pf.Files.Select(DataPath).ToArray();
                    tpanels.Add(f, pnl);
                }
                return pnl;
            }
        }


        AsteroidField GetAsteroidField(StarSystem sys, Data.Universe.AsteroidField ast)
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
                        FLLog.Error("System", "Exclusion zone " + excz.ZoneName + " zone does not exist in " + sys.Nickname);
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
        public Nebula GetNebula(StarSystem sys, Data.Universe.Nebula nbl)
        {
            var n = new Nebula();
            n.SourceFile = nbl.IniFile;
            n.Zone = sys.ZoneDict[nbl.ZoneName];
            foreach(var f in nbl.TexturePanels.Files)
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
            if (nbl.Exterior != null && nbl.Exterior.Shape != null)
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
                        FLLog.Error("System", "Exclusion zone " + excz.ZoneName + " zone does not exist in " + sys.Nickname);
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

        public GameItemCollection<Ship> Ships = new GameItemCollection<Ship>();

        public GameItemCollection<Archetype> Archetypes = new GameItemCollection<Archetype>();



        ResolvedModel ResolveDrawable(string file) => ResolveDrawable((IEnumerable<string>) null, file);

        ResolvedModel ResolveDrawable(string libs, string file) => ResolveDrawable(new[] {libs}, file);
        ResolvedModel ResolveDrawable(IEnumerable<string> libs, string file)
        {
            if (string.IsNullOrWhiteSpace(file)) return null;
            var mdl = new ResolvedModel() {
                ModelFile = DataPath(file),
                SourcePath = file,
            };
            if (libs != null)
                mdl.LibraryFiles = libs.Select(x => DataPath(x)).Where(x => x != null).ToArray();
            return mdl;
        }



        void FillBlock<T>(string nick, string blockId, List<T> source, ref T dest) where T : Data.Pilots.PilotBlock
        {
            if (!string.IsNullOrEmpty(blockId))
            {
                var obj = source.FirstOrDefault(x => x.Nickname.Equals(blockId, StringComparison.OrdinalIgnoreCase));
                if (obj == null) {
                    FLLog.Warning("Pilot", $"{nick}: Unable to find {typeof(T).Name} '{blockId}'");
                } else {
                    dest = obj;
                }
            }
        }

        void FillPilot(Pilot pilot, Data.Pilots.Pilot src)
        {
            if (src.Inherit != null)
            {
                var parent = fldata.Pilots.Pilots.FirstOrDefault(x =>
                    x.Nickname.Equals(src.Inherit, StringComparison.OrdinalIgnoreCase));
                if(parent == null)
                    FLLog.Error("Data", $"Pilot {src.Nickname} references missing inherit {src.Inherit}");
                else
                    FillPilot(pilot, parent);
            }

            string n = src.Nickname;
            FillBlock(n,src.BuzzHeadTowardId, fldata.Pilots.BuzzHeadTowardBlocks, ref pilot.BuzzHeadToward);
            FillBlock(n, src.BuzzPassById, fldata.Pilots.BuzzPassByBlocks, ref pilot.BuzzPassBy);
            FillBlock(n, src.CountermeasureId, fldata.Pilots.CountermeasureBlocks, ref pilot.Countermeasure);
            FillBlock(n, src.DamageReactionId, fldata.Pilots.DamageReactionBlocks, ref pilot.DamageReaction);
            FillBlock(n, src.EngineKillId, fldata.Pilots.EngineKillBlocks, ref pilot.EngineKill);
            FillBlock(n, src.EvadeBreakId, fldata.Pilots.EvadeBreakBlocks, ref pilot.EvadeBreak);
            FillBlock(n, src.EvadeDodgeId, fldata.Pilots.EvadeDodgeBlocks, ref pilot.EvadeDodge);
            FillBlock(n, src.FormationId, fldata.Pilots.FormationBlocks, ref pilot.Formation);
            FillBlock(n, src.GunId, fldata.Pilots.GunBlocks, ref pilot.Gun);
            FillBlock(n, src.JobId, fldata.Pilots.JobBlocks, ref pilot.Job);
            FillBlock(n, src.MineId, fldata.Pilots.MineBlocks, ref pilot.Mine);
            FillBlock(n, src.MissileId, fldata.Pilots.MissileBlocks, ref pilot.Missile);
            FillBlock(n, src.MissileReactionId, fldata.Pilots.MissileReactionBlocks, ref pilot.MissileReactionBlock);
            FillBlock(n, src.RepairId, fldata.Pilots.RepairBlocks, ref pilot.Repair);
            FillBlock(n, src.StrafeId, fldata.Pilots.StrafeBlocks, ref pilot.Strafe);
            FillBlock(n, src.TrailId, fldata.Pilots.TrailBlocks, ref pilot.Trail);
        }

        private Dictionary<string, Pilot> pilots = new Dictionary<string, Pilot>(StringComparer.OrdinalIgnoreCase);

        public Pilot GetPilot(string nickname)
        {
            if(string.IsNullOrEmpty(nickname)) return null;
            pilots.TryGetValue(nickname, out var p);
            return p;
        }

        void InitPilots()
        {
            FLLog.Info("Game", "Initing Pilots");
            foreach (var orig in fldata.Pilots.Pilots)
            {
                var p = new Pilot() {Nickname = orig.Nickname};
                FillPilot(p, orig);
                pilots[p.Nickname] = p;
            }
        }

        public GameItemCollection<SimpleObject> SimpleObjects = new();
        public GameItemCollection<DebrisInfo> Debris = new();

        void InitDebris()
        {
            FLLog.Info("Game", "Initing Debris");
            foreach (var orig in fldata.Ships.Simples
                         .Concat(fldata.Solar.Simples)
                         .Concat(fldata.Explosions.Simples))

            {
                var db = new SimpleObject();
                db.Nickname = orig.Nickname;
                db.CRC = FLHash.CreateID(orig.Nickname);
                db.Model = ResolveDrawable(orig.MaterialLibrary, orig.DaArchetypeName);
                SimpleObjects.Add(db);
            }

            foreach (var orig in fldata.Explosions.Debris)
            {
                var db = new DebrisInfo();
                db.Nickname = orig.Nickname;
                db.CRC = FLHash.CreateID(orig.Nickname);
                db.Lifetime = orig.Lifetime;
                Debris.Add(db);
            }
            fldata.Ships.Simples = null;
            fldata.Solar.Simples = null;
            fldata.Explosions.Simples = null;
            fldata.Explosions.Debris = null;
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
            foreach (var orig in fldata.Explosions.Explosions)
            {
                var ex = new GameData.Explosion() {Nickname = orig.Nickname};
                ex.CRC = CrcTool.FLModelCrc(ex.Nickname);
                if(orig.Effects.Count > 0)
                    ex.Effect = Effects.Get(orig.Effects[0].Name);
                Explosions.Add(ex);
            }
        }
        void InitShips()
        {
            FLLog.Info("Game", "Initing " + fldata.Ships.Ships.Count + " ships");
            foreach (var orig in fldata.Ships.Ships)
            {
                var ship = new GameData.Ship();
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
                    if (!fldata.HpTypes.Types.TryGetValue(hp.Type, out var typedef)) {
                        FLLog.Error("Ship", $"Unrecognised hp_type {hp.Type} in {ship.Nickname}");
                        continue;
                    }
                    if (!ship.PossibleHardpoints.TryGetValue(hp.Type, out var possible)) {
                        possible = new List<string>();
                        ship.PossibleHardpoints.Add(hp.Type, possible);
                    }
                    foreach (var tgt in hp.Hardpoints) {
                        if (!ship.HardpointTypes.TryGetValue(tgt, out var types)) {
                            types = new List<HpType>();
                            ship.HardpointTypes.Add(tgt, types);
                        }
                        types.Add(typedef);
                        possible.Add(tgt);
                    }
                }
                Ships.Add(ship);
            }
            fldata.Ships = null; //free memory
        }

        void InitAsteroids()
        {
            FLLog.Info("Game", "Initing " + fldata.Asteroids.Asteroids.Count + "asteroids");
            foreach (var ast in fldata.Asteroids.Asteroids)
            {
                var asteroid = new GameData.Asteroid();
                asteroid.Nickname = ast.Nickname;
                asteroid.ModelFile = ResolveDrawable(ast.MaterialLibrary, ast.DaArchetype);
                asteroid.CRC = CrcTool.FLModelCrc(asteroid.Nickname);
                Asteroids.Add(asteroid);
            }
            foreach (var dynast in fldata.Asteroids.DynamicAsteroids)
            {
                var dyn = new DynamicAsteroid();
                dyn.Nickname = dynast.Nickname;
                dyn.ModelFile = ResolveDrawable(dynast.MaterialLibrary, dynast.DaArchetype);
                dyn.CRC = CrcTool.FLModelCrc(dyn.Nickname);
                DynamicAsteroids.Add(dyn);
            }
        }

        public GameItemCollection<Sun> Stars;

        void InitStars()
        {
            FLLog.Info("Game", "Initing " + fldata.Stars.Stars.Count + " stars");
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
            foreach (var glow in fldata.Stars.StarGlows) {
                glows[glow.Nickname] = glow;
            }
            foreach (var sp in fldata.Stars.Spines) {
                spines[sp.Nickname] = sp;
            }
            Stars = new GameItemCollection<Sun>();
            foreach (var star in fldata.Stars.Stars)
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
            FLLog.Info("Game", "Initing " + fldata.Solar.Solars.Count + " archetypes");
            foreach (var arch in fldata.Solar.Solars)
            {
                var obj = new GameData.Archetype();
                obj.Type = arch.Type;
                obj.Loadout = GetLoadout(arch.LoadoutName);
                obj.NavmapIcon = arch.ShapeName;
                obj.SolarRadius = arch.SolarRadius ?? 0;
                foreach (var dockSphere in arch.DockingSpheres)
                {
                    obj.DockSpheres.Add(new DockSphere()
                    {
                        Name = dockSphere.Name,
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
                if (arch.Type == Data.Solar.ArchetypeType.tradelane_ring)
                {
                    obj.DockSpheres.Add(new DockSphere()
                    {
                        Name = "tradelane",
                        Hardpoint = "HpRightLane",
                        Radius = 30
                    });
                    obj.DockSpheres.Add(new DockSphere()
                    {
                        Name = "tradelane",
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

        public (ModelResource, float[]) GetSolar(string solar)
        {
            var at = Archetypes.Get(solar);
            return (at.ModelFile.LoadFile(resource), at.LODRanges);
        }


        public IDrawable GetProp(string prop)
        {
            string f;
            if (fldata.PetalDb.Props.TryGetValue(prop, out f))
            {
                return resource.GetDrawable(DataPath(f)).Drawable;
            }
            else
            {
                FLLog.Error("PetalDb", "No prop exists: " + prop);
                return null;
            }
        }

        public IDrawable GetCart(string cart)
        {
            return resource.GetDrawable(DataPath(fldata.PetalDb.Carts[cart])).Drawable;
        }

        public IDrawable GetRoom(string room)
        {
            return resource.GetDrawable(DataPath(fldata.PetalDb.Rooms[room])).Drawable;
        }

        Dictionary<string, ObjectLoadout> _loadouts = new Dictionary<string, ObjectLoadout>(StringComparer.OrdinalIgnoreCase);

        public IEnumerable<ObjectLoadout> Loadouts => _loadouts.Values;

        ObjectLoadout GetLoadout(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;
            _loadouts.TryGetValue(key, out var ld);
            return ld;
        }
        public bool TryGetLoadout(string name, out ObjectLoadout l)
        {
            if (string.IsNullOrWhiteSpace(name)) {
                l = null;
                return false;
            }
            return _loadouts.TryGetValue(name, out l);
        }

        public SystemObject GetSystemObject(string system, Data.Universe.SystemObject o)
        {
            var obj = new SystemObject();
            obj.Nickname = o.Nickname;
            obj.Reputation = Factions.Get(o.Reputation);
            obj.Visit = (VisitFlags) (o.Visit ?? 0);
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
            obj.Comment = Data.CommentEscaping.Unescape(o.Comment);
            if (o.DockWith != null)
            {
                obj.Dock = new DockAction() { Kind = DockKinds.Base, Target = o.DockWith };
            }
            else if (o.Goto != null)
            {
                obj.Dock = new DockAction() { Kind = DockKinds.Jump, Target = o.Goto.System, Exit = o.Goto.Exit, Tunnel = o.Goto.TunnelEffect };
            }
            if (o.Rotate != null)
            {
                obj.Rotation = MathHelper.MatrixFromEulerDegrees(o.Rotate.Value).ExtractRotation();
            }

            obj.Archetype = Archetypes.Get(o.Archetype);

            obj.TradelaneSpaceName = o.TradelaneSpaceName;
            if (o.NextRing != null && o.TradelaneSpaceName != 0) {
                obj.IdsLeft = o.TradelaneSpaceName;
            }
            else if (o.PrevRing != null && o.TradelaneSpaceName != 0) {
                obj.IdsRight = o.TradelaneSpaceName;
            }
            if (obj.Archetype?.Type == Data.Solar.ArchetypeType.tradelane_ring)
            {
                obj.Dock = new DockAction()
                {
                    Kind = DockKinds.Tradelane,
                    Target = o.NextRing,
                    TargetLeft = o.PrevRing
                };
            }
            else if (obj.Archetype == null) {
                FLLog.Error("Systems", $"Object {obj.Nickname} in {system} has bad archetype '{o.Archetype ?? "NULL"}'");
            }
            obj.Star = Stars.Get(o.Star);
            obj.Loadout = GetLoadout(o.Loadout);
            return obj;
        }


        //Used to spawn objects within mission scripts
        public GameData.Archetype GetSolarArchetype(string id) => Archetypes.Get(id);


        public GameItemCollection<FuseResources> Fuses = new();

        void InitFuses()
        {
            foreach (var fuse in fldata.Fuses.Fuses)
            {
                var fr = new FuseResources() { Fuse = fuse };
                fr.Nickname = fuse.Name;
                fr.CRC = FLHash.CreateID(fuse.Name);
                foreach (var act in fuse.Actions)
                {
                    if (resource is GameResourceManager && act is FuseStartEffect fza)
                    {
                        if(string.IsNullOrEmpty(fza.Effect)) continue;
                        if (!fr.Fx.ContainsKey(fza.Effect))
                        {
                            fr.Fx[fza.Effect] = Effects.Get(fza.Effect);
                        }
                    }
                }
                fr.GameData = this;
                Fuses.Add(fr);
            }
        }

        GameData.Items.EffectEquipment GetAttachedFx(Data.Equipment.AttachedFx fx)
        {
            var equip = new GameData.Items.EffectEquipment()
            {
                Particles = Effects.Get(fx.Particles)
            };
            return equip;
        }

        GameData.Items.LightEquipment GetLight(LightInheritHelper lt)
        {
            var equip = new GameData.Items.LightEquipment();
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
    }
}

