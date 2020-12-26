// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using LibreLancer.Data.Fuses;
using LibreLancer.Data.Solar;
using LibreLancer.GameData;
using LibreLancer.Utf.Anm;
using LibreLancer.Utf.Dfm;
using FileSystem = LibreLancer.Data.FileSystem;

namespace LibreLancer
{
    public class GameDataManager
    {
        public Data.FreelancerData Ini => fldata;
        Data.FreelancerData fldata;
        ResourceManager resource;
        GameResourceManager glResource;
        List<GameData.IntroScene> IntroScenes;
        public FileSystem VFS;
        public GameDataManager(string path, ResourceManager resman)
        {
            resource = resman;
            glResource = (resource as GameResourceManager);
            VFS = FileSystem.FromFolder(path);
            var flini = new Data.FreelancerIni(VFS);
            fldata = new Data.FreelancerData(flini, VFS);
        }
        public string DataVersion => fldata.DataVersion;
        public string GetInterfaceXml(string id)
        {
            if (fldata.Freelancer.XInterfacePath == null)
            {
                using (var reader = new StreamReader(typeof(GameDataManager).Assembly.GetManifestResourceStream("LibreLancer.Interface.Default." + id + ".xml")))
                {
                    return reader.ReadToEnd();
                }
            }
            else
            {
                using (var reader = new StreamReader(fldata.Freelancer.XInterfacePath + id + ".xml"))
                {
                    return reader.ReadToEnd();
                }
            }
        }
        public string ResolveDataPath(string input)
        {
            return VFS.Resolve(fldata.Freelancer.DataPath + input);
        }

        string ResolveDataNoExcept(string input)
        {
            var v =  VFS.Resolve(fldata.Freelancer.DataPath + input, false);
            if(v == null) FLLog.Error("GameData", $"File {fldata.Freelancer.DataPath}{input} not found");
            return v;
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
                movies.Add(ResolveDataPath(file));
            }
            return movies;
        }
        public List<Data.RichFont> GetRichFonts()
        {
            return fldata.RichFonts.Fonts;
        }
        public GameData.Base GetBase(string id)
        {
            return bases[id];
        }

