using System;
using OpenTK.Graphics.OpenGL;
namespace LibreLancer
{
    public abstract class Texture : IDisposable
    {
        internal int ID;
        public SurfaceFormat Format { get; protected set; }
        static bool compressedChecked = false;
        protected static void CheckCompressed()
        {
            if (!compressedChecked)
            {
                GLExtensions.CheckExtensions();
                compressedChecked = true;
            }
        }
        public int LevelCount
        {
            get;
            protected set;
        }
        bool isDisposed = false;
        public bool IsDisposed
        {
            get
            {
                return isDisposed;
            }
        }
        internal abstract void Bind();
        public void BindTo(TextureUnit unit)
        {
            GL.ActiveTexture(unit);
            Bind();
        }
        internal static int CalculateMipLevels(int width, int height = 0)
        {
            int levels = 1;
            int size = Math.Max(width, height);
            while (size > 1)
            {
                size = size / 2;
                levels++;
            }
            return levels;
        }
        public virtual void Dispose()
        {
            isDisposed = true;
        }
    }
}

