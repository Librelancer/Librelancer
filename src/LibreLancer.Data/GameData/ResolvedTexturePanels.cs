using System;
using System.Collections.Generic;

namespace LibreLancer.Data.GameData;

public class ResolvedTexturePanels : IDataEquatable<ResolvedTexturePanels>
{
    public required string SourcePath;
    public List<string> TextureShapes = [];
    public Dictionary<string, TextureShape> Shapes = new(StringComparer.OrdinalIgnoreCase);
    public string[]? LibraryFiles;

    public bool DataEquals(ResolvedTexturePanels other) =>
        DataEquality.IdEquals(SourcePath, other.SourcePath);
}
