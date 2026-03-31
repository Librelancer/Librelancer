using LibreLancer.ImUI;

namespace LancerEdit.GameContent;

public class InfocardSaveStrategy(InfocardBrowserTab tab) : ISaveStrategy
{
    private InfocardBrowserTab tab = tab;

    public void Save() => tab.Manager.Save();

    public void DrawMenuOptions()
    {
        if (Theme.IconMenuItem(Icons.Save, "Save Infocards", true))
        {
            Save();
        }
        Theme.IconMenuItem(Icons.Save, "Save As", false);
    }

    public bool ShouldSave => tab.Manager.Dirty;
}
