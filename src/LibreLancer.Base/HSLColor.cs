// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer
{
	public struct HSLColor
	{
		public float H;
		public float S;
		public float L;

		public HSLColor(float h, float s, float l)
		{
			H = h;
			S = s;
			L = l;
		}

		public Color3f ToRGB()
		{
			float r, g, b;
			if (S == 0) {
				r = g = b = L;
			} else {
				float t1, t2;
				float th = H / 6.0f;

				if (L < 0.5f)
				{
					t2 = L * (1 + S);
				}
				else
				{
					t2 = (L + S) - (L * S);
				}
				t1 = 2f * L - t2;

				float tr, tg, tb;
				tr = th + (1.0f / 3.0f);
				tg = th;
				tb = th - (1.0f / 3.0f);

				r = ColorCalc(tr, t1, t2);
				g = ColorCalc(tg, t1, t2);
				b = ColorCalc(tb, t1, t2);
			}
			return new Color3f (r, g, b);
		}

		private static float ColorCalc(float c, float t1, float t2)
		{

			if (c < 0) c += 1f;
			if (c > 1) c -= 1f;
			if (6.0f * c < 1.0f) return t1 + (t2 - t1) * 6.0f * c;
			if (2.0f * c < 1.0f) return t2;
			if (3.0f * c < 2.0f) return t1 + (t2 - t1) * (2.0f / 3.0f - c) * 6.0f;
			return t1;
		}

		public static HSLColor FromRGB(Color3f c)
		{
			float r = c.R;
			float g = c.G;
			float b = c.B;

			float min = Math.Min(Math.Min(r, g), b);
			float max = Math.Max(Math.Max(r, g), b);
			float delta = max - min;

			float H = 0;
			float S = 0;
			float L = (float)((max + min) / 2.0f);

			if (delta != 0)
			{
				if (L < 0.5f)
				{
					S = (float)(delta / (max + min));
				}
				else
				{
					S = (float)(delta / (2.0f - max - min));
				}


				if (r == max)
				{
					H = (g - b) / delta;
				}
				else if (g == max)
				{
					H = 2f + (b - r) / delta;
				}
				else if (b == max)
				{
					H = 4f + (r - g) / delta;
				}
			}

			return new HSLColor(H, S, L);
		}
	}
}

