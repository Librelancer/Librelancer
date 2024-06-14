// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using LibreLancer.Render;

namespace LibreLancer.Thn
{
	public class ThnCamera : ICamera
	{
		public long frameNo = 0;

        public ThnObject Object = new ThnObject()
        {
            Camera = new ThnCameraProps(),
            Translate = Vector3.Zero,
            Rotate = Matrix4x4.Identity,
            Name = "DEFAULT_OBJECT_UNINITED"
        };

		Matrix4x4 view;
		Matrix4x4 projection;
        private Matrix4x4 ogProjection;
		Matrix4x4 viewProjection;
		BoundingFrustum frustum;
		Rectangle viewport;

		public ThnCamera(Rectangle vp)
		{
			viewport = vp;
			Update();
		}

        public void SetViewport(Rectangle vp)
        {
            viewport = vp;
        }

        public void DefaultZ()
        {
            CalcCameraProps(out float fovv, out float aspectRatio);
            projection = Matrix4x4.CreatePerspectiveFieldOfView(fovv, aspectRatio,
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

        public bool FrustumCheck(BoundingSphere sphere) => frustum.Intersects(sphere);
        public bool FrustumCheck(BoundingBox box) => frustum.Intersects(box);



        void CalcCameraProps(out float fovV, out float aspectRatio)
        {
            float screen_ratio = (float) viewport.Width / (float) viewport.Height;
            int hvaspect = (int) (Object.Camera.AspectRatio * 100);
            float ratio = Object.Camera.AspectRatio;
            float fovh = Object.Camera.FovH;
            if (hvaspect == 133) {
                ratio = screen_ratio;
                fovh = MathHelper.RadiansToDegrees(FOVUtil.CalcFovx(fovh, screen_ratio));
            } else if (hvaspect == 185) {
                ratio = (screen_ratio * 1.39f); //cinematic ratio (1.85 / 1.33)
                fovh =  MathHelper.RadiansToDegrees(FOVUtil.CalcFovx(fovh, screen_ratio));
            }
            fovV = FOVUtil.FovVRad(fovh, ratio);
            aspectRatio = ratio;
        }

		public void Update()
        {
            CalcCameraProps(out float fovv, out float aspectRatio);
            //TODO: Tweak clip plane some more - isn't quite right
			//NOTE: near clip plane can't be too small or it causes z-fighting
			projection = Matrix4x4.CreatePerspectiveFieldOfView(fovv, aspectRatio, Object.Camera.Znear, Object.Camera.Zfar);
            ogProjection = projection;
            var transform = Object.Rotate * Matrix4x4.CreateTranslation(Object.Translate);
            Matrix4x4.Invert(transform, out view);
			viewProjection = view * projection;
			frustum = new BoundingFrustum(viewProjection);
		}

		public Vector3 Position
		{
			get
			{
				return Object.Translate;
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
