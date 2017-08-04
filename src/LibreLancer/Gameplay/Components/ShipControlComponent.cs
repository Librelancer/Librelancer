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
 * Portions created by the Initial Developer are Copyright (C) 2013-2017
 * the Initial Developer. All Rights Reserved.
 */
using System;
using Jitter.LinearMath;
namespace LibreLancer
{
	[Flags]
	public enum StrafeControls
	{
		None = 0,
		Left = 2,
		Right = 4,
		Up = 8,
		Down = 16
	}
	public class ShipControlComponent : GameComponent
	{
		public float EnginePower = 0f; //from 0 to 1
		//TODO: I forget how this is configured in .ini files. Constants.ini?
		//Some mods have a per-ship (engine?) cruise speed. Check how this is implemented, and include as native feature.
		public float CruiseSpeed = 300f;
		public float StrafeForce = 20000; //TODO: Set this from ship definition (include as component, or set directly at instantiation?)
		public bool ThrustEnabled = false;
		public double ThrustPercent = 1;
		float thrustDrainRate = CalculateDrainRate(150);
		float thrustRecovery = 1 / 6f; //6 seconds to regain thrust? TODO: confirm proper rate
		float drain = 150;
		public float ThrusterDrain
		{
			get
			{
				return drain;
			}
			set
			{
				drain = value;
				thrustDrainRate = CalculateDrainRate(drain);
			}
		}
		public bool CruiseEnabled = false;
		public StrafeControls CurrentStrafe = StrafeControls.None;
		public ShipControlComponent(GameObject parent) : base(parent)
		{
		}

		static float CalculateDrainRate(float x)
		{
			//y = 4953.718 - (189.3364 / 0.03826031) * (1 - e ^ (-0.03826031 * x))
			//Total seconds of thrust, complete bs TODO: come up with real formula
			var y = 4953.718 - (189.3364 / 0.03826031) * (1 - Math.Pow(Math.E, -0.03826031 * x));
			//convert to drain % per second
			return (float)(1 / y);
		}

		public override void FixedUpdate(TimeSpan time)
		{
			var engine = Parent.GetComponent<EngineComponent>(); //Get mounted engine
			if (Parent.PhysicsComponent == null) return;
			if (engine == null) return;
			//Drag = -linearDrag * Velocity
			var drag = -engine.Engine.LinearDrag * Parent.PhysicsComponent.LinearVelocity;
			var engine_force = EnginePower * engine.Engine.MaxForce;
			if (ThrustEnabled)
			{
				engine_force += 72000; //TODO: Load thruster from equipment Mr. Lazy
				ThrustPercent -= thrustDrainRate * time.TotalSeconds;
				ThrustPercent = MathHelper.Clamp(ThrustPercent, 0.0, 1.0);
				if (ThrustPercent == 0)
					ThrustEnabled = false;
			}
			else
			{
				ThrustPercent = MathHelper.Clamp(ThrustPercent + time.TotalSeconds * thrustRecovery, 0.0, 1.0);
			}
			if (CruiseEnabled)
			{ //Cruise has entirely different force calculation
				engine_force = CruiseSpeed * engine.Engine.LinearDrag;
				//Set fx sparam. TODO: This is poorly named
				engine.Speed = 1.0f;
			}
			else
			{
				engine.Speed = EnginePower * 0.9f;
			}

			JVector strafe = JVector.Zero;
			//TODO: Trying to strafe during cruise should drop you out
			if (!CruiseEnabled) //Cannot strafe during cruise
			{
				if ((CurrentStrafe & StrafeControls.Left) == StrafeControls.Left)
				{
					strafe -= JVector.Left; // Subtraction intentional
				}
				else if ((CurrentStrafe & StrafeControls.Right) == StrafeControls.Right)
				{
					strafe -= JVector.Right; // Subtraction intentional
				}
				if ((CurrentStrafe & StrafeControls.Up) == StrafeControls.Up)
				{
					strafe += JVector.Up;
				}
				else if ((CurrentStrafe & StrafeControls.Down) == StrafeControls.Down)
				{
					strafe += JVector.Down;
				}
				if (strafe != JVector.Zero)
				{
					strafe.Normalize();
					strafe = JVector.Transform(strafe, Parent.PhysicsComponent.Orientation);
					//Apply strafe force
					strafe *= StrafeForce;
				}
			}
			var totalForce = (
				drag +
				strafe +
				(JVector.Transform(JVector.Forward, Parent.PhysicsComponent.Orientation) * engine_force)
			);
			if (totalForce.Length() > float.Epsilon)
				Parent.PhysicsComponent.IsActive = true;
			Parent.PhysicsComponent.AddForce(totalForce);
		}
	}
}
