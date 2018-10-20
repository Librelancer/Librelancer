// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer
{
	public class PIDController
	{
		public double P;
		public double I;
		public double D;

		double integral;
		double lastError;

		public void Reset()
		{
			integral = lastError = 0;
		}

		public double Update(double setpoint, double actual, double timeFrame)
		{
			double present = setpoint - actual;
			integral += present * timeFrame;
			double deriv = (present - lastError) / timeFrame;
			lastError = present;
			return present * P + integral * I + deriv * D;
		}
	}
}
