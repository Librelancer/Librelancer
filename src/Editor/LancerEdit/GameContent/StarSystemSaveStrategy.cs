using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibreLancer.ContentEdit;
using LibreLancer.GameData.World;
using LibreLancer.ImUI;

namespace LancerEdit;

public class StarSystemSaveStrategy : ISaveStrategy
{
    private SystemEditorTab tab;
    public StarSystemSaveStrategy(SystemEditorTab tab) => this.tab = tab;
    
    public void Save()
    {
        bool writeUniverse = tab.SystemData.IsUniverseDirty();
        tab.SystemData.Apply();
        foreach (var item in tab.World.Objects.Where(x => x.SystemObject != null))
        {
            if (item.TryGetComponent<ObjectEditData>(out var dat))
            {
                dat.Apply();
                if (dat.IsNewObject)
                {
                    tab.CurrentSystem.Objects.Add(item.SystemObject);
                }

                item.Components.Remove(dat);
            }
        }

        foreach (var o in tab.DeletedObjects)
            tab.CurrentSystem.Objects.Remove(o);
        tab.DeletedObjects = new List<SystemObject>();
        var resolved = tab.Data.GameData.ResolveDataPath("universe/" + tab.CurrentSystem.SourceFile);
        File.WriteAllText(resolved, IniSerializer.SerializeStarSystem(tab.CurrentSystem));
        if (writeUniverse)
        {
            var path = tab.Data.GameData.VFS.Resolve(tab.Data.GameData.Ini.Freelancer.UniversePath);
            File.WriteAllText(path,
                IniSerializer.SerializeUniverse(tab.Data.GameData.Systems, tab.Data.GameData.Bases));
        }

        tab.ObjectsDirty = false;
    }

    public bool ShouldSave => tab.ObjectsDirty || tab.SystemData.IsDirty();

    public void DrawMenuOptions()
    {
        if(Theme.IconMenuItem(Icons.Save, $"Save '{tab.CurrentSystem.Nickname}'",
            tab.ObjectsDirty || tab.SystemData.IsDirty()))
            Save();
        Theme.IconMenuItem(Icons.Save, "Save As", false);
    }
}