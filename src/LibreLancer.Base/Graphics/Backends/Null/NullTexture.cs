// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Graphics.Backends.Null
{
    class NullTexture : ITexture {

        public NullTexture(SurfaceFormat format, int levelCount, int estimatedTextureMemory)
        {
            Format = format;
            LevelCount = levelCount;
            EstimatedTextureMemory = estimatedTextureMemory;
        }

        public uint ID;
        public SurfaceFormat Format { get; protected set; }

        public int EstimatedTextureMemory { get; protected set; }
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

        public void BindTo(int unit)
        {
        }


        public virtual void Dispose()
        {
            isDisposed = true;
        }
    }
}

