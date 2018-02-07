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

		public override void FixedUpdate(TimeSpan time)
		{
			var cmp = currenttradelane.GetComponent<DockComponent>();
			var tgt = Parent.GetWorld().GetObject(lane == "HpRightLane" ? cmp.Action.Target : cmp.Action.TargetLeft);
			if (tgt == null)
			{
				var ctrl = Parent.GetComponent<ShipControlComponent>();
				ctrl.EnginePower = 0.4f;
				ctrl.Active = true;
				Parent.Components.Remove(this);
				Parent.World.BroadcastMessage(Parent, GameMessageKind.ManeuverFinished);
				return;
			}
			var eng = Parent.GetComponent<EngineComponent>();
			if (eng != null) eng.Speed = 0.9f;

			var tgtcmp = tgt.GetComponent<DockComponent>();
			var targetPoint = (tgt.GetHardpoint(lane).Transform * tgt.GetTransform()).Transform(Vector3.Zero);
			var direction = targetPoint - Parent.PhysicsComponent.Body.Position;
			var distance = direction.Length;
			if (distance < 200)
			{
				currenttradelane = tgt;
				return;
			}
			direction.Normalize();
			Parent.PhysicsComponent.Body.LinearVelocity = direction * 2500;

			//var currRot = Quaternion.FromMatrix(Parent.PhysicsComponent.Body.Transform.ClearTranslation());
			var targetRot = Quaternion.LookAt(Parent.PhysicsComponent.Body.Position, targetPoint);
            //var slerped = Quaternion.Slerp(currRot, targetRot, 0.02f); //TODO: Slerp doesn't work?
            Parent.PhysicsComponent.Body.SetTransform(new Matrix4(Matrix3.CreateFromQuaternion(targetRot)) *
                                                      Matrix4.CreateTranslation(Parent.PhysicsComponent.Body.Position));
		}

	}
}
