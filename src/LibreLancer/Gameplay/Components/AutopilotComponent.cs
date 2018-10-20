// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
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
			PitchControl.P = 4;
			YawControl.P = 4;
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
				targetPoint = TargetObject.PhysicsComponent == null ? TargetObject.GetTransform().Transform(Vector3.Zero) : TargetObject.PhysicsComponent.Body.Position;
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
				var hp = docking.GetDockHardpoints(Parent.PhysicsComponent.Body.Position).Skip(lastTargetHp).First();
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
				var d2 = (targetPoint - Parent.PhysicsComponent.Body.Position).Length;
				if (d2 < 80) maxSpeed = 0.3f;
			}
            //Bring ship to within 40 metres of target
            var targetRadius = TargetObject.PhysicsComponent.Body.Collider.Radius;
            var myRadius = Parent.PhysicsComponent.Body.Collider.Radius;
			var distance = (targetPoint - Parent.PhysicsComponent.Body.Position).Length;

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

	}
}
