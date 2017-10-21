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
		public float ThrusterDrain = 150;
		public bool CruiseEnabled = false;
		public StrafeControls CurrentStrafe = StrafeControls.None;
		public float Pitch; //From -1 to 1
		public float Yaw; //From -1 to 1
		public float Roll; //From -1 to 1
		//FL Handling characteristics
		public JVector AngularDrag = new JVector(40000, 40000, 141000);
		public JVector SteeringTorque = new JVector(50000, 50000, 230000);
		public JVector RotationInertia = new JVector(8400, 8400, 8400);

		public ShipControlComponent(GameObject parent) : base(parent)
		{
		}

		//TODO: Engine Kill
		JVector setInertia = JVector.Zero;
		public override void FixedUpdate(TimeSpan time)
		{
			//Cancel out whatever the heck Jitter does and put in our own inertia
			//This seems to somewhat work
			if (setInertia != RotationInertia)
			{
				var inertia = JMatrix.Identity;
				inertia.M11 = RotationInertia.X;
				inertia.M22 = RotationInertia.Z;
				inertia.M33 = RotationInertia.Y;
				Parent.PhysicsComponent.SetMassProperties(inertia, Parent.PhysicsComponent.Mass, false);
				setInertia = RotationInertia;
			}

			var engine = Parent.GetComponent<EngineComponent>(); //Get mounted engine
			var power = Parent.GetComponent<PowerCoreComponent>();
			if (Parent.PhysicsComponent == null) return;
			if (engine == null) return;
			if (power == null) return;
			//Drag = -linearDrag * Velocity
			var drag = -engine.Engine.LinearDrag * Parent.PhysicsComponent.LinearVelocity;
			var engine_force = EnginePower * engine.Engine.MaxForce;
			power.CurrentThrustCapacity += power.ThrustChargeRate * (float)(time.TotalSeconds);
			power.CurrentThrustCapacity = MathHelper.Clamp(power.CurrentThrustCapacity, 0, power.ThrustCapacity);
			foreach (var thruster in Parent.GetChildComponents<ThrusterComponent>())
			{
				thruster.Enabled = false;
			}
			if (ThrustEnabled)
			{
				foreach (var thruster in Parent.GetChildComponents<ThrusterComponent>())
				{
					engine_force += thruster.Equip.Force;
					thruster.Enabled = true;
					power.CurrentThrustCapacity -= (float)(thruster.Equip.Drain * time.TotalSeconds);
					power.CurrentThrustCapacity = MathHelper.Clamp(power.CurrentThrustCapacity, 0, power.ThrustCapacity);
					if (power.CurrentThrustCapacity == 0) ThrustEnabled = false;
				}
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
			//add angular drag
			var angularDrag = JVector.Zero;
			Parent.PhysicsComponent.AddTorque(ComponentMultiply(Parent.PhysicsComponent.AngularVelocity * -1, AngularDrag));
			//steer
			//based on the amazing work of Why485 (https://www.youtube.com/user/Why485)
			var angularForce = ComponentMultiply(new JVector(Pitch, Yaw, 0), SteeringTorque);
			//transform torque by direction = unity's AddRelativeTorque
			Parent.PhysicsComponent.AddTorque(JVector.Transform(angularForce, Parent.PhysicsComponent.Orientation));
			//auto-roll?
			if (Pitch == 0 && Yaw == 0) //only auto-roll when not steering (probably incorrect)
			{
				var coords = Parent.PhysicsComponent.Orientation.GetEuler();
				//TODO: Fix to work without directly setting orientation
				/*float rollForce = 0;
				if (currRoll > float.Epsilon)
				{
					rollForce = 0.3f;
				}
				else if (currRoll < -float.Epsilon)
				{
					rollForce = -0.3f;
				}
				var correctionForce = JVector.Transform(new JVector(0, 0, SteeringTorque.Z) * rollForce, Parent.PhysicsComponent.Orientation);
				Parent.PhysicsComponent.AddTorque(correctionForce);*/
				//TODO: Maybe make this based off the forces?
				var lerped = MathHelper.Lerp((float)coords.Z, 0, 0.007f);
				Parent.PhysicsComponent.Orientation = JMatrix.CreateFromYawPitchRoll((float)coords.X, (float)coords.Y, lerped);
			}
		}


		static JVector ComponentMultiply(JVector a, JVector b)
		{
			return new JVector(
				a.X * b.X,
				a.Y * b.Y,
				a.Z * b.Z);
		}
	}
}
