using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.Data.GameData;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Universe;
using LibreLancer.ImUI;
using StarSystem = LibreLancer.Data.GameData.World.StarSystem;
using SystemObject = LibreLancer.Data.GameData.World.SystemObject;

namespace LancerEdit.GameContent;

public class StarSystemSaveStrategy : ISaveStrategy
{
    private SystemEditorTab tab;
    public StarSystemSaveStrategy(SystemEditorTab tab) => this.tab = tab;


    Task<ShortestPathIni> LoadShortestPathsAsync(string path) => Task.Run(() =>
        ShortestPathWriter.LoadShortestPathsSorted(path, tab.Data.GameData.VFS));

    Task WriteShortestPaths() => Task.Run(async () =>
    {
        var fileIllegal = tab.Data.ShortestPathRoot + "shortest_illegal_path.ini";
        var fileLegal = tab.Data.ShortestPathRoot + "shortest_legal_path.ini";
        var fileAll = tab.Data.ShortestPathRoot + "systems_shortest_path.ini";

        var illegal = LoadShortestPathsAsync(fileIllegal);
        var legal = LoadShortestPathsAsync(fileLegal);
        var all = LoadShortestPathsAsync(fileAll);
        ShortestPaths.CalculateShortestPaths(tab.Data.GameData.Items);

        var allNew = new ShortestPathIni();
        var legalNew = new ShortestPathIni();
        var illegalNew = new ShortestPathIni();

        void AddPath(ShortestPathIni ini, StarSystem src, Dictionary<StarSystem, List<StarSystem>> paths)
        {
            if (paths.Count <= 0) return;
            var con = new SystemConnections();
            foreach (var kv in paths.OrderBy(x => x.Key.Nickname))
            {
                con.Entries.Add(new ShortestPathEntry(src.Nickname, kv.Key.Nickname,
                    kv.Value.Select(x => x.Nickname).ToArray()));
            }
            ini.SystemConnections.Add(con);
        }

        foreach (var s in tab.Data.GameData.Items.Systems.OrderBy(x => x.Nickname))
        {
            AddPath(allNew, s, s.ShortestPathsAny);
            AddPath(legalNew, s, s.ShortestPathsLegal);
            AddPath(illegalNew, s, s.ShortestPathsIllegal);
        }

        if (!ShortestPathWriter.PathInisEqual(allNew, await all))
        {
            var resolved = tab.Data.GameData.VFS.GetBackingFileName(fileAll);
            IniWriter.WriteIniFile(resolved, ShortestPathWriter.Serialize(allNew));
            FLLog.Info("Ini", $"Saved to {resolved}");
        }

        if (!ShortestPathWriter.PathInisEqual(legalNew, await legal))
        {
            var resolved = tab.Data.GameData.VFS.GetBackingFileName(fileLegal);
            IniWriter.WriteIniFile(resolved, ShortestPathWriter.Serialize(legalNew));
            FLLog.Info("Ini", $"Saved to {resolved}");
        }

        if (!ShortestPathWriter.PathInisEqual(illegalNew, await illegal))
        {
            var resolved = tab.Data.GameData.VFS.GetBackingFileName(fileIllegal);
            IniWriter.WriteIniFile(resolved, ShortestPathWriter.Serialize(illegalNew));
            FLLog.Info("Ini", $"Saved to {resolved}");
        }
    });

    public void Save()
    {
        bool writeUniverse = tab.IsUniverseDirty();

        tab.ObjectsList.SaveAndApply(tab.CurrentSystem);
        tab.LightsList.SaveAndApply(tab.CurrentSystem);
        tab.ZoneList.SaveAndApply(tab.CurrentSystem, tab.Data.GameData);

        tab.CurrentSystem.CopyTo(tab.OriginalSystem);
        tab.ResetOriginalObjects();
        var paths = WriteShortestPaths();

        var resolved = tab.Data.GameData.VFS.GetBackingFileName(tab.Data.UniverseVfsFolder + tab.CurrentSystem.SourceFile);
        IniWriter.WriteIniFile(resolved, IniSerializer.SerializeStarSystem(tab.CurrentSystem));
        FLLog.Info("Ini", $"Saved to {resolved}");
        if (writeUniverse)
        {
            var path = tab.Data.GameData.VFS.GetBackingFileName(tab.Data.GameData.Items.Ini.Freelancer.UniversePath);
            IniWriter.WriteIniFile(path, IniSerializer.SerializeUniverse(tab.Data.GameData.Items.Systems, tab.Data.GameData.Items.Bases));
            FLLog.Info("Ini", $"Saved to {path}");
        }
        paths.Wait();
        tab.OnSaved();
    }

    public bool ShouldSave =>
        tab.ObjectsList.Dirty || tab.IsSystemDirty || tab.ZoneList.Dirty || tab.LightsList.Dirty;

    public void DrawMenuOptions()
    {
        if(Theme.IconMenuItem(Icons.Save, $"Save '{tab.CurrentSystem.Nickname}'", true))
            Save();
        Theme.IconMenuItem(Icons.Save, "Save As", false);
    }
}
