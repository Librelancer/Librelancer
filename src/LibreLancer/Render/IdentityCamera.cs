using System;
using OpenTK;
namespace LibreLancer
{
	public class IdentityCamera : ICamera
	{
		static IdentityCamera _instance;
		public static IdentityCamera Instance {
			get {
				if (_instance == null)
					_instance = new IdentityCamera ();
				return _instance;
			}
		}
		public Matrix4 ViewProjection {
			get {
				return Matrix4.Identity;
			}
		}
	}
}

