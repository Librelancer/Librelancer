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
using System.Collections.Generic;
namespace LibreLancer
{
	public class DockComponent : GameComponent
	{
		public DockAction Action;
		public string DockHardpoint;
		public string DockAnimation;
		public int TriggerRadius;

		public DockComponent(GameObject parent) : base(parent)
		{
		}

		public IEnumerable<Hardpoint> GetDockHardpoints()
		{
			if (Action.Kind != DockKinds.Tradelane)
			{
				var hpname = DockHardpoint.Replace("DockMount", "DockPoint");
				yield return Parent.GetHardpoint(hpname + "02");
				yield return Parent.GetHardpoint(hpname + "01");
			}
			yield return Parent.GetHardpoint(DockHardpoint);
		}

		public bool TryTriggerAnimation(GameObject obj)
		{
			var rad = RadiusFromBoundingBox(obj.PhysicsComponent.Shape.BoundingBox);
			foreach (var hps in GetDockHardpoints())
			{
				var targetPos = (hps.Transform * Parent.GetTransform()).Transform(Vector3.Zero);
				var dist = (targetPos - obj.PhysicsComponent.Position).Length;
				if (dist < 20 + rad)
				{
					TriggerAnimation();
					return true;
				}
			}
			return false;
		}

		public bool RequestDock(GameObject obj)
		{
			var can = CanDock(obj);
			if (can && Action.Kind == DockKinds.Tradelane)
			{
				var control = obj.GetComponent<ShipControlComponent>();
				control.Active = false;
				var movement = new TradelaneMoveComponent(obj, Parent, DockHardpoint);
				obj.Components.Add(movement);
			}
			return can;
		}

		public bool CanDock(GameObject obj)
		{
			var hp = Parent.GetHardpoint(DockHardpoint);
			var targetPos = (hp.Transform * Parent.GetTransform()).Transform(Vector3.Zero);
			if ((targetPos - obj.PhysicsComponent.Position).Length < (TriggerRadius + RadiusFromBoundingBox(obj.PhysicsComponent.Shape.BoundingBox)))
			{
				return true;
			}
			return false;
		}

		void TriggerAnimation()
		{
			var component = Parent.GetComponent<AnimationComponent>();
			if (component == null) return;
			if (!component.HasAnimation(DockAnimation)) return;
			component.StartAnimation(DockAnimation, false);
		}

		static float RadiusFromBoundingBox(LibreLancer.Jitter.LinearMath.JBBox box)
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

		public override void Update(TimeSpan time)
		{
		}
	}
}
