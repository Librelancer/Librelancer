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

		public Matrix4 Projection { get; private set; }
		public Matrix4 View { get; private set; }
		Matrix4 viewprojection;
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
		public Matrix4 ViewProjection {
			get {
				if (_vpdirty) {
					UpdateVp ();
				}
				return viewprojection;
			}
		}
		//public Plane ReflectionPlane { get; set; }
		//public Vector3 ReflectionPosition { get; private set; }
		//public Matrix ReflectionView { get; private set; }

		public bool Free { get; set; }

		public DebugCamera(Viewport viewport)
		{
			this.Viewport = viewport;
			Free = false;
			//idk this makes it work
		}

		public void UpdateProjection()
		{
			Projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(35f), Viewport.AspectRatio, 3f, 250000f);
		}

		/// <summary>
		/// Allows the game component to update itself.
		/// </summary>
		public void Update(TimeSpan delta)
		{
			if (Free)
			{
				Matrix4 rotationMatrix = Matrix4.CreateRotationX(Rotation.Y) * Matrix4.CreateRotationY(Rotation.X);

				//Vector3 rotatedVector = VectorMath.Transform(MoveVector, rotationMatrix);
				var rotatedVector = rotationMatrix.Transform(MoveVector);;
				Position += (float)(delta.TotalSeconds * MoveSpeed) * rotatedVector;

				Vector3 originalTarget = VectorMath.Forward;
				Vector3 rotatedTarget = rotationMatrix.Transform(originalTarget);
				Vector3 target = Position + rotatedTarget;

				Vector3 upVector = rotationMatrix.Transform(VectorMath.Up);

				var v = Matrix4.LookAt(Position, target, upVector);
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

					if (direction.Length >= MoveSpeed)
					{
						direction.Normalize();
						currentTarget += direction * MoveSpeed;
					}
					else currentTarget = selectedTarget;
				}

				Matrix4 rotationMatrix = Matrix4.CreateRotationX(Rotation.Y) * Matrix4.CreateRotationY(Rotation.X);

				Vector3 position = new Vector3(0, 0, Zoom);
				position = rotationMatrix.Transform(position);
				Position = currentTarget + position;

				Vector3 upVector = rotationMatrix.Transform (VectorMath.Up);

				View = Matrix4.LookAt(Position, currentTarget, upVector);
				_vpdirty = true;
			}

			// Reflection
			/*Matrix reflectionMatrix = Matrix.CreateReflection(ReflectionPlane);
            ReflectionPosition = VectorMath.Transform(Position, reflectionMatrix);
            Vector3 rtar = VectorMath.Transform(target, reflectionMatrix);
            Vector3 rup = Vector3.Cross(VectorMath.Transform(Vector3.Right, rotationMatrix), rtar - ReflectionPosition);

            ReflectionView = Matrix.CreateLookAt(ReflectionPosition, rtar, rup);*/
		}
	}
}