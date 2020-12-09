// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer
{
	public class ThnCamera : ICamera
	{
		public long frameNo = 0;
        public ThnObject Object;
		public ThnCameraTransform Transform = new ThnCameraTransform();

		Matrix4x4 view;
		Matrix4x4 projection;
        private Matrix4x4 ogProjection;
		Matrix4x4 viewProjection;
		BoundingFrustum frustum;
		Viewport viewport;

		public ThnCamera(Viewport vp)
		{
			viewport = vp;
			Update();
		}

        public void DefaultZ()
        {
            var fovv = FovVRad(Transform.FovH, Transform.AspectRatio);
            projection = Matrix4x4.CreatePerspectiveFieldOfView(fovv, Transform.AspectRatio,
                2.5f, 1000000f);
            viewProjection = view * projection;
            frameNo++;
        }
        public void CameraZ()
        {
            projection = ogProjection;
            viewProjection = view * projection;
            frameNo++;
        }

        static float FovVRad(float fovhdeg, float aspect)
        {
            //fovh is multiplied 2 before being converted to fovy for the projection matrix
            var fovh = MathHelper.DegreesToRadians(2 * fovhdeg);
            return (float) (2 * Math.Atan(Math.Tan(fovh / 2) * 1 / aspect));
        }
		public void Update()
        {
            var fovv = FovVRad(Transform.FovH, Transform.AspectRatio);
            //TODO: Tweak clip plane some more - isn't quite right
			//NOTE: near clip plane can't be too small or it causes z-fighting
			projection = Matrix4x4.CreatePerspectiveFieldOfView(fovv, Transform.AspectRatio, Transform.Znear, Transform.Zfar);
            ogProjection = projection;
			Vector3 originalTarget = -Vector3.UnitZ;
            Vector3 rotatedTarget = Vector3.Transform(originalTarget, Transform.Orientation);
            Vector3 target = Transform.LookAt == null ? Position + rotatedTarget : Transform.LookAt();
			Vector3 upVector = Transform.LookAt == null ? Vector3.Transform(Vector3.UnitY, Transform.Orientation) : Vector3.UnitY;
			view = Matrix4x4.CreateLookAt(Position, target, upVector);
			frameNo++;
			viewProjection = view * projection;
			frustum = new BoundingFrustum(viewProjection);
		}

		public long FrameNumber
		{
			get
			{
				return frameNo;
			}
		}

		public BoundingFrustum Frustum
		{
			get
			{
				return frustum;
			}
		}

		public Vector3 Position
		{
			get
			{
				return Transform.Position;
			}
		}

		public Matrix4x4 Projection
		{
			get
			{
				return projection;
			}
		}

		public Matrix4x4 View
		{
			get
			{
				return view;
			}
		}

		public Matrix4x4 ViewProjection
		{
			get
			{
				return viewProjection;
			}
		}
	}
}
