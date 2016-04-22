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
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
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

