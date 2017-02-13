/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2017
 * the Initial Developer. All Rights Reserved.
 */
using System;
namespace LibreLancer
{
	public class ThnCamera : ICamera
	{
		long frameNo = 0;

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
			projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(fovy), Transform.AspectRatio, 10f, 100000000f);
			Vector3 originalTarget = VectorMath.Forward;
			Vector3 rotatedTarget = Transform.Orientation.Transform(originalTarget);
			Vector3 target = Transform.LookAt == null ? Position + rotatedTarget : Transform.LookAt.Transform.ExtractTranslation();
			Vector3 upVector = Transform.Orientation.Transform(VectorMath.Up);
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
