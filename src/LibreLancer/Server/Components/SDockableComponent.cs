// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Client.Components;
using LibreLancer.GameData;
using LibreLancer.GameData.World;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Server.Components
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
            float animRadius = 30;
            if (Action.Kind == DockKinds.Tradelane) animRadius = 300;
            var rad = obj.PhysicsComponent?.Body.Collider.Radius ?? 15;
            var pos = Vector3.Transform(Vector3.Zero, obj.WorldTransform);
			foreach (var hps in GetDockHardpoints(i, pos))
            {
                var targetPos = Vector3.Transform(Vector3.Zero, hps.Transform * Parent.WorldTransform);
				var dist = (targetPos - pos).Length();
				if (dist < animRadius + rad)
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

        private bool leftActive = false;
        private int inactiveTicksLeft = 0;
        private bool rightActive = false;
        private int inactiveTicksRight = 0;
        private const int INACTIVE_TIME = 16;
        
		void TriggerAnimation(int i)
		{
            if (DockSpheres[i].Hardpoint.Equals("hpleftlane", StringComparison.OrdinalIgnoreCase)) {
                Parent.World.Server.ActivateLane(Parent, true);
                inactiveTicksLeft = INACTIVE_TIME;
                leftActive = true;
            } 
            else if (DockSpheres[i].Hardpoint.Equals("hprightlane", StringComparison.OrdinalIgnoreCase)) {
                Parent.World.Server.ActivateLane(Parent, false);
                inactiveTicksRight = INACTIVE_TIME;
                rightActive = true;
            }
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

        void StartTradelane(GameObject ship, string tlHardpoint)
        {
            var movement = new TradelaneMoveComponent(ship, Parent, tlHardpoint);
            ship.Components.Add(movement);
            if (ship.TryGetComponent<SNPCComponent>(out var npc))
            {
                npc.StartTradelane();
            }
            else if (ship.TryGetComponent<SPlayerComponent>(out var player))
            {
                player.Player.StartTradelane();
            }
            movement.LaneEntered();
            ship.Components.Add(movement);
        }
        
        public override void Update(double time)
        {
            bool leftThisTick = false;
            bool rightThisTick = false;
            for(int i = activeDockings.Count - 1; i >= 0; i--)
            {
                var dock = activeDockings[i];
                if (Action.Kind == DockKinds.Tradelane)
                {
                    if (dock.TLHardpoint.Equals("hpleftlane", StringComparison.OrdinalIgnoreCase))
                        leftThisTick = true;
                    else if (dock.TLHardpoint.Equals("hprightlane", StringComparison.OrdinalIgnoreCase))
                        rightThisTick = true;
                }
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
                        StartTradelane(dock.Ship, dock.TLHardpoint);
                        if (dock.Ship.Formation != null &&
                            dock.Ship.Formation.LeadShip == dock.Ship)
                        {
                            foreach(var ship in dock.Ship.Formation.Followers)
                                StartTradelane(ship, dock.TLHardpoint);
                        }
                    }
                    activeDockings.RemoveAt(i);
                }
            }

            if (inactiveTicksLeft > 0 && !leftThisTick)
            {
                inactiveTicksLeft--;
                if (inactiveTicksLeft == 0)
                {
                    leftActive = false;
                    Parent.World.Server.DeactivateLane(Parent, true);
                }
            }
            if (inactiveTicksRight > 0 && !rightThisTick)
            {
                inactiveTicksRight--;
                if (inactiveTicksRight == 0)
                {
                    rightActive = false;
                    Parent.World.Server.DeactivateLane(Parent, false);
                }
            }
        }
    }
}
