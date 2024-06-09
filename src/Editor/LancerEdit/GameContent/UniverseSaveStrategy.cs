using System.IO;
using LibreLancer.ContentEdit;
using LibreLancer.ImUI;
using LibreLancer.Ini;

namespace LancerEdit.GameContent;

public class UniverseSaveStrategy : ISaveStrategy
{
    public UniverseEditorTab Tab;
    public void Save()
    {
        foreach (var s in Tab.AllSystems)
            s.System.UniversePosition = s.Position;
        var path = Tab.Data.GameData.VFS.GetBackingFileName(Tab.Data.GameData.Ini.Freelancer.UniversePath);
        using (var stream = File.Create(path))
        {
            var sections = IniSerializer.SerializeUniverse(Tab.Data.GameData.Systems, Tab.Data.GameData.Bases);
            IniWriter.WriteIni(stream, sections);
        }

        Tab.Dirty = false;
    }

    public void DrawMenuOptions()
    {
        if(Theme.IconMenuItem(Icons.Save, $"Save Universe", Tab.Dirty))
            Save();
        Theme.IconMenuItem(Icons.Save, "Save As", false);
    }

    public bool ShouldSave => Tab.Dirty;
}
