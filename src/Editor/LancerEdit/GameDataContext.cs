using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.GameData;
using LibreLancer.Graphics;
using LibreLancer.ImUI;
using LibreLancer.Data;
using LibreLancer.Data.IO;
using LibreLancer.Sounds;

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

    public void Load(MainWindow win, string folder, Action onComplete, Action<Exception> onError)
    {
        Folder = folder;
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


    private int renderCounter = 0;


    public bool RenderAllArchetypePreviews()
    {
        //Delay Processing so messages can show
        ImGuiHelper.AnimatingElement();
        if (renderCounter < 5) {
            renderCounter++;
            return false;
        }
        //Do things

        foreach (var a in GameData.Archetypes)
        {
            // Dispose everything for each preview rendered
            // important so big mods don't eat all VRAM at once
            using var rm = new GameResourceManager(Resources);
            using var renderer = new ArchetypePreviews(win, rm);
            GetArchetypePreview(a, renderer);
        }
        return true;
    }

    private Dictionary<string, (Texture2D, int)> renderedArchetypes = new Dictionary<string, (Texture2D, int)>();

    public int GetArchetypePreview(LibreLancer.GameData.Archetype archetype, ArchetypePreviews renderer = null)
    {
        if (renderedArchetypes.TryGetValue(archetype.Nickname, out var arch))
            return arch.Item2;
        Texture2D tx;
        if(renderer != null)
            tx = renderer.RenderPreview(archetype, 128, 128);
        else
        {
            using var r2 = new ArchetypePreviews(win, Resources);
            tx = r2.RenderPreview(archetype, 128, 128);
        }
        arch = (tx, ImGuiHelper.RegisterTexture(tx));
        renderedArchetypes[archetype.Nickname] = arch;
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
