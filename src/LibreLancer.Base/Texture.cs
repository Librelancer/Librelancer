// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer
{
    public abstract class Texture : IDisposable
    {
        public uint ID;
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

        public abstract void BindTo(int unit);

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

		public override int GetHashCode()
		{
			unchecked
			{
				return (int)ID;
			}
		}

        
        protected static void GetMipSize(int level, int inWidth, int inHeight, out int width, out int height)
        {
            width = inWidth;
            height = inHeight;
            int i = 0;
            while (i < level) {
                width /= 2;
                height /= 2;
                i++;
            }
        }
        
        protected TextureFiltering currentFiltering = TextureFiltering.Linear;
        protected void SetTargetFiltering(int target, TextureFiltering filtering)
        {
            currentFiltering = filtering;
            if (LevelCount > 1)
			{
                if (GLExtensions.Anisotropy && currentFiltering != TextureFiltering.Anisotropic) { 
                    GL.TexParameterf(target, GL.GL_TEXTURE_MAX_ANISOTROPY_EXT, 1);
                }
                switch (filtering)
				{
                    case TextureFiltering.Anisotropic:
                        GL.TexParameteri(target, GL.GL_TEXTURE_MIN_FILTER, GL.GL_LINEAR_MIPMAP_LINEAR);
                        GL.TexParameteri(target, GL.GL_TEXTURE_MAG_FILTER, GL.GL_LINEAR);
                        if(GLExtensions.Anisotropy) {
                            GL.TexParameterf(target, GL.GL_TEXTURE_MAX_ANISOTROPY_EXT, RenderState.Instance.AnisotropyLevel);
                        }
                        break;
					case TextureFiltering.Trilinear:
						GL.TexParameteri(target, GL.GL_TEXTURE_MIN_FILTER, GL.GL_LINEAR_MIPMAP_LINEAR);
						GL.TexParameteri(target, GL.GL_TEXTURE_MAG_FILTER, GL.GL_LINEAR);
						break;
					case TextureFiltering.Bilinear:
						GL.TexParameteri(target, GL.GL_TEXTURE_MIN_FILTER, GL.GL_LINEAR_MIPMAP_NEAREST);
						GL.TexParameteri(target, GL.GL_TEXTURE_MAG_FILTER, GL.GL_LINEAR);
						break;
					case TextureFiltering.Linear:
						GL.TexParameteri(target, GL.GL_TEXTURE_MIN_FILTER, GL.GL_LINEAR);
						GL.TexParameteri(target, GL.GL_TEXTURE_MAG_FILTER, GL.GL_LINEAR);
						break;
					case TextureFiltering.Nearest:
						GL.TexParameteri(target, GL.GL_TEXTURE_MIN_FILTER, GL.GL_NEAREST);
						GL.TexParameteri(target, GL.GL_TEXTURE_MAG_FILTER, GL.GL_NEAREST);
						break;
				}
			}
			else
			{
				switch (filtering)
				{
					case TextureFiltering.Nearest:
						GL.TexParameteri(target, GL.GL_TEXTURE_MIN_FILTER, GL.GL_NEAREST);
						GL.TexParameteri(target, GL.GL_TEXTURE_MAG_FILTER, GL.GL_NEAREST);
						break;
					default:
						GL.TexParameteri(target, GL.GL_TEXTURE_MIN_FILTER, GL.GL_LINEAR);
						GL.TexParameteri(target, GL.GL_TEXTURE_MAG_FILTER, GL.GL_LINEAR);
						break;
				}
			}
        }
        public virtual void Dispose()
        {
			GL.DeleteTexture(ID);
            isDisposed = true;
        }
    }
}

