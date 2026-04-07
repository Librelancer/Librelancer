using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.Data.Ini;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent;

public class BaseNpcSaveStrategy(BaseNpcEditorTab tab) : ISaveStrategy
{
    public void Save()
    {
        var mbasesPath = tab.Data.GameData.Items.DataPath("MISSIONS\\mbases.ini");
        if (mbasesPath != null)
        {
            var filePath = tab.Data.GameData.VFS.GetBackingFileName(mbasesPath);
            if (filePath != null)
            {
                IniWriter.WriteIniFile(filePath, IniSerializer.SerializeMBases(tab.Data.GameData.Items.Bases));
                tab.Dirty = false;
                tab.OnSaved();
                return;
            }
        }
        FLLog.Warning("BaseNpcEditor", "Could not find backing file for mbases.ini");
    }

    public void DrawMenuOptions()
    {
        if (Theme.IconMenuItem(Icons.Save, "Save MBases", true))
        {
            Save();
        }
        Theme.IconMenuItem(Icons.Save, "Save As", false);
    }

    public bool ShouldSave => tab.Dirty;
}
