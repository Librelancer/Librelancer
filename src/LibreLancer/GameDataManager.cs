using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibreLancer.Data;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.IO;
using LibreLancer.Graphics;
using LibreLancer.Items;
using LibreLancer.Physics;
using LibreLancer.Resources;
using LibreLancer.Utf.Anm;

namespace LibreLancer;

public class GameDataManager
{
    public ResourceManager Resources;
    private GameResourceManager glResource;
    public GameItemDb Items;

    public FileSystem VFS => Items.VFS;

    private AnmFile characterAnimations;

    public GameDataManager(GameItemDb items, ResourceManager resources)
    {
        Resources = resources;
        Items = items;
        glResource = Resources as GameResourceManager;
    }

    public void LoadData(IUIThread ui, bool preloadCharacterAnimations = false, Action onIniLoaded = null)
    {
        Items.LoadData(() =>
        {
            if (glResource != null && ui != null)
            {
                glResource.AddPreload(
                    Items.Ini.EffectShapes.Files.Select(txmfile => Items.DataPath(txmfile))
                );
                foreach (var shape in Items.Ini.EffectShapes.Shapes)
                {
                    glResource.AddShape(shape.Key, shape.Value);
                }
                ui.QueueUIThread(() => glResource.Preload());
            }
            if(ui != null && onIniLoaded != null) ui.QueueUIThread(onIniLoaded);
            if (preloadCharacterAnimations)
            {
                GetCharacterAnimations();
            }
        });
    }

    public AnmFile GetCharacterAnimations()
    {
        if (characterAnimations == null)
        {
            characterAnimations = new AnmFile();
            var stringTable = new StringDeduplication();
            foreach (var file in Items.Ini.Bodyparts.Animations)
            {
                var path = Items.DataPath(file);
                using var stream = Items.VFS.Open(path);
                AnmFile.ParseToTable(characterAnimations.Scripts, characterAnimations.Buffer, stringTable, stream,
                    path);
            }

            characterAnimations.Buffer.Shrink();
        }

        return characterAnimations;
    }

    public IEnumerable<Maneuver> GetManeuvers()
    {
        foreach (var m in Items.Ini.Hud.Maneuvers)
        {
            yield return new Maneuver()
            {
                Action = m.Action,
                InfocardA = GetString(m.InfocardA),
                InfocardB = GetString(m.InfocardB),
                ActiveModel = m.ActiveModel,
                InactiveModel = m.InactiveModel,
            };
        }
    }

    public Texture2D GetSplashScreen()
    {
        if (!glResource.TextureExists("__startupscreen_1280.tga"))
        {
            if (Items.VFS.FileExists(Items.Ini.Freelancer.DataPath + "INTERFACE/INTRO/IMAGES/startupscreen_1280.tga"))
            {
                glResource.AddTexture(
                    "__startupscreen_1280.tga",
                    Items.DataPath("INTERFACE/INTRO/IMAGES/startupscreen_1280.tga")
                );
            }
            else if (Items.VFS.FileExists(Items.Ini.Freelancer.DataPath + "INTERFACE/INTRO/IMAGES/startupscreen.tga"))
            {
                glResource.AddTexture(
                    "__startupscreen_1280.tga",
                    Items.DataPath("INTERFACE/INTRO/IMAGES/startupscreen.tga")
                );
            }
            else
            {
                FLLog.Error("Splash", "Splash screen not found");
                return Resources.WhiteTexture;
            }

        }

        return (Texture2D)Resources.FindTexture("__startupscreen_1280.tga");
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
        if (mdl.Source == RigidModelSource.SinglePart)
            res.ConvexCollection.CreateShape(cvx, new ConvexMeshId(0, 0));
        else
        {
            foreach (var p in mdl.AllParts)
                res.ConvexCollection.CreateShape(cvx, new ConvexMeshId(0, CrcTool.FLModelCrc(p.Name)));
        }
    }

    public void PreloadObjects(PreloadObject[] objs, ResourceManager resources = null)
    {
        resources ??= Resources;
        if (objs == null) return;
        foreach (var o in objs)
        {
            if (o.Type == PreloadType.Ship)
            {
                foreach (var v in o.Values)
                {
                    var sh = Items.Ships.Get(v);
                    sh?.ModelFile?.LoadFile(resources);
                }
            }
            else if (o.Type == PreloadType.Equipment)
            {
                foreach (var v in o.Values)
                {
                    var eq = Items.Equipment.Get(v);
                    eq?.ModelFile?.LoadFile(resources);
                }
            }
        }
    }

    bool cursorsDone = false;
    public void PopulateCursors()
    {
        if (cursorsDone) return;
        cursorsDone = true;

        Resources.LoadResourceFile(
            Items.DataPath(Items.Ini.Mouse.TxmFile)
        );
        foreach (var lc in Items.Ini.Mouse.Cursors)
        {
            var shape = Items.Ini.Mouse.Shapes.Where((arg) => arg.Name.Equals(lc.Shape, StringComparison.OrdinalIgnoreCase)).First();
            var cur = new Cursor();
            cur.Nickname = lc.Nickname;
            cur.Scale = lc.Scale;
            cur.Spin = lc.Spin;
            cur.Color = lc.Color;
            cur.Hotspot = lc.Hotspot;
            cur.Dimensions = shape.Dimensions;
            cur.Texture = Items.Ini.Mouse.TextureName;
            glResource.AddCursor(cur, cur.Nickname);
        }
    }


