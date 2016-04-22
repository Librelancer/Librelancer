using System;
using OpenTK;
namespace LibreLancer
{
	public class ChaseCamera : ICamera
	{
		public Vector3 ChasePosition;
		public Vector3 ChaseDirection;
		public Vector3 Up;
		public Vector3 DesiredPositionOffset = new Vector3(0, 25f, 100f);
		public Vector3 LookAtOffset = new Vector3(0, 0, 0);
		public Vector3 DesiredPosition {
			get {
				UpdateWorldPositions ();
				return desiredPosition;
			}
		}
		private Vector3 desiredPosition;
		public Vector3 LookAt {
			get {
				UpdateWorldPositions ();

				return lookAt;
			}
		}
		private Vector3 lookAt;
		#region Camera physics (typically set when creating camera)

		/// <summary>
		/// Physics coefficient which controls the influence of the camera's position
		/// over the spring force. The stiffer the spring, the closer it will stay to
		/// the chased object.
		/// </summary>
		public float Stiffness
		{
			get { return stiffness; }
			set { stiffness = value; }
		}
		private float stiffness = 1800.0f;

		/// <summary>
		/// Physics coefficient which approximates internal friction of the spring.
		/// Sufficient damping will prevent the spring from oscillating infinitely.
		/// </summary>
		public float Damping
		{
			get { return damping; }
			set { damping = value; }
		}
		private float damping = 600.0f;

		/// <summary>
		/// Mass of the camera body. Heaver objects require stiffer springs with less
		/// damping to move at the same rate as lighter objects.
		/// </summary>
		public float Mass
		{
			get { return mass; }
			set { mass = value; }
		}
		private float mass = 50.0f;

		#endregion

		#region Perspective properties

		/// <summary>
		/// Perspective aspect ratio. Default value should be overriden by application.
		/// </summary>
		public float AspectRatio
		{
			get { return aspectRatio; }
			set { aspectRatio = value; }
		}
		private float aspectRatio = 4.0f / 3.0f;

		/// <summary>
		/// Perspective field of view.
		/// </summary>
		public float FieldOfView
		{
			get { return fieldOfView; }
			set { fieldOfView = value; }
		}
		private float fieldOfView = MathHelper.DegreesToRadians(45.0f);

		/// <summary>
		/// Distance to the near clipping plane.
		/// </summary>
		public float NearPlaneDistance
		{
			get { return nearPlaneDistance; }
			set { nearPlaneDistance = value; }
		}
		private float nearPlaneDistance = 1.0f;

		/// <summary>
		/// Distance to the far clipping plane.
		/// </summary>
		public float FarPlaneDistance
		{
			get { return farPlaneDistance; }
			set { farPlaneDistance = value; }
		}
		private float farPlaneDistance = 100000.0f;

		#endregion
		public ChaseCamera ()
		{
		}

		public void Reset()
		{
			UpdateWorldPositions();

			// Stop motion
			velocity = Vector3.Zero;

			// Force desired position
			position = desiredPosition;

			UpdateMatrices();
		}
		public BoundingFrustum Frustum {
			get {
				return frustum;
			}
		}
		BoundingFrustum frustum;
		public void Update(TimeSpan delta)
		{
			UpdateWorldPositions ();
			float elapsed = (float)delta.TotalSeconds;

			// Calculate spring force
			Vector3 stretch = position - desiredPosition;
			Vector3 force = -stiffness * stretch - damping * velocity;

			// Apply acceleration
			Vector3 acceleration = force / mass;
			velocity += acceleration * elapsed;

			// Apply velocity
			position += velocity * elapsed;

			UpdateMatrices();
		}
		/// <summary>
		/// Rebuilds object space values in world space. Invoke before publicly
		/// returning or privately accessing world space values.
		/// </summary>
		private void UpdateWorldPositions()
		{
			// Construct a matrix to transform from object space to worldspace
			Matrix4 transform = Matrix4.Identity;
			transform.SetForward (ChaseDirection);
			transform.SetUp(Up);
			transform.SetRight (Vector3.Cross (Up, ChaseDirection));

			// Calculate desired camera properties in world space
			desiredPosition = ChasePosition +
				Vector3.TransformNormal(DesiredPositionOffset, transform);
			lookAt = ChasePosition +
				Vector3.TransformNormal(LookAtOffset, transform);
		}

		/// <summary>
		/// Rebuilds camera's view and projection matricies.
		/// </summary>
		private void UpdateMatrices()
		{
			view = Matrix4.LookAt(this.Position, this.LookAt, this.Up);
			projection = Matrix4.CreatePerspectiveFieldOfView(FieldOfView,
				AspectRatio, NearPlaneDistance, FarPlaneDistance);
			vp = view * projection;
			frustum = new BoundingFrustum (vp);
		}

		Matrix4 vp;
		public Matrix4 ViewProjection {
			get {
				return vp;
			}
		}

		Matrix4 projection;
		public Matrix4 Projection {
			get {
				return projection;
			}
		}
		Matrix4 view;
		public Matrix4 View {
			get {
				return view;
			}
		}

		Vector3 position;
		public Vector3 Position {
			get {
				return position;
			}
		}

		public Vector3 Velocity
		{
			get { return velocity; }
		}
		private Vector3 velocity;
	}
}

