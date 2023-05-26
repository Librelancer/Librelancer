// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;

namespace LibreLancer.Render.Cameras
{
    public class LookAtCamera : ICamera
    {
        Matrix4x4 view;
        Matrix4x4 projection;
        Matrix4x4 vp;
        Vector3 pos;

        public Vector2 ZRange = new Vector2(0.1f, 300000f);
        public bool GameFOV;
        
        public void Update(float vw, float vh, Vector3 from, Vector3 to, Matrix4x4? rot = null)
        {
            pos = from;

            float fov = GameFOV
                ? FOVUtil.CalcFovx(50, vw / vh)
                : MathHelper.DegreesToRadians(50);
            
            projection = Matrix4x4.CreatePerspectiveFieldOfView(fov, vw / vh, ZRange.X, ZRange.Y);
            var up = Vector3.Transform(Vector3.UnitY, rot ?? Matrix4x4.Identity);
            view = Matrix4x4.CreateLookAt(from, to, up);
            vp = view * projection;
            fn++;
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
                return fn;
            }
            set {
                fn = value;
            }
        }
    }
}
