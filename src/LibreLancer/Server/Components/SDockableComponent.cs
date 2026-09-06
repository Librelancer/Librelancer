// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.Ships;
using LibreLancer.World;
using LibreLancer.World.Components;
using DockSphereType = LibreLancer.Data.Schema.Solar.DockSphereType;

namespace LibreLancer.Server.Components
{
    public class DockingPoint
    {
        public bool Open;
        public bool OpenAnimationStarted;
        public float CloseTimer;
        public DockSphere DockSphere;

        public DockingPoint(DockSphere s)
        {
            DockSphere = s;
        }

        private const float OPEN_DURATION = 10;

        public void TriggerOpen(GameObject parent, GameWorld world, bool force = false)
        {
            CloseTimer = OPEN_DURATION;
            if (Open && (!force || OpenAnimationStarted))
            {
                return;
            }

            OpenAnimationStarted = Animate(false, parent, world) || OpenAnimationStarted;
            Open = true;
        }

        private bool Animate(bool close, GameObject parent, GameWorld world)
        {
            var component = parent.GetComponent<AnimationComponent>();
            if (component == null)
            {
                return false;
            }

            if (!component.HasAnimation(DockSphere.Script))
            {
                FLLog.Debug("Server", $"dock animation missing for {parent.Nickname}: {DockSphere.Script ?? "<none>"}");
                return false;
            }

            component.StartAnimation(DockSphere.Script!, false, 0, 1, 0, close);
            world.Server!.StartAnimation(parent);
            FLLog.Debug("Server", $"{(close ? "closing" : "opening")} {parent.Nickname} {DockSphere.Script}");
            return true;
        }

        public void Update(GameObject parent, GameWorld world, float dt)
        {
            if (Open)
            {
                CloseTimer -= dt;

                if (CloseTimer < 0)
                {
                    Open = false;
                    OpenAnimationStarted = false;
                    Animate(true, parent, world);
                }
            }
        }
    }

    public class SDockableComponent : GameComponent
    {
        private const double NPC_MOOR_TIME = 60.0;

        public DockAction Action;

        public DockingPoint[] DockPoints;
        private readonly DockHardpoints hardpoints;
        private bool dockingLightsOn;

        public SDockableComponent(GameObject parent, DockAction action, DockSphere[] dockSpheres) : base(parent)
        {
            Action = action;
            DockPoints = dockSpheres.Select(x => new DockingPoint(x)).ToArray();
            hardpoints = new(Action, DockPoints);
        }

        private bool HasDockAnimation(int i) =>
            Parent.GetComponent<AnimationComponent>()?.HasAnimation(DockPoints[i].DockSphere.Script) == true;

        private void TryTriggerAnimation(
            int i,
            GameObject obj,
            GameWorld world,
            string? tradelaneHardpoint = null)
        {
            if (i < 0 || i >= DockPoints.Length)
            {
                return;
            }

            float animRadius = 100;
            if (Action.Kind == DockKinds.Tradelane)
            {
                animRadius = 300;
            }

            var rad = obj.PhysicsComponent?.Body.Collider.Radius ?? 15;
            var pos = obj.WorldTransform.Position;
            var hp = Action.Kind == DockKinds.Tradelane && tradelaneHardpoint != null
                ? Parent.GetHardpoint(tradelaneHardpoint)
                : hardpoints.GetDockHardpoints(Parent, i, obj.WorldTransform.Position, false).FirstOrDefault();
            if (hp == null)
            {
                return;
            }

            var targetPos = (Action.Kind == DockKinds.Tradelane
                    ? hp.TransformNoRotate
                    : hp.Transform) * Parent.WorldTransform;
            var dist = (targetPos.Position - pos).Length();

            var forceAnimation = HasDockAnimation(i) &&
                                 DockPoints[i].Open &&
                                 !DockPoints[i].OpenAnimationStarted;
            if ((!DockPoints[i].Open || forceAnimation) &&
                dist < animRadius + rad)
            {
                TriggerAnimation(i, world, forceAnimation, tradelaneHardpoint);
            }
        }

