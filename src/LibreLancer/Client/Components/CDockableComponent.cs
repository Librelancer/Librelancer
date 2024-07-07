// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using System.Numerics;
using LibreLancer.GameData.World;
using LibreLancer.World;

namespace LibreLancer.Client.Components
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
                var fwd = Vector3.Transform(-Vector3.UnitZ, Parent.PhysicsComponent.Body.Orientation);
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
        public override void Update(double time)
		{
		}
	}
}
