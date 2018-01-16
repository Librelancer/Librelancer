using System;
using System.Runtime.InteropServices;
namespace System.Numerics
{
	/// <summary>
	/// This type can be cast to and from <see cref="T:LibreLancer.Vector4"/>
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct Vector3
	{
		public float X;
		public float Y;
		public float Z;
		public Vector3(float x, float y, float z)
		{
			X = x;
			Y = y;
			Z = z;
		}

		public static implicit operator LibreLancer.Vector3(Vector3 l)
		{
			return new LibreLancer.Vector3(l.X, l.Y, l.Z);
		}

		public static implicit operator Vector3(LibreLancer.Vector4 l)
		{
			return new Vector3(l.X, l.Y, l.Z);
		}
	}
}
