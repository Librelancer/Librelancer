// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Numerics;

namespace LibreLancer
{
	public class TradelaneMoveComponent : GameComponent
	{
		GameObject currenttradelane;
		string lane;
		public TradelaneMoveComponent(GameObject parent, GameObject tradelane, string lane) : base(parent)
		{
			currenttradelane = tradelane;
			this.lane = lane;
		}

		public override void FixedUpdate(double time)
		{
			var cmp = currenttradelane.GetComponent<CDockComponent>();
			var tgt = Parent.GetWorld().GetObject(lane == "HpRightLane" ? cmp.Action.Target : cmp.Action.TargetLeft);
			if (tgt == null)
			{
				var ctrl = Parent.GetComponent<ShipPhysicsComponent>();
				ctrl.EnginePower = 0.4f;
				ctrl.Active = true;
				Parent.Components.Remove(this);
				Parent.World.BroadcastMessage(Parent, GameMessageKind.ManeuverFinished);
				return;
			}
			var eng = Parent.GetComponent<CEngineComponent>();
			if (eng != null) eng.Speed = 0.9f;

			var tgtcmp = tgt.GetComponent<CDockComponent>();
            var targetPoint = Vector3.Transform(Vector3.Zero, tgt.GetHardpoint(lane).Transform * tgt.WorldTransform);
			var direction = targetPoint - Parent.PhysicsComponent.Body.Position;
			var distance = direction.Length();
			if (distance < 200)
			{
				currenttradelane = tgt;
				return;
			}
			direction.Normalize();
			Parent.PhysicsComponent.Body.LinearVelocity = direction * 2500;

			//var currRot = Quaternion.FromMatrix(Parent.PhysicsComponent.Body.Transform.ClearTranslation());
			var targetRot = QuaternionEx.LookAt(Parent.PhysicsComponent.Body.Position, targetPoint);
            //var slerped = Quaternion.Slerp(currRot, targetRot, 0.02f); //TODO: Slerp doesn't work?
            Parent.PhysicsComponent.Body.SetTransform(Matrix4x4.CreateFromQuaternion(targetRot) *
                                                      Matrix4x4.CreateTranslation(Parent.PhysicsComponent.Body.Position));
		}

	}
}
