using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibreLancer.ContentEdit;
using LibreLancer.GameData.World;
using LibreLancer.ImUI;
using LibreLancer.Ini;

namespace LancerEdit.GameContent;

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
                item.RemoveComponent(dat);
            }
        }
        if(tab.ZoneList.Dirty)
            tab.ZoneList.ApplyZones(tab.CurrentSystem);
        foreach (var o in tab.DeletedObjects)
            tab.CurrentSystem.Objects.Remove(o);
        tab.DeletedObjects = new List<SystemObject>();
        var resolved = tab.Data.GameData.VFS.GetBackingFileName(tab.Data.GameData.Ini.Freelancer.DataPath + "/universe/" + tab.CurrentSystem.SourceFile);
        IniWriter.WriteIniFile(resolved, IniSerializer.SerializeStarSystem(tab.CurrentSystem));
        if (writeUniverse)
        {
            var path = tab.Data.GameData.VFS.GetBackingFileName(tab.Data.GameData.Ini.Freelancer.UniversePath);
            IniWriter.WriteIniFile(path, IniSerializer.SerializeUniverse(tab.Data.GameData.Systems, tab.Data.GameData.Bases));
        }

        tab.ObjectsDirty = false;
    }

    public bool ShouldSave => tab.ObjectsDirty || tab.SystemData.IsDirty() || tab.ZoneList.Dirty;

    public void DrawMenuOptions()
    {
        if(Theme.IconMenuItem(Icons.Save, $"Save '{tab.CurrentSystem.Nickname}'",
            tab.ObjectsDirty || tab.SystemData.IsDirty() || tab.ZoneList.Dirty))
            Save();
        Theme.IconMenuItem(Icons.Save, "Save As", false);
    }
}
