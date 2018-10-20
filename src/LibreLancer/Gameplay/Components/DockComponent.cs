// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

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

		string tlHP;
		public DockComponent(GameObject parent) : base(parent)
		{
		}

		public IEnumerable<Hardpoint> GetDockHardpoints(Vector3 position)
		{
			if (Action.Kind != DockKinds.Tradelane)
			{
				var hpname = DockHardpoint.Replace("DockMount", "DockPoint");
				yield return Parent.GetHardpoint(hpname + "02");
				yield return Parent.GetHardpoint(hpname + "01");
				yield return Parent.GetHardpoint(DockHardpoint);
			}
			else if (Action.Kind == DockKinds.Tradelane)
			{
				var heading = position - Parent.PhysicsComponent.Body.Position;
                var fwd = Parent.PhysicsComponent.Body.Transform.GetForward();
				var dot = Vector3.Dot(heading, fwd);
				if (dot > 0)
				{
					tlHP = "HpLeftLane";
					yield return Parent.GetHardpoint("HpLeftLane");
				}
				else
				{
					tlHP = "HpRightLane";
					yield return Parent.GetHardpoint("HpRightLane");
				}
			}
		}

		public bool TryTriggerAnimation(GameObject obj)
		{
            var rad = obj.PhysicsComponent.Body.Collider.Radius;
			foreach (var hps in GetDockHardpoints(obj.PhysicsComponent.Body.Position))
			{
				var targetPos = (hps.Transform * Parent.GetTransform()).Transform(Vector3.Zero);
				var dist = (targetPos - obj.PhysicsComponent.Body.Position).Length;
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
				var movement = new TradelaneMoveComponent(obj, Parent, tlHP);
				obj.Components.Add(movement);
			}
			return can;
		}

		public bool CanDock(GameObject obj)
		{
			var hp = Parent.GetHardpoint(tlHP ?? DockHardpoint);
			var targetPos = (hp.Transform * Parent.GetTransform()).Transform(Vector3.Zero);
			if ((targetPos - obj.PhysicsComponent.Body.Position).Length < (TriggerRadius * 2 + obj.PhysicsComponent.Body.Collider.Radius))
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

		public override void Update(TimeSpan time)
		{
		}
	}
}
