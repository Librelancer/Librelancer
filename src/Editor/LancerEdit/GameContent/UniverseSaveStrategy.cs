using System.IO;
using LibreLancer.ContentEdit;
using LibreLancer.Data.Ini;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent;

public class UniverseSaveStrategy : ISaveStrategy
{
    public UniverseEditorTab Tab;
    public void Save()
    {
        foreach (var s in Tab.AllSystems)
            s.System.UniversePosition = s.Position;
        var path = Tab.Data.GameData.VFS.GetBackingFileName(Tab.Data.GameData.Ini.Freelancer.UniversePath);
        IniWriter.WriteIniFile(path, IniSerializer.SerializeUniverse(Tab.Data.GameData.Systems, Tab.Data.GameData.Bases));
        Tab.Dirty = false;
        Tab.OnSaved();
    }

    public void DrawMenuOptions()
    {
        if(Theme.IconMenuItem(Icons.Save, $"Save Universe", true))
            Save();
        Theme.IconMenuItem(Icons.Save, "Save As", false);
    }

    public bool ShouldSave => Tab.Dirty;
}
