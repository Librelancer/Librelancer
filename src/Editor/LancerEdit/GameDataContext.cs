using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.GameData;
using LibreLancer.ImUI;
using LibreLancer.Render.Cameras;
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

    public void Load(MainWindow win, string folder, Action onComplete)
    {
        Folder = folder;
        Resources = new GameResourceManager(win);
        this.win = win;
        Task.Run(() =>
        {
            GameData = new GameDataManager(folder, Resources);
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
        });
    }
    

    private Dictionary<string, (Texture2D, int)> renderedArchetypes = new Dictionary<string, (Texture2D, int)>();
    
    public int GetArchetypePreview(Archetype archetype)
    {
        if (renderedArchetypes.TryGetValue(archetype.Nickname, out var arch))
            return arch.Item2;
        var mdl = archetype.ModelFile?.LoadFile(Resources);
        if (mdl is IRigidModelFile rmf)
        {
            var tx = ModelPreviews.RenderPreview(win, rmf.CreateRigidModel(true), Resources, 128, 128);
            arch = (tx, ImGuiHelper.RegisterTexture(tx));
            renderedArchetypes[archetype.Nickname] = arch;
        }
        return -1;
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