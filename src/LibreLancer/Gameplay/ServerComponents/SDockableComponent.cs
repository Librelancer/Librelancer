// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Linq;
using System.Collections.Generic;
using LibreLancer.GameData;

namespace LibreLancer
{
    
	public class SDockableComponent : GameComponent
	{
		public DockAction Action;

        string tlHP;

        public DockSphere[] DockSpheres;
        
		public SDockableComponent(GameObject parent) : base(parent)
		{
		}

        IEnumerable<Hardpoint> GetDockHardpoints(int i, Vector3 position)
		{
			if (Action.Kind != DockKinds.Tradelane)
			{
				var hpname = DockSpheres[i].Hardpoint.Replace("DockMount", "DockPoint");
				yield return Parent.GetHardpoint(hpname + "02");
				yield return Parent.GetHardpoint(hpname + "01");
				yield return Parent.GetHardpoint(DockSpheres[i].Hardpoint);
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

        bool TryTriggerAnimation(int i, GameObject obj)
		{
            var rad = obj.PhysicsComponent?.Body.Collider.Radius ?? 15;
            var pos = Vector3.Transform(Vector3.Zero, obj.WorldTransform);
			foreach (var hps in GetDockHardpoints(i, pos))
            {
                var targetPos = Vector3.Transform(Vector3.Zero, hps.Transform * Parent.WorldTransform);
				var dist = (targetPos - pos).Length();
				if (dist < 20 + rad)
				{
					TriggerAnimation(i);
					return true;
				}
			}
			return false;
		}
        

        bool CanDock(int i, GameObject obj)
		{
            var rad = obj.PhysicsComponent?.Body.Collider.Radius ?? 15;
            var pos = Vector3.Transform(Vector3.Zero, obj.WorldTransform);
            
			var hp = Parent.GetHardpoint(tlHP ?? DockSpheres[i].Hardpoint);
			var targetPos = Vector3.Transform(Vector3.Zero, hp.Transform * Parent.WorldTransform);
			if ((targetPos - pos).Length() < (DockSpheres[i].Radius * 2 + rad))
			{
				return true;
			}
			return false;
		}

		void TriggerAnimation(int i)
		{
			var component = Parent.GetComponent<AnimationComponent>();
			if (component == null) return;
			if (!component.HasAnimation(DockSpheres[i].Script)) return;
            Parent.World.Server.StartAnimation(Parent, DockSpheres[i].Script);
            component.StartAnimation(DockSpheres[i].Script, false);
		}

        class DockingAction
        {
            public int Dock;
            public GameObject Ship;
            public bool HasTriggeredAnimation = false;
            public int LastTargetHp = 0;
        }

        private List<DockingAction> activeDockings = new List<DockingAction>();


        public void StartDock(GameObject obj, int index)
        {
            activeDockings.Add(new DockingAction() { Dock = index, Ship = obj });
        }
        
        public override void FixedUpdate(double time)
        {
            for(int i = activeDockings.Count - 1; i >= 0; i--)
            {
                var dock = activeDockings[i];
                if (!dock.HasTriggeredAnimation && TryTriggerAnimation(dock.Dock, dock.Ship))
                {
                    dock.HasTriggeredAnimation = true;
                }
                if (CanDock(dock.Dock, dock.Ship)) {
                    if (Action.Kind == DockKinds.Base)
                    {
                        var player = dock.Ship.GetComponent<SPlayerComponent>();
                        player.Player.ForceLand(Action.Target);
                    }
                    else if (Action.Kind == DockKinds.Jump)
                    {
                        var player = dock.Ship.GetComponent<SPlayerComponent>();
                        player.Player.JumpTo(Action.Target, Action.Exit);
                    }
                    activeDockings.RemoveAt(i);
                }
            }
        }

        public override void Update(double time)
		{
            
		}
	}
}
