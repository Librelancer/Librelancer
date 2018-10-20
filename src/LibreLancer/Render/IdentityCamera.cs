// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer
{
	public class IdentityCamera : ICamera
	{
		static IdentityCamera _instance;
		public static IdentityCamera Instance {
			get {
				return _instance;
			}
		}

		FreelancerGame game;
		public IdentityCamera(FreelancerGame game)
		{
			this.game = game;
			_instance = this;
		}

        long _fn = 0;
        public long FrameNumber
        {
            get
            {
                return _fn++;
            }
        }

		public Matrix4 ViewProjection {
			get {
				float screenAspect = game.Width / (float)game.Height;
				float uiAspect = 4f / 3f;
				if (screenAspect > uiAspect)
					return Matrix4.CreateScale(uiAspect / screenAspect, 1, 1);

				return Matrix4.Identity;
			}
		}

		public Vector2 ScreenToPixel(float screenx, float screeny)
		{
			float scaleX = 1;
			float scaleY = 1;
			float screenAspect = game.Width / (float)game.Height;
			float uiAspect = 4f / 3f;
			if (screenAspect > uiAspect)
				scaleX = uiAspect / screenAspect;

			float distx = screenx * (game.Width / 2) * scaleX;
			float x = (game.Width / 2) + distx;

			float disty = screeny * (game.Height / 2);
			float y = (game.Height / 2) - disty;

			return new Vector2(x, y * scaleY);
		}

		public Matrix4 View {
			get {
				return Matrix4.Identity;
			}
		}
		public Matrix4 Projection {
			get {
				return Matrix4.Identity;
			}
		}
		public Vector3 Position {
			get {
				return Vector3.Zero;
			}
		}
		public BoundingFrustum Frustum {
			get {
				return new BoundingFrustum (Matrix4.Identity);
			}
		}
	}
}

