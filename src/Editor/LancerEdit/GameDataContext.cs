using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.Graphics;
using LibreLancer.ImUI;
using LibreLancer.Data.IO;
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
        if (cache != null) {
            cacheDir = Path.Combine(cache, CacheID(folder));
            Directory.CreateDirectory(cacheDir);
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

    private Archetype[] allArchetypes;
    private int renderIndex = 0;
    private List<Task> writeTasks;

    public bool IterateRenderArchetypePreviews()
    {
        if (allArchetypes == null)
        {
            allArchetypes = GameData.Archetypes.ToArray();
            renderIndex = 0;
            writeTasks = new List<Task>();
            FLLog.Debug("ArchetypePreviews", "Render start");
            return true;
        }
        for (int i = 0; i < 60; i++)
        {
            if (renderIndex >= allArchetypes.Length)
            {
                FLLog.Debug("ArchetypePreviews", "Render complete");
                allArchetypes = null;
                renderIndex = 0;
                Task.WaitAll(writeTasks.ToArray());
                writeTasks = null;
                return false;
            }
            if (LoadCachedPreview(allArchetypes[renderIndex]))
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
        return true;
    }

    bool LoadCachedPreview(LibreLancer.GameData.Archetype archetype)
    {
        if (renderedArchetypes.ContainsKey(archetype.Nickname))
            return true;
        if (cacheDir == null)
            return false;
        var cachePath = Path.Combine(cacheDir, archetype.CRC.ToString("X") + ".png");
        if (File.Exists(cachePath))
        {
            using var stream = File.OpenRead(cachePath);
            var tex = Generic.TextureFromStream(win.RenderContext, stream);
            renderedArchetypes[archetype.Nickname] = ((Texture2D)tex, ImGuiHelper.RegisterTexture((Texture2D)tex));
            return true;
        }
        return false;
    }

    private Dictionary<string, (Texture2D, int)> renderedArchetypes = new Dictionary<string, (Texture2D, int)>();

    public int GetArchetypePreview(LibreLancer.GameData.Archetype archetype, ArchetypePreviews renderer = null, List<Task> saveAsync = null)
    {
        if (renderedArchetypes.TryGetValue(archetype.Nickname, out var arch))
            return arch.Item2;
        Texture2D tx;
        if(renderer != null)
            tx = renderer.RenderPreview(archetype, 128, 128);
        else
        {
            using var r2 = new ArchetypePreviews(win, Resources, cacheDir);
            tx = r2.RenderPreview(archetype, 128, 128);
        }
        arch = (tx, ImGuiHelper.RegisterTexture(tx));
        renderedArchetypes[archetype.Nickname] = arch;
        if (cacheDir != null) {
            var cachePath = Path.Combine(cacheDir, archetype.CRC.ToString("X") + ".png");
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
