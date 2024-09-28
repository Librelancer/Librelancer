using System;
using System.Collections.Generic;
using LibreLancer.GameData;

namespace LibreLancer.Render;

public class TexturePanelCollection
{
    public HashSet<string> TextureShapes = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, RenderShape> Shapes = new(StringComparer.OrdinalIgnoreCase);

    public void AddFile(ResolvedTexturePanels pf)
    {
        foreach(var sh in pf.TextureShapes)
            TextureShapes.Add(sh);
        foreach(var kv in pf.Shapes)
            Shapes[kv.Key] = kv.Value;
    }

    public RenderShape GetShape(string shape)
    {
        if(string.IsNullOrEmpty(shape))
            return new(ResourceManager.NullTextureName, new RectangleF(0,0,1,1));
        if (Shapes.TryGetValue(shape, out var ts))
        {
            return ts;
        }
        else if (TextureShapes.Contains(shape))
        {
            return new(shape, new RectangleF(0, 0, 1, 1));
        }
        return new(ResourceManager.NullTextureName, new RectangleF(0, 0, 1, 1));
    }
}
