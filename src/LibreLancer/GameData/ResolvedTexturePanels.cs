using System;
using System.Collections.Generic;
using LibreLancer.Render;

namespace LibreLancer.GameData;

public class ResolvedTexturePanels : IDataEquatable<ResolvedTexturePanels>
{
    public string SourcePath;
    public List<string> TextureShapes = new List<string>();
    public Dictionary<string, RenderShape> Shapes = new(StringComparer.OrdinalIgnoreCase);
    public string[] LibraryFiles;

    public void Load(ResourceManager res)
    {
        foreach(var f in LibraryFiles)
            res.LoadResourceFile(f);
    }

    public bool DataEquals(ResolvedTexturePanels other) =>
        DataEquality.IdEquals(SourcePath, other.SourcePath);
}
