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
	public enum EngineStates
	{
		Standard,
		CruiseCharging,
		Cruise,
		EngineKill
	}
	public class ShipControlComponent : GameComponent
	{
		public bool Active { get; set; }

		public GameData.Ship Ship;
		public float EnginePower = 0f; //from 0 to 1
		//TODO: I forget how this is configured in .ini files. Constants.ini?
		//Some mods have a per-ship (engine?) cruise speed. Check how this is implemented, and include as native feature.
		public bool ThrustEnabled = false;
		public EngineStates EngineState = EngineStates.Standard;
		public StrafeControls CurrentStrafe = StrafeControls.None;
		public float Pitch; //From -1 to 1
		public float Yaw; //From -1 to 1
		public float Roll; //From -1 to 1
		//I know it's hacky :(
		public float PlayerPitch;
		public float PlayerYaw;

		public ShipControlComponent(GameObject parent) : base(parent)
		{
			Active = true;
		}
		//TODO: Engine Kill
		JVector setInertia = JVector.Zero;
		public override void FixedUpdate(TimeSpan time)
		{
			if (!Active) return;
			//Cancel out whatever the heck Jitter does and put in our own inertia
			//This seems to somewhat work
			if (setInertia != Ship.RotationInertia)
			{
				var inertia = JMatrix.Identity;
				inertia.M11 = Ship.RotationInertia.X;
				inertia.M22 = Ship.RotationInertia.Z;
				inertia.M33 = Ship.RotationInertia.Y;
				Parent.PhysicsComponent.SetMassProperties(inertia, Parent.PhysicsComponent.Mass, false);
				setInertia = Ship.RotationInertia;
			}
			//Don't de-activate
			Parent.PhysicsComponent.IsActive = true;
			Parent.PhysicsComponent.AllowDeactivation = false;
			//Stuff
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
			if (EngineState == EngineStates.Cruise)
			{ //Cruise has entirely different force calculation
				engine_force = Ship.CruiseSpeed * engine.Engine.LinearDrag;
				//Set fx sparam. TODO: This is poorly named
				engine.Speed = 1.0f;
			}
			else
			{
				engine.Speed = EnginePower * 0.9f;
			}

			JVector strafe = JVector.Zero;
			//TODO: Trying to strafe during cruise should drop you out
			if (EngineState != EngineStates.Cruise) //Cannot strafe during cruise
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
					strafe *= Ship.StrafeForce;
				}
			}
			var totalForce = (
				drag +
				strafe +
				(JVector.Transform(JVector.Forward, Parent.PhysicsComponent.Orientation) * engine_force)
			);
			Parent.PhysicsComponent.AddForce(totalForce);
			//add angular drag
			var angularDrag = JVector.Zero;
			Parent.PhysicsComponent.AddTorque(ComponentMultiply(Parent.PhysicsComponent.AngularVelocity * -1, Ship.AngularDrag));
			//steer
			//based on the amazing work of Why485 (https://www.youtube.com/user/Why485)
			var steerControl = new JVector(Math.Abs(PlayerPitch) > 0 ? PlayerPitch : Pitch,
										   Math.Abs(PlayerYaw) > 0 ? PlayerYaw : Yaw,
										   0);
			var angularForce = ComponentMultiply(steerControl, Ship.SteeringTorque);
			//transform torque by direction = unity's AddRelativeTorque
			Parent.PhysicsComponent.AddTorque(JVector.Transform(angularForce, Parent.PhysicsComponent.Orientation));
			//auto-roll?
			if (Math.Abs(steerControl.X) < 0.005f && Math.Abs(steerControl.Y) < 0.005f) //only auto-roll when not steering (probably incorrect)
			{
				var coords = Parent.PhysicsComponent.Orientation.GetEuler();
				//TODO: Fix to work without directly setting orientation
				//TODO: Maybe make this based off the forces?
				var lerped = MathHelper.Lerp((float)coords.Z, 0, (float)((0.009f * 60f) * time.TotalSeconds));
				Parent.PhysicsComponent.Orientation = JMatrix.CreateFromYawPitchRoll((float)coords.Y, (float)coords.X, lerped);
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
