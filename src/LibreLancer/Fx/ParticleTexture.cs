// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using LibreLancer.Utf.Mat;

namespace LibreLancer.Fx
{
    public class ParticleTexture
    {
        private static Vector2[] DefaultCoordinates = {
            new Vector2(0,0),
            new Vector2(1,0),
            new Vector2(0,1),
            new Vector2(1,1)
        };
        
        public string Name;
        public Texture2D Texture;
        public Vector2[] Coordinates;
        public int FrameCount = 1;
        TextureShape shape;
        TexFrameAnimation frameanim;

        public void Update(string name, ResourceManager res)
        {
            if (string.IsNullOrEmpty(name))
            {
                Name = name;
                Texture = res.NullTexture;
                Coordinates = DefaultCoordinates;
                FrameCount = 1;
            }

            if (Name != name) {
                Texture = null;
                frameanim = null;
                shape = null;
            }
            if (shape == null && frameanim == null && Texture != null)
            {
                if (Texture == null || Texture.IsDisposed)
                    Texture = res.FindTexture(name) as Texture2D;
            }
            else if (shape == null)
            {
                if (res.TryGetShape(name, out shape))
                {
                    Texture = (Texture2D) res.FindTexture(shape.Texture);
                    Coordinates = new[] {
                        new Vector2(shape.Dimensions.X, shape.Dimensions.Y),
                        new Vector2(shape.Dimensions.X + shape.Dimensions.Width, shape.Dimensions.Y),
                        new Vector2(shape.Dimensions.X, shape.Dimensions.Y + shape.Dimensions.Height),
                        new Vector2(shape.Dimensions.X + shape.Dimensions.Width, shape.Dimensions.Y + shape.Dimensions.Height)
                    };
                }
                else if (res.TryGetFrameAnimation(name, out frameanim))
                {
                    Texture = res.FindTexture(Texture + "_0") as Texture2D;
                    Coordinates = new Vector2[frameanim.FrameCount * 4];
                    for (int i = 0; i < frameanim.FrameCount; i++)
                    {
                        var j = (i * 4);
                        var rect = frameanim.Frames[i];
                        var uv1 = new Vector2(rect.UV1.X, 1 - rect.UV1.Y);
                        var uv2 = new Vector2(rect.UV2.X, 1 - rect.UV2.Y);
                        Coordinates[j] = uv1;
                        Coordinates[j + 1] = new Vector2(uv2.X, uv1.Y);
                        Coordinates[j + 2] = new Vector2(uv1.X, uv2.Y);
                        Coordinates[j + 3] = uv2;
                    }
                    FrameCount = frameanim.FrameCount;
                }
                else
                {
                    Texture = res.FindTexture(name) as Texture2D;
                    Coordinates = DefaultCoordinates;
                }
            }
        }
    }
}