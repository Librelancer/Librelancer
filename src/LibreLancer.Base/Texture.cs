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
        public virtual void Dispose()
        {
			GL.DeleteTexture(ID);
            isDisposed = true;
        }
    }
}

