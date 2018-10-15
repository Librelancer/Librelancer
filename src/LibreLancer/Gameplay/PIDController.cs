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
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
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
