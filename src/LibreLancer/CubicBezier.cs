/* This file has been ported from JavaScript to C#
 * https://github.com/gre/bezier-easing
 * BezierEasing - use bezier curve for transition easing function
 * by Gaëtan Renaudeau 2014 - 2015 - MIT License
 */
using System;

namespace LibreLancer
{
	public delegate double BezierFunction(double x);
	public static class CubicBezier
	{
		// These values are established by empiricism with tests (tradeoff: performance VS precision)
		const int NEWTON_ITERATIONS = 4;
		const double NEWTON_MIN_SLOPE = 0.001;
		const double SUBDIVISION_PRECISION = 0.0000001;
		const int SUBDIVISION_MAX_ITERATIONS = 10;

		const int kSplineTableSize = 11;
		const double kSampleStepSize = 1.0 / (kSplineTableSize - 1.0);

		static double A (double aA1, double aA2)
		{
			return 1.0 - 3.0 * aA2 + 3.0 * aA1;
		}
		static double B (double aA1, double aA2)
		{
			return 3.0 * aA2 - 6.0 * aA1;
		}
		static double C (double aA1)
		{
			return 3.0 * aA1;
		}
		// Returns x(t) given t, x1, and x2, or y(t) given t, y1 and y2
		static double CalcBezier(double aT, double aA1, double aA2)
		{
			return ((A(aA1, aA2) * aT + B(aA1, aA2)) * aT + C(aA1)) * aT;
		}

		// Returns dx/dt given t, x1, and x2, or dy/dt given t, y1, and y2.
		static double GetSlope(double aT, double aA1, double aA2)
		{
			return 3.0 * A(aA1, aA2) * aT * aT + 2.0 * B(aA1, aA2) * aT + C(aA1);
		}

		static double BinarySubdivide (double aX, double aA, double aB, double mX1, double mX2)
		{
			double currentX, currentT = 0;
			int i = 0;

			do {
				currentT = aA + (aB - aA) / 2.0;
				currentX = CalcBezier(currentT, mX1, mX2) - aX;
				if (currentX > 0.0) {
					aB = currentT;
				} else {
					aA = currentT;
				}
			} while (Math.Abs(currentX) > SUBDIVISION_PRECISION && ++i < SUBDIVISION_MAX_ITERATIONS);
			return currentT;
		}

		static double NewtonRaphsonIterate(double aX, double aGuessT, double mX1, double mX2)
		{
			for (int i = 0; i < NEWTON_ITERATIONS; ++i) {
				var currentSlope = GetSlope(aGuessT, mX1, mX2);
				if (currentSlope == 0.0) {
					return aGuessT;
				}
				var currentX = CalcBezier(aGuessT, mX1, mX2) - aX;
				aGuessT -= currentX / currentSlope;
			}
			return aGuessT;
		}

		class BezierVariables
		{
			public double[] sampleValues;
			public double mX1;
			public double mX2;
			public double getTForX (double aX) 
			{
				var intervalStart = 0.0;
				var currentSample = 1;
				var lastSample = kSplineTableSize - 1;

				for (; currentSample != lastSample && sampleValues[currentSample] <= aX; ++currentSample) {
					intervalStart += kSampleStepSize;
				}
				--currentSample;

				// Interpolate to provide an initial guess for t
				var dist = (aX - sampleValues[currentSample]) / (sampleValues[currentSample + 1] - sampleValues[currentSample]);
				var guessForT = intervalStart + dist * kSampleStepSize;

				var initialSlope = GetSlope(guessForT, mX1, mX2);
				if (initialSlope >= NEWTON_MIN_SLOPE) {
					return NewtonRaphsonIterate(aX, guessForT, mX1, mX2);
				} else if (initialSlope == 0.0) {
					return guessForT;
				} else {
					return BinarySubdivide(aX, intervalStart, intervalStart + kSampleStepSize, mX1, mX2);
				}
			}
		}
		public static BezierFunction Bezier(double mX1, double mY1, double mX2, double mY2)
		{
			if (!(0 <= mX1 && mX1 <= 1 && 0 <= mX2 && mX2 <= 1)) {
				throw new Exception("bezier x values must be in [0, 1] range");
			}
			var vars = new BezierVariables ();
			vars.mX1 = mX1;
			vars.mX2 = mX2;
	
			//Precompute samples table
			vars.sampleValues = new double[kSplineTableSize];
			if (mX1 != mY1 || mX2 != mY2) {
				for (int i = 0; i < kSplineTableSize; i++) {
					vars.sampleValues[i] = CalcBezier(i * kSampleStepSize, mX1, mX2);
				}
			}

			return delegate(double x) {
				if(mX1 == mY1 && mX2 == mY2) {
					return x; //linear
				}
				// Because JavaScript (C#) number are imprecise, we should guarantee the extremes are right.
				if(x == 0)
					return 0;
				if (x == 1)
					return 1;
				return CalcBezier(vars.getTForX(x), mY1, mY2);
			};
		}
	}
}

