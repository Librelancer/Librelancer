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
 * The Original Code is RenderTools code (http://flapi.sourceforge.net/).
 * 
 * The Initial Developer of the Original Code is Malte Rupprecht (mailto:rupprema@googlemail.com).
 * Portions created by the Initial Developer are Copyright (C) 2011, 2012
 * the Initial Developer. All Rights Reserved.
 */
using System;
namespace LibreLancer
{
	public class ChaseCamera : ICamera
	{
		public Viewport Viewport
		{
			get
			{
				return _vp;
			}
			set
			{
				_vp = value;
				UpdateProjection();
			}
		}
		Viewport _vp;

		public Vector3 ChasePosition { get; set; }
		public Matrix4 ChaseOrientation { get; set; }
		public Vector3 DesiredPositionOffset = new Vector3(0, 4f, 28f);
		public Vector3 OffsetDirection;

		public Matrix4 Projection { get; private set; }
		public Matrix4 View { get; private set; }
		Matrix4 viewprojection;
		bool _vpdirty = true;
		public BoundingFrustum _frustum = null;
		public BoundingFrustum Frustum
		{
			get
			{
				if (_frustum == null)
				{
					UpdateVp();
				}
				return _frustum;
			}
		}
		void UpdateVp()
		{
			viewprojection = View * Projection;
			_frustum = new BoundingFrustum(viewprojection);

			_vpdirty = false;
		}
		public Matrix4 ViewProjection
		{
			get
			{
				if (_vpdirty)
				{
					UpdateVp();
				}
				return viewprojection;
			}
		}
		Vector3 _position;
		public Vector3 Position
		{
			get
			{
				return _position;
			}
			set
			{
				_position = value;
			}
		}

		public ChaseCamera(Viewport viewport)
		{
			this.Viewport = viewport;
			ChaseOrientation = Matrix4.Identity;
			OffsetDirection = DesiredPositionOffset.Normalized();
		}

		public void UpdateProjection()
		{
			Projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(50f), Viewport.AspectRatio, 10f, 100000000f);
		}
        public long FrameNumber
        {
            get
            {
                return fnum;
            }
        }
        long fnum = 0;
		/// <summary>
		/// Allows the game component to update itself.
		/// </summary>
		public void Update(TimeSpan delta)
		{
            fnum++;
			Matrix4 rotationMatrix = ChaseOrientation;

			_position = ChasePosition + (rotationMatrix.Transform(DesiredPositionOffset));

			Vector3 upVector = rotationMatrix.Transform(VectorMath.Up);

			View = Matrix4.LookAt(Position, ChasePosition, upVector);
			_vpdirty = true;
		}
	}
}