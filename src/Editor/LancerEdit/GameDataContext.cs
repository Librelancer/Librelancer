using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.Data;
using LibreLancer.Graphics;
using LibreLancer.ImUI;
using LibreLancer.Data.IO;
using LibreLancer.GameData;
using LibreLancer.ImageLib;
using LibreLancer.Sounds;
using Archetype = LibreLancer.GameData.Archetype;

namespace LancerEdit;

public class GameDataContext : IDisposable
{
    public GameDataManager GameData;
    public GameResourceManager Resources;
    public SoundManager Sounds;
    public FontManager Fonts;

    private MainWindow win;

    public EditableInfocardManager Infocards => (EditableInfocardManager)GameData.Ini.Infocards;

    public string Folder;
    public string UniverseVfsFolder;

    private string cacheDir;

    //Play it safe with MAX_PATH on windows
    private const string ALPHABET = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_";

    static string CacheID(string path)
    {
        var data = SHA256.HashData(Encoding.UTF8.GetBytes(path.ToUpper()));
        var builder = new StringBuilder();
        builder.Append("0");
        var val = new BigInteger(data, true);
        var divisor = ALPHABET.Length;
        while (val > 0)
        {
            val = BigInteger.DivRem(val, divisor, out var rem);
            builder.Append(ALPHABET[(int)rem]);
        }

        return builder.ToString();
    }

    public void Load(MainWindow win, string folder, string cache, Action onComplete, Action<Exception> onError)
    {
        Folder = folder;
        if (cache != null)
        {
            cacheDir = Path.Combine(cache, CacheID(folder));
            Directory.CreateDirectory(cacheDir);
            string astmats = Path.Combine(cacheDir, "asteroidmats");
            try
            {
                if (File.Exists(Path.Combine(cacheDir, "asteroidmats")))
                {
                    foreach (var ln in File.ReadAllLines(astmats))
                    {
                        if (string.IsNullOrWhiteSpace(ln)) continue;
                        var m = JSON.Deserialize<AsteroidInfo>(ln.Trim());
                        asteroidInfos[m.Asteroid] = m;
                    }
                }
            }
            catch
            {
                asteroidInfos = new(StringComparer.OrdinalIgnoreCase);
            }
        }

        var vfs = FileSystem.FromPath(folder);
        Resources = new GameResourceManager(win, vfs);
        this.win = win;
        Task.Run(() =>
        {
            try
            {
                GameData = new GameDataManager(vfs, Resources);
                GameData.LoadData(win);
                //Replace infocard manager with editable version
                GameData.Ini.Infocards = new EditableInfocardManager(GameData.Ini.Infocards.Dlls);
                char[] splits = ['\\', '/'];
                var uniSplit = GameData.Ini.Freelancer.UniversePath.Split(splits, StringSplitOptions.RemoveEmptyEntries);
                UniverseVfsFolder = $"{string.Join('\\', uniSplit.Take(uniSplit.Length - 1))}\\";
                FLLog.Info("Game", "Finished loading game data");
                win.QueueUIThread(() =>
                {
                    Sounds = new SoundManager(GameData, win.Audio, win);
                    Fonts = new FontManager();
                    Fonts.LoadFontsFromGameData(GameData);
                    onComplete();
                });
            }
            catch (Exception e)
            {
                win.QueueUIThread(() => onError(e));
            }
        });
    }

    private Dictionary<string, (Texture2D, int)> renderedArchetypes = new Dictionary<string, (Texture2D, int)>();

    record struct AsteroidInfo(string Asteroid, string Material, uint MaterialCrc);

    private Dictionary<string, AsteroidInfo> asteroidInfos = new(StringComparer.OrdinalIgnoreCase);

    private Archetype[] allArchetypes;
    private Asteroid[] allAsteroids;
    private int renderIndex = 0;
    private List<Task> writeTasks;

    public bool IterateRenderArchetypePreviews()
    {
        if (allArchetypes == null)
        {
            allArchetypes = GameData.Archetypes.ToArray();
            allAsteroids = GameData.Asteroids.ToArray();
            renderIndex = 0;
            writeTasks = new List<Task>();
            FLLog.Debug("ArchetypePreviews", "Render start");
            return true;
        }

        for (int i = 0; i < 60; i++)
        {
            if (renderIndex >= (allArchetypes.Length + allAsteroids.Length))
            {
                FLLog.Debug("ArchetypePreviews", "Render complete");
                allArchetypes = null;
                allAsteroids = null;
                renderIndex = 0;
                Task.WaitAll(writeTasks.ToArray());
                writeTasks = null;
                return false;
            }

            if (renderIndex < allArchetypes.Length)
            {
                if (LoadCachedPreview(allArchetypes[renderIndex].CRC.ToString("X")))
                {
                    renderIndex++;
                    continue;
                }

                // Dispose everything for each preview rendered
                // important so big mods don't eat all VRAM at once
                using var rm = new GameResourceManager(Resources);
                using var renderer = new ArchetypePreviews(win, rm, cacheDir);
                GetArchetypePreview(allArchetypes[renderIndex], renderer, writeTasks);
                renderIndex++;
            }
            else
            {
                if (LoadCachedPreview("AST_" + allAsteroids[renderIndex - allArchetypes.Length].CRC.ToString("X")))
                {
                    renderIndex++;
                    continue;
                }

                using var rm = new GameResourceManager(Resources);
                using var renderer = new ArchetypePreviews(win, rm, cacheDir);
                GetAsteroidPreview(allAsteroids[renderIndex - allArchetypes.Length], renderer, writeTasks);
                renderIndex++;
                renderIndex++;
            }
        }

        return true;
    }

