// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Graphics
{
    static class SurfaceFormatExtensions
    {
        internal static int GetSize(this SurfaceFormat format)
        {
            switch (format)
            {
                case SurfaceFormat.Dxt1:
                case SurfaceFormat.Rgtc1:
                    return 8;
                case SurfaceFormat.Dxt3:
                case SurfaceFormat.Dxt5:
                case SurfaceFormat.Rgtc2:
                    return 16;
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}

