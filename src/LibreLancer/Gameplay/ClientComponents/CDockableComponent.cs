// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Linq;
using System.Collections.Generic;
namespace LibreLancer
{
	public class CDockComponent : GameComponent
	{
		public DockAction Action;
		public string DockHardpoint;
		public string DockAnimation;
		public int TriggerRadius;

		string tlHP;
		public CDockComponent(GameObject parent) : base(parent)
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

        public bool TryDockTL(GameObject obj, string hardpoint)
        {
            var hp = Parent.GetHardpoint(hardpoint);
            var targetPos = Vector3.Transform(Vector3.Zero, hp.Transform * Parent.WorldTransform);
            if ((targetPos - obj.PhysicsComponent.Body.Position).Length() < (TriggerRadius * 2 + obj.PhysicsComponent.Body.Collider.Radius))
            {
                var control = obj.GetComponent<ShipPhysicsComponent>();
                control.Active = false;
                var movement = new TradelaneMoveComponent(obj, Parent, hardpoint);
                obj.Components.Add(movement);
                return true;
            }
            return false;
        }

        public override void Update(double time)
		{
		}
	}
}
