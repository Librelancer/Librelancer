// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and confiditons defined in
// LICENSE, which is part of this source code package

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
			ChasePosition = Vector3.Zero;
		}

		public void Reset()
		{
			UpdateWanted();
			Position = desiredPosition;
			Vector3 upVector = ChaseOrientation.Transform(Vector3.Up);
			View = Matrix4.LookAt(Position, lookAt, upVector);
		}

		void UpdateWanted()
		{
			desiredPosition = ChasePosition + ChaseOrientation.Transform(DesiredPositionOffset);
		}


       
		public void UpdateProjection()
		{
            const float defaultFOV = 50;

			Projection = Matrix4.CreatePerspectiveFieldOfView(FOVUtil.CalcFovx(defaultFOV, Viewport.AspectRatio), Viewport.AspectRatio, 3f, 10000000f);
		}

        public void UpdateFrameNumber(long f)
        {
            fnum = f;
        }
        public long FrameNumber
        {
            get
            {
                return fnum;
            }
        }
		Vector3 lookAt = Vector3.Zero;
		Vector3 desiredPosition;
        long fnum = 0;
		/// <summary>
		/// Allows the game component to update itself.
		/// </summary>
		public void Update(TimeSpan delta)
		{
            fnum++;

			UpdateWanted();
            Position = desiredPosition;
            View = Matrix4.CreateTranslation(-Position) * ChaseOrientation.Inverted();
			_vpdirty = true;
		}
	}
}