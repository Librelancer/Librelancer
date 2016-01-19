using System;

namespace LibreLancer
{
	public struct Color3f
	{
		public float R;
		public float G;
		public float B;
		public Color3f(float r, float g, float b)
		{
			R = r;
			G = g;
			B = b;
		}
		public override string ToString ()
		{
			return string.Format ("[R:{0}, G:{1}, B:{2}]", R, G, B);
		}
	}
}

