using System;
using System.Runtime.InteropServices;
namespace System.Numerics
{
	/// <summary>
	/// This type can be cast to and from <see cref="T:LibreLancer.Vector4"/>
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct Vector4
	{
		public float X;
		public float Y;
		public float Z;
		public float W;
		public Vector4(float x, float y, float z, float w)
		{
			X = x;
			Y = y;
			Z = z;
			W = w;
		}

		public static implicit operator LibreLancer.Vector4(Vector4 l)
		{
			return new LibreLancer.Vector4(l.X, l.Y, l.Z, l.W);
		}

		public static implicit operator Vector4(LibreLancer.Vector4 l)
		{
			return new Vector4(l.X, l.Y, l.Z, l.W);
		}
	}
}
