// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer
{
	public static class Easings
	{
		/// <summary>
		/// Equation for a circular (sqrt(1-t^2)) easing
		/// </summary>
		public static readonly Easing Circular = new Easing(CircEaseIn, CircEaseOut);

		static double CircEaseIn( double t, double b, double c, double d )
		{
			return -c * ( Math.Sqrt( 1 - ( t /= d ) * t ) - 1 ) + b;
		}
		static double CircEaseOut( double t, double b, double c, double d )
		{
			return c * Math.Sqrt( 1 - ( t = t / d - 1 ) * t ) + b;
		}
	}
	public class Easing
	{
		/// <summary>
		/// An easing function
		/// </summary>
		/// <param name="t">Current time in seconds.</param>
		/// <param name="b">Starting value.</param>
		/// <param name="c">Change in value.</param>
		/// <param name="d">Duration of animation.</param>
		/// <returns>The correct value.</returns>
		public delegate double EaseFunction(double t, double b, double c, double d);
		public EaseFunction EaseIn;
		public EaseFunction EaseOut;
		public Easing(EaseFunction ein, EaseFunction eout)
		{
			EaseIn = ein;
			EaseOut = eout;
		}
	}
}

