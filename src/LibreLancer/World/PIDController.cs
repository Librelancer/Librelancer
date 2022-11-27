// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.World
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
            if (double.IsNaN(integral) || double.IsNaN(lastError)) Reset();
			double present = setpoint - actual;
			integral += present * timeFrame;
			double deriv = (present - lastError) / timeFrame;
			lastError = present;
			return present * P + integral * I + deriv * D;
		}
	}
}
