// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Graphics
{
    public enum SurfaceFormat
    {
        Bgra8,
        Bgr565,
        Bgra5551,
        Bgra4444,
        Dxt1,
        Dxt3,
        Dxt5,
        Rgtc1,
        Rgtc2,
        NormalizedByte2,
        NormalizedByte4,
        Rgba1010102,
        Rg32,
        Rgba64,
        //Alpha8, - Removed in OpenGL 3.1,
		R8, //Just red channel - NOT an XNA value
		Depth, //Depth texture - NOT an XNA value
        Single,
        Vector2,
        Vector4,
        HalfSingle,
        HalfVector2,
        HalfVector4,
        HdrBlendable
    }
}

