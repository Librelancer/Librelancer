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
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LibreLancer.Server.Components
{
    public class DockingPoint
    {
        public bool Open;
        public float CloseTimer;
        public DockSphere DockSphere;

        public DockingPoint(DockSphere s)
        {
            DockSphere = s;
        }

        private const float OPEN_DURATION = 5;
        public void TriggerOpen(GameObject parent)
        {
            CloseTimer = OPEN_DURATION;
            if (Open) return;
            Animate(false, parent);
            Open = true;
        }

        void Animate(bool close, GameObject parent)
        {
            var component = parent.GetComponent<AnimationComponent>();
            if (component == null) return;
            if (!component.HasAnimation(DockSphere.Script)) return;
            component.StartAnimation(DockSphere.Script, false, 0,1,0, close);
            parent.World.Server.StartAnimation(parent);
        }

        public void Update(GameObject parent, float dt)
        {
            if (Open)
            {
                CloseTimer -= dt;
                if (CloseTimer < 0)
                {
                    Open = false;
                    Animate(true, parent);
                }
            }
        }
    }

    public class SDockableComponent : GameComponent
	{
		public DockAction Action;

        public DockingPoint[] DockPoints;

		public SDockableComponent(GameObject parent, DockSphere[] dockSpheres) : base(parent)
        {
            DockPoints = dockSpheres.Select(x => new DockingPoint(x)).ToArray();
        }

        IEnumerable<Hardpoint> GetDockHardpoints(int i, Vector3 position)
		{
			if (Action.Kind != DockKinds.Tradelane)
			{
				var hpname = DockPoints[i].DockSphere.Hardpoint.Replace("DockMount", "DockPoint");
				yield return Parent.GetHardpoint(hpname + "02");
				yield return Parent.GetHardpoint(hpname + "01");
				yield return Parent.GetHardpoint(DockPoints[i].DockSphere.Hardpoint);
			}
			else if (Action.Kind == DockKinds.Tradelane)
			{
				var heading = position - Parent.PhysicsComponent.Body.Position;
                var fwd = Vector3.Transform(-Vector3.UnitZ, Parent.PhysicsComponent.Body.Orientation);
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

        void TryTriggerAnimation(int i, GameObject obj)
        {
            float animRadius = 30;
            if (Action.Kind == DockKinds.Tradelane) animRadius = 300;
            var rad = obj.PhysicsComponent?.Body.Collider.Radius ?? 15;
            var pos = obj.WorldTransform.Position;
			foreach (var hps in GetDockHardpoints(i, pos))
            {
                var targetPos = (hps.Transform * Parent.WorldTransform).Position;
				var dist = (targetPos - pos).Length();
				if (dist < animRadius + rad)
				{
					TriggerAnimation(i);
                    break;
                }
			}
		}


        bool CanDock(int i, GameObject obj, string tlHP = null)
		{
            var rad = obj.PhysicsComponent?.Body.Collider.Radius ?? 15;
            var pos = obj.WorldTransform.Position;

			var hp = Parent.GetHardpoint(tlHP ?? DockPoints[i].DockSphere.Hardpoint);
            var targetPos = (hp.Transform * Parent.WorldTransform).Position;
			if ((targetPos - pos).Length() < (DockPoints[i].DockSphere.Radius * 2 + rad))
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
            if (Action.Kind == DockKinds.Tradelane &&
                DockPoints[i].DockSphere.Hardpoint.Equals("hpleftlane", StringComparison.OrdinalIgnoreCase)) {
                Parent.World.Server.ActivateLane(Parent, true);
                inactiveTicksLeft = INACTIVE_TIME;
                leftActive = true;
            }
            else if (Action.Kind == DockKinds.Tradelane &&
                DockPoints[i].DockSphere.Hardpoint.Equals("hprightlane", StringComparison.OrdinalIgnoreCase)) {
                Parent.World.Server.ActivateLane(Parent, false);
                inactiveTicksRight = INACTIVE_TIME;
                rightActive = true;
            }
			DockPoints[i].TriggerOpen(Parent);
		}

        class DockingAction
        {
            public int Dock;
            public GameObject Ship;
            public int LastTargetHp = 0;
            public string TLHardpoint;
        }

        private List<DockingAction> activeDockings = new List<DockingAction>();


        public void StartDock(GameObject obj, int index)
        {
            var pos = obj.WorldTransform.Position;
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
            var movement = new STradelaneMoveComponent(ship, Parent, tlHardpoint);
            ship.AddComponent(movement);
            ShipPhysicsComponent component = Parent.GetComponent<ShipPhysicsComponent>();
            if (component is not null)
                component.Active = false;
            if (ship.TryGetComponent<SNPCComponent>(out var npc))
            {
                npc.StartTradelane();
            }
            else if (ship.TryGetComponent<SPlayerComponent>(out var player))
            {
                player.Player.StartTradelane();
            }
            movement.LaneEntered();
        }

        public override void Update(double time)
        {
            bool leftThisTick = false;
            bool rightThisTick = false;
            for(int i = activeDockings.Count - 1; i >= 0; i--)
            {
                var dock = activeDockings[i];
                if (!dock.Ship.Flags.HasFlag(GameObjectFlags.Exists))
                {
                    activeDockings.RemoveAt(i);
                    continue;
                }
                if (Action.Kind == DockKinds.Tradelane)
                {
                    if (dock.TLHardpoint.Equals("hpleftlane", StringComparison.OrdinalIgnoreCase))
                        leftThisTick = true;
                    else if (dock.TLHardpoint.Equals("hprightlane", StringComparison.OrdinalIgnoreCase))
                        rightThisTick = true;
                }
                TryTriggerAnimation(dock.Dock, dock.Ship);
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
            foreach(var dp in DockPoints)
                dp.Update(Parent, (float)time);

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