    bool LoadCachedPreview(string cacheId)
    {
        if (renderedArchetypes.ContainsKey(cacheId))
            return true;
        if (cacheDir == null)
            return false;
        var cachePath = Path.Combine(cacheDir, cacheId + ".png");
        if (File.Exists(cachePath))
        {
            using var stream = File.OpenRead(cachePath);
            var tex = Generic.TextureFromStream(win.RenderContext, stream);
            renderedArchetypes[cacheId] = ((Texture2D)tex, ImGuiHelper.RegisterTexture((Texture2D)tex));
            return true;
        }

        return false;
    }

    int RenderAndCache(string cacheId, Func<Texture2D> render, List<Task> saveAsync = null)
    {
        if (renderedArchetypes.TryGetValue(cacheId, out var arch))
            return arch.Item2;
        Texture2D tx = render();
        arch = (tx, ImGuiHelper.RegisterTexture(tx));
        renderedArchetypes[cacheId] = arch;
        if (cacheDir != null)
        {
            var cachePath = Path.Combine(cacheDir, cacheId + ".png");
            var dat = new Bgra8[tx.Width * tx.Height];
            tx.GetData(dat);
            if (saveAsync != null)
            {
                saveAsync.Add(Task.Run(() =>
                {
                    using var stream = File.Create(cachePath);
                    PNG.Save(stream, tx.Width, tx.Height, dat, true);
                }));
            }
            else
            {
                using var stream = File.Create(cachePath);
                PNG.Save(stream, tx.Width, tx.Height, dat, true);
            }
        }

        return arch.Item2;
    }

    public int GetArchetypePreview(LibreLancer.GameData.Archetype archetype, ArchetypePreviews renderer = null,
        List<Task> saveAsync = null)
    {
        return RenderAndCache(archetype.CRC.ToString("X"), () =>
        {
            if (renderer != null)
                return renderer.RenderPreview(archetype, 128, 128);
            using var r2 = new ArchetypePreviews(win, Resources, cacheDir);
            return r2.RenderPreview(archetype, 128, 128);
        }, saveAsync);
    }

    static uint GetFirstMaterial(ResolvedModel src, ResourceManager res)
    {
        var mdl = src.LoadFile(res);
        var rm = ((IRigidModelFile)mdl.Drawable).CreateRigidModel(true, res);
        foreach (var p in rm.AllParts)
        {
            if (p.Mesh == null) continue;
            foreach (var l in p.Mesh.Levels)
            {
                if (l == null || l.Drawcalls == null) continue;
                foreach (var dc in l.Drawcalls)
                {
                    return dc.MaterialCrc;
                }
            }
        }
        return 0;
    }

    AsteroidInfo GetMatInfo(Asteroid ast, ResourceManager res)
    {
        if (!asteroidInfos.TryGetValue(ast.Nickname, out var ai))
        {
            var matCrc = GetFirstMaterial(ast.ModelFile, res);
            var matName = res.FindMaterial(matCrc)?.Name ?? "???";
            ai = new(ast.Nickname, matName, matCrc);
            asteroidInfos[ast.Nickname] = ai;
            if (cacheDir != null) {
                File.AppendAllText(Path.Combine(cacheDir, "asteroidmats"), JsonSerializer.Serialize(ai) + "\n");
            }
        }
        return ai;
    }

    public (int, string, uint) GetAsteroidPreview(Asteroid archetype, ArchetypePreviews renderer = null,
        List<Task> saveAsync = null)
    {
        var tex = RenderAndCache("AST_" + archetype.CRC.ToString("X"), () =>
        {
            if (renderer != null) {
                GetMatInfo(archetype, renderer.Resources);
                return renderer.RenderPreview(archetype, 128, 128);
            }
            GetMatInfo(archetype, Resources);
            using var r2 = new ArchetypePreviews(win, Resources, cacheDir);
            return r2.RenderPreview(archetype, 128, 128);
        }, saveAsync);
        var info = GetMatInfo(archetype, Resources);
        return (tex, info.Material, info.MaterialCrc);
    }

    public void Dispose()
    {
        Sounds.Dispose();
        Resources.Dispose();
        foreach (var ax in renderedArchetypes)
        {
            ImGuiHelper.DeregisterTexture(ax.Value.Item1);
            ax.Value.Item1.Dispose();
        }
    }
}
