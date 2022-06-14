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
					yield return Parent.GetHardpoint("HpLeftLane");
				}
				else
				{
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
        

        bool CanDock(int i, GameObject obj, string tlHP = null)
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
            public string TLHardpoint;
        }

        private List<DockingAction> activeDockings = new List<DockingAction>();


        public void StartDock(GameObject obj, int index)
        {
            var pos = Vector3.Transform(Vector3.Zero, obj.WorldTransform);
            if (Action.Kind == DockKinds.Tradelane)
            {
                activeDockings.Add(new DockingAction()
                {
                    Dock = index,
                    Ship = obj,
                    TLHardpoint =  GetDockHardpoints(index, pos).First().Name
                });
            }
            else
            {
                activeDockings.Add(new DockingAction() {Dock = index, Ship = obj});
            }
        }

        
        public override void Update(double time)
        {
            for(int i = activeDockings.Count - 1; i >= 0; i--)
            {
                var dock = activeDockings[i];
                if (!dock.HasTriggeredAnimation && TryTriggerAnimation(dock.Dock, dock.Ship))
                {
                    dock.HasTriggeredAnimation = true;
                }
                if (CanDock(dock.Dock, dock.Ship, dock.TLHardpoint)) {
                    if (Action.Kind == DockKinds.Base)
                    {
                        if (dock.Ship.TryGetComponent<SPlayerComponent>(out var player))
                        {
                            player.Player.ForceLand(Action.Target);
                        } 
                        else if (dock.Ship.TryGetComponent<SNPCComponent>(out var npc))
                        {
                            npc.Docked();
                        }
                    }
                    else if (Action.Kind == DockKinds.Jump)
                    {
                        if (dock.Ship.TryGetComponent<SPlayerComponent>(out var player))
                        {
                            player.Player.JumpTo(Action.Target, Action.Exit);
                        } 
                        else if (dock.Ship.TryGetComponent<SNPCComponent>(out var npc))
                        {
                            npc.Docked();
                        }
                    }
                    else if (Action.Kind == DockKinds.Tradelane)
                    {
                        var movement = new TradelaneMoveComponent(dock.Ship, Parent, dock.TLHardpoint);
                        dock.Ship.Components.Add(movement);
                        if (dock.Ship.TryGetComponent<SNPCComponent>(out var npc))
                        {
                            npc.StartTradelane();
                        }
                        else if (dock.Ship.TryGetComponent<SPlayerComponent>(out var player))
                        {
                            player.Player.StartTradelane();
                            movement.LaneEntered();
                        }
                    }
                    activeDockings.RemoveAt(i);
                }
            }
        }
    }
}
