// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer;
namespace LibreLancer
{
    public class LookAtCamera : ICamera
    {
        Matrix4x4 view;
        Matrix4x4 projection;
        Matrix4x4 vp;
        Vector3 pos;
        public void Update(float vw, float vh, Vector3 from, Vector3 to, Matrix4x4? rot = null)
        {
            pos = from;
            projection = Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(50), vw / vh, 0.1f, 300000);
            var up = Vector3.Transform(Vector3.UnitY, rot ?? Matrix4x4.Identity);
            view = Matrix4x4.CreateLookAt(from, to, up);
            vp = view * projection;
        }
        public Matrix4x4 ViewProjection {
            get {
                return vp;
            }
        }

        public Matrix4x4 Projection {
            get {
                return projection;
            }
        }

        public Matrix4x4 View {
            get {
                return view;
            }
        }

        public Vector3 Position {
            get {
                return pos;
            }
        }

        public BoundingFrustum Frustum {
            get {
                return new BoundingFrustum(vp);
            }
        }

        long fn = 0;
        public long FrameNumber {
            get {
                return fn++;
            }
            set {
                fn = value;
            }
        }
    }
}
