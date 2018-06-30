using System;
using LibreLancer;
namespace LancerEdit
{
    public class LookAtCamera : ICamera
    {
        Matrix4 view;
        Matrix4 projection;
        Matrix4 vp;
        Vector3 pos;
        public void Update(float vw, float vh, Vector3 from, Vector3 to, Matrix4? rot = null)
        {
            pos = from;
            projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(50), vw / vh, 0.1f, 300000);
            view = Matrix4.LookAt(from, to, -Vector3.Up) * (rot ?? Matrix4.Identity);
            vp = view * projection;
        }
        public Matrix4 ViewProjection {
            get {
                return vp;
            }
        }

        public Matrix4 Projection {
            get {
                return projection;
            }
        }

        public Matrix4 View {
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
        }
    }
}
