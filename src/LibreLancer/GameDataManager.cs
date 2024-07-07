// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data;
using LibreLancer.Data.Equipment;
using LibreLancer.Data.Fuses;
using LibreLancer.Data.Goods;
using LibreLancer.Data.Missions;
using LibreLancer.Data.Solar;
using LibreLancer.GameData;
using LibreLancer.GameData.Items;
using LibreLancer.GameData.Market;
using LibreLancer.GameData.World;
using LibreLancer.Graphics;
using LibreLancer.Render;
using LibreLancer.Thorn.VM;
using LibreLancer.Utf.Anm;
using Archetype = LibreLancer.GameData.Archetype;
using DockSphere = LibreLancer.GameData.World.DockSphere;
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

        IEnumerable<Data.Universe.Base> InitBases(LoadingTasks tasks)
        {
            FLLog.Info("Game", "Initing " + fldata.Universe.Bases.Count + " bases");
            foreach (var inibase in fldata.Universe.Bases)
            {
                if (inibase.Nickname.StartsWith("intro", StringComparison.InvariantCultureIgnoreCase))
                    yield return inibase;
                Data.MBase mbase;
                fldata.MBases.Bases.TryGetValue(inibase.Nickname, out mbase);
                var b = new Base();
                b.Nickname = inibase.Nickname;
                b.CRC = FLHash.CreateID(b.Nickname);
                b.SourceFile = inibase.SourceFile;
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
                    nr.SourceFile = room.FilePath;
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
                    if (room.Nickname.Equals(inibase.StartRoom, StringComparison.OrdinalIgnoreCase)) b.StartRoom = nr;
                    nr.Camera = room.Camera?.Name;
                    nr.FixedNpcs = new List<BaseFixedNpc>();
                    if (mbase == null) continue;
                    var mroom = mbase.FindRoom(room.Nickname);
                    if (mroom != null)
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

        private Dictionary<uint, string> goodHashes = new Dictionary<uint, string>();
        private Dictionary<string, ResolvedGood> goods = new Dictionary<string, ResolvedGood>();
        private Dictionary<string, ResolvedGood> equipToGood = new Dictionary<string, ResolvedGood>();
        Dictionary<string, long> shipPrices = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        public IEnumerable<ResolvedGood> AllGoods => goods.Values;

        public bool TryGetGood(string nickname, out ResolvedGood good) => goods.TryGetValue(nickname, out good);

        public string GoodFromCRC(uint crc) => goodHashes[crc];

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
            Dictionary<string, Data.Goods.Good> hulls = new Dictionary<string, Data.Goods.Good>(256, StringComparer.OrdinalIgnoreCase);
            List<Data.Goods.Good> ships = new List<Good>();
            foreach (var g in fldata.Goods.Goods)
            {
                switch (g.Category)
                {
                    case Data.Goods.GoodCategory.ShipHull:
                        hulls.Add(g.Nickname, g);
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
                            var good = new ResolvedGood() {Equipment = equip, Ini = g, CRC = CrcTool.FLModelCrc(g.Nickname) };
                            equip.Good = good;
                            goods.Add(g.Nickname, good);
                            goodHashes.Add(CrcTool.FLModelCrc(g.Nickname), g.Nickname);
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
                    else if (goods.TryGetValue(gd.Good, out var good))
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
                    var s = new TextureShape()
                    {
                        Texture = shape.Value.TextureName,
                        Nickname = shape.Value.ShapeName,
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
            var explosionTask = tasks.Begin(InitExplosions);
            var ships = tasks.Begin(InitShips, explosionTask);
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
                        if (room.Nickname == b.StartRoom)
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
            var equipmentTask = tasks.Begin(InitEquipment);
            var goodsTask = tasks.Begin(InitGoods, equipmentTask);
            var loadoutsTask = tasks.Begin(InitLoadouts, equipmentTask);
            var archetypesTask = tasks.Begin(InitArchetypes, loadoutsTask);
            tasks.Begin(InitMarkets, baseTask, goodsTask, archetypesTask);
            tasks.Begin(InitBodyParts);
            tasks.Begin(() => InitSystems(tasks),
                baseTask,
                archetypesTask,
                equipmentTask,
                ships,
                factionsTask,
                loadoutsTask,
                pilotTask
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
                res.ConvexCollection.CreateShape(cvx, 0);
            else
            {
                foreach(var p in mdl.AllParts)
                    res.ConvexCollection.CreateShape(cvx, CrcTool.FLModelCrc(p.Name));
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
                        mequip.ExplodeFx = GetEffect(mequip.Explosion.Effect);
                    }
                    equip = mequip;
                }
                else
                {
                    var effect = fldata.Effects.FindEffect(mn.ConstEffect);
                    string visbeam;
                    if (effect == null) visbeam = "";
                    else visbeam = effect.VisBeam ?? "";
                    var mequip = new GameData.Items.MunitionEquip()
                    {
                        Def = mn,
                        ConstEffect_Spear = fldata.Effects.BeamSpears.FirstOrDefault((x) => x.Nickname.Equals(visbeam, StringComparison.OrdinalIgnoreCase)),
                        ConstEffect_Bolt = fldata.Effects.BeamBolts.FirstOrDefault((x) => x.Nickname.Equals(visbeam, StringComparison.OrdinalIgnoreCase))
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
                        eqp.FlashEffect = GetEffect(gn.FlashParticleName);
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
                    eqp.Particles = GetEffect(th.Particles);
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
                    tlequip.RingActive = GetEffect(tl.TlRingActive);
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
                sys.LocalFaction = Factions.Get(inisys.LocalFaction);
                sys.UniversePosition = inisys.Pos ?? Vector2.Zero;
                sys.AmbientColor = inisys.AmbientColor;
                sys.IdsName = inisys.IdsName;
                sys.IdsInfo = inisys.IdsInfo;
                sys.Nickname = inisys.Nickname;
                sys.CRC = CrcTool.FLModelCrc(sys.Nickname);
                sys.MsgIdPrefix = inisys.MsgIdPrefix;
                sys.BackgroundColor = inisys.SpaceColor;
                sys.MusicSpace = inisys.MusicSpace;
                sys.MusicBattle = inisys.MusicBattle;
                sys.MusicDanger = inisys.MusicDanger;
                sys.Spacedust = inisys.Spacedust;
                sys.SpacedustMaxParticles = inisys.SpacedustMaxParticles;
                sys.FarClip = inisys.SpaceFarClip ?? 20000f;
                sys.NavMapScale = inisys.NavMapScale;
                sys.SourceFile = inisys.SourceFile;
                foreach (var ec in inisys.EncounterParameters)
                {
                    sys.EncounterParameters.Add(new EncounterParameters()
                    {
                        Nickname = ec.Nickname,
                        SourceFile = ec.Filename
                    });
                }

                var p = new List<PreloadObject>();
                foreach(var a in inisys.ArchetypeShip) p.Add(new PreloadObject(PreloadType.Ship, a));
                foreach(var a in inisys.ArchetypeSimple) p.Add(new PreloadObject(PreloadType.Simple, a));
                foreach(var a in inisys.ArchetypeEquipment) p.Add(new PreloadObject(PreloadType.Equipment, a));
                foreach(var a in inisys.ArchetypeSnd) p.Add(new PreloadObject(PreloadType.Sound, a));
                foreach(var a in inisys.ArchetypeSolar) p.Add(new PreloadObject(PreloadType.Solar, a));
                foreach(var a in inisys.ArchetypeVoice) p.Add(
                    new PreloadObject(PreloadType.Voice, a.Select(x => new HashValue(x)).ToArray()));
                sys.Preloads = p.ToArray();

                if (inisys.TexturePanels != null)
                    sys.TexturePanelsFiles.AddRange(inisys.TexturePanels.Files);

                sys.StarsBasic = ResolveDrawable(inisys.BackgroundBasicStarsPath);
                sys.StarsComplex = ResolveDrawable(inisys.BackgroundComplexStarsPath);
                sys.StarsNebula = ResolveDrawable(inisys.BackgroundNebulaePath);
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
                        z.IdsInfo = zne.IdsInfo.ToArray();
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
                            var a = GetAsteroidField(sys, ast);
                            if (ast != null)
                                sys.AsteroidFields.Add(a);
                        }
                    }
                    if (inisys.Nebulae != null)
                    {
                        foreach (var nbl in inisys.Nebulae)
                        {
                            if (sys.ZoneDict.ContainsKey(nbl.ZoneName))
                            {
                                sys.Nebulae.Add(GetNebula(sys, nbl));
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
        }
        public IEnumerator<object> LoadSystemResources(StarSystem sys)
        {
            if (fldata.Stars != null)
            {
                foreach (var txmfile in fldata.Stars.TextureFiles)
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

        class CachedTexturePanels
        {
            public int ID;
            public Data.Universe.TexturePanels P;
            public string[] ResourceFiles;
        }

        Dictionary<string, CachedTexturePanels> tpanels = new Dictionary<string, CachedTexturePanels>(StringComparer.OrdinalIgnoreCase);
        int tpId = 0;
        private object tPanelsLock = new object();
        CachedTexturePanels TexturePanelFile(string f)
        {
            lock (tPanelsLock)
            {
                CachedTexturePanels pnl;
                if (!tpanels.TryGetValue(f, out pnl))
                {
                    pnl = new CachedTexturePanels() {ID = tpId++, P = new Data.Universe.TexturePanels(f, VFS)};
                    pnl.ResourceFiles = pnl.P.Files.Select(DataPath).ToArray();
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
            var panels = new Data.Universe.TexturePanels();
            if (ast.TexturePanels != null)
            {
                foreach (var f in ast.TexturePanels.Files)
                {
                    var pnlref = TexturePanelFile(DataPath(f));
                    var pf = pnlref.P;
                    panels.TextureShapes.AddRange(pf.TextureShapes);
                    foreach (var sh in pf.Shapes)
                        panels.Shapes[sh.Key] = sh.Value;
                    sys.ResourceFiles.AddRange(pnlref.ResourceFiles);
                }
            }
            if (ast.Band != null)
            {
                a.Band = new AsteroidBand();
                a.Band.RenderParts = ast.Band.RenderParts.Value;
                a.Band.Height = ast.Band.Height.Value;
                a.Band.Shape = panels.Shapes[ast.Band.Shape].TextureName;
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
                a.CubeRotation.AxisX = ast.Cube_RotationX ?? AsteroidCubeRotation.Default_AxisX;
                a.CubeRotation.AxisY = ast.Cube_RotationY ?? AsteroidCubeRotation.Default_AxisY;
                a.CubeRotation.AxisZ = ast.Cube_RotationZ ?? AsteroidCubeRotation.Default_AxisZ;
                a.CubeSize = ast.Field.CubeSize ?? 100; //HACK: Actually handle null cube correctly
                a.SetFillDist(ast.Field.FillDist.Value);
                a.EmptyCubeFrequency = ast.Field.EmptyCubeFrequency ?? 0f;
                foreach (var c in ast.Cube)
                {
                    var sta = new StaticAsteroid()
                    {
                        Position = c.Position,
                        Info = c.Info,
                        Archetype = c.Name
                    };
                    var arch = fldata.Asteroids.FindAsteroid(c.Name);
                    sta.Drawable = ResolveDrawable(arch.MaterialLibrary, arch.DaArchetype);
                    sta.Rotation = MathHelper.QuatFromEulerDegrees(c.Rotation);
                    a.Cube.Add(sta);
                }
            }
            a.ExclusionZones = new List<ExclusionZone>();
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

                    var e = new ExclusionZone();
                    e.Zone = zone;
                    //e.FogFar = excz.FogFar ?? n.FogRange.Y;
                    if (excz.ZoneShellPath != null)
                    {
                        e.ShellPath = excz.ZoneShellPath;
                        e.ShellTint = excz.Tint ?? Color3f.White;
                        e.ShellScalar = excz.ShellScalar ?? 1f;
                        e.ShellMaxAlpha = excz.MaxAlpha ?? 1f;
                    }
                    a.ExclusionZones.Add(e);
                }
            }
            a.BillboardCount = ast.AsteroidBillboards == null ? -1 : ast.AsteroidBillboards.Count.Value;
            if (a.BillboardCount != -1)
            {
                a.BillboardDistance = ast.AsteroidBillboards.StartDist.Value;
                a.BillboardFadePercentage = ast.AsteroidBillboards.FadeDistPercent.Value;
                Data.Universe.TextureShape sh = null;
                if (panels != null)
                {
                    if (!panels.Shapes.TryGetValue(ast.AsteroidBillboards.Shape, out sh))
                    {
                        a.BillboardCount = -1;
                        FLLog.Error("Asteroids", "Field " + ast.ZoneName + " can't find billboard shape " + ast.AsteroidBillboards.Shape);
                        return a;
                    }
                    else
                    {
                        sh = panels.Shapes[ast.AsteroidBillboards.Shape];
                    }
                }
                else
                    sh = new Data.Universe.TextureShape(ast.AsteroidBillboards.Shape, ast.AsteroidBillboards.Shape, new RectangleF(0, 0, 1, 1));
                a.BillboardShape = new TextureShape()
                {
                    Texture = sh.TextureName,
                    Dimensions = sh.Dimensions,
                    Nickname = ast.AsteroidBillboards.Shape
                };
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
            var panels = new Data.Universe.TexturePanels();
            foreach(var f in nbl.TexturePanels.Files)
            {
                var pnlref = TexturePanelFile(DataPath(f));
                var pf = pnlref.P;
                panels.TextureShapes.AddRange(pf.TextureShapes);
                foreach (var sh in pf.Shapes)
                    panels.Shapes[sh.Key] = sh.Value;
                sys.ResourceFiles.AddRange(pnlref.ResourceFiles);
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
                CloudShape[] shapes = new CloudShape[clds.PuffShape.Count];
                for (int i = 0; i < shapes.Length; i++)
                {
                    var name = clds.PuffShape[i];
                    if (!panels.Shapes.ContainsKey(name))
                    {
                        FLLog.Error("Nebula", "Shape " + name + " does not exist in " + nbl.TexturePanels.Files[0]);
                        shapes[i].Texture = ResourceManager.NullTextureName;
                        shapes[i].Dimensions = new RectangleF(0, 0, 1, 1);
                    }
                    else
                    {
                        shapes[i].Texture = panels.Shapes[name].TextureName;
                        shapes[i].Dimensions = panels.Shapes[name].Dimensions;
                    }
                }
                n.InteriorCloudShapes = new WeightedRandomCollection<CloudShape>(
                    shapes,
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
                CloudShape[] shapes = new CloudShape[nbl.Exterior.Shape.Count];
                for (int i = 0; i < shapes.Length; i++)
                {
                    var name = nbl.Exterior.Shape[i];
                    if (!panels.Shapes.ContainsKey(name))
                    {
                        FLLog.Error("Nebula", "Shape " + name + " does not exist in " + nbl.TexturePanels.Files[0]);
                        shapes[i].Texture = ResourceManager.NullTextureName;
                        shapes[i].Dimensions = new RectangleF(0, 0, 1, 1);
                    }
                    else
                    {
                        shapes[i].Texture = panels.Shapes[name].TextureName;
                        shapes[i].Dimensions = panels.Shapes[name].Dimensions;
                    }
                }
                n.ExteriorCloudShapes = new WeightedRandomCollection<CloudShape>(
                    shapes,
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
                n.ExclusionZones = new List<ExclusionZone>();
                foreach (var excz in nbl.ExclusionZones)
                {

                    Zone zone;
                    if (!sys.ZoneDict.TryGetValue(excz.ZoneName, out zone))
                    {
                        FLLog.Error("System", "Exclusion zone " + excz.ZoneName + " zone does not exist in " + sys.Nickname);
                        continue;
                    }
                    var e = new ExclusionZone();
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

        void InitExplosions()
        {
            FLLog.Info("Game", "Initing Explosions");
            foreach (var orig in fldata.Explosions.Explosions)
            {
                var ex = new GameData.Explosion() {Nickname = orig.Nickname};
                ex.CRC = CrcTool.FLModelCrc(ex.Nickname);
                if(orig.Effects.Count > 0)
                    ex.Effect = GetEffect(orig.Effects[0].Name);
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
                foreach (var fuse in orig.Fuses)
                {
                    ship.Fuses.Add(new DamageFuse()
                    {
                        Fuse = GetFuse(fuse.Fuse),
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

        void InitArchetypes()
        {
            FLLog.Info("Game", "Initing " + fldata.Solar.Solars.Count + " archetypes");
            foreach (var ax in fldata.Solar.Solars)
            {
                var arch = ax.Value;
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
                if(arch.CollisionGroups.Count > 0)
                {
                    obj.CollisionGroups = arch.CollisionGroups.ToArray();
                }
                obj.Nickname = arch.Nickname;
                obj.CRC = CrcTool.FLModelCrc(obj.Nickname);
                obj.LODRanges = arch.LODRanges;
                obj.ModelFile = ResolveDrawable(arch.MaterialPaths, arch.DaArchetypeName);
                Archetypes.Add(obj);
            }
        }

        public (ModelResource, float[]) GetSolar(string solar)
        {
            var at = Archetypes.Get(solar);
            return (at.ModelFile.LoadFile(resource), at.LODRanges);
        }

        public ModelResource GetAsteroid(string asteroid)
        {
            var ast = fldata.Asteroids.FindAsteroid(asteroid);
            resource.LoadResourceFile(DataPath(ast.MaterialLibrary));
            return resource.GetDrawable(DataPath(ast.DaArchetype));
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
            obj.IdsInfo = o.IdsInfo.ToArray();
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
            if (obj.Archetype?.Type == Data.Solar.ArchetypeType.sun)
            {
                if (o.Star != null) //Not sure what to do if there's no star?
                {
                    var sun = new GameData.Archetypes.Sun();
                    sun.Nickname = o.Star;
                    sun.Type = ArchetypeType.sun;
                    sun.NavmapIcon = obj.Archetype.NavmapIcon;
                    sun.SolarRadius = obj.Archetype.SolarRadius;
                    var star = fldata.Stars.FindStar(o.Star);
                    //general
                    sun.Radius = star.Radius.Value;
                    //glow
                    var starglow = fldata.Stars.FindStarGlow(star.StarGlow);
                    sun.GlowSprite = starglow.Shape;
                    sun.GlowColorInner = starglow.InnerColor;
                    sun.GlowColorOuter = starglow.OuterColor;
                    sun.GlowScale = starglow.Scale;
                    //center
                    if (star.StarCenter != null)
                    {
                        var centerglow = fldata.Stars.FindStarGlow(star.StarCenter);
                        sun.CenterSprite = centerglow.Shape;
                        sun.CenterColorInner = centerglow.InnerColor;
                        sun.CenterColorOuter = centerglow.OuterColor;
                        sun.CenterScale = centerglow.Scale;
                    }
                    if (star.Spines != null)
                    {
                        var spines = fldata.Stars.FindSpines(star.Spines);
                        if (spines != null)
                        {
                            sun.SpinesSprite = spines.Shape;
                            sun.SpinesScale = spines.RadiusScale;
                            sun.Spines = new List<Spine>(spines.Items.Count);
                            foreach (var sp in spines.Items)
                                sun.Spines.Add(new Spine(sp.LengthScale, sp.WidthScale, sp.InnerColor, sp.OuterColor, sp.Alpha));
                        }
                        else
                            FLLog.Error("Stararch", "Could not find spines " + star.Spines);
                    }
                    obj.Star = sun;
                }
            }
            else if (obj.Archetype?.Type == Data.Solar.ArchetypeType.tradelane_ring)
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

            obj.Loadout = GetLoadout(o.Loadout);
            return obj;
        }


        //Used to spawn objects within mission scripts
        public GameData.Archetype GetSolarArchetype(string id) => Archetypes.Get(id);



        private Dictionary<string, FuseResources> fuses =
            new Dictionary<string, FuseResources>(StringComparer.OrdinalIgnoreCase);

        public FuseResources GetFuse(string fusename)
        {
            lock (fuses) {
                FuseResources fuse;
                if (!fuses.TryGetValue(fusename, out fuse))
                {
                    var fz = fldata.Fuses.Fuses[fusename];
                    fuse = new GameData.FuseResources() {Fuse = fz};
                    foreach (var act in fz.Actions)
                    {
                        if (resource is GameResourceManager && act is FuseStartEffect fza)
                        {
                            if(string.IsNullOrEmpty(fza.Effect)) continue;
                            if (!fuse.Fx.ContainsKey(fza.Effect))
                            {
                                fuse.Fx[fza.Effect] = GetEffect(fza.Effect);
                            }
                        }
                    }
                    fuse.GameData = this;
                    fuses.Add(fusename, fuse);
                }
                return fuse;
            }
        }

        public bool HasEffect(string effectName)
        {
            return fldata.Effects.FindEffect(effectName) != null || fldata.Effects.FindVisEffect(effectName) != null;
        }

        public ResolvedFx GetEffect(string effectName)
        {
            var effect = fldata.Effects.FindEffect(effectName);
            Data.Effects.VisEffect visfx;
            if (effect == null)
                visfx = fldata.Effects.FindVisEffect(effectName);
            else
                visfx = fldata.Effects.FindVisEffect(effect.VisEffect);
            if (effect == null && visfx == null)
            {
                FLLog.Error("Fx", $"Can't find fx '{effectName}'");
                return null;
            }
            if (visfx == null) return null;
            if(string.IsNullOrWhiteSpace(visfx.AlchemyPath)) return null;
            var alepath = DataPath(visfx.AlchemyPath);
            if (alepath == null) return null;
            return new ResolvedFx()
            {
                AlePath = alepath,
                VisFxCrc = (uint)visfx.EffectCrc,
                LibraryFiles = visfx.Textures.Select(DataPath).Where(x => x != null).ToArray()
            };
        }

        GameData.Items.EffectEquipment GetAttachedFx(Data.Equipment.AttachedFx fx)
        {
            var equip = new GameData.Items.EffectEquipment()
            {
                Particles = GetEffect(fx.Particles)
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

