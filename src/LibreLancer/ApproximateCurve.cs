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

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearRegression;

namespace LibreLancer
{
	public static class ApproximateCurve
	{
		/// <summary>
		/// Build a cubic function from the points (after transforming X to be [0..1]).
		/// WARNING: Probably very slow!
		/// </summary>
		/// <returns>The cubic function.</returns>
		/// <param name="points">Points to build data from</param>
		public static Vector4 GetCubicFunction(Vector2[] points)
		{
			if (points.Length == 0)
				throw new ArgumentException("Can't build function from 0 points");
			if (points.Length == 1)
				return new Vector4(Vector3.Zero, points[0].Y);
			//Transform points array to go from 0 to 1
			Array.Sort(points,(a, b) => a.X.CompareTo(b.X));
			for (int i = 0; i < points.Length; i++)
				points[i].X -= points[0].X;
			var max = points[points.Length - 1].X;
			for (int i = 0; i < points.Length; i++)
				points[i].X /= max;
			//Build function with Math.NET
			double[] x = new double[points.Length];
			double[] y = new double[points.Length];
			for (int i = 0; i < points.Length; i++)
			{
				x[i] = points[i].X;
				y[i] = points[i].Y;
			}
			var result = Polynomial(x, y, 3);
			var polynomial = new Vector4((float)result[3], (float)result[2], (float)result[1], (float)result[0]);
			return polynomial;
		}

		static double[] Polynomial(double[] x, double[] y, int order)
		{
			var design = Matrix<double>.Build.Dense(x.Length, order + 1, (i, j) => Math.Pow(x[i], j));
			return MultipleRegression.Svd(design, Vector<double>.Build.Dense(y)).ToArray();
		}
	}
}

