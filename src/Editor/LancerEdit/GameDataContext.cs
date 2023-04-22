using System;
using System.Threading.Tasks;
using LibreLancer;
using LibreLancer.Sounds;

namespace LancerEdit;

public class GameDataContext : IDisposable
{
    public GameDataManager GameData;
    public GameResourceManager Resources;
    public SoundManager Sounds;
    public FontManager Fonts;

    public string Folder;

    public void Load(MainWindow win, string folder, Action onComplete)
    {
        Task.Run(() =>
        {
            Folder = folder;
            Resources = new GameResourceManager(win);
            GameData = new GameDataManager(folder, Resources);
            GameData.LoadData(win);
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
    
    public void Dispose()
    {
        Sounds.Dispose();
        Resources.Dispose();
    }
}