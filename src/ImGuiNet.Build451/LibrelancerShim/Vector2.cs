using System;
using System.Runtime.InteropServices;
namespace System.Numerics
{
	/// <summary>
	/// This type can be cast to and from <see cref="T:LibreLancer.Vector2"/>
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct Vector2
	{
		public static readonly Vector2 Zero = new Vector2(0, 0);

		public float X;
		public float Y;
		public Vector2(float x, float y)
		{
			X = x;
			Y = y;
		}

		public static implicit operator LibreLancer.Vector2(Vector2 l)
		{
			return new LibreLancer.Vector2(l.X, l.Y);
		}

		public static implicit operator Vector2(LibreLancer.Vector2 l)
		{
			return new Vector2(l.X, l.Y);
		}
		public override string ToString()
		{
			return string.Format("[X: {0}, Y: {1}]", X, Y);
		}
	}
}