        private int GetTradelaneDockIndex(string hardpoint, int fallback)
        {
            for (var i = 0; i < DockPoints.Length; i++)
            {
                if (DockPoints[i].DockSphere.Hardpoint.Equals(
                        hardpoint,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return fallback;
        }

        private bool IsDockingRingIndex(int index) =>
            Action.Kind == DockKinds.Base &&
            index == 0 &&
            index < DockPoints.Length &&
            DockPoints[index].DockSphere.Type == DockSphereType.ring;

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

        private bool CanDock(int i, GameObject obj, GameWorld world, string? tlHP = null)
        {
            var rad = obj.PhysicsComponent?.Body.Collider.Radius ?? 15;
            var pos = obj.PhysicsComponent?.Body.Position ?? obj.WorldTransform.Position;

            var hp = Parent.GetHardpoint(tlHP ?? DockPoints[i].DockSphere.Hardpoint);
            if (hp == null)
            {
                return false;
            }

            var targetPos = ((Action.Kind == DockKinds.Tradelane
                    ? hp.TransformNoRotate
                    : hp.Transform) * Parent.WorldTransform).Position;

            if (Action.Kind == DockKinds.Tradelane && tlHP != null)
            {
                var nextNickname = tlHP.Equals(
                    "HpRightLane",
                    StringComparison.OrdinalIgnoreCase)
                    ? Action.Target
                    : Action.TargetLeft;
                var nextRing = nextNickname == null ? null : world.GetObject(nextNickname);
                var nextHardpoint = nextRing?.GetHardpoint(tlHP);
                if (nextRing != null && nextHardpoint != null)
                {
                    var nextPosition =
                        (nextHardpoint.TransformNoRotate * nextRing.WorldTransform).Position;
                    return TradelaneMotion.HasCrossedEntryPlane(
                        pos,
                        targetPos,
                        nextPosition - targetPos,
                        DockPoints[i].DockSphere.Radius + rad,
                        rad);
                }
            }

            if ((targetPos - pos).Length() < (DockPoints[i].DockSphere.Radius + rad))
            {
                return true;
            }

            return false;
        }

        private float DistanceToDockMount(int i, GameObject obj)
        {
            var hp = Parent.GetHardpoint(DockPoints[i].DockSphere.Hardpoint);
            if (hp == null)
            {
                return float.MaxValue;
            }

            var targetPos = (hp.Transform * Parent.WorldTransform).Position;
            return (targetPos - obj.WorldTransform.Position).Length();
        }

        private float GetRingFlyThroughRange(int i, GameObject obj)
        {
            var rad = obj.PhysicsComponent?.Body.Collider.Radius ?? 15;
            return Math.Max(250, DockPoints[i].DockSphere.Radius + rad);
        }

        private int inactiveTicksLeft = 0;
        private int inactiveTicksRight = 0;
        private const int INACTIVE_TIME = 16;

        private void TriggerAnimation(
            int i,
            GameWorld world,
            bool force = false,
            string? tradelaneHardpoint = null)
        {
            var laneHardpoint = tradelaneHardpoint ?? DockPoints[i].DockSphere.Hardpoint;
            if (Action.Kind == DockKinds.Tradelane &&
                laneHardpoint.Equals("hpleftlane", StringComparison.OrdinalIgnoreCase))
            {
                world.Server!.ActivateLane(Parent, true);
                inactiveTicksLeft = INACTIVE_TIME;
            }
            else if (Action.Kind == DockKinds.Tradelane &&
                     laneHardpoint.Equals("hprightlane", StringComparison.OrdinalIgnoreCase))
            {
                world.Server!.ActivateLane(Parent, false);
                inactiveTicksRight = INACTIVE_TIME;
            }

            DockPoints[i].TriggerOpen(Parent, world, force);
        }

        private DockSphereType GetDockTypeForShip(GameObject ship)
        {
            if (Action.Kind == DockKinds.Tradelane)
            {
                return DockSphereType.ring;
            }

            if (!ship.TryGetComponent<ShipComponent>(out var shipComponent))
            {
                return DockSphereType.berth;
            }

            return shipComponent.Ship.MissionProperty switch
            {
                ShipMissionProperty.can_use_large_moors => DockSphereType.moor_large,
                ShipMissionProperty.can_use_med_moors => DockSphereType.moor_medium,
                _ => DockSphereType.berth
            };
        }

        private bool CanUseDockIndex(GameObject ship, int index)
        {
            if (index < 0 || index >= DockPoints.Length)
            {
                return false;
            }

            var dockType = DockPoints[index].DockSphere.Type;
            return IsDockingRingIndex(index) ||
                   dockType == DockSphereType.ring ||
                   dockType == DockSphereType.airlock ||
                   dockType == DockSphereType.jump ||
                   dockType == GetDockTypeForShip(ship);
        }

        private bool HasCompatibleDockIndex(GameObject ship) =>
            Enumerable.Range(0, DockPoints.Length).Any(i => CanUseDockIndex(ship, i));

        private static bool IsMoor(DockSphereType type) =>
            type is DockSphereType.moor_small or DockSphereType.moor_medium or DockSphereType.moor_large;

        private class DockingAction
        {
            public int Dock;
            public required GameObject Ship;
            public string? TLHardpoint;
            public bool Moored;
            public double MoorTimeLeft;
            public bool RingDocking;
            public double RingDockTimeLeft;
        }

        private readonly List<DockingAction> activeDockings = [];

        private readonly List<(GameObject Ship, int RequestedIndex, GotoKind Kind, DockSphereType Type)> dockQueue = [];

        public bool IsQueuedForDock(GameObject ship) =>
            dockQueue.Any(x => x.Ship == ship);

        public void StartDock(
            GameObject obj,
            int index,
            GotoKind kind = GotoKind.Goto,
            GameWorld? world = null,
            string? requestedTradelaneHardpoint = null)
        {
            if (activeDockings.Any(x => x.Ship == obj) || IsQueuedForDock(obj))
                return;

            var dockIndex = index;
            if (Action.Kind == DockKinds.Base && !HasCompatibleDockIndex(obj))
            {
                var dockType = GetDockTypeForShip(obj);
                FLLog.Warning(
                    "Docking",
                    $"{obj.Nickname ?? obj.NetID.ToString()} cannot dock at {Parent.Nickname}: no {dockType} dock spheres");
                return;
            }

            if (Action.Kind == DockKinds.Base &&
                !TryGetAvailableDockIndex(obj, index, out dockIndex))
            {
                QueueDock(obj, index, kind);
                return;
            }

            StartDockNow(obj, dockIndex, kind, world, requestedTradelaneHardpoint);
        }

        private static string? ValidTradelaneHardpoint(string? hardpoint)
        {
            if (hardpoint?.Equals("HpLeftLane", StringComparison.OrdinalIgnoreCase) == true)
                return "HpLeftLane";

            if (hardpoint?.Equals("HpRightLane", StringComparison.OrdinalIgnoreCase) == true)
                return "HpRightLane";

            return null;
        }

        private void StartDockNow(
            GameObject obj,
            int index,
            GotoKind kind,
            GameWorld? world,
            string? requestedTradelaneHardpoint = null)
        {
            var pos = obj.WorldTransform.Position;
            string? tradelaneHardpoint = null;

            if (Action.Kind == DockKinds.Tradelane)
            {
                tradelaneHardpoint = ValidTradelaneHardpoint(requestedTradelaneHardpoint) ??
                                     hardpoints.GetDockHardpoints(Parent, index, pos, false).First().Name;
                index = GetTradelaneDockIndex(tradelaneHardpoint, index);
                activeDockings.Add(new DockingAction()
                {
                    Dock = index,
                    Ship = obj,
                    TLHardpoint = tradelaneHardpoint
                });
            }
            else
            {
                activeDockings.Add(new DockingAction() { Dock = index, Ship = obj });
            }

            if (world != null)
            {
                TryTriggerAnimation(index, obj, world, tradelaneHardpoint);
            }

            if (obj.TryGetComponent<AutopilotComponent>(out var ap))
                ap.StartDock(Parent, kind, index, tradelaneHardpoint);
        }

        private void QueueDock(GameObject ship, int requestedIndex, GotoKind kind)
        {
            if (activeDockings.Any(x => x.Ship == ship) || IsQueuedForDock(ship))
                return;

            var dockType = GetQueueDockType(ship, requestedIndex);
            var entry = (ship, requestedIndex, kind, dockType);
            if (!ship.TryGetComponent<SPlayerComponent>(out _))
            {
                dockQueue.Add(entry);
                return;
            }

            var insert = dockQueue.FindIndex(x =>
                x.Type == dockType &&
                !x.Ship.TryGetComponent<SPlayerComponent>(out _));
            if (insert < 0)
                dockQueue.Add(entry);
            else
                dockQueue.Insert(insert, entry);
        }

        private bool TryGetAvailableDockIndex(GameObject ship, int requestedIndex, out int index)
        {
            index = requestedIndex;
            if (CanUseDockIndex(ship, requestedIndex) &&
                IsDockIndexAvailableForDocking(requestedIndex))
                return true;

            if (IsDockingRingIndex(requestedIndex))
            {
                return false;
            }

            for (int i = 0; i < DockPoints.Length; i++)
            {
                if (CanUseDockIndex(ship, i) &&
                    IsDockIndexAvailableForDocking(i))
                {
                    index = i;
                    return true;
                }
            }

            return false;
        }

        private DockSphereType GetQueueDockType(GameObject ship, int requestedIndex) =>
            IsDockingRingIndex(requestedIndex) ? DockSphereType.ring : GetDockTypeForShip(ship);

        private bool IsDockIndexAvailableForDocking(int index) =>
            index >= 0 &&
            index < DockPoints.Length &&
            !IsUndockIndexBusy(index);

        private void ProcessDockQueue(GameWorld world)
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

                if (!TryGetAvailableDockIndex(queued.Ship, queued.RequestedIndex, out var index))
                {
                    i++;
                    continue;
                }

                dockQueue.RemoveAt(i);
                StartDockNow(queued.Ship, index, queued.Kind, world);
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

        private void StartDockingRingFlyThrough(DockingAction dock)
        {
            if (dock.Ship.TryGetComponent<ShipPhysicsComponent>(out var physics))
            {
                physics.Active = false;
            }

            dock.Ship.PhysicsComponent!.Collidable = false;
            dock.Ship.PhysicsComponent.Body.Collidable = false;
            dock.Ship.PhysicsComponent.Body.LinearVelocity =
                Vector3.Transform(-Vector3.UnitZ, dock.Ship.PhysicsComponent.Body.Orientation) * 80;

            if (dock.Ship.TryGetComponent<ShipSteeringComponent>(out var steering))
            {
                steering.InThrottle = 1;
                steering.Cruise = false;
            }

            dock.RingDocking = true;
            dock.RingDockTimeLeft = 3.0;
            FLLog.Debug("Docking", $"{dock.Ship} entering docking ring {DockPoints[dock.Dock].DockSphere.Hardpoint}");
        }

        private static Vector3 FormationOffset(GameObject ship) =>
            ship.Formation is not null
                ? ship.Formation.GetShipOffset(ship)
                : Vector3.Zero;

        private static Vector3 TradelanePoint(GameObject ring, string hardpoint, GameObject ship)
        {
            var transform = ring.GetHardpoint(hardpoint)!.TransformNoRotate * ring.WorldTransform;
            return transform.Transform(FormationOffset(ship));
        }

        private void StartTradelane(GameObject ship, string tlHardpoint, GameWorld world)
        {
            var entryHardpoint = Parent.GetHardpoint(tlHardpoint)!;
            var entryTransform = entryHardpoint.TransformNoRotate * Parent.WorldTransform;
            var entryPosition = TradelanePoint(Parent, tlHardpoint, ship);
            var nextNickname = tlHardpoint.Equals(
                "HpRightLane",
                StringComparison.OrdinalIgnoreCase)
                ? Action.Target
                : Action.TargetLeft;
            var nextRing = nextNickname == null ? null : world.GetObject(nextNickname);
            var nextHardpoint = nextRing?.GetHardpoint(tlHardpoint);
            var orientation = ship.PhysicsComponent!.Body.Orientation;
            var enteredAtPenultimate = false;
            if (nextRing != null && nextHardpoint != null)
            {
                var nextPosition = TradelanePoint(nextRing, tlHardpoint, ship);
                var direction = nextPosition - entryPosition;
                if (direction.LengthSquared() > float.Epsilon)
                {
                    orientation = TradelaneMotion.OrientationForDirection(
                        direction,
                        entryTransform.Orientation);
                    var speed = ship.PhysicsComponent.Body.LinearVelocity.Length();
                    ship.PhysicsComponent.Body.LinearVelocity =
                        Vector3.Normalize(direction) * speed;
                    ship.PhysicsComponent.Body.AngularVelocity = Vector3.Zero;
                }

                if (nextRing.TryGetComponent<SDockableComponent>(out var nextDock))
                {
                    var ringAfterNext = tlHardpoint.Equals(
                        "HpRightLane",
                        StringComparison.OrdinalIgnoreCase)
                        ? nextDock.Action.Target
                        : nextDock.Action.TargetLeft;
                    enteredAtPenultimate = string.IsNullOrWhiteSpace(ringAfterNext);
                }
            }

            var movement = new STradelaneMoveComponent(
                ship,
                Parent,
                tlHardpoint,
                orientation,
                enteredAtPenultimate);
            ship.AddComponent(movement);

            if (ship.TryGetComponent<ShipPhysicsComponent>(out var component))
            {
                component.Active = false;
            }

            ship.GetComponent<AutopilotComponent>()?.Cancel();

            if (ship.TryGetComponent<SNPCComponent>(out var npc))
            {
                npc.StartTradelane();
            }
            else if (ship.TryGetComponent<SPlayerComponent>(out var player))
            {
                player.Player.StartTradelane(Parent, orientation);
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

        private readonly List<(GameObject Ship, int Index)> undockers = [];
        private readonly HashSet<int> reservedUndockIndices = [];

        public void UndockShip(GameObject ship, GameWorld world, int index)
        {
            reservedUndockIndices.Remove(index);
            TriggerAnimation(index, world);
            undockers.Add((ship, index));
        }

        public void ReleaseUndockIndex(int index)
        {
            reservedUndockIndices.Remove(index);
        }

        private bool IsUndockIndexBusy(int index)
        {
            return reservedUndockIndices.Contains(index) ||
                   undockers.Any(x =>
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

        public bool TryReserveUndockIndex(out int index)
        {
            if (!TryGetUndockIndex(out index))
            {
                return false;
            }

            reservedUndockIndices.Add(index);
            return true;
        }

        public bool TryReserveUndockIndex(int index)
        {
            if (!IsUndockIndexAvailable(index))
            {
                return false;
            }

            reservedUndockIndices.Add(index);
            return true;
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

                if (dock.Moored)
                {
                    dock.MoorTimeLeft -= time;
                    if (dock.MoorTimeLeft > 0)
                    {
                        continue;
                    }

                    FLLog.Debug("Docking", $"{dock.Ship} leaving moor {DockPoints[dock.Dock].DockSphere.Hardpoint}");
                    activeDockings.RemoveAt(i);
                    UndockShip(dock.Ship, world, dock.Dock);
                    dock.Ship.GetComponent<AutopilotComponent>()?.Undock(Parent, dock.Dock);
                    continue;
                }

                if (dock.RingDocking)
                {
                    dock.RingDockTimeLeft -= time;
                    if (dock.RingDockTimeLeft > 0)
                    {
                        continue;
                    }

                    FLLog.Debug("Docking", $"{dock.Ship} docking ring fly-through complete");
                    if (dock.Ship.TryGetComponent<SPlayerComponent>(out var ringPlayer))
                    {
                        ringPlayer.Player.ForceLand(Action.Target);
                    }
                    else if (dock.Ship.TryGetComponent<SNPCComponent>(out var ringNpc))
                    {
                        ringNpc.Docked();
                    }

                    activeDockings.RemoveAt(i);
                    continue;
                }

                if (Action.Kind == DockKinds.Tradelane)
                {
                    var tlHardpoint = dock.TLHardpoint;
                    if (tlHardpoint == null)
                    {
                        activeDockings.RemoveAt(i);
                        continue;
                    }

                    if (tlHardpoint.Equals("hpleftlane", StringComparison.OrdinalIgnoreCase))
                    {
                        leftThisTick = true;
                    }
                    else if (tlHardpoint.Equals("hprightlane", StringComparison.OrdinalIgnoreCase))
                    {
                        rightThisTick = true;
                    }
                }

                TryTriggerAnimation(dock.Dock, dock.Ship, world, dock.TLHardpoint);
                if (IsDockingRingIndex(dock.Dock) &&
                    !dock.RingDocking &&
                    DistanceToDockMount(dock.Dock, dock.Ship) <= GetRingFlyThroughRange(dock.Dock, dock.Ship))
                {
                    foreach (var ship in BreakDockingFormation(dock.Ship))
                        QueueDock(ship, dock.Dock, GotoKind.Goto);

                    StartDockingRingFlyThrough(dock);
                    continue;
                }

                if (!CanDock(dock.Dock, dock.Ship, world, dock.TLHardpoint))
                {
                    continue;
                }

                if (Action.Kind == DockKinds.Base)
                {
                    var dockType = DockPoints[dock.Dock].DockSphere.Type;
                    foreach (var ship in BreakDockingFormation(dock.Ship))
                        QueueDock(ship, dock.Dock, GotoKind.Goto);

                    if (IsDockingRingIndex(dock.Dock) &&
                        !dock.RingDocking)
                    {
                        StartDockingRingFlyThrough(dock);
                        continue;
                    }

                    if (dock.Ship.TryGetComponent<SPlayerComponent>(out var player))
                    {
                        player.Player.ForceLand(Action.Target);
                    }
                    else if (IsMoor(dockType) &&
                             dock.Ship.TryGetComponent<SNPCComponent>(out _))
                    {
                        if (dock.Ship.TryGetComponent<AutopilotComponent>(out var ap))
                        {
                            ap.Cancel();
                        }

                        if (dock.Ship.TryGetComponent<ShipSteeringComponent>(out var steering))
                        {
                            steering.InThrottle = 0;
                            steering.Cruise = false;
                        }

                        dock.Moored = true;
                        dock.MoorTimeLeft = NPC_MOOR_TIME;
                        FLLog.Debug("Docking", $"{dock.Ship} moored at {DockPoints[dock.Dock].DockSphere.Hardpoint}");
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
                    var tlHardpoint = dock.TLHardpoint!;
                    StartTradelane(dock.Ship, tlHardpoint, world);

                    if (dock.Ship.Formation != null &&
                        dock.Ship.Formation.LeadShip == dock.Ship)
                    {
                        foreach (var ship in dock.Ship.Formation.Followers)
                            StartTradelane(ship, tlHardpoint, world);
                    }
                }

                activeDockings.RemoveAt(i);
            }

            foreach (var dp in DockPoints)
                dp.Update(Parent, world, (float) time);

            var shouldShowDockingLights = DockPoints.Any(x => x.Open);
            if (shouldShowDockingLights != dockingLightsOn)
            {
                dockingLightsOn = shouldShowDockingLights;
                world.Server!.SetDockingLights(Parent, dockingLightsOn);
            }

            ProcessDockQueue(world);

            if (inactiveTicksLeft > 0 && !leftThisTick)
            {
                inactiveTicksLeft--;

                if (inactiveTicksLeft == 0)
                {
                    world.Server!.DeactivateLane(Parent, true);
                }
            }

            if (inactiveTicksRight > 0 && !rightThisTick)
            {
                inactiveTicksRight--;

                if (inactiveTicksRight == 0)
                {
                    world.Server!.DeactivateLane(Parent, false);
                }
            }
        }
    }
}
