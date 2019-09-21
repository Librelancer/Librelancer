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
            rigRotate = Quaternion.Identity;
            UpdateRotateTarget(TimeSpan.Zero);
            rigRotate = targetRigRotate;
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

        Quaternion rigRotate = Quaternion.Identity;
        Quaternion targetRigRotate = Quaternion.Identity;
        Quaternion lookAhead = Quaternion.Identity;

        public Vector2 MousePosition;

        long fnum = 0;
        public bool MouseFlight = true;

        void UpdateLookAhead(TimeSpan delta)
        {
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

            lookAhead = DampS(lookAhead, Quaternion.FromEulerAngles(MathHelper.DegreesToRadians(vertical), MathHelper.DegreesToRadians(horizontal), 0), SmoothSpeed, (float)delta.TotalSeconds);
        }

        void UpdateRotateTarget(TimeSpan delta)
        {
            if (MouseFlight)
            {
                var mat = Matrix4.CreateFromQuaternion(rigRotate);
                var transformUp = CalcDir(ref mat, Vector3.Up);
                var orient = ChaseOrientation;
                var shipFwd = CalcDir(ref orient, Vector3.Backward);
                targetRigRotate = Quaternion.LookRotation(shipFwd, transformUp);
            }
            else
                targetRigRotate = ChaseOrientation.ExtractRotation();
        }

        public void Update(TimeSpan delta)
		{
            fnum++;


            UpdateRotateTarget(delta);
            rigRotate = DampS(rigRotate, targetRigRotate, SmoothSpeed, (float)delta.TotalSeconds);
            UpdateLookAhead(delta);

            var rigTransform = Matrix4.CreateFromQuaternion(rigRotate) * Matrix4.CreateTranslation(ChasePosition); //Camera Rig
            var lookAheadTransform = Matrix4.CreateFromQuaternion(lookAhead); //LookAhead Rig
            var camTransform = Matrix4.CreateTranslation(DesiredPositionOffset);

            Vector3 lookAheadPosition = ChasePosition + ChaseOrientation.Transform(Vector3.Forward * 100);
            var lookAheadStack = lookAheadTransform * rigTransform;
            var lookAheadRigUp = CalcDir(ref lookAheadStack, Vector3.Up);

            var transformStack = camTransform * lookAheadTransform * rigTransform;
            var camRotation = Matrix4.CreateFromQuaternion(Quaternion.LookRotation(ChasePosition - lookAheadPosition, lookAheadRigUp));
            var tr = camRotation * Matrix4.CreateTranslation(transformStack.Transform(Vector3.Zero));

            //TODO: Finish with lookahead rig. there's some maths that go crazy there but it's needed to get this to work at all
            //var tr = transformStack;
            var v = tr;
            CameraUp = CalcDir(ref tr, Vector3.Up);
            CameraForward = CalcDir(ref tr, Vector3.Forward);
            Position = v.Transform(Vector3.Zero);
            v.Invert();
            View = v;
            
            _vpdirty = true;
		}

        Vector3 CalcDir(ref Matrix4 mat, Vector3 v)
        {
            var v0 = mat.Transform(Vector3.Zero);
            var v1 = mat.Transform(v);
            return (v1 - v0).Normalized();
        }
        public Vector3 CameraUp;

        public Vector3 CameraForward;
        //Stable way of interpolating quaternions with variable timestep
        static Quaternion DampS(Quaternion a, Quaternion b, float lambda, float dt)
        {
            return Quaternion.Slerp(a, b, 1 - (float)Math.Exp(-lambda * dt));
        }
    }
}