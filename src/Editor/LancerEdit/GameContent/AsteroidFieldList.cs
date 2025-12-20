using System;
using System.Collections.Generic;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.Data;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Ini;

namespace LancerEdit.GameContent;

public class AsteroidFieldList
{
    public List<AsteroidField> Fields = new List<AsteroidField>();
    public Dictionary<AsteroidField, AsteroidField> OriginalFields = new Dictionary<AsteroidField, AsteroidField>();

    public void SetFields(List<AsteroidField> source, Dictionary<string, Zone> newZones)
    {
        Fields = new List<AsteroidField>(source.Count);
        OriginalFields = new Dictionary<AsteroidField, AsteroidField>(source.Count);
        foreach (var f in source)
        {
            var cloned = f.Clone(newZones);
            Fields.Add(cloned);
            OriginalFields[cloned] = f;
        }
    }

    public bool CheckDirty()
    {
        foreach (var f in Fields)
        {
            if (!DataEquality.ObjectEquals(f, OriginalFields[f]))
            {
                return true;
            }
        }
        return false;
    }

    public void SaveAndApply(StarSystem system, GameDataManager gameData)
    {
        system.AsteroidFields = new List<AsteroidField>();
        foreach (var f in Fields)
        {
            if (!DataEquality.ObjectEquals(f, OriginalFields[f]))
            {
                var sections = IniSerializer.SerializeAsteroidField(f);
                var filename = gameData.VFS.GetBackingFileName(gameData.Items.DataPath(f.SourceFile));
                IniWriter.WriteIniFile(filename, sections);
                FLLog.Info("Ini", $"Saved to {filename}");
            }
            var cloned = f.Clone(system.ZoneDict);
            OriginalFields[f] = cloned;
            system.AsteroidFields.Add(cloned);
        }
    }

}