        public IEnumerable<Data.Schema.Audio.AudioEntry> AllSounds => Items.Ini.Audio.Entries;
        public Data.Schema.Audio.AudioEntry GetAudioEntry(string id)
        {
            var audio = Items.Ini.Audio.Entries.FirstOrDefault((arg) => arg.Nickname.ToLowerInvariant() == id.ToLowerInvariant());
            if (audio == null)
            {
                FLLog.Warning("Audio", $"Audio entry '{id}' not found");
            }
            return audio;
        }
        public Stream GetAudioStream(string id)
        {
            var audio = Items.Ini.Audio.Entries.FirstOrDefault((arg) => arg.Nickname.ToLowerInvariant() == id.ToLowerInvariant());
            if (audio == null)
            {
                FLLog.Warning("Audio", $"Audio entry '{id}' not found");
                return null;
            }
            if (Items.VFS.FileExists(Items.DataPath(audio.File)))
                return Items.VFS.Open(Items.DataPath(audio.File));
            return null;
        }
        public string GetVoicePath(string id)
        {
            return Items.DataPath("AUDIO\\" + id + ".utf");
        }

        public string GetInfocardText(int id, FontManager fonts)
        {
            var res = Items.Ini.Infocards.GetXmlResource(id);
            if (res == null) return null;
            return Infocards.RDLParse.Parse(res, fonts).ExtractText();
        }
        public Infocards.Infocard GetInfocard(int id, FontManager fonts)
        {
            return Infocards.RDLParse.Parse(Items.Ini.Infocards.GetXmlResource(id), fonts);
        }

        public bool GetRelatedInfocard(int ogId, FontManager fonts, out Infocards.Infocard ic)
        {
            ic = null;
            if (Items.Ini.InfocardMap.Map.TryGetValue(ogId, out int newId))
            {
                ic = GetInfocard(newId, fonts);
                return true;
            }
            return false;
        }
        public string GetString(int id)
        {
            return Items.Ini.Infocards.GetStringResource(id);
        }
        public IntroScene GetIntroScene()
        {
            var rand = new Random();
            return Items.IntroScenes[rand.Next(0, Items.IntroScenes.Count)];
        }
#if DEBUG
        public IntroScene GetIntroSceneSpecific(int i)
        {
            if (i > Items.IntroScenes.Count)
                return null;
            return Items.IntroScenes[i];
        }
#endif

    public IEnumerator<object> LoadSystemResources(StarSystem sys)
    {
        if (Items.Ini.Stars != null)
        {
            foreach (var txmfile in Items.Ini.Stars.TextureFiles
                         .SelectMany(x => x.Files))
                Resources.LoadResourceFile(Items.DataPath(txmfile));
        }
        yield return null;
        sys.StarsBasic?.LoadFile(Resources);
        sys.StarsComplex?.LoadFile(Resources);
        sys.StarsNebula?.LoadFile(Resources);
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
            Resources.LoadResourceFile(resfile);
            if (a % 3 == 0) yield return null;
            a++;
        }
    }

    public void LoadAllSystem(StarSystem system)
    {
        var iterator = LoadSystemResources(system);
        while (iterator.MoveNext()) { }
    }


    public (ModelResource, float[]) GetSolar(string solar)
    {
        var at = Items.Archetypes.Get(solar);
        return (at.ModelFile.LoadFile(Resources), at.LODRanges);
    }


    public IDrawable GetProp(string prop)
    {
        string f;
        if (Items.Ini.PetalDb.Props.TryGetValue(prop, out f))
        {
            return Resources.GetDrawable(Items.DataPath(f)).Drawable;
        }
        else
        {
            FLLog.Error("PetalDb", "No prop exists: " + prop);
            return null;
        }
    }

    public IDrawable GetCart(string cart)
    {
        return Resources.GetDrawable(Items.DataPath(Items.Ini.PetalDb.Carts[cart])).Drawable;
    }

    public IDrawable GetRoom(string room)
    {
        return Resources.GetDrawable(Items.DataPath(Items.Ini.PetalDb.Rooms[room])).Drawable;
    }


    public Dictionary<string, string> GetBaseNavbarIcons()
    {
        return Items.Ini.BaseNavBar.Navbar;
    }

    public List<string> GetIntroMovies()
    {
        var movies = new List<string>();
        foreach (var file in Items.Ini.Freelancer.StartupMovies)
        {
            var path = Items.DataPath(file);
            if (path != null)
                movies.Add(path);
        }
        return movies;
    }

    public bool GetCostume(string costume, out Bodypart body, out Bodypart head, out Bodypart leftHand, out Bodypart rightHand)
    {
        var cs = Items.Ini.Costumes.FindCostume(costume);
        head = Items.Bodyparts.Get(cs.Head);
        body = Items.Bodyparts.Get(cs.Body);
        leftHand = Items.Bodyparts.Get(cs.LeftHand);
        rightHand = Items.Bodyparts.Get(cs.RightHand);
        return true;
    }

    public string GetCostumeForNPC(string npc)
    {
        return Items.Ini.SpecificNPCs.Npcs.FirstOrDefault(x => x.Nickname.Equals(npc, StringComparison.OrdinalIgnoreCase))
            ?.BaseAppr;
    }
}

