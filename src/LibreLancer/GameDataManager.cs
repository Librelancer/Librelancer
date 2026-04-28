using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    private GameResourceManager? glResource;
    public GameItemDb Items;

    public FileSystem VFS => Items.VFS;

    private AnmFile? characterAnimations;

    public GameDataManager(GameItemDb items, ResourceManager resources)
    {
        Resources = resources;
        Items = items;
        glResource = Resources as GameResourceManager;
    }

    public void LoadData(IUIThread? ui, bool preloadCharacterAnimations = false, Action? onIniLoaded = null)
    {
        Items.LoadData(() =>
        {
            if (glResource != null && ui != null)
            {
                glResource.AddPreload(
                    Items.Ini.EffectShapes.Files.Select(txmfile => Items.DataPath(txmfile)).Where(x => x != null)
                        .OfType<string>()
                );

                foreach (var shape in Items.Ini.EffectShapes.Shapes)
                {
                    glResource.AddShape(shape.Key, shape.Value);
                }

                ui.QueueUIThread(() => glResource.Preload());
            }

            if (ui != null && onIniLoaded != null)
            {
                ui.QueueUIThread(onIniLoaded);
            }

            if (preloadCharacterAnimations)
            {
                GetCharacterAnimations();
            }
        });
    }

    public AnmFile GetCharacterAnimations()
    {
        if (characterAnimations != null)
        {
            return characterAnimations;
        }

        characterAnimations = new AnmFile();
        var stringTable = new StringDeduplication();

        foreach (var path in Items.Ini.Bodyparts.Animations.Select(file => Items.DataPath(file)).Where(x => x != null)
                     .OfType<string>())
        {
            using var stream = Items.VFS.Open(path);
            AnmFile.ParseToTable(characterAnimations.Scripts, characterAnimations.Buffer, stringTable, stream, path);
        }

        characterAnimations.Buffer.Commit();

        return characterAnimations;
    }

    public IEnumerable<Maneuver> GetManeuvers()
    {
        return Items.Ini.Hud.Maneuvers.Select(m => new Maneuver()
        {
            Action = m.Action,
            InfocardA = GetString(m.InfocardA),
            InfocardB = GetString(m.InfocardB),
            ActiveModel = m.ActiveModel,
            InactiveModel = m.InactiveModel,
        });
    }

    public Texture2D? GetSplashScreen()
    {
        const string splashTextureFileName = "startupscreen.tga";
        const string splashTextureFileNameLarge = "startupscreen_1280.tga";

        if (glResource is null)
        {
            return null;
        }

        if (glResource.TextureExists(splashTextureFileNameLarge))
        {
            return Resources.FindTexture(splashTextureFileNameLarge) as Texture2D;
        }

        if (glResource.TextureExists(splashTextureFileName))
        {
            return Resources.FindTexture(splashTextureFileName) as Texture2D;
        }

        if (Items.VFS.FileExists(Items.Ini.Freelancer.DataPath + $"INTERFACE/INTRO/IMAGES/{splashTextureFileNameLarge}"))
        {
            glResource.AddTexture(
                splashTextureFileNameLarge,
                Items.DataPath($"INTERFACE/INTRO/IMAGES/{splashTextureFileNameLarge}")!
            );

            return glResource.FindTexture(splashTextureFileNameLarge) as Texture2D;
        }

        if (Items.VFS.FileExists(Items.Ini.Freelancer.DataPath + $"INTERFACE/INTRO/IMAGES/{splashTextureFileName}"))
        {
            glResource.AddTexture(
                splashTextureFileName,
                Items.DataPath($"INTERFACE/INTRO/IMAGES/{splashTextureFileName}")!
            );

            return glResource.FindTexture(splashTextureFileName) as Texture2D;
        }

        FLLog.Error("Splash", "Splash screen not found");
        return Resources.WhiteTexture;
    }

    private void PreloadSur(IDrawable dr, ResourceManager res)
    {
        if (dr is not IRigidModelFile rm)
        {
            return;
        }

        var mdl = rm.CreateRigidModel(res is GameResourceManager, res);
        var surpath = Path.ChangeExtension(mdl.Path, ".sur");
        if (!File.Exists(surpath))
        {
            return;
        }

        var cvx = res.ConvexCollection.UseFile(surpath);

        if (mdl.Source == RigidModelSource.SinglePart)
        {
            res.ConvexCollection.CreateShape(cvx, new ConvexMeshId(0, 0));
        }
        else
        {
            foreach (var p in mdl.AllParts)
                res.ConvexCollection.CreateShape(cvx, new ConvexMeshId(0, CrcTool.FLModelCrc(p.Name)));
        }
    }

    public void PreloadObjects(PreloadObject[]? objs, ResourceManager? resources = null)
    {
        resources ??= Resources;
        if (objs == null)
        {
            return;
        }

        foreach (var o in objs)
        {
            switch (o.Type)
            {
                case PreloadType.Ship:
                {
                    foreach (var v in o.Values)
                    {
                        var sh = Items.Ships.Get(v);
                        sh?.ModelFile?.LoadFile(resources);
                    }

                    break;
                }
                case PreloadType.Equipment:
                {
                    foreach (var v in o.Values)
                    {
                        var eq = Items.Equipment.Get(v);
                        eq?.ModelFile?.LoadFile(resources);
                    }

                    break;
                }
            }
        }
    }

    private bool cursorsDone = false;

    public void PopulateCursors()
    {
        if (cursorsDone)
        {
            return;
        }

        cursorsDone = true;

        Resources.LoadResourceFile(Items.DataPath(Items.Ini.Mouse.TxmFile!)!);

        foreach (var lc in Items.Ini.Mouse.Cursors)
        {
            var shape = Items.Ini.Mouse.Shapes.First(arg => arg.Name!.Equals(lc.Shape, StringComparison.OrdinalIgnoreCase));
            var cur = new Cursor
            {
                Nickname = lc.Nickname,
                Scale = lc.Scale,
                Spin = lc.Spin,
                Color = lc.Color,
                Hotspot = lc.Hotspot,
                Dimensions = shape.Dimensions,
                Texture = Items.Ini.Mouse.TextureName
            };

            glResource?.AddCursor(cur, cur.Nickname);
        }
    }

    public IEnumerable<Data.Schema.Audio.AudioEntry> AllSounds => Items.Ini.Audio.Entries;

    public Data.Schema.Audio.AudioEntry? GetAudioEntry(string id)
    {
        var audio = Items.Ini.Audio.Entries.FirstOrDefault((arg) =>
            string.Equals(arg.Nickname, id, StringComparison.InvariantCultureIgnoreCase));

        if (audio == null)
        {
            FLLog.Warning("Audio", $"Audio entry '{id}' not found");
        }

        return audio;
    }

    public Stream? GetAudioStream(string id)
    {
        var audio = Items.Ini.Audio.Entries.FirstOrDefault((arg) =>
            string.Equals(arg.Nickname, id, StringComparison.InvariantCultureIgnoreCase));

        if (audio != null)
        {
            return Items.VFS.FileExists(Items.DataPath(audio.File))
                ? Items.VFS.Open(Items.DataPath(audio.File)!)
                : null;
        }

        FLLog.Warning("Audio", $"Audio entry '{id}' not found");
        return null;

    }

    public string? GetVoicePath(string id)
    {
        return Items.DataPath("AUDIO\\" + id + ".utf");
    }

    public string? GetInfocardText(int id, FontManager fonts)
    {
        var res = Items.Ini.Infocards.GetXmlResource(id);
        return res == null ? null : Infocards.RDLParse.Parse(res, fonts).ExtractText();
    }

    public Infocards.Infocard GetInfocard(int id, FontManager fonts)
    {
        return Infocards.RDLParse.Parse(Items.Ini.Infocards.GetXmlResource(id), fonts);
    }

    public bool GetRelatedInfocard(int ogId, FontManager fonts, [MaybeNullWhen(false)] out Infocards.Infocard ic)
    {
        ic = null;

        if (!Items.Ini.InfocardMap.Map.TryGetValue(ogId, out int newId))
        {
            return false;
        }

        ic = GetInfocard(newId, fonts);
        return true;

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

    public IEnumerator<object?> LoadSystemResources(StarSystem sys)
    {
        if (Items.Ini.Stars != null)
        {
            foreach (var txmFile in Items.Ini.Stars.TextureFiles
                         .SelectMany(x => x.Files))
            {
                Resources.LoadResourceFile(Items.DataPath(txmFile));
            }
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
                obj.Archetype?.ModelFile?.LoadFile(glResource);
                if (a % 3 == 0)
                {
                    yield return null;
                }

                a++;
            }
        }

        foreach (var resFile in sys.ResourceFiles)
        {
            Resources.LoadResourceFile(resFile);
            if (a % 3 == 0)
            {
                yield return null;
            }

            a++;
        }
    }

    public void LoadAllSystem(StarSystem system)
    {
        var iterator = LoadSystemResources(system);

        while (iterator.MoveNext())
        {
        }
    }

    public (ModelResource?, float[]?) GetSolar(string solar)
    {
        var at = Items.Archetypes.Get(solar);
        return (at?.ModelFile?.LoadFile(Resources), at?.LODRanges);
    }

    public IDrawable? GetProp(string prop)
    {
        if (Items.Ini.PetalDb.Props.TryGetValue(prop, out var path))
        {
            return Resources.GetDrawable(Items.DataPath(path))?.Drawable;
        }

        FLLog.Error("PetalDb", "No prop exists: " + prop);
        return null;
    }

    public IDrawable? GetCart(string cart)
    {
        return Resources.GetDrawable(Items.DataPath(Items.Ini.PetalDb.Carts[cart]))?.Drawable;
    }

    public IDrawable? GetRoom(string room)
    {
        return Resources.GetDrawable(Items.DataPath(Items.Ini.PetalDb.Rooms[room]))?.Drawable;
    }

    public Dictionary<string, string> GetBaseNavbarIcons()
    {
        return Items.Ini.BaseNavBar.Navbar;
    }

    public string? GetCostumeForNPC(string npc)
    {
        return Items.Ini.SpecificNPCs.Npcs
            .FirstOrDefault(x => x.Nickname.Equals(npc, StringComparison.OrdinalIgnoreCase))
            ?.BaseAppr;
    }
}
