using LibreLancer.ContentEdit;
using LibreLancer.Data.Ini;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent;

public class NewsSaveStrategy(NewsEditorTab tab) : ISaveStrategy
{
    public void Save()
    {
        tab.Data.GameData.Items.News = tab.News.Clone();
        var newsPath = tab.Data.GameData.Items.DataPath("MISSIONS/news.ini");
        var filePath = tab.Data.GameData.VFS.GetBackingFileName(newsPath);
        IniWriter.WriteIniFile(filePath, IniSerializer.SerializeNews(tab.News));
        tab.Dirty = false;
        tab.OnSaved();
    }

    public void DrawMenuOptions()
    {
        if (Theme.IconMenuItem(Icons.Save, $"Save News", true))
        {
            Save();
        }
        Theme.IconMenuItem(Icons.Save, "Save As", false);
    }

    public bool ShouldSave => tab.Dirty;
}
