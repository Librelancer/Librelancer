// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using LibreLancer.Data;
using LibreLancer.Graphics;
using LibreLancer.Render;
using LibreLancer.Resources;
using LibreLancer.Utf.Mat;

namespace LibreLancer.Fx
{
    public class ParticleTexture
    {
        public string? Name;
        public Texture2D? Texture;
        public int FrameCount = 1;
        private bool useShape = false;
        private TextureShape shape;
        private TexFrameAnimation? frameAnim;

        public Vector4 GetCoordinates(int frame)
        {
            if (frameAnim != null)
            {
                var f = frameAnim.Frames[frame];
                var x = f.UV1.X;
                var y = (1 - f.UV1.Y);
                var width = f.UV2.X - f.UV1.X;
                var height = (1 - f.UV2.Y) - y;
                return new Vector4(x, y, width, height);
            }

            return new Vector4(0, 0, 1, 1);
        }

        public void Update(string name, ResourceManager res)
        {
            if (string.IsNullOrEmpty(name))
            {
                Name = name;
                Texture = res.NullTexture;
                FrameCount = 1;
            }

            if (Name != name)
            {
                Texture = null;
                frameAnim = null;
                useShape = false;
            }

            Name = name;

            if (Texture is { IsDisposed: false })
            {
                return;
            }

            if (!useShape && frameAnim == null && Texture != null)
            {
                Texture = res.FindTexture(name) as Texture2D;
                shape.Dimensions = new RectangleF(0, 0, 1, 1);
            }
            else if (!useShape && frameAnim == null)
            {
                if (res.TryGetShape(name, out var newShape))
                {
                    shape = newShape!.Value;
                    Texture = (Texture2D?)res.FindTexture(shape.Texture);
                    useShape = true;
                }
                else if (res.TryGetFrameAnimation(name, out frameAnim))
                {
                    Texture = res.FindTexture(name + "_0") as Texture2D;
                    FrameCount = frameAnim!.FrameCount;
                }
                else
                {
                    Texture = res.FindTexture(name) as Texture2D;
                    shape.Dimensions = new RectangleF(0, 0, 1, 1);
                }
            }
            else if (useShape)
            {
                Texture = (Texture2D?)res.FindTexture(shape.Texture);
            }
            else if (frameAnim != null)
            {
                Texture = res.FindTexture(name + "_0") as Texture2D;
            }
        }
    }
}
