// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer
{
	public class ThnCamera : ICamera
	{
		public long frameNo = 0;

		public ThnCameraTransform Transform = new ThnCameraTransform();

		Matrix4 view;
		Matrix4 projection;
		Matrix4 viewProjection;
		BoundingFrustum frustum;
		Viewport viewport;

		public ThnCamera(Viewport vp)
		{
			viewport = vp;
			Update();
		}

		public void Update()
		{
			var fovy = Transform.FovH * Transform.AspectRatio;
			//TODO: Tweak clip plane some more - isn't quite right
			//NOTE: near clip plane can't be too small or it causes z-fighting
			projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(fovy), Transform.AspectRatio, Transform.Znear, Transform.Zfar);
			Vector3 originalTarget = Vector3.Forward;
			Vector3 rotatedTarget = Transform.Orientation.Transform(originalTarget);
            Vector3 target = Transform.LookAt == null ? Position + rotatedTarget : Transform.LookAt();
			Vector3 upVector = Transform.Orientation.Transform(Vector3.Up);
			view = Matrix4.LookAt(Position, target, upVector);
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

		public Matrix4 Projection
		{
			get
			{
				return projection;
			}
		}

		public Matrix4 View
		{
			get
			{
				return view;
			}
		}

		public Matrix4 ViewProjection
		{
			get
			{
				return viewProjection;
			}
		}
	}
}
