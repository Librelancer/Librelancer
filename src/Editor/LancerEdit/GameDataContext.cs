using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
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

    // Playing it safe with MAX_PATH on windows
    // Standard hex SHA256 is 64 characters in length
    // This ID is more compact, at the cost of complexity + some reduction in hash length
    private const string ALPHABET = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_;-+=,#$@%^!~`()[]{}";

    static string CacheID(string path)
    {
        var data = SHA256.HashData(Encoding.UTF8.GetBytes(path.ToUpper()));
        var builder = new StringBuilder();
        // Get the first 192 bits of hash, don't need all 256 for this purpose
        var val = new BigInteger(data.AsSpan().Slice(0,24), true);
        var divisor = ALPHABET.Length;
        while (val > 0)
        {
            val = BigInteger.DivRem(val, divisor, out var rem);
            builder.Append(ALPHABET[(int)rem]);
        }

        return builder.ToString();
    }

    T YieldAndWait<T>(Task<T> task)
    {
        var awaiter = Task.Run(async () => await task);
        while(!awaiter.IsCompleted)
        {
            win.Yield();
            Thread.Sleep(0);
        }
        if (awaiter.Exception != null)
            throw awaiter.Exception;
        return awaiter.Result;
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
                var sw = Stopwatch.StartNew();
                GameData = new GameDataManager(vfs, Resources);
                GameData.LoadData(win);
                //Replace infocard manager with editable version
                GameData.Ini.Infocards = new EditableInfocardManager(GameData.Ini.Infocards.Dlls);
                char[] splits = ['\\', '/'];
                var uniSplit = GameData.Ini.Freelancer.UniversePath.Split(splits, StringSplitOptions.RemoveEmptyEntries);
                UniverseVfsFolder = $"{string.Join('\\', uniSplit.Take(uniSplit.Length - 1))}\\";
                sw.Stop();
                FLLog.Info("Game", $"Finished loading game data in {sw.Elapsed.TotalSeconds:0.000} seconds");
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
    private List<Task<int>> drawTasks;

    public float PreviewLoadPercent { get; private set; } = 0;

    private const int MEMORY_BUDGET = 72 * 1024 * 1024; //72MiB

    private GameResourceManager rmBulk;
    private PreviewRenderer prevBulk;
    public bool IterateRenderArchetypePreviews(int max = 120)
    {
        if (allArchetypes == null)
        {
            allArchetypes = GameData.Archetypes.ToArray();
            allAsteroids = GameData.Asteroids.ToArray();
            renderIndex = 0;
            PreviewLoadPercent = 0;
            drawTasks = new List<Task<int>>();
            FLLog.Debug("ArchetypePreviews", "Render start");
            return true;
        }

        rmBulk ??= new GameResourceManager(Resources);
        prevBulk ??= new PreviewRenderer(win, rmBulk);

        for (int i = 0; i < max; i++)
        {
            FLLog.Debug("Render", $"Budget used: {DebugDrawing.SizeSuffix(rmBulk.EstimatedTextureMemory)}");
            //Dispose when we hit memory budget
            if (rmBulk.EstimatedTextureMemory >= MEMORY_BUDGET)
            {
                FLLog.Error("RENDER", "HIT BUDGET, DISPOSE");
                rmBulk.Dispose();
                prevBulk.Dispose();
                rmBulk = new GameResourceManager(Resources);
                prevBulk = new PreviewRenderer(win, rmBulk);
            }
            if (renderIndex >= (allArchetypes.Length + allAsteroids.Length))
            {
                FLLog.Debug("ArchetypePreviews", "Render complete");
                allArchetypes = null;
                allAsteroids = null;
                renderIndex = 0;
                YieldAndWait(Task.WhenAll(drawTasks.ToArray()));
                PreviewLoadPercent = 1;
                drawTasks = null;
                prevBulk.Dispose();
                rmBulk.Dispose();
                prevBulk = null;
                rmBulk = null;
                return false;
            }

            if (renderIndex < allArchetypes.Length)
            {
                if (LoadCachedPreview(allArchetypes[renderIndex].CRC.ToString("X")))
                {
                    renderIndex++;
                    continue;
                }

                i++;
                drawTasks.Add(RegisterArchetypePreview(allArchetypes[renderIndex], prevBulk));
                renderIndex++;
            }
            else
            {
                if (LoadCachedPreview("AST_" + allAsteroids[renderIndex - allArchetypes.Length].CRC.ToString("X")))
                {
                    renderIndex++;
                    continue;
                }

                i++;
                drawTasks.Add(DrawAsteroidPreview(allAsteroids[renderIndex - allArchetypes.Length], prevBulk));
                renderIndex++;
            }

            PreviewLoadPercent = (renderIndex) / (float)(allArchetypes.Length + allAsteroids.Length);
        }

        return true;
    }

    bool LoadCachedPreview(string cacheId)
    {
        if (renderedArchetypes.ContainsKey(cacheId))
            return true;
        if (cacheDir == null)
            return false;
        var cachePath = Path.Combine(cacheDir, cacheId + ".dds.zstd");
        if (File.Exists(cachePath))
        {
            Texture2D tex;
            try
            {
                using var stream = new ZstdSharp.DecompressionStream(File.OpenRead(cachePath), 0, true, false);
                tex = (Texture2D)DDS.FromStream(win.RenderContext, stream);
            }
            catch (Exception)
            {
                return false;
            }
            renderedArchetypes[cacheId] = (tex, ImGuiHelper.RegisterTexture(tex));
            return true;
        }

        return false;
    }

    Task<int> RenderAndCache(string cacheId, Func<Texture2D> render)
    {
        if (renderedArchetypes.TryGetValue(cacheId, out var existing))
            return Task.FromResult(existing.Item2);
        Texture2D tx = render();
        if (cacheDir != null)
        {
            var cachePath = Path.Combine(cacheDir, cacheId + ".dds.zstd");
            int w = tx.Width, h = tx.Height;
            var dat = new Bgra8[tx.Width * tx.Height];
            tx.GetData(dat);
            tx.Dispose();
            return Task.Run(async () =>
            {
                TaskCompletionSource<int> compSource = new TaskCompletionSource<int>();
                var dxt1 = TextureImport.CreateDDS(dat, w, h, DDSFormat.DXT1, MipmapMethod.Lanczos4, true);
                win.QueueUIThread(() =>
                {
                    using var ms = new MemoryStream(dxt1);
                    tx = (Texture2D)DDS.FromStream(win.RenderContext, ms);
                    var arch = (tx, ImGuiHelper.RegisterTexture(tx));
                    renderedArchetypes[cacheId] = arch;
                    compSource.SetResult(arch.Item2);
                });
                await using var zstd = new ZstdSharp.CompressionStream(File.Create(cachePath), 3, 0, false);
                zstd.Write(dxt1);
                return await compSource.Task;
            });
        }

        var arch = (tx, ImGuiHelper.RegisterTexture(tx));
        renderedArchetypes[cacheId] = arch;
        return Task.FromResult(arch.Item2);
    }

    public int GetArchetypePreview(Archetype archetype) =>
        YieldAndWait(RegisterArchetypePreview(archetype, null));

    Task<int> RegisterArchetypePreview(Archetype archetype, PreviewRenderer renderer) =>
        RenderAndCache(archetype.CRC.ToString("X"), () =>
        {
            if (renderer != null)
                return renderer.Render(archetype, 128, 128);
            using var r2 = new PreviewRenderer(win, Resources);
            return r2.Render(archetype, 128, 128);
        });

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

    public (int, string, uint) GetAsteroidPreview(Asteroid archetype)
    {
        var tex =  YieldAndWait(DrawAsteroidPreview(archetype, null));
        var info = GetMatInfo(archetype, Resources);
        return (tex, info.Material, info.MaterialCrc);
    }


    Task<int> DrawAsteroidPreview(Asteroid archetype, PreviewRenderer renderer) =>
        RenderAndCache("AST_" + archetype.CRC.ToString("X"), () =>
        {
            if (renderer != null) {
                GetMatInfo(archetype, renderer.Resources);
                return renderer.Render(archetype, 128, 128);
            }
            GetMatInfo(archetype, Resources);
            using var r2 = new PreviewRenderer(win, Resources);
            return r2.Render(archetype, 128, 128);
        });

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
