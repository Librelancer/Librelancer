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
using System.Collections.Generic;
using Jitter;
using Jitter.LinearMath;
using Jitter.Dynamics;
using Jitter.Dynamics.Constraints;

namespace LibreLancer
{
	public class PlayerController
	{
		RigidBody p;
		public JVector TargetVelocity { get; set; }
		public PlayerController(RigidBody player)
		{
			p = player;
		}

	
		JVector deltaVelocity;
		public void Iterate()
		{
			
			deltaVelocity = TargetVelocity - p.LinearVelocity;
			deltaVelocity.Y = 0.0f;
			Console.WriteLine ("X: {0}, Y: {1}, Z: {2}", TargetVelocity.X, TargetVelocity.Y, TargetVelocity.Z);
			// determine how 'stiff' the character follows the target velocity
			//deltaVelocity *= 0.02f;

			if (deltaVelocity.LengthSquared () != 0.0f) {
				// activate it, in case it fall asleep :)
				p.IsActive = true;
				p.ApplyImpulse (deltaVelocity * p.Mass);
			}

		}
	}
}

