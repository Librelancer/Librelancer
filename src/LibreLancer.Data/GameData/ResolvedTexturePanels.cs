using System;
using System.Collections.Generic;

namespace LibreLancer.Data.GameData;

public class ResolvedTexturePanels : IDataEquatable<ResolvedTexturePanels>
{
    public string SourcePath;
    public List<string> TextureShapes = new List<string>();
    public Dictionary<string, TextureShape> Shapes = new(StringComparer.OrdinalIgnoreCase);
    public string[] LibraryFiles;

    public bool DataEquals(ResolvedTexturePanels other) =>
        DataEquality.IdEquals(SourcePath, other.SourcePath);
}