        private AnmFile characterAnimations;
        public AnmFile GetCharacterAnimations()
        {
            if (characterAnimations == null)
            {
                characterAnimations = new AnmFile();
                foreach (var file in fldata.Bodyparts.Animations)
                {
                    AnmFile.ParseToTable(characterAnimations.Scripts, ResolveDataPath(file));
                }
            }
            return characterAnimations;
        }
        public bool GetCostume(string costume, out DfmFile body, out DfmFile head, out DfmFile leftHand, out DfmFile rightHand)
        {
            var cs = fldata.Costumes.FindCostume(costume);
            body = head = leftHand = rightHand = null;
            if(cs.Head != null)
                head = (DfmFile)resource.GetDrawable(VFS.Resolve(cs.Head.MeshPath));
            if(cs.Body != null)
                body = (DfmFile) resource.GetDrawable(VFS.Resolve(cs.Body.MeshPath));
            if(cs.LeftHand != null)
                leftHand = (DfmFile) resource.GetDrawable(VFS.Resolve(cs.LeftHand.MeshPath));
            if(cs.RightHand != null)
                rightHand = (DfmFile) resource.GetDrawable(VFS.Resolve(cs.RightHand.MeshPath));
            return true;
        }
        IEnumerable<Data.Universe.Base> InitBases()
        {
            FLLog.Info("Game", "Initing " + fldata.Universe.Bases.Count + " bases");
            bases = new Dictionary<string, GameData.Base>(fldata.Universe.Bases.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var inibase in fldata.Universe.Bases)
            {
                if (inibase.Nickname.StartsWith("intro", StringComparison.InvariantCultureIgnoreCase))
                    yield return inibase;
                Data.MBase mbase;
                fldata.MBases.Bases.TryGetValue(inibase.Nickname, out mbase);
                var b = new GameData.Base();
                b.Nickname = inibase.Nickname;
                b.IdsName = inibase.IdsName;
                b.System = inibase.System;
                b.TerrainTiny = inibase.TerrainTiny;
                b.TerrainSml = inibase.TerrainSml;
                b.TerrainMdm = inibase.TerrainMdm;
                b.TerrainLrg = inibase.TerrainLrg;
                b.TerrainDyna1 = inibase.TerrainDyna1;
                b.TerrainDyna2 = inibase.TerrainDyna2;
                foreach (var room in inibase.Rooms)
                {
                    var nr = new GameData.BaseRoom();
                    nr.Music = room.Music;
                    nr.MusicOneShot = room.MusicOneShot;
                    nr.ThnPaths = new List<string>();
                    nr.PlayerShipPlacement = room.PlayerShipPlacement;
                    nr.ForSaleShipPlacements = room.ForShipSalePlacements;
                    nr.InitAction = () =>
                    {
                        nr.SetScript = ResolveDataPath(room.SetScript);
                        foreach (var path in room.SceneScripts)
                            nr.ThnPaths.Add(ResolveDataPath(path));
                         if (room.LandingScript != null)
                             nr.LandScript = ResolveDataPath(room.LandingScript);
                         if (room.StartScript != null)
                             nr.StartScript = ResolveDataPath(room.StartScript);
                         if (room.LaunchingScript != null)
                             nr.LaunchScript = ResolveDataPath(room.LaunchingScript);
                         if (room.GoodscartScript != null)
                             nr.GoodscartScript = ResolveDataPath(room.GoodscartScript);
                     };
                    nr.Hotspots = new List<GameData.BaseHotspot>();
                    foreach (var hp in room.Hotspots)
                        nr.Hotspots.Add(new GameData.BaseHotspot()
                        {
                            Name = hp.Name,
                            Behavior = hp.Behavior,
                            Room = hp.RoomSwitch,
                            SetVirtualRoom = hp.VirtualRoom
                        });
                    nr.Nickname = room.Nickname;
                    if (room.Nickname == inibase.StartRoom) b.StartRoom = nr;
                    nr.Camera = room.Camera;
                    nr.Npcs = new List<GameData.BaseNpc>();
                    if (mbase == null) continue;
                    var mroom = mbase.FindRoom(room.Nickname);
                    if (mroom != null)
                    {
                        foreach (var npc in mroom.NPCs)
                        {
                            /*var newnpc = new GameData.BaseNpc();
                            newnpc.StandingPlace = npc.StandMarker;
                            var gfnpc = mbase.FindNpc(npc.Npc);
                            newnpc.HeadMesh = fldata.Bodyparts.FindBodypart(gfnpc.Head).MeshPath;
                            newnpc.BodyMesh = fldata.Bodyparts.FindBodypart(gfnpc.Body).MeshPath;
                            newnpc.LeftHandMesh = fldata.Bodyparts.FindBodypart(gfnpc.LeftHand).MeshPath;
                            newnpc.RightHandMesh = fldata.Bodyparts.FindBodypart(gfnpc.RightHand).MeshPath;
                            nr.Npcs.Add(newnpc);*/
                        }
                    }
                    b.Rooms.Add(nr);
                }
                bases.Add(inibase.Nickname, b);
            }
            fldata.MBases = null; //Free memory
        }
        void InitGoods()
        {
            FLLog.Info("Game", "Initing " + fldata.Goods.Goods.Count + " goods");
            Dictionary<string, Data.Goods.Good> hulls = new Dictionary<string, Data.Goods.Good>(256, StringComparer.OrdinalIgnoreCase);
            foreach (var g in fldata.Goods.Goods)
            {
                switch (g.Category)
                {
                    case Data.Goods.GoodCategory.ShipHull:
                        hulls.Add(g.Nickname, g);
                        //Handled in ship (albeit slowly)
                        break;
                    case Data.Goods.GoodCategory.Ship:
                        Data.Goods.Good hull;
                        if (!hulls.TryGetValue(g.Hull, out hull))
                        {
                            hull = fldata.Goods.Goods.First(x => x.Nickname.Equals(g.Hull, StringComparison.OrdinalIgnoreCase));
                        }
                        var sp = new GameData.Market.ShipPackage();
                        sp.Ship = hull.Ship;
                        sp.Nickname = g.Nickname;
                        sp.BasePrice = hull.Price;
                        shipPackages.Add(g.Nickname, sp);
                        break;
                    case Data.Goods.GoodCategory.Equipment:
                        break;
                    case Data.Goods.GoodCategory.Commodity:
                        break;
                }
            }
            fldata.Goods = null; //Free memory
        }
        void InitMarkets()
        {
            FLLog.Info("Game", "Initing " + fldata.Markets.BaseGoods.Count + " shops");
            foreach (var m in fldata.Markets.BaseGoods)
            {
                GameData.Base b;
                if(!bases.TryGetValue(m.Base, out b))
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
                }
            }
            fldata.Markets = null; //Free memory
        }
        Dictionary<string, GameData.Base> bases;
        Dictionary<string, GameData.Market.ShipPackage> shipPackages = new Dictionary<string, GameData.Market.ShipPackage>();

        private List<Task> asyncTasks = new List<Task>();
        void AsyncAction(Action a) =>  asyncTasks.Add(Task.Run(a));

        void WaitTasks()
        {
            Task.WaitAll(asyncTasks.ToArray());
            asyncTasks = new List<Task>();
        }
        public void LoadData()
        {
            fldata.LoadData();
            AsyncAction(() =>
            {
                FLLog.Info("Game", "Loading Character Animations");
                GetCharacterAnimations();
            });
            FLLog.Info("Game", "Initing Tables");
            var introbases = InitBases().ToArray();
            InitShips();
            InitArchetypes();
            InitEquipment();
            InitGoods();
            InitMarkets();
            InitSystems();
            FLLog.Info("Game", "Loading intro scenes");
            IntroScenes = new List<GameData.IntroScene>();
            foreach (var b in introbases)
            {
                foreach (var room in b.Rooms)
                {
                    if (room.Nickname == b.StartRoom)
                    {
                        var isc = new GameData.IntroScene();
                        isc.Scripts = new List<ThnScript>();
                        foreach (var p in room.SceneScripts)
                        {
                            var path = ResolveDataPath(p);
                            isc.ThnName = path;
                            isc.Scripts.Add(new ThnScript(path));
                        }
                        isc.Music = room.Music;
                        IntroScenes.Add(isc);
                    }
                }
            }
            if (glResource != null)
            {
                glResource.AddPreload(
                    fldata.EffectShapes.Files.Select(txmfile => ResolveDataPath(txmfile))
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
            }
            FLLog.Info("Game", "Waiting on threads");
            WaitTasks();
            fldata.Universe = null; //Free universe ini!
            GC.Collect(); //We produced a crapload of garbage
        }

        bool cursorsDone = false;
        public void PopulateCursors()
        {
            if (cursorsDone) return;
            cursorsDone = true;

            resource.LoadResourceFile(
                ResolveDataPath(fldata.Mouse.TxmFile)
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
        public Data.Audio.AudioEntry GetAudioEntry(string id)
        {
            return fldata.Audio.Entries.Where((arg) => arg.Nickname.ToLowerInvariant() == id.ToLowerInvariant()).First();
        }
        public string GetAudioPath(string id)
        {
            var audio = fldata.Audio.Entries.Where((arg) => arg.Nickname.ToLowerInvariant() == id.ToLowerInvariant()).First();
            return VFS.Resolve(fldata.Freelancer.DataPath + audio.File, false);
        }
        public string GetVoicePath(string id)
        {
            return ResolveDataPath("AUDIO\\" + id + ".utf");
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
        public void LoadHardcodedFiles()
        {
            resource.LoadResourceFile(ResolveDataPath("INTERFACE/interface.generic.vms"));
        }
        public IDrawable GetMenuButton()
        {
            return resource.GetDrawable(ResolveDataPath("INTERFACE/INTRO/OBJECTS/front_button.cmp"));
        }
        public Texture2D GetSplashScreen()
        {
            if (!glResource.TextureExists("__startupscreen_1280.tga"))
            {
                if (VFS.FileExists(fldata.Freelancer.DataPath + "INTERFACE/INTRO/IMAGES/startupscreen_1280.tga"))
                {
                    glResource.AddTexture(
                        "__startupscreen_1280.tga",
                        ResolveDataPath("INTERFACE/INTRO/IMAGES/startupscreen_1280.tga")
                    );
                } else if (VFS.FileExists(fldata.Freelancer.DataPath + "INTERFACE/INTRO/IMAGES/startupscreen.tga"))
                {
                    glResource.AddTexture(
                        "__startupscreen_1280.tga",
                        ResolveDataPath("INTERFACE/INTRO/IMAGES/startupscreen.tga")
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
        public Texture2D GetFreelancerLogo()
        {
            if (!glResource.TextureExists("__freelancerlogo.tga"))
            {
                glResource.AddTexture(
                    "__freelancerlogo.tga",
                    ResolveDataPath("INTERFACE/INTRO/IMAGES/front_freelancerlogo.tga")
                );
            }
            return (Texture2D)resource.FindTexture("__freelancerlogo.tga");
        }
        public IEnumerable<Maneuver> GetManeuvers()
        {
            var p = fldata.Freelancer.DataPath.Replace('\\', Path.DirectorySeparatorChar);
            foreach (var m in fldata.Hud.Maneuvers)
            {
                yield return new Maneuver()
                {
                    Action = m.Action,
                    InfocardA = fldata.Infocards.GetStringResource(m.InfocardA),
                    InfocardB = fldata.Infocards.GetStringResource(m.InfocardB),
                    ActiveModel = Path.Combine(p,m.ActiveModel),
                    InactiveModel = Path.Combine(p,m.InactiveModel)
                };
            }
        }

        public bool SystemExists(string id) => systems.ContainsKey(id);
        public IEnumerable<string> ListSystems() => systems.Keys;
        public IEnumerable<string> ListBases() => bases.Keys;

        Dictionary<string, GameData.Items.Equipment> equipments = new Dictionary<string, GameData.Items.Equipment>(StringComparer.OrdinalIgnoreCase);
        Dictionary<uint, GameData.Items.Equipment> equipmentHashes = new Dictionary<uint, GameData.Items.Equipment>();
        void InitEquipment()
        {
            FLLog.Info("Game", "Initing " + fldata.Equipment.Equip.Count + " equipments");
            Dictionary<string, LightInheritHelper> lights = new Dictionary<string, LightInheritHelper>(StringComparer.OrdinalIgnoreCase);
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
                if (val is Data.Equipment.PowerCore)
                {
                    var pc = (val as Data.Equipment.PowerCore);
                    var eqp = new GameData.Items.PowerEquipment();
                    eqp.Def = pc;
                    eqp.ModelFile = ResolveDrawable(pc.MaterialLibrary, pc.DaArchetype);
                    equip = eqp;
                }
                if (val is Data.Equipment.Gun)
                {
                    var gn = (val as Data.Equipment.Gun);
                    var mn = fldata.Equipment.Munitions.FirstOrDefault((x) => x.Nickname.Equals(gn.ProjectileArchetype, StringComparison.OrdinalIgnoreCase));
                    if (mn == null)
                    {
                        FLLog.Error("Equipment", $"Munition {gn.ProjectileArchetype} not found (Gun {gn.Nickname})");
                        continue;
                    }
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
                    var eqp = new GameData.Items.GunEquipment()
                    {
                        Munition = mequip,
                        Def = gn
                    };
                    equip = eqp;
                    equip.ModelFile = ResolveDrawable(gn.MaterialLibrary, gn.DaArchetype);
                }
                if (val is Data.Equipment.Thruster)
                {
                    var th = (val as Data.Equipment.Thruster);
                    var eqp = new GameData.Items.ThrusterEquipment()
                    {
                        Drain = th.PowerUsage,
                        Force = th.MaxForce,
                        HpParticles = th.HpParticles
                    };
                    equip = eqp;
                    eqp.Particles = GetEffect(th.Particles);
                    equip.ModelFile = ResolveDrawable(th.MaterialLibrary, th.DaArchetype);
                }

                if (val is Data.Equipment.Engine deng)
                    equip = new GameData.Items.EngineEquipment() {Def = deng};
                if(equip == null) 
                    continue;
                equip.Nickname = val.Nickname;
                equip.CRC = FLHash.CreateID(equip.Nickname);
                equip.HPChild = val.HPChild;
                equip.LODRanges = val.LODRanges;
                equipments[equip.Nickname] = equip;
                equipmentHashes[equip.CRC] = equip;
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
                equipments[eq.Nickname] = eq;
                equipmentHashes[eq.CRC] = eq;
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
        Dictionary<string, GameData.StarSystem> systems = new Dictionary<string, GameData.StarSystem>(StringComparer.OrdinalIgnoreCase);

        public IEnumerable<GameData.StarSystem> AllSystems => systems.Values;
        void InitSystems()
        {
            FLLog.Info("Game", "Initing " + fldata.Universe.Systems.Count + " systems");
            _solarLoadouts = new Dictionary<string, Loadout>();
            foreach (var l in fldata.Loadouts.Loadouts)
                _solarLoadouts[l.Nickname] = l;
            foreach (var inisys in fldata.Universe.Systems)
            {
                if (inisys.MultiUniverse) continue; //Skip multiuniverse for now
                FLLog.Info("System", inisys.Nickname);
                var sys = new GameData.StarSystem();
                sys.UniversePosition = inisys.Pos ?? Vector2.Zero;
                sys.AmbientColor = inisys.AmbientColor;
                sys.Name = GetString(inisys.IdsName);
                sys.Infocard = inisys.IdsInfo;
                sys.Nickname = inisys.Nickname;
                sys.BackgroundColor = inisys.SpaceColor;
                sys.MusicSpace = inisys.MusicSpace;
                sys.FarClip = inisys.SpaceFarClip ?? 20000f;
                sys.NavMapScale = inisys.NavMapScale;
                sys.StarspheresAction = () =>
                {
                    if (inisys.BackgroundBasicStarsPath != null)
                    {
                        try
                        {
                            sys.StarsBasic = resource.GetDrawable(ResolveDataPath(inisys.BackgroundBasicStarsPath));
                        }
                        catch (Exception)
                        {
                            sys.StarsBasic = null;
                            FLLog.Error("System", "Failed to load starsphere " + inisys.BackgroundBasicStarsPath);
                        }
                    }
                    if (inisys.BackgroundComplexStarsPath != null)
                    {
                        sys.StarsComplex = resource.GetDrawable(ResolveDataPath(inisys.BackgroundComplexStarsPath));
                    }

                    if (inisys.BackgroundNebulaePath != null)
                    {
                        sys.StarsNebula = resource.GetDrawable(ResolveDataPath(inisys.BackgroundNebulaePath));
                    }
                };
                if (inisys.LightSources != null)
                {
                    foreach (var src in inisys.LightSources)
                    {
                        var lt = new RenderLight();
                        var srcCol = src.Color.Value;
                        lt.Color = new Color3f(srcCol.R, srcCol.G, srcCol.B);
                        lt.Position = src.Pos.Value;
                        lt.Range = src.Range.Value;
                        lt.Direction = src.Direction ?? new Vector3(0, 0, 1);
                        lt.Kind = ((src.Type ?? Data.Universe.LightType.Point) == Data.Universe.LightType.Point) ? LightKind.Point : LightKind.Directional;
                        lt.Attenuation = src.Attenuation ?? Vector3.UnitY;
                        if (src.AttenCurve != null)
                        {
                            lt.Kind = LightKind.PointAttenCurve;
                            lt.Attenuation = GetQuadratic(src.AttenCurve);
                        }
                        sys.LightSources.Add(lt);
                    }
                }
                foreach (var obj in inisys.Objects)
                {
                    sys.Objects.Add(GetSystemObject(obj));
                }
                if (inisys.Zones != null)
                    foreach (var zne in inisys.Zones)
                    {
                        var z = new GameData.Zone();
                        z.Nickname = zne.Nickname;
                        z.EdgeFraction = zne.EdgeFraction ?? 0.25f;
                        z.PropertyFlags = (ZonePropFlags) (zne.PropertyFlags ?? 0);
                        z.PropertyFogColor = zne.PropertyFogColor ?? Color4.White;
                        z.VisitFlags = zne.Visit ?? 0;
                        z.Position = zne.Pos ?? Vector3.Zero;
                        z.Sort = zne.Sort ?? 0;
                        if(zne.Pos == null) FLLog.Warning("Zone", $"Zone {zne.Nickname} in {inisys.Nickname} has no position");
                        if (zne.Rotate != null)
                        {
                            var r = zne.Rotate.Value;

                            var qx = Quaternion.CreateFromYawPitchRoll(
                                MathHelper.DegreesToRadians(r.Y),
                                MathHelper.DegreesToRadians(r.X),
                                MathHelper.DegreesToRadians(r.Z)
                            );
                            z.RotationMatrix = Matrix4x4.CreateFromQuaternion(qx);
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
                        switch (zne.Shape.Value)
                        {
                            case Data.Universe.ZoneShape.ELLIPSOID:
                                z.Shape = new GameData.ZoneEllipsoid(z,
                                    zne.Size.Value.X,
                                    zne.Size.Value.Y,
                                    zne.Size.Value.Z
                                );
                                break;
                            case Data.Universe.ZoneShape.SPHERE:
                                z.Shape = new GameData.ZoneSphere(z,
                                    (zne.Size ?? Vector3.One).X //bug
                                );
                                break;
                            case Data.Universe.ZoneShape.BOX:
                                z.Shape = new GameData.ZoneBox(z,
                                    zne.Size.Value.X,
                                    zne.Size.Value.Y,
                                    zne.Size.Value.Z
                                );
                                break;
                            case Data.Universe.ZoneShape.CYLINDER:
                                z.Shape = new GameData.ZoneCylinder(z,
                                    zne.Size.Value.X,
                                    zne.Size.Value.Y
                                );
                                break;
                            case Data.Universe.ZoneShape.RING:
                                z.Shape = new GameData.ZoneRing(z,
                                    zne.Size.Value.X,
                                    zne.Size.Value.Y,
                                    zne.Size.Value.Z
                                );
                                break;
                            default:
                                Console.WriteLine(zne.Nickname);
                                Console.WriteLine(zne.Shape.Value);
                                throw new NotImplementedException();
                        }
                        sys.Zones.Add(z);
                        sys.ZoneDict[z.Nickname] = z;
                    }
                AsyncAction(() =>
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
                systems.Add(sys.Nickname, sys);
            }
        }
        public IEnumerator<object> LoadSystemResources(GameData.StarSystem sys)
        {
            if (fldata.Stars != null)
            {
                foreach (var txmfile in fldata.Stars.TextureFiles)
                    resource.LoadResourceFile(ResolveDataPath(txmfile));
            }
            yield return null;
            sys.LoadStarspheres();
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
            foreach (var nb in sys.Nebulae)
            {
                nb.LoadResources();
                if (a % 3 == 0) yield return null;
                a++;
            }
            foreach (var resfile in sys.ResourceFiles)
            {
                resource.LoadResourceFile(resfile);
                if (a % 3 == 0) yield return null;
                a++;
            }
        }
        public GameData.StarSystem GetSystem(string id) => systems[id];

        public void LoadAllSystem(GameData.StarSystem system)
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
                    pnl.ResourceFiles = pnl.P.Files.Select(ResolveDataPath).ToArray();
                    tpanels.Add(f, pnl);
                }
                return pnl;
            }
        }

        GameData.AsteroidField GetAsteroidField(GameData.StarSystem sys, Data.Universe.AsteroidField ast)
        {
            var a = new GameData.AsteroidField();
            if (!sys.ZoneDict.ContainsKey(ast.ZoneName))
            {
                FLLog.Error("System", $"{sys.Nickname}: {ast.ZoneName} zone missing in Asteroid ref");
                return null;
            }
            a.Zone = sys.ZoneDict[ast.ZoneName];
            var panels = new Data.Universe.TexturePanels();
            if (ast.TexturePanels != null)
            {
                foreach (var f in ast.TexturePanels.Files)
                {
                    var pnlref = TexturePanelFile(ResolveDataPath(f));
                    var pf = pnlref.P;
                    panels.TextureShapes.AddRange(pf.TextureShapes);
                    foreach (var sh in pf.Shapes)
                        panels.Shapes[sh.Key] = sh.Value;
                    if (!sys.TexturePanelFiles.Contains(pnlref.ID))
                    {
                        sys.TexturePanelFiles.Add(pnlref.ID);
                        sys.ResourceFiles.AddRange(pnlref.ResourceFiles);
                    }
                }
            }
            if (ast.Band != null)
            {
                a.Band = new GameData.AsteroidBand();
                a.Band.RenderParts = ast.Band.RenderParts.Value;
                a.Band.Height = ast.Band.Height.Value;
                a.Band.Shape = panels.Shapes[ast.Band.Shape].TextureName;
                a.Band.Fade = new Vector4(ast.Band.Fade[0], ast.Band.Fade[1], ast.Band.Fade[2], ast.Band.Fade[3]);
                var cs = ast.Band.ColorShift ?? Vector3.One;
                a.Band.ColorShift = new Color4(cs.X, cs.Y, cs.Z, 1f);
                a.Band.TextureAspect = ast.Band.TextureAspect ?? 1f;
                a.Band.OffsetDistance = ast.Band.OffsetDist ?? 0f;
            }
            a.Cube = new List<GameData.StaticAsteroid>();
            a.CubeRotation = new GameData.AsteroidCubeRotation();
            a.CubeRotation.AxisX = ast.Cube_RotationX ?? GameData.AsteroidCubeRotation.Default_AxisX;
            a.CubeRotation.AxisY = ast.Cube_RotationY ?? GameData.AsteroidCubeRotation.Default_AxisY;
            a.CubeRotation.AxisZ = ast.Cube_RotationZ ?? GameData.AsteroidCubeRotation.Default_AxisZ;
            a.CubeSize = ast.Field.CubeSize ?? 100; //HACK: Actually handle null cube correctly
            a.SetFillDist(ast.Field.FillDist.Value);
            a.EmptyCubeFrequency = ast.Field.EmptyCubeFrequency ?? 0f;
            foreach (var c in ast.Cube)
            {
                var sta = new GameData.StaticAsteroid()
                {
                    Rotation = c.Rotation,
                    Position = c.Position,
                    Info = c.Info,
                    Archetype = c.Name
                };
                var arch = fldata.Asteroids.FindAsteroid(c.Name);
                sta.Drawable = ResolveDrawable(arch.MaterialLibrary, arch.DaArchetype);
                sta.RotationMatrix =
                    Matrix4x4.CreateRotationX(MathHelper.DegreesToRadians(c.Rotation.X)) *
                    Matrix4x4.CreateRotationY(MathHelper.DegreesToRadians(c.Rotation.Y)) *
                    Matrix4x4.CreateRotationZ(MathHelper.DegreesToRadians(c.Rotation.Z));
                a.Cube.Add(sta);
            }
            a.ExclusionZones = new List<GameData.ExclusionZone>();
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

                    var e = new GameData.ExclusionZone();
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
        public GameData.Nebula GetNebula(GameData.StarSystem sys, Data.Universe.Nebula nbl)
        {
            var n = new GameData.Nebula();
            n.Zone = sys.ZoneDict[nbl.ZoneName];
            var panels = new Data.Universe.TexturePanels();
            foreach(var f in nbl.TexturePanels.Files)
            {
                var pnlref = TexturePanelFile(ResolveDataPath(f));
                var pf = pnlref.P;
                panels.TextureShapes.AddRange(pf.TextureShapes);
                foreach (var sh in pf.Shapes)
                    panels.Shapes[sh.Key] = sh.Value;
                if (!sys.TexturePanelFiles.Contains(pnlref.ID))
                {
                    sys.TexturePanelFiles.Add(pnlref.ID);
                    sys.ResourceFiles.AddRange(pnlref.ResourceFiles);
                }
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
                GameData.CloudShape[] shapes = new GameData.CloudShape[clds.PuffShape.Count];
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
                n.InteriorCloudShapes = new WeightedRandomCollection<GameData.CloudShape>(
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
                GameData.CloudShape[] shapes = new GameData.CloudShape[nbl.Exterior.Shape.Count];
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
                n.ExteriorCloudShapes = new WeightedRandomCollection<GameData.CloudShape>(
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
                n.ExclusionZones = new List<GameData.ExclusionZone>();
                foreach (var excz in nbl.ExclusionZones)
                {
                    
                    Zone zone;
                    if (!sys.ZoneDict.TryGetValue(excz.ZoneName, out zone))
                    {
                        FLLog.Error("System", "Exclusion zone " + excz.ZoneName + " zone does not exist in " + sys.Nickname);
                        continue;
                    }
                    var e = new GameData.ExclusionZone();
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
                if (ex.ShellPath != null) ex.Shell = ResolveDrawable("", ex.ShellPath);
            }

            return n;
        }
        public GameData.Ship GetShip(int crc)
        {
            return shipHashes[(uint)crc];
        }
        public GameData.Ship GetShip(string nickname)
        {
            return ships[nickname];
        }

        Dictionary<string, GameData.Ship> ships = new Dictionary<string, GameData.Ship>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, GameData.Archetype> archetypes = new Dictionary<string, GameData.Archetype>(StringComparer.OrdinalIgnoreCase);
        Dictionary<uint, GameData.Ship> shipHashes = new Dictionary<uint, GameData.Ship>();
        ResolvedModel ResolveDrawable(IEnumerable<string> libs, string file)
        {
            var mdl = new ResolvedModel() {
                ModelFile = ResolveDataNoExcept(file)
            };
            if (mdl.ModelFile == null) return null;
            if (libs != null)
            {
                mdl.LibraryFiles = libs.Select(x => ResolveDataNoExcept(x)).Where(x => x != null).ToArray();
            }
            return mdl;
        }

        ResolvedModel ResolveDrawable(string libs, string file)
        {
            var mdl = new ResolvedModel() {
                ModelFile = ResolveDataNoExcept(file)
            };
            if (mdl.ModelFile == null) return null;
            if (!string.IsNullOrEmpty(libs))
            {
                mdl.LibraryFiles = new[] {ResolveDataNoExcept(libs)};
                if (mdl.LibraryFiles[0] == null) mdl.LibraryFiles = new string[0];
            }
            return mdl;
        }
        
        void InitShips()
        {
            FLLog.Info("Game", "Initing " + fldata.Ships.Ships.Count + " ships");
            foreach (var orig in fldata.Ships.Ships)
            {
                var ship = new GameData.Ship();
                ship.ModelFile = ResolveDrawable(orig.MaterialLibraries, orig.DaArchetypeName);
                ship.Mass = orig.Mass;
                ship.AngularDrag = orig.AngularDrag;
                ship.RotationInertia = orig.RotationInertia;
                ship.SteeringTorque = orig.SteeringTorque;
                ship.CruiseSpeed = 300;
                ship.StrafeForce = orig.StrafeForce;
                ship.ChaseOffset = orig.CameraOffset;
                ship.CameraHorizontalTurnAngle = orig.CameraHorizontalTurnAngle;
                ship.CameraVerticalTurnUpAngle = orig.CameraVerticalTurnUpAngle;
                ship.CameraVerticalTurnDownAngle = orig.CameraVerticalTurnDownAngle;
                ship.Nickname = orig.Nickname;
                ship.NameIds = orig.IdsName;
                ship.CRC = FLHash.CreateID(ship.Nickname);
                ships.Add(ship.Nickname, ship);
                shipHashes.Add(ship.CRC, ship);
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
                obj.LoadoutName = arch.LoadoutName;
                obj.NavmapIcon = arch.ShapeName;
                obj.SolarRadius = arch.SolarRadius ?? 0;
                foreach (var dockSphere in arch.DockingSpheres)
                {
                    obj.DockSpheres.Add(new GameData.DockSphere()
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
                    obj.DockSpheres.Add(new GameData.DockSphere()
                    {
                        Name = "tradelane",
                        Hardpoint = "HpRightLane",
                        Radius = 30
                    });
                    obj.DockSpheres.Add(new GameData.DockSphere()
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
                obj.ArchetypeName = arch.GetType().Name;
                obj.LODRanges = arch.LODRanges;
                obj.ModelFile = ResolveDrawable(arch.MaterialPaths, arch.DaArchetypeName);
                archetypes.Add(ax.Key, obj);
            }
        }
        
        public (IDrawable, float[]) GetSolar(string solar)
        {
            var at = archetypes[solar];
            return (at.ModelFile.LoadFile(resource), at.LODRanges);
        }

        public IDrawable GetAsteroid(string asteroid)
        {
            var ast = fldata.Asteroids.FindAsteroid(asteroid);
            resource.LoadResourceFile(ResolveDataPath(ast.MaterialLibrary));
            return resource.GetDrawable(ResolveDataPath(ast.DaArchetype));
        }

        public IDrawable GetProp(string prop)
        {
            string f;
            if (fldata.PetalDb.Props.TryGetValue(prop, out f))
            {
                return resource.GetDrawable(ResolveDataPath(f));
            }
            else
            {
                FLLog.Error("PetalDb", "No prop exists: " + prop);
                return null;
            }
        }

        public IDrawable GetCart(string cart)
        {
            return resource.GetDrawable(ResolveDataPath(fldata.PetalDb.Carts[cart]));
        }

        public IDrawable GetRoom(string room)
        {
            return resource.GetDrawable(ResolveDataPath(fldata.PetalDb.Rooms[room]));
        }

        Dictionary<string, Data.Solar.Loadout> _solarLoadouts = new Dictionary<string, Data.Solar.Loadout>(StringComparer.OrdinalIgnoreCase);
        Loadout GetLoadout(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;
            _solarLoadouts.TryGetValue(key, out var ld);
            return ld;
        }
        public GameData.SystemObject GetSystemObject(Data.Universe.SystemObject o)
        {
            var obj = new GameData.SystemObject();
            obj.Nickname = o.Nickname;
            obj.Visit = o.Visit ?? 0;
            obj.DisplayName = GetString(o.IdsName);
            obj.Position = o.Pos.Value;
            obj.Spin = o.Spin ?? Vector3.Zero;
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
                obj.Rotation =
                    Matrix4x4.CreateRotationX(MathHelper.DegreesToRadians(o.Rotate.Value.X)) *
                    Matrix4x4.CreateRotationY(MathHelper.DegreesToRadians(o.Rotate.Value.Y)) *
                    Matrix4x4.CreateRotationZ(MathHelper.DegreesToRadians(o.Rotate.Value.Z));
            }
            obj.Archetype = archetypes[o.Archetype];
            if (obj.Archetype.Type == Data.Solar.ArchetypeType.sun)
            {
                if (o.Star != null) //Not sure what to do if there's no star?
                {
                    var sun = new GameData.Archetypes.Sun();
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
                            sun.Spines = new List<GameData.Spine>(spines.Items.Count);
                            foreach (var sp in spines.Items)
                                sun.Spines.Add(new GameData.Spine(sp.LengthScale, sp.WidthScale, sp.InnerColor, sp.OuterColor, sp.Alpha));
                        }
                        else
                            FLLog.Error("Stararch", "Could not find spines " + star.Spines);
                    }
                    obj.Archetype = sun;
                }
            }
            else
            {
                if (obj.Archetype.Type == Data.Solar.ArchetypeType.tradelane_ring)
                {
                    obj.Dock = new DockAction()
                    {
                        Kind = DockKinds.Tradelane,
                        Target = o.NextRing,
                        TargetLeft = o.PrevRing
                    };
                }
            }

            var ld = GetLoadout(o.Loadout);
            var archld = GetLoadout(obj.Archetype.LoadoutName);
            if (ld != null) ProcessLoadout(ld, obj);
            if (archld != null) ProcessLoadout(archld, obj);
            return obj;
        }


        //Used to spawn objects within mission scripts
        public GameData.Archetype GetSolarArchetype(string id) => archetypes[id];

        public GameData.Items.Equipment GetEquipment(string id)
        {
            GameData.Items.Equipment eq;
            equipments.TryGetValue(id, out eq); //Should throw error, but we don't parse all equipment yet
            return eq;
        }

        public GameData.Items.Equipment GetEquipment(uint crc)
        {
            GameData.Items.Equipment eq;
            equipmentHashes.TryGetValue(crc, out eq);
            return eq;
        }

        void ProcessLoadout(Data.Solar.Loadout ld, GameData.SystemObject obj)
        {
            foreach (var eq in ld.Equip)
            {
                GameData.Items.Equipment equip = GetEquipment(eq.Nickname);
                //if (equip is GameData.Items.GunEquipment) continue;
                if (equip != null)
                {
                    if (string.IsNullOrEmpty(eq.Hardpoint))
                        obj.LoadoutNoHardpoint.Add(equip);
                    else
                    {
                        if (!obj.Loadout.ContainsKey(eq.Hardpoint)) obj.Loadout.Add(eq.Hardpoint, equip);
                    }
                }
            }
        }

        private Dictionary<string, FuseResources> fuses =
            new Dictionary<string, FuseResources>(StringComparer.OrdinalIgnoreCase);

        public FuseResources GetFuse(string fusename)
        {
            FuseResources fuse;
            if (!fuses.TryGetValue(fusename, out fuse))
            {
                var fz = fldata.Fuses.Fuses[fusename];
                fuse = new GameData.FuseResources() {Fuse = fz};
                foreach (var act in fz.Actions)
                {
                    if (act is FuseStartEffect fza)
                    {
                        //if (!fuse.Fx.ContainsKey(fza.Effect))
                            //fuse.Fx[fza.Effect] = GetEffect(fza.Effect).GetEffect(resource);
                    }
                }

                fuses.Add(fusename, fuse);
            }
            return fuse;
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
            var alepath = ResolveDataNoExcept(visfx.AlchemyPath);
            if (alepath == null) return null;
            return new ResolvedFx()
            {
                AlePath = alepath,
                VisFxCrc = (uint)visfx.EffectCrc,
                LibraryFiles = visfx.Textures.Select(ResolveDataNoExcept).Where(x => x != null).ToArray()
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

