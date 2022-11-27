// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Render;

namespace LibreLancer.Render.Cameras
{
    public class MatrixCamera : ICamera
    {
        public Matrix4x4 Matrix;

        public MatrixCamera(Matrix4x4 vp)
        {
            this.Matrix = vp;
        }
        
        Matrix4x4 ICamera.ViewProjection => Matrix4x4.Identity;

        Matrix4x4 ICamera.Projection => Matrix4x4.Identity;

        Matrix4x4 ICamera.View => Matrix4x4.Identity;

        Vector3 ICamera.Position => Vector3.Zero;

        BoundingFrustum ICamera.Frustum => throw new NotImplementedException();

        long ICamera.FrameNumber => 256;
    }
}
