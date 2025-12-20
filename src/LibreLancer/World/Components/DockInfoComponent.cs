// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.GameData.World;

namespace LibreLancer.World.Components
{
    public class DockCameraInfo
    {
        public GameObject Parent;
        public Hardpoint DockHardpoint;
    }

    public class UndockInfo
    {
        public Hardpoint Start;
        public Hardpoint End;
    }

	public class DockInfoComponent : GameComponent
	{
		public DockAction Action;
        public DockSphere[] Spheres;

		string tlHP;
		public DockInfoComponent(GameObject parent) : base(parent)
		{
		}

        public DockCameraInfo GetDockCamera(int index)
        {
            var hpname = Spheres[index].Hardpoint.Replace("DockMount", "DockCam");
            var hp = Parent.GetHardpoint(hpname);
            if (hp == null)
                return null;
            return new DockCameraInfo() { DockHardpoint = hp, Parent = Parent };
        }

        public UndockInfo GetUndockInfo(int index)
        {
            var hpname = Spheres[index].Hardpoint.Replace("DockMount", "DockPoint");
            var start = Parent.GetHardpoint(Spheres[index].Hardpoint);
            var end = Parent.GetHardpoint(hpname + "02");
            return new UndockInfo() { Start = start, End = end };
        }

        public float GetTriggerRadius(int index = 0)
        {
            return Spheres[index].Radius;
        }

		public IEnumerable<Hardpoint> GetDockHardpoints(Vector3 position, int index = 0)
		{
			if (Action.Kind != DockKinds.Tradelane)
			{
				var hpname = Spheres[index].Hardpoint.Replace("DockMount", "DockPoint");
				yield return Parent.GetHardpoint(hpname + "02");
				yield return Parent.GetHardpoint(hpname + "01");
				yield return Parent.GetHardpoint(Spheres[index].Hardpoint);
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
