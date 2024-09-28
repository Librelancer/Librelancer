// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Render
{
    public record struct RenderShape(string Texture, RectangleF Dimensions)
    {
        public static readonly RenderShape Empty = new RenderShape(ResourceManager.NullTextureName, new RectangleF(0, 0, 1, 1));
    }
}

