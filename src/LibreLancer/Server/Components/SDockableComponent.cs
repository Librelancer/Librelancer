// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data;
using LibreLancer.Data.GameData.World;
using LibreLancer.World;
using LibreLancer.World.Components;

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

        private const float OPEN_DURATION = 10;

        public void TriggerOpen(GameObject parent, GameWorld world)
        {
            CloseTimer = OPEN_DURATION;
            if (Open)
            {
                return;
            }

            Animate(false, parent, world);
            Open = true;
        }

        private void Animate(bool close, GameObject parent, GameWorld world)
        {
            var component = parent.GetComponent<AnimationComponent>();
            if (component == null)
            {
                return;
            }

            if (!component.HasAnimation(DockSphere.Script))
            {
                return;
            }

            component.StartAnimation(DockSphere.Script!, false, 0, 1, 0, close);
            world.Server!.StartAnimation(parent);
            FLLog.Debug("Server", $"{(close ? "closing" : "opening")} {parent.Nickname} {DockSphere.Script}");
        }

        public void Update(GameObject parent, GameWorld world, float dt)
        {
            if (Open)
            {
                CloseTimer -= dt;

                if (CloseTimer < 0)
                {
                    Open = false;
                    Animate(true, parent, world);
                }
            }
        }
    }

    public class SDockableComponent : GameComponent
    {
        public DockAction Action;

        public DockingPoint[] DockPoints;
        private DockHardpoints hardpoints;
        private Random r = new();

        public SDockableComponent(GameObject parent, DockAction action, DockSphere[] dockSpheres) : base(parent)
        {
            Action = action;
            DockPoints = dockSpheres.Select(x => new DockingPoint(x)).ToArray();
            hardpoints = new(Action, DockPoints);
        }

        private void TryTriggerAnimation(int i, GameObject obj, GameWorld world)
        {
            float animRadius = 30;
            if (Action.Kind == DockKinds.Tradelane)
            {
                animRadius = 300;
            }

            var rad = obj.PhysicsComponent?.Body.Collider.Radius ?? 15;
            var pos = obj.WorldTransform.Position;

            foreach (var hps in hardpoints.GetDockHardpoints(Parent, i, Vector3.Zero, false))
            {
                var targetPos = (hps.Transform * Parent.WorldTransform).Position;
                var dist = (targetPos - pos).Length();

                if (dist < animRadius + rad)
                {
                    TriggerAnimation(i, world);
                    break;
                }
            }
        }

        private bool CanPlayerTradelane(GameObject ship, string tradelaneNickname)
        {
            if (ship.TryGetComponent<SPlayerComponent>(out var player))
            {
                var mplayer = player.Player.MPlayer;
                if (mplayer.CanTl == 1)
                {
                    return true;
                }

                var hash = new HashValue(tradelaneNickname);
                if (mplayer.CanTl == 0 && mplayer.TlExceptions.Any(x => x.ItemA == hash || x.ItemB == hash))
                {
                    return true;
                }

                return false;
            }

            return true; // NPCs can always tradelane
        }

        private bool CanDock(int i, GameObject obj, string? tlHP = null)
        {
            var rad = obj.PhysicsComponent?.Body.Collider.Radius ?? 15;
            var pos = obj.WorldTransform.Position;

            var hp = Parent.GetHardpoint(tlHP ?? DockPoints[i].DockSphere.Hardpoint);
            var targetPos = (hp!.Transform * Parent.WorldTransform).Position;

            if ((targetPos - pos).Length() < (DockPoints[i].DockSphere.Radius * 2 + rad))
            {
                return true;
            }

            return false;
        }

        private bool leftActive = false;
        private bool rightActive = false;

        private int inactiveTicksLeft = 0;
        private int inactiveTicksRight = 0;
        private const int INACTIVE_TIME = 16;

        private void TriggerAnimation(int i, GameWorld world)
        {
            if (Action.Kind == DockKinds.Tradelane &&
                DockPoints[i].DockSphere.Hardpoint.Equals("hpleftlane", StringComparison.OrdinalIgnoreCase))
            {
                world.Server!.ActivateLane(Parent, true);
                inactiveTicksLeft = INACTIVE_TIME;
                leftActive = true;
            }
            else if (Action.Kind == DockKinds.Tradelane &&
                     DockPoints[i].DockSphere.Hardpoint.Equals("hprightlane", StringComparison.OrdinalIgnoreCase))
            {
                world.Server!.ActivateLane(Parent, false);
                inactiveTicksRight = INACTIVE_TIME;
                rightActive = true;
            }

            DockPoints[i].TriggerOpen(Parent, world);
        }

        private class DockingAction
        {
            public int Dock;
            public required GameObject Ship;
            public int LastTargetHp = 0;
            public string? TLHardpoint;
        }

        private List<DockingAction> activeDockings = [];

        private readonly List<(GameObject Ship, int RequestedIndex, GotoKind Kind)> dockQueue = [];

        public bool IsQueuedForDock(GameObject ship) =>
            dockQueue.Any(x => x.Ship == ship);

        public void StartDock(GameObject obj, int index, GotoKind kind = GotoKind.Goto)
        {
            if (activeDockings.Any(x => x.Ship == obj) || IsQueuedForDock(obj))
                return;

            var dockIndex = index;
            if (Action.Kind == DockKinds.Base &&
                !TryGetAvailableDockIndex(index, out dockIndex))
            {
                QueueDock(obj, index, kind);
                return;
            }

            StartDockNow(obj, dockIndex, kind);
        }

        private void StartDockNow(GameObject obj, int index, GotoKind kind)
        {
            var pos = obj.WorldTransform.Position;

            if (Action.Kind == DockKinds.Tradelane)
            {
                activeDockings.Add(new DockingAction()
                {
                    Dock = index,
                    Ship = obj,
                    TLHardpoint = hardpoints.GetDockHardpoints(Parent, index, pos, false).First().Name
                });
            }
            else
            {
                activeDockings.Add(new DockingAction() { Dock = index, Ship = obj });
            }

            if (obj.TryGetComponent<AutopilotComponent>(out var ap))
                ap.StartDock(Parent, kind);
        }

        private void QueueDock(GameObject ship, int requestedIndex, GotoKind kind)
        {
            if (activeDockings.Any(x => x.Ship == ship) || IsQueuedForDock(ship))
                return;

            var entry = (ship, requestedIndex, kind);
            if (!ship.TryGetComponent<SPlayerComponent>(out _))
            {
                dockQueue.Add(entry);
                return;
            }

            var insert = dockQueue.FindIndex(x => !x.Ship.TryGetComponent<SPlayerComponent>(out _));
            if (insert < 0)
                dockQueue.Add(entry);
            else
                dockQueue.Insert(insert, entry);
        }

        private bool TryGetAvailableDockIndex(int requestedIndex, out int index)
        {
            index = requestedIndex;
            if (IsDockIndexAvailableForDocking(requestedIndex))
                return true;

            for (int i = 0; i < DockPoints.Length; i++)
            {
                if (IsDockIndexAvailableForDocking(i))
                {
                    index = i;
                    return true;
                }
            }

            return false;
        }

        private bool IsDockIndexAvailableForDocking(int index) =>
            index >= 0 &&
            index < DockPoints.Length &&
            !IsUndockIndexBusy(index);

        private void ProcessDockQueue()
        {
            if (Action.Kind != DockKinds.Base)
                return;

            var i = 0;
            while (i < dockQueue.Count)
            {
                var queued = dockQueue[i];
                if (!queued.Ship.Flags.HasFlag(GameObjectFlags.Exists))
                {
                    dockQueue.RemoveAt(i);
                    continue;
                }

                if (!TryGetAvailableDockIndex(queued.RequestedIndex, out var index))
                    return;

                dockQueue.RemoveAt(i);
                StartDockNow(queued.Ship, index, queued.Kind);
            }
        }

        private static GameObject[] BreakDockingFormation(GameObject lead)
        {
            var formation = lead.Formation;
            if (formation == null || formation.LeadShip != lead)
                return [];

            var followers = formation.Followers.ToArray();
            foreach (var follower in followers)
                formation.Remove(follower);
            formation.Remove(lead);
            return followers;
        }

        private void StartTradelane(GameObject ship, string tlHardpoint)
        {
            var movement = new STradelaneMoveComponent(ship, Parent, tlHardpoint);
            ship.AddComponent(movement);

            if (Parent.TryGetComponent<ShipPhysicsComponent>(out var component))
            {
                component.Active = false;
            }

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

        public Transform3D GetSpawnPoint(int index)
        {
            if (!TryGetSpawnPoint(index, out var spawnPoint))
                throw new ArgumentOutOfRangeException(nameof(index), index, "Dock point has insufficient spawn hardpoints");

            return spawnPoint;
        }

        public bool TryGetSpawnPoint(int index, out Transform3D spawnPoint)
        {
            spawnPoint = default;
            if (index < 0 || index >= DockPoints.Length)
                return false;

            var hps = hardpoints.GetDockHardpoints(Parent, index, Vector3.Zero, false).ToArray();
            if (hps.Length < 2)
                return false;

            var tr = (hps[^1].Transform * Parent.WorldTransform);
            var tr2 = (hps[^2].Transform * Parent.WorldTransform);
            spawnPoint = new Transform3D(tr.Position, QuaternionEx.LookAt(tr.Position, tr2.Position));
            return true;
        }

        private List<(GameObject Ship, int Index)> undockers = [];

        public void UndockShip(GameObject ship, GameWorld world, int index)
        {
            TriggerAnimation(index, world);
            undockers.Add((ship, index));
        }

        private bool IsUndockIndexBusy(int index)
        {
            return undockers.Any(x =>
                       x.Index == index &&
                       x.Ship.Flags.HasFlag(GameObjectFlags.Exists)) ||
                   activeDockings.Any(x =>
                       x.Dock == index &&
                       x.Ship.Flags.HasFlag(GameObjectFlags.Exists));
        }

        public bool IsUndockIndexAvailable(int index) =>
            index >= 0 &&
            index < DockPoints.Length &&
            !DockPoints[index].Open &&
            !IsUndockIndexBusy(index) &&
            TryGetSpawnPoint(index, out _);

        public bool TryGetUndockIndex(out int index)
        {
            index = 0;
            if (DockPoints.Length == 0)
                return false;

            if (DockPoints.Length > 1 &&
                DockPoints[0].DockSphere.Type == Data.Schema.Solar.DockSphereType.ring)
            {
                return IsUndockIndexAvailable(index);
            }

            for (int i = 0; i < DockPoints.Length; i++)
            {
                if (IsUndockIndexAvailable(i))
                {
                    index = i;
                    return true;
                }
            }

            return false;
        }

        public int GetUndockIndex()
        {
            if (TryGetUndockIndex(out var index))
                return index;

            if (DockPoints.Length > 1 &&
                DockPoints[0].DockSphere.Type == Data.Schema.Solar.DockSphereType.ring)
            {
                return 0; // Must undock from 0
            }
            else
            {
                // First free point (?)
                for (int i = 0; i < DockPoints.Length; i++)
                {
                    if (!DockPoints[i].Open)
                    {
                        return i;
                    }
                }

                // Random
                return r.Next(0, DockPoints.Length);
            }
        }

        public override void Update(double time, GameWorld world)
        {
            bool leftThisTick = false;
            bool rightThisTick = false;

            for (int i = undockers.Count - 1; i >= 0; i--)
            {
                var undock = undockers[i];

                if (!undock.Ship.Flags.HasFlag(GameObjectFlags.Exists))
                {
                    undockers.RemoveAt(i);
                    continue;
                }

                var hps = hardpoints.GetDockHardpoints(Parent, undock.Index, Vector3.Zero, true);

                if (hps.Length < 2)
                {
                    FLLog.Debug("Docking", $"{undock.Ship} launch complete (insufficient hardpoints {hps.Length})");
                    undockers.RemoveAt(i);
                    world.Server!.LaunchComplete(undock.Ship);
                    continue;
                }

                var totaldistance = Vector3.Distance(hps[^1].Transform.Position, hps[0].Transform.Position);
                var hp0World = hps[0].Transform * Parent.WorldTransform;
                var pDistance = Vector3.Distance(undock.Ship.WorldTransform.Position, hp0World.Position);

                if (pDistance + 20 >= totaldistance)
                {
                    undockers.RemoveAt(i);
                    FLLog.Debug("Docking", $"{undock.Ship} launch complete {pDistance} + 20 >= {totaldistance}");
                    world.Server!.LaunchComplete(undock.Ship);
                }
            }

            for (int i = activeDockings.Count - 1; i >= 0; i--)
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
                    {
                        leftThisTick = true;
                    }
                    else if (dock.TLHardpoint.Equals("hprightlane", StringComparison.OrdinalIgnoreCase))
                    {
                        rightThisTick = true;
                    }
                }

                TryTriggerAnimation(dock.Dock, dock.Ship, world);
                if (!CanDock(dock.Dock, dock.Ship, dock.TLHardpoint))
                {
                    continue;
                }

                if (Action.Kind == DockKinds.Base)
                {
                    foreach (var ship in BreakDockingFormation(dock.Ship))
                        QueueDock(ship, dock.Dock, GotoKind.Goto);

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
                        player.Player.JumpTo(Action.Target!, Action.Exit!, world.Server!.GatherJumpers());
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
                        foreach (var ship in dock.Ship.Formation.Followers)
                            StartTradelane(ship, dock.TLHardpoint);
                    }
                }

                activeDockings.RemoveAt(i);
            }

            foreach (var dp in DockPoints)
                dp.Update(Parent, world, (float) time);

            ProcessDockQueue();

            if (inactiveTicksLeft > 0 && !leftThisTick)
            {
                inactiveTicksLeft--;

                if (inactiveTicksLeft == 0)
                {
                    leftActive = false;
                    world.Server!.DeactivateLane(Parent, true);
                }
            }

            if (inactiveTicksRight > 0 && !rightThisTick)
            {
                inactiveTicksRight--;

                if (inactiveTicksRight == 0)
                {
                    rightActive = false;
                    world.Server!.DeactivateLane(Parent, false);
                }
            }
        }
    }
}
