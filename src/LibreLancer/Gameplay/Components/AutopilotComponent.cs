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
using System.Linq;
using LibreLancer.Jitter.LinearMath;
namespace LibreLancer
{
	public enum AutopilotBehaviours
	{
		None,
		Goto,
		Dock
	}
	public class AutopilotComponent : GameComponent
	{
		public AutopilotBehaviours CurrentBehaviour = AutopilotBehaviours.None;
		public PIDController PitchControl = new PIDController();
		public PIDController YawControl = new PIDController();

		public event Action<DockAction> DockComplete;
		public GameObject TargetObject;

		public AutopilotComponent(GameObject parent) : base(parent)
		{
			PitchControl.P = 5;
			YawControl.P = 5;
		}

		bool hasTriggeredAnimation = false;
		int lastTargetHp = 0;
		void ResetDockState()
		{
			hasTriggeredAnimation = false;
			lastTargetHp = 0;
		}

		public override void FixedUpdate(TimeSpan time)
		{
			var control = Parent.GetComponent<ShipControlComponent>();
			if (control == null) return;
			control.Pitch = control.Yaw = 0;
			if (CurrentBehaviour == AutopilotBehaviours.None)
			{
				ResetDockState();
				return;
			}
			Vector3 targetPoint = Vector3.Zero;
			float radius = -1;
			float maxSpeed = 1f;
			if (CurrentBehaviour == AutopilotBehaviours.Goto)
			{
				targetPoint = TargetObject.PhysicsComponent == null ? TargetObject.GetTransform().Transform(Vector3.Zero) : TargetObject.PhysicsComponent.Position;
				ResetDockState();
			}
			else
			{
				var docking = TargetObject.GetComponent<DockComponent>();
				if (docking == null)
				{
					CurrentBehaviour = AutopilotBehaviours.None;
					ResetDockState();
					Parent.World.BroadcastMessage(Parent, GameMessageKind.ManeuverFinished);
					return;
				}
				var hp = docking.GetDockHardpoints(Parent.PhysicsComponent.Position).Skip(lastTargetHp).First();
				radius = 5;
				targetPoint = (hp.Transform * TargetObject.GetTransform()).Transform(Vector3.Zero);
				if (lastTargetHp > 0) maxSpeed = 0.3f;
				if (lastTargetHp == 2) radius = docking.TriggerRadius;
				if (!hasTriggeredAnimation && docking.TryTriggerAnimation(Parent)) hasTriggeredAnimation = true;
				if (docking.RequestDock(Parent))
				{
					ResetDockState();
					DockComplete(docking.Action);
				}
				var d2 = (targetPoint - Parent.PhysicsComponent.Position).Length;
				if (d2 < 80) maxSpeed = 0.3f;
			}
			//Bring ship to within 40 metres of target
			var targetRadius = RadiusFromBoundingBox(TargetObject.PhysicsComponent.Shape.BoundingBox);
			var myRadius = RadiusFromBoundingBox(Parent.PhysicsComponent.Shape.BoundingBox);
			var distance = (targetPoint - Parent.PhysicsComponent.Position).Length;

			var distrad = radius < 0 ? (targetRadius + myRadius + 40) : radius + myRadius;
			bool distanceSatisfied =  distrad >= distance;
			if (distanceSatisfied)
				control.EnginePower = 0;
			else
				control.EnginePower = maxSpeed;

			//Orientation
			var dt = time.TotalSeconds;
			var vec = Parent.InverseTransformPoint(targetPoint);
			//normalize it
			vec.Normalize();
			//
			bool directionSatisfied = (Math.Abs(vec.X) < 0.0015f && Math.Abs(vec.Y) < 0.0015f);
			if (!directionSatisfied)
			{
				control.Yaw = MathHelper.Clamp((float)YawControl.Update(0, vec.X, dt), -1, 1);
				control.Pitch = MathHelper.Clamp((float)PitchControl.Update(0, -vec.Y, dt), -1, 1);
			}
			else
			{
				control.Yaw = 0;
				control.Pitch = 0;
			}
			if (distanceSatisfied && directionSatisfied && CurrentBehaviour == AutopilotBehaviours.Goto)
			{
				Parent.World.BroadcastMessage(Parent, GameMessageKind.ManeuverFinished);
				CurrentBehaviour = AutopilotBehaviours.None;
			}
			if (distanceSatisfied && directionSatisfied && CurrentBehaviour == AutopilotBehaviours.Dock)
			{
				if (lastTargetHp < 2) lastTargetHp++;
				else
					control.EnginePower = maxSpeed;
			}
		}

		static float RadiusFromBoundingBox(JBBox box)
		{
			float radius = 0;
			radius = Math.Max(Math.Abs(box.Max.X), radius);
			radius = Math.Max(Math.Abs(box.Max.Y), radius);
			radius = Math.Max(Math.Abs(box.Max.Z), radius);
			radius = Math.Max(Math.Abs(box.Min.X), radius);
			radius = Math.Max(Math.Abs(box.Min.Y), radius);
			radius = Math.Max(Math.Abs(box.Min.Z), radius);
			return radius;
		}
	}
}
