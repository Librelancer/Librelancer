// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer
{
    //Based on camera rigs from https://github.com/brihernandez/FreelancerFlightExample
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

        public float HorizontalTurnAngle = 15f;
        public float VerticalTurnUpAngle = 5f;
        public float VerticalTurnDownAngle = 5f;
        public float SmoothSpeed = 10f; //Figure out how to translate from FL

        public Vector3 DesiredPositionOffset = new Vector3(0, 4f, 28f);

        //Camera Values
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
            lookAhead = Quaternion.Identity;
            rigRotate = ChaseOrientation.ExtractRotation();
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

        Quaternion rigRotate = Quaternion.Identity;
        Quaternion lookAhead = Quaternion.Identity;

        public Vector2 MousePosition;

        long fnum = 0;
        public bool MouseFlight = true;
		public void Update(TimeSpan delta)
		{
            fnum++;

            // Normalize screen positions so that the range is -1 to 1. Makes the math easier.
            var mouseScreenX = (MousePosition.X - (Viewport.Width * 0.5f)) / (Viewport.Width * 0.5f);
            var mouseScreenY = -(MousePosition.Y - (Viewport.Height * 0.5f)) / (Viewport.Height * 0.5f);

            // Clamp these screen position to make sure the rig doesn't oversteer.
            mouseScreenX = MathHelper.Clamp(mouseScreenX, -1f, 1f);
            mouseScreenY = MathHelper.Clamp(mouseScreenY, -1f, 1f);

            if (!MouseFlight) mouseScreenX = mouseScreenY = 0;

            float horizontal = 0f;
            float vertical = 0f;
            horizontal = HorizontalTurnAngle * mouseScreenX;
            vertical = (mouseScreenY < 0.0f) ? VerticalTurnUpAngle * mouseScreenY : VerticalTurnDownAngle * mouseScreenY;
           
            lookAhead = DampS(lookAhead, Quaternion.FromEulerAngles(MathHelper.DegreesToRadians(-vertical), MathHelper.DegreesToRadians(-horizontal), 0), SmoothSpeed, (float)delta.TotalSeconds);
            rigRotate = DampS(rigRotate, ChaseOrientation.ExtractRotation(), SmoothSpeed, (float)delta.TotalSeconds);


            var lookAheadPosition = ChaseOrientation.Transform(Vector3.Forward) * 100;

            var rigTransform = Matrix4.CreateFromQuaternion(rigRotate) * Matrix4.CreateTranslation(ChasePosition);
            var lookAheadTransform = Matrix4.CreateFromQuaternion(lookAhead) * Matrix4.CreateTranslation(DesiredPositionOffset);

            var tr = lookAheadTransform * rigTransform;
            var lookAheadRigPos = tr.Transform(Vector3.Zero);

            var lookAtTr = Matrix4.CreateFromQuaternion(Quaternion.LookAt(lookAheadRigPos, lookAheadPosition));

            
            var v = tr;
            Position = v.Transform(Vector3.Zero);
            v.Invert();
            View = v;
            
            _vpdirty = true;
		}

        //Stable way of interpolating quaternions with variable timestep
        static Quaternion DampS(Quaternion a, Quaternion b, float lambda, float dt)
        {
            return Quaternion.Slerp(a, b, 1 - (float)Math.Exp(-lambda * dt));
        }
    }
}