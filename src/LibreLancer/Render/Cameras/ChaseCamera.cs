// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Data.Schema.Cameras;

namespace LibreLancer.Render.Cameras
{
    //Based on camera rigs from https://github.com/brihernandez/FreelancerFlightExample
    public class ChaseCamera : ICamera
	{
		public Rectangle Viewport
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
		Rectangle _vp;

		public Vector3 ChasePosition { get; set; }
		public Matrix4x4 ChaseOrientation { get; set; }

        public float HorizontalTurnAngle = 15f;
        public float VerticalTurnUpAngle = 5f;
        public float VerticalTurnDownAngle = 5f;
        public float SmoothSpeed = 10f; //Figure out how to translate from FL

        public Vector3 DesiredPositionOffset = new Vector3(0, 4f, 28f);

        //Camera Values
		public Matrix4x4 Projection { get; private set; }
		public Matrix4x4 View { get; private set; }
		Matrix4x4 viewprojection;
		bool _vpdirty = true;
        private BoundingFrustum _frustum;

        public bool FrustumCheck(BoundingSphere sphere)
        {
            if (_vpdirty) UpdateVp();
            return _frustum.Intersects(sphere);
        }

        public bool FrustumCheck(BoundingBox box)
        {
            if (_vpdirty) UpdateVp();
            return _frustum.Intersects(box);
        }


		void UpdateVp()
		{
			viewprojection = View * Projection;
			_frustum = new BoundingFrustum(viewprojection);
			_vpdirty = false;
		}

		public Matrix4x4 ViewProjection
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

        private CameraIni ini;
		public ChaseCamera(Rectangle viewport, CameraIni ini)
		{
            this.ini = ini;
			this.Viewport = viewport;
			ChasePosition = Vector3.Zero;
        }

		public void Reset()
		{
            lookAhead = Quaternion.Identity;
            rigRotate = Quaternion.Identity;
            UpdateRotateTarget(0);
            rigRotate = targetRigRotate;
		}

        public void UpdateProjection()
		{
            var aspect = Viewport.AspectRatio;
            var fovV = FOVUtil.CalcFovx(ini.ThirdPersonCamera.FovX <= 0 ? 70 : ini.ThirdPersonCamera.FovX, aspect);
			Projection = Matrix4x4.CreatePerspectiveFieldOfView(fovV, aspect, 3f, 10000000f);
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

        void UpdateLookAhead(double delta)
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

            lookAhead = DampS(lookAhead, Quaternion.CreateFromYawPitchRoll(MathHelper.DegreesToRadians(horizontal), MathHelper.DegreesToRadians(vertical), 0), SmoothSpeed, (float)delta);
        }

        void UpdateRotateTarget(double delta)
        {
            if (MouseFlight)
            {
                var mat = Matrix4x4.CreateFromQuaternion(rigRotate);
                var transformUp = CalcDir(ref mat, Vector3.UnitY);
                var orient = ChaseOrientation;
                var shipFwd = CalcDir(ref orient, Vector3.UnitZ);
                targetRigRotate = QuaternionEx.LookRotation(shipFwd, transformUp);
            }
            else
                targetRigRotate = ChaseOrientation.ExtractRotation();
        }

        public void Update(double delta)
		{
            fnum++;


            UpdateRotateTarget(delta);
            rigRotate = DampS(rigRotate, targetRigRotate, SmoothSpeed, (float)delta);
            UpdateLookAhead(delta);

            var rigTransform = Matrix4x4.CreateFromQuaternion(rigRotate) * Matrix4x4.CreateTranslation(ChasePosition); //Camera Rig
            var lookAheadTransform = Matrix4x4.CreateFromQuaternion(lookAhead); //LookAhead Rig
            var camTransform = Matrix4x4.CreateTranslation(DesiredPositionOffset);

            Vector3 lookAheadPosition = ChasePosition + Vector3.Transform(-Vector3.UnitZ * 100, ChaseOrientation);
            var lookAheadStack = lookAheadTransform * rigTransform;
            var lookAheadRigUp = CalcDir(ref lookAheadStack, Vector3.UnitY);

            var transformStack = camTransform * lookAheadTransform * rigTransform;
            var camRotation = Matrix4x4.CreateFromQuaternion(QuaternionEx.LookRotation(ChasePosition - lookAheadPosition, lookAheadRigUp));
            var tr = camRotation * Matrix4x4.CreateTranslation(Vector3.Transform(Vector3.Zero, transformStack));

            //TODO: Finish with lookahead rig. there's some maths that go crazy there but it's needed to get this to work at all
            //var tr = transformStack;
            var v = tr;
            CameraUp = CalcDir(ref tr, Vector3.UnitY);
            CameraForward = CalcDir(ref tr, -Vector3.UnitZ);
            Position = Vector3.Transform(Vector3.Zero, tr);
            Matrix4x4.Invert(v, out v);
            View = v;

            _vpdirty = true;
		}

        Vector3 CalcDir(ref Matrix4x4 mat, Vector3 v)
        {
            var v0 = Vector3.Transform(Vector3.Zero, mat);
            var v1 = Vector3.Transform(v, mat);
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
