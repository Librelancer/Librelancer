// MIT License - Copyright (c) 2011, 2012 Malte Rupprecht
//             - Copyright (c) 2013- Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
namespace LibreLancer
{
	public class DebugCamera : ICamera
	{
		public float MoveSpeed = 3000f;
		public Viewport Viewport { 
			get { 
				return _vp; 
			}  set {
				_vp = value;
				UpdateProjection(); 
			}
		}
		Viewport _vp;

		private Vector3 currentTarget, selectedTarget;
		public Vector3 Target
		{
			set { selectedTarget = value; }
		}

		public Vector2 Rotation { get; set; }
		public float Zoom { get; set; }

		public Vector3 Position { get; set; }
		public Vector3 MoveVector { get; set; }

		public Matrix4x4 Projection { get; private set; }
		public Matrix4x4 View { get; private set; }
		Matrix4x4 viewprojection;
		bool _vpdirty = true;
		public BoundingFrustum _frustum = null;
		public BoundingFrustum Frustum {
			get {
				if (_frustum == null) {
					UpdateVp ();
				}
				return _frustum;
			}
		}
		void UpdateVp()
		{
			viewprojection = View * Projection;
			_frustum = new BoundingFrustum (viewprojection);

			_vpdirty = false;
		}
		public Matrix4x4 ViewProjection {
			get {
				if (_vpdirty) {
					UpdateVp ();
				}
				return viewprojection;
			}
		}

        public bool Free { get; set; }

		public DebugCamera(Viewport viewport)
		{
			this.Viewport = viewport;
			Free = false;
			//idk this makes it work
		}

		public void UpdateProjection()
		{
            const float defaultFOV = 50;

            Projection = Matrix4x4.CreatePerspectiveFieldOfView(FOVUtil.CalcFovx(defaultFOV, Viewport.AspectRatio), Viewport.AspectRatio, 3f, 10000000f);
            _vpdirty = true;
        }

        public long FrameNumber
        {
            get { return fnum; }
        }

        long fnum;

		public void Update(TimeSpan delta)
		{
            fnum++;
            if (Free)
			{
				Matrix4x4 rotationMatrix = Matrix4x4.CreateRotationX(Rotation.Y) * Matrix4x4.CreateRotationY(Rotation.X);

                var rotatedVector = Vector3.Transform(MoveVector, rotationMatrix);
				Position += (float)(delta.TotalSeconds * MoveSpeed) * rotatedVector;

				Vector3 originalTarget = -Vector3.UnitZ;
                Vector3 rotatedTarget = Vector3.Transform(originalTarget, rotationMatrix);
				Vector3 target = Position + rotatedTarget;

                Vector3 upVector = Vector3.Transform(Vector3.UnitY, rotationMatrix);

				var v = Matrix4x4.CreateLookAt(Position, target, upVector);
				if (View != v) {
					View = v;
					_vpdirty = true;
				}
				MoveVector = Vector3.Zero;
			}
			else
			{
				if (currentTarget != selectedTarget)
				{
					Vector3 direction = selectedTarget - currentTarget;

					if (direction.Length() >= MoveSpeed)
					{
						direction.Normalize();
						currentTarget += direction * MoveSpeed;
					}
					else currentTarget = selectedTarget;
				}

				Matrix4x4 rotationMatrix = Matrix4x4.CreateRotationX(Rotation.Y) * Matrix4x4.CreateRotationY(Rotation.X);

				Vector3 position = new Vector3(0, 0, Zoom);
                position = Vector3.Transform(position, rotationMatrix);
				Position = currentTarget + position;

                Vector3 upVector = Vector3.Transform(Vector3.UnitY, rotationMatrix);

				View = Matrix4x4.CreateLookAt(Position, currentTarget, upVector);
				_vpdirty = true;
			}
        }
	}
}