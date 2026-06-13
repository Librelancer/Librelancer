// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer.Client.Components;
using LibreLancer.Net.Protocol;

namespace LibreLancer.World.Components
{
    public enum AutopilotBehaviors
    {
        None,
        Goto,
        Dock,
        Formation,
        Undock
    }

    internal abstract class AutopilotBehavior
    {
        private bool hasTriggeredCruise = false;
        private StrafeControls avoidanceStrafe = StrafeControls.None;
        private Vector2 avoidanceVector = Vector2.Zero;
        private float avoidanceClearTimer = 0;
        private const float AvoidanceClearDelay = 1.0f;

        public abstract AutopilotBehaviors Behavior { get; }

        protected AutopilotBehavior(AutopilotComponent component)
        {
            Component = component;
        }

        protected readonly AutopilotComponent Component;
        protected GameObject Parent => Component.Parent;

        public abstract void ImGuiDebug();

        public abstract bool Update(
            ShipSteeringComponent control,
            ShipInputComponent? input,
            double time,
            GameWorld world);

        private void Start(GotoKind kind, float maxThrottle, float gotoRadius)
        {
            CanCruise = kind != GotoKind.GotoNoCruise;
            MaxThrottle = maxThrottle;
            GotoRadius = gotoRadius;
        }

        public void Start(GotoKind kind,
            GameObject targetObject,
            float maxThrottle,
            float gotoRadius)
        {
            TargetObject = targetObject;
            Start(kind, maxThrottle, gotoRadius);
        }

        public void Start(GotoKind kind,
            Vector3 targetPosition,
            float maxThrottle,
            float gotoRadius)
        {
            _targetPosition = targetPosition;
            _targetRadius = 5;
            Start(kind, maxThrottle, gotoRadius);
        }

        protected bool CanCruise;

        protected GameObject? TargetObject;
        private Vector3 _targetPosition;
        private float _targetRadius;

        protected float MaxThrottle;
        protected float GotoRadius;

        protected void SetThrottle(float throttle, ShipSteeringComponent control, ShipInputComponent? input)
        {
            input?.AutopilotThrottle = throttle;
            control.InThrottle = throttle;
        }

        protected void TriggerCruise(ShipSteeringComponent control, bool shouldCruise)
        {
            if (hasTriggeredCruise)
            {
                return;
            }

            hasTriggeredCruise = true;
            control.Cruise = CanCruise && shouldCruise;
            hasTriggeredCruise = true;
        }

        protected bool TargetValid() => TargetObject == null ||
                                        TargetObject.Flags.HasFlag(GameObjectFlags.Exists);

        protected bool Dockable(out DockInfoComponent? dockInfoComponent)
        {
            if (TargetObject != null)
            {
                return TargetObject.TryGetComponent(out dockInfoComponent);
            }

            dockInfoComponent = null;
            return false;
        }

        protected Vector3 GetTargetPoint()
        {
            return TargetObject == null ? _targetPosition : TargetObject.WorldTransform.Position;
        }

        protected float GetTargetRadius()
        {
            if (TargetObject == null)
            {
                return _targetRadius;
            }

            return TargetObject.PhysicsComponent!.Body!.Collider.Radius;
        }

        protected Hardpoint? GetTargetHardpoint(DockInfoComponent docking, bool reverse, int index)
        {
            var hps = docking.GetDockHardpoints(Parent.PhysicsComponent!.Body!.Position);

            if (reverse)
            {
                hps = hps.Reverse();
            }

            return hps.Skip(index).FirstOrDefault();
        }

        protected bool TurnTowards(double time, Vector3 targetPoint)
        {
            // Orientation
            var dt = time;
            var vec = Parent.InverseTransformPoint(targetPoint);
            // normalize it
            vec.Normalize();

            var directionSatisfied = (Math.Abs(vec.X) < 0.0015f && Math.Abs(vec.Y) < 0.0015f);

            if (!directionSatisfied)
            {
                Component.OutYaw = MathHelper.Clamp((float) Component.YawControl.Update(0, vec.X, dt), -1, 1);
                Component.OutPitch = MathHelper.Clamp((float) Component.PitchControl.Update(0, -vec.Y, dt), -1, 1);
                return false;
            }

            Component.OutYaw = 0;
            Component.OutPitch = 0;
            return true;
        }

        protected bool MoveToPoint(
            double time,
            Vector3 point,
            float radius,
            float range,
            float maxSpeed,
            bool shouldStop,
            ShipSteeringComponent control,
            ShipInputComponent? input,
            GameWorld world)
        {
            float targetPower = 0;
            // Bring ship to within GotoRange metres of target
            var targetRadius = GetTargetRadius();
            var myRadius = Parent.PhysicsComponent!.Body!.Collider.Radius;
            var distance = (point - Parent.PhysicsComponent.Body.Position).Length();

            TriggerCruise(control, (distance - range) > 2000);

            if ((distance - range) < 500)
            {
                control.Cruise = false; // Disable cruise at small distance
            }

            var distrad = targetRadius + myRadius + radius + range;
            var distanceSatisfied = distrad >= distance;

            if (distanceSatisfied && shouldStop)
            {
                targetPower = 0;
            }
            else
            {
                targetPower = maxSpeed;
            }

            if (targetPower > maxSpeed)
            {
                targetPower = maxSpeed;
            }

            input?.AutopilotThrottle = targetPower;
            control.InThrottle = targetPower;

            if (distanceSatisfied)
                return true;

            if (Behavior != AutopilotBehaviors.Undock)
            {
                var avoidance = GetAvoidancePlan(world, point, targetRadius + radius + range, time);
                Component.SetAutopilotStrafe(avoidance.Strafe, avoidance.StrafeVector);
            }

            var directionSatisfied = TurnTowards(time, point);


            return distanceSatisfied && directionSatisfied;
        }

        protected readonly struct AvoidancePlan(bool active, StrafeControls strafe, Vector2 strafeVector)
        {
            public readonly bool Active = active;
            public readonly StrafeControls Strafe = strafe;
            public readonly Vector2 StrafeVector = strafeVector;

            public static AvoidancePlan None => new(false, StrafeControls.None, Vector2.Zero);
        }

        protected AvoidancePlan GetAvoidancePlan(
            GameWorld world,
            Vector3 targetPoint,
            float destinationRadius,
            double time)
        {
            var body = Parent.PhysicsComponent?.Body;
            if (body == null)
            {
                return ClearAvoidance();
            }

            var toTarget = targetPoint - body.Position;
            var distanceToTarget = toTarget.Length();
            if (distanceToTarget < 1f)
            {
                return ClearAvoidance();
            }

            var pathDirection = toTarget / distanceToTarget;
            var up = body.RotateVector(Vector3.UnitY);
            var right = Vector3.Cross(pathDirection, up);
            if (right.LengthSquared() < 0.001f)
            {
                right = body.RotateVector(Vector3.UnitX);
            }
            else
            {
                right = Vector3.Normalize(right);
            }
            up = Vector3.Normalize(Vector3.Cross(right, pathDirection));

            var myRadius = body.Collider.Radius;
            var probeLength = MathHelper.Clamp(distanceToTarget - destinationRadius, 250f, 900f);
            if (probeLength <= 1f)
            {
                return ClearAvoidance();
            }

            var halfWidth = myRadius * 4f;
            var halfHeight = myRadius * 4f;
            var evadeOffset = MathF.Max(halfWidth, halfHeight) + myRadius + 120f;
            var queryRange = probeLength + evadeOffset + 250f;
            var candidates = CreateAvoidanceCandidates(evadeOffset);
            Span<float> scores = stackalloc float[candidates.Length];
            for (int i = 0; i < scores.Length; i++)
            {
                scores[i] = avoidanceVector != Vector2.Zero
                    ? -0.35f * MathF.Max(0, Vector2.Dot(candidates[i].Direction, avoidanceVector))
                    : 0f;
                scores[i] += MathF.Abs(candidates[i].Direction.X) * 0.0001f +
                             MathF.Abs(candidates[i].Direction.Y) * 0.0002f;
            }

            bool pathBlocked = false;
            foreach (var other in world.SpatialLookup.GetNearbyObjects(Parent, body.Position, queryRange))
            {
                if (!ShouldAvoid(other))
                {
                    continue;
                }

                var otherBody = other.PhysicsComponent?.Body;
                if (otherBody == null)
                {
                    continue;
                }

                var relative = otherBody.Position - body.Position;
                var ahead = Vector3.Dot(relative, pathDirection);
                var lateralX = Vector3.Dot(relative, right);
                var lateralY = Vector3.Dot(relative, up);
                var obstacleRadius = otherBody.Collider.Radius;
                if (ahead <= 0 || ahead > probeLength + obstacleRadius)
                {
                    continue;
                }

                var currentOverlapX = MathF.Max(0, halfWidth + obstacleRadius - MathF.Abs(lateralX));
                var currentOverlapY = MathF.Max(0, halfHeight + obstacleRadius - MathF.Abs(lateralY));
                if (currentOverlapX > 0 && currentOverlapY > 0)
                {
                    pathBlocked = true;
                    var away = new Vector2(-lateralX, -lateralY);
                    if (away.LengthSquared() > 0.001f)
                    {
                        away = Vector2.Normalize(away);
                        for (int i = 0; i < candidates.Length; i++)
                        {
                            scores[i] -= Vector2.Dot(candidates[i].Direction, away) *
                                         (currentOverlapX + currentOverlapY) / MathF.Max(ahead, 1f);
                        }
                    }
                }

                for (int i = 0; i < candidates.Length; i++)
                {
                    var shiftedX = lateralX - candidates[i].Offset.X;
                    var shiftedY = lateralY - candidates[i].Offset.Y;
                    var overlapX = MathF.Max(0, halfWidth + obstacleRadius - MathF.Abs(shiftedX));
                    var overlapY = MathF.Max(0, halfHeight + obstacleRadius - MathF.Abs(shiftedY));
                    var threat = (overlapX * overlapY) / MathF.Max(ahead, 1f);
                    scores[i] += threat;
                }
            }

            if (world.Physics != null)
            {
                pathBlocked |= ScoreRaycastAvoidance(
                    world,
                    body,
                    pathDirection,
                    right,
                    up,
                    candidates,
                    scores,
                    probeLength,
                    halfWidth,
                    halfHeight);
            }

            if (!pathBlocked)
            {
                var heldIndex = BestMatchingCandidate(candidates, avoidanceVector);
                DrawAvoidanceDebug(world, body.Position, pathDirection, right, up, probeLength, halfWidth, halfHeight,
                    candidates, heldIndex, false);
                if (avoidanceVector != Vector2.Zero)
                {
                    avoidanceClearTimer += (float)time;
                    if (avoidanceClearTimer < AvoidanceClearDelay)
                    {
                        return new AvoidancePlan(true, avoidanceStrafe, avoidanceVector);
                    }
                }
                ClearAvoidance();
                return AvoidancePlan.None;
            }

            int bestIndex = 0;
            for (int i = 1; i < scores.Length; i++)
            {
                if (scores[i] < scores[bestIndex])
                {
                    bestIndex = i;
                }
            }

            var best = candidates[bestIndex];
            avoidanceClearTimer = 0;
            avoidanceStrafe = best.Strafe;
            avoidanceVector = best.Direction;
            DrawAvoidanceDebug(world, body.Position, pathDirection, right, up, probeLength, halfWidth, halfHeight,
                candidates, bestIndex, true);
            return new AvoidancePlan(true, avoidanceStrafe, avoidanceVector);
        }

        private AvoidancePlan ClearAvoidance()
        {
            avoidanceStrafe = StrafeControls.None;
            avoidanceVector = Vector2.Zero;
            avoidanceClearTimer = 0;
            return AvoidancePlan.None;
        }

        private static int BestMatchingCandidate(
            (StrafeControls Strafe, Vector2 Direction, Vector2 Offset)[] candidates,
            Vector2 direction)
        {
            if (direction == Vector2.Zero)
            {
                return -1;
            }

            int best = 0;
            float bestScore = float.MinValue;
            for (int i = 0; i < candidates.Length; i++)
            {
                var score = Vector2.Dot(candidates[i].Direction, direction);
                if (score > bestScore)
                {
                    best = i;
                    bestScore = score;
                }
            }
            return best;
        }

        private static (StrafeControls Strafe, Vector2 Direction, Vector2 Offset)[] CreateAvoidanceCandidates(
            float evadeOffset)
        {
            var candidates = new (StrafeControls Strafe, Vector2 Direction, Vector2 Offset)[16];
            for (int i = 0; i < candidates.Length; i++)
            {
                var angle = i * (MathF.PI * 2f / candidates.Length);
                var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                candidates[i] = (VectorToStrafeControls(direction), direction, direction * evadeOffset);
            }
            return candidates;
        }

        private static StrafeControls VectorToStrafeControls(Vector2 direction)
        {
            var controls = StrafeControls.None;
            if (direction.X < -0.25f)
            {
                controls |= StrafeControls.Left;
            }
            else if (direction.X > 0.25f)
            {
                controls |= StrafeControls.Right;
            }

            if (direction.Y > 0.25f)
            {
                controls |= StrafeControls.Up;
            }
            else if (direction.Y < -0.25f)
            {
                controls |= StrafeControls.Down;
            }

            return controls;
        }

        private static void DrawAvoidanceDebug(
            GameWorld world,
            Vector3 origin,
            Vector3 pathDirection,
            Vector3 right,
            Vector3 up,
            float probeLength,
            float halfWidth,
            float halfHeight,
            (StrafeControls Strafe, Vector2 Direction, Vector2 Offset)[] candidates,
            int bestIndex,
            bool pathBlocked)
        {
            var end = origin + pathDirection * probeLength;
            var directColor = pathBlocked ? Color4.Red : Color4.Cyan;
            var edgeColor = pathBlocked ? Color4.Orange : Color4.DarkCyan;

            world.DrawDebugLine(origin, end, directColor);
            DrawDebugBox(world, origin, end, right, up, halfWidth, halfHeight, edgeColor);

            for (int i = 0; i < candidates.Length; i++)
            {
                var laneOrigin = origin + right * candidates[i].Offset.X + up * candidates[i].Offset.Y;
                var laneEnd = laneOrigin + pathDirection * probeLength;
                var color = i == bestIndex ? Color4.Lime : Color4.SlateGray;
                world.DrawDebugLine(laneOrigin, laneEnd, color);
            }
        }

        private static void DrawDebugBox(
            GameWorld world,
            Vector3 origin,
            Vector3 end,
            Vector3 right,
            Vector3 up,
            float halfWidth,
            float halfHeight,
            Color4 color)
        {
            var nearA = origin + right * halfWidth + up * halfHeight;
            var nearB = origin - right * halfWidth + up * halfHeight;
            var nearC = origin - right * halfWidth - up * halfHeight;
            var nearD = origin + right * halfWidth - up * halfHeight;
            var farA = end + right * halfWidth + up * halfHeight;
            var farB = end - right * halfWidth + up * halfHeight;
            var farC = end - right * halfWidth - up * halfHeight;
            var farD = end + right * halfWidth - up * halfHeight;

            world.DrawDebugLine(nearA, nearB, color);
            world.DrawDebugLine(nearB, nearC, color);
            world.DrawDebugLine(nearC, nearD, color);
            world.DrawDebugLine(nearD, nearA, color);
            world.DrawDebugLine(farA, farB, color);
            world.DrawDebugLine(farB, farC, color);
            world.DrawDebugLine(farC, farD, color);
            world.DrawDebugLine(farD, farA, color);
            world.DrawDebugLine(nearA, farA, color);
            world.DrawDebugLine(nearB, farB, color);
            world.DrawDebugLine(nearC, farC, color);
            world.DrawDebugLine(nearD, farD, color);
        }

        private bool ScoreRaycastAvoidance(
            GameWorld world,
            LibreLancer.Physics.PhysicsObject body,
            Vector3 pathDirection,
            Vector3 right,
            Vector3 up,
            (StrafeControls Strafe, Vector2 Direction, Vector2 Offset)[] candidates,
            Span<float> scores,
            float probeLength,
            float halfWidth,
            float halfHeight)
        {
            var pathBlocked = RaycastBlocked(world, body, body.Position, pathDirection, probeLength, out _);
            if (RaycastBlocked(world, body, body.Position + right * halfWidth, pathDirection, probeLength, out _))
            {
                pathBlocked = true;
                PenalizeDirection(candidates, scores, new Vector2(1, 0), 3.5f);
            }
            if (RaycastBlocked(world, body, body.Position - right * halfWidth, pathDirection, probeLength, out _))
            {
                pathBlocked = true;
                PenalizeDirection(candidates, scores, new Vector2(-1, 0), 3.5f);
            }
            if (RaycastBlocked(world, body, body.Position + up * halfHeight, pathDirection, probeLength, out _))
            {
                pathBlocked = true;
                PenalizeDirection(candidates, scores, new Vector2(0, 1), 3.5f);
            }
            if (RaycastBlocked(world, body, body.Position - up * halfHeight, pathDirection, probeLength, out _))
            {
                pathBlocked = true;
                PenalizeDirection(candidates, scores, new Vector2(0, -1), 3.5f);
            }
            for (int i = 0; i < candidates.Length; i++)
            {
                var origin = body.Position + right * candidates[i].Offset.X + up * candidates[i].Offset.Y;
                if (RaycastBlocked(world, body, origin, pathDirection, probeLength, out var hitDistance))
                {
                    scores[i] += 8f + ((probeLength - hitDistance + 1f) / probeLength) * 8f;
                }
            }

            return pathBlocked;
        }

        private static void PenalizeDirection(
            (StrafeControls Strafe, Vector2 Direction, Vector2 Offset)[] candidates,
            Span<float> scores,
            Vector2 blockedDirection,
            float amount)
        {
            for (int i = 0; i < candidates.Length; i++)
            {
                scores[i] += MathF.Max(0, Vector2.Dot(candidates[i].Direction, blockedDirection)) * amount;
            }
        }

        private bool RaycastBlocked(
            GameWorld world,
            LibreLancer.Physics.PhysicsObject body,
            Vector3 origin,
            Vector3 direction,
            float probeLength,
            out float hitDistance)
        {
            hitDistance = 0;
            if (!world.Physics!.PointRaycast(body, origin, direction, probeLength, out var contactPoint, out var hitObject,
                    out _))
            {
                return false;
            }

            if (hitObject?.Tag is GameObject hitGameObject && !ShouldAvoid(hitGameObject))
            {
                return false;
            }

            hitDistance = Vector3.Distance(origin, contactPoint);
            return true;
        }

        private bool ShouldAvoid(GameObject other)
        {
            if (!other.Flags.HasFlag(GameObjectFlags.Exists) ||
                other == TargetObject ||
                other.Kind is GameObjectKind.Waypoint or GameObjectKind.Missile or GameObjectKind.Loot)
            {
                return false;
            }

            var formationLead = Parent.Formation?.LeadShip;
            if (formationLead != null && other == formationLead)
            {
                return false;
            }

            return other.PhysicsComponent?.Body != null;
        }
    }

    internal sealed class DockBehavior(AutopilotComponent c) : AutopilotBehavior(c)
    {
        public override AutopilotBehaviors Behavior => AutopilotBehaviors.Dock;

        private int lastTargetHp = 0;

        public override void ImGuiDebug()
        {
            if (Dockable(out var docking))
            {
                ImGui.Text($"Docking with: {docking?.Parent}");
                var hp = GetTargetHardpoint(docking!, false, lastTargetHp);
                ImGui.Text($"Target hardpoint: {hp?.Name}");
            }
        }

        public override bool Update(ShipSteeringComponent control, ShipInputComponent? input, double time, GameWorld world)
        {
            if (!TargetValid() || !Dockable(out var docking))
            {
                return true; // finished
            }

            var hp = GetTargetHardpoint(docking!, false, lastTargetHp);

            if (hp == null)
            {
                // No dock hardpoints available, cancel docking
                FLLog.Error("Autopilot", $"No dock hardpoints available for {Parent.Nickname} docking");
                return true; // finished
            }

            float radius = 5;
            var maxSpeed = 1f;
            var targetPoint = (hp.Transform * TargetObject!.WorldTransform).Position;

            if (lastTargetHp > 0)
            {
                maxSpeed = 0.3f;
            }

            if (lastTargetHp == 2)
            {
                radius = docking!.GetTriggerRadius();
            }

            var d2 = (targetPoint - Parent.PhysicsComponent!.Body!.Position).Length();

            if (d2 < 80)
            {
                maxSpeed = 0.3f;
            }

            if (!MoveToPoint(time, targetPoint, radius, GotoRadius, maxSpeed, true, control, input, world))
            {
                return false; // not finished
            }

            if (lastTargetHp < 2)
            {
                lastTargetHp++;
            }
            else
            {
                SetThrottle(1, control, input);
            }

            return false; // not finished
        }
    }

    internal sealed class UndockBehavior(AutopilotComponent c, int index) : AutopilotBehavior(c)
    {
        public override AutopilotBehaviors Behavior => AutopilotBehaviors.Undock;

        private const double MAX_TIME_UNDOCK = 8.0;

        private double totalTime = 0.0;
        private double delay = 1.2;

        public override void ImGuiDebug()
        {
            ImGui.Text("Undocking");
        }

        public override bool Update(ShipSteeringComponent control,
            ShipInputComponent? input,
            double time,
            GameWorld world)
        {
            if (delay > 0)
            {
                SetThrottle(0, control, input);
                Component.OutPitch = 0;
                Component.OutYaw = 0;
                delay -= time;
                return false; // not finished
            }

            totalTime += time;

            if (totalTime > MAX_TIME_UNDOCK)
            {
                FLLog.Warning("Autopilot", $"Undock force quit at {totalTime}");
                return true; // finished
            }

            if (!TargetValid() ||
                !Dockable(out var docking))
            {
                return true; // finished
            }

            var info = docking!.GetUndockInfo(index);
            var targetPoint = (info.End!.Transform * TargetObject!.WorldTransform).Position;
            var startPoint = (info.Start!.Transform * TargetObject.WorldTransform).Position;
            return MoveToPoint(time, targetPoint, 25, 10, 1f, false, control, input, world) ||
                   Vector3.Distance(startPoint, targetPoint) - 20 <
                   Vector3.Distance(Parent.LocalTransform.Position, startPoint);
        }
    }

    internal sealed class GotoBehavior(AutopilotComponent c) : AutopilotBehavior(c)
    {
        public override AutopilotBehaviors Behavior => AutopilotBehaviors.Goto;

        public override bool Update(ShipSteeringComponent control, ShipInputComponent? input, double time, GameWorld world)
        {
            if (!TargetValid())
            {
                return true;
            }

            return MoveToPoint(time, GetTargetPoint(), GetTargetRadius(), GotoRadius, MaxThrottle, true, control,
                input, world);
        }

        public override void ImGuiDebug()
        {
            ImGui.Text($"Goto: {GetTargetPoint()}");
        }
    }

    internal sealed class FormationBehavior(AutopilotComponent c) : AutopilotBehavior(c)
    {
        public override AutopilotBehaviors Behavior => AutopilotBehaviors.Formation;

        private const float CruiseDampenDistance = 250;
        private const float ThrottleDampenDistance = 125;
        private const float LeaderCruiseCatchupDistance = 450;
        private const float FormationSpeedBoost = 0.2f;
        private const float MaxFormationCruiseSpeedReduction = 5;
        private const float FormationFacingDistance = 100;
        private const float ThrottleMatchDistance = 100;
        private const float MinCatchupThrottle = 0.35f;

        private static bool LeadIsCruising(GameObject lead)
        {
            if (lead.TryGetComponent<ShipPhysicsComponent>(out var leadControl) &&
                (leadControl.CruiseEnabled ||
                 leadControl.EngineState is EngineStates.Cruise or EngineStates.CruiseCharging))
            {
                return true;
            }

            return lead.TryGetComponent<CEngineComponent>(out var eng) &&
                   eng.CruiseThrust is CruiseThrustState.Cruising or CruiseThrustState.CruiseCharging;
        }

        private static float LeadThrottle(GameObject lead)
        {
            if (lead.TryGetComponent<ShipPhysicsComponent>(out var leadControl))
            {
                return leadControl.EnginePower;
            }

            if (lead.TryGetComponent<CEngineComponent>(out var eng))
            {
                return MathHelper.Clamp(eng.Speed / 0.9f, 0, 1);
            }

            return 0;
        }

        private static float ApproachThrottle(float distance, float leadThrottle)
        {
            if (distance > ThrottleDampenDistance)
            {
                return 1 + FormationSpeedBoost;
            }

            if (distance > ThrottleMatchDistance)
            {
                var throttle = MathHelper.Lerp(MinCatchupThrottle, 1, distance / ThrottleDampenDistance);
                return MathF.Max(leadThrottle, throttle);
            }

            return leadThrottle;
        }

        private static float CruiseSpeedOffset(float distance)
        {
            var dampen = 1 - MathHelper.Clamp(distance / CruiseDampenDistance, 0, 1);
            return -MaxFormationCruiseSpeedReduction * dampen;
        }

        private static Vector3 ShipPosition(GameObject ship) =>
            ship.PhysicsComponent?.Body?.Position ?? ship.WorldTransform.Position;

        private static float ShipSpeed(GameObject ship)
        {
            return ship.PhysicsComponent?.Body?.LinearVelocity.Length() ?? 0;
        }

        private bool ShouldCruise(GameObject lead)
        {
            var leaderDistance = Vector3.Distance(ShipPosition(lead), Parent.PhysicsComponent!.Body!.Position);
            return LeadIsCruising(lead) || leaderDistance > LeaderCruiseCatchupDistance;
        }

        private static Vector3? FormationFacingPoint(GameObject self, GameObject lead, Vector3 targetPoint, float distance)
        {
            if (distance > FormationFacingDistance)
            {
                return targetPoint;
            }

            if (ShipSpeed(self) > ShipSpeed(lead) + 20)
            {
                return targetPoint;
            }

            var leadVelocity = lead.PhysicsComponent?.Body?.LinearVelocity ?? Vector3.Zero;
            if (leadVelocity.LengthSquared() > 400)
            {
                return self.WorldTransform.Position + Vector3.Normalize(leadVelocity) * 1000;
            }

            return null;
        }

        public override void ImGuiDebug()
        {
            ImGui.Text("In Formation");
        }

        public override bool Update(ShipSteeringComponent control, ShipInputComponent? input, double time, GameWorld world)
        {
            if (Parent.Formation == null ||
                Parent.Formation.LeadShip == Parent)
            {
                return true;
            }

            var body = Parent.PhysicsComponent!.Body!;
            var targetPoint = Parent.Formation.GetShipPosition(Parent, Component.LocalPlayer);
            var distance = (targetPoint - body.Position).Length();
            var lead = Parent.Formation.LeadShip;
            var leadThrottle = LeadThrottle(lead);
            var selfPhysics = Parent.GetComponent<ShipPhysicsComponent>();

            control.Cruise = ShouldCruise(lead);
            control.CruiseSpeedOffset =
                control.Cruise &&
                selfPhysics?.EngineState == EngineStates.Cruise &&
                distance < CruiseDampenDistance
                    ? CruiseSpeedOffset(distance)
                    : 0;
            SetThrottle(ApproachThrottle(distance, leadThrottle), control, input);

            var avoidance = GetAvoidancePlan(world, targetPoint, ThrottleDampenDistance, time);
            Component.SetAutopilotStrafe(avoidance.Strafe, avoidance.StrafeVector);

            Vector3? facingPoint = FormationFacingPoint(Parent, lead, targetPoint, distance);
            if (facingPoint != null)
            {
                TurnTowards(time, facingPoint.Value);
            }
            else
            {
                Component.OutYaw = 0;
                Component.OutPitch = 0;
            }

            return false;
        }
    }


    public class AutopilotComponent : GameComponent
    {
        private AutopilotBehavior? instance;
        public bool LocalPlayer = false;
        internal StrafeControls AutopilotStrafe { get; private set; } = StrafeControls.None;
        internal Vector2 AutopilotStrafeVector { get; private set; } = Vector2.Zero;

        public AutopilotBehaviors CurrentBehavior
            => instance?.Behavior ?? AutopilotBehaviors.None;

        public readonly PIDController PitchControl = new();
        public readonly PIDController YawControl = new();

        public float OutPitch;
        public float OutYaw;

        public AutopilotComponent(GameObject parent) : base(parent)
        {
            PitchControl.P = 4;
            YawControl.P = 4;
        }

        public delegate void BehaviorChangedCallback(AutopilotBehaviors newBehavior, AutopilotBehaviors oldBehavior);

        public BehaviorChangedCallback? BehaviorChanged;

        private void SetInstance(AutopilotBehavior? i)
        {
            var old = CurrentBehavior;
            instance = i;

            if (CurrentBehavior != old)
            {
                BehaviorChanged?.Invoke(CurrentBehavior, old);
            }
        }

        public void ImGuiDebug()
        {
            ImGui.Text($"Autopilot: {CurrentBehavior}");
            instance?.ImGuiDebug();
        }

        public void GotoVec(Vector3 vec, GotoKind kind, float maxThrottle = 1, float gotoRange = 40)
        {
            SetInstance(new GotoBehavior(this));
            instance?.Start(kind, vec, maxThrottle, gotoRange);
        }

        public void GotoObject(GameObject obj, GotoKind kind, float maxThrottle = 1, float gotoRange = 40)
        {
            SetInstance(new GotoBehavior(this));
            instance?.Start(kind, obj, maxThrottle, gotoRange);
        }

        public void Cancel()
        {
            SetInstance(null);
            SetAutopilotStrafe(StrafeControls.None, Vector2.Zero);
        }

        public void StartDock(GameObject target, GotoKind kind)
        {
            SetInstance(new DockBehavior(this));
            instance?.Start(kind, target, 1, 40);
        }

        public void Undock(GameObject target, int index)
        {
            SetInstance(new UndockBehavior(this, index));
            instance?.Start(GotoKind.GotoNoCruise,
                target, 1, 10);
        }

        public void StartFormation()
        {
            if (Parent.TryGetComponent<ShipPhysicsComponent>(out var physics))
            {
                physics.StopCruise();
            }

            if (Parent.TryGetComponent<ShipSteeringComponent>(out var steering))
            {
                steering.Cruise = false;
                steering.CruiseSpeedOffset = 0;
                steering.InThrottle = 1;
            }

            if (Parent.TryGetComponent<ShipInputComponent>(out var input))
            {
                input.AutopilotThrottle = 1;
                input.InFormation = true;
            }

            SetInstance(new FormationBehavior(this));
            instance?.Start(GotoKind.Goto, Vector3.Zero, 1, 10);
        }

        public override void Update(double time, GameWorld world)
        {
            var control = Parent?.GetComponent<ShipSteeringComponent>();
            var input = Parent?.GetComponent<ShipInputComponent>();
            SetAutopilotStrafe(StrafeControls.None, Vector2.Zero);

            if (input != null)
            {
                input.AutopilotThrottle = 0;
                input.InFormation = CurrentBehavior == AutopilotBehaviors.Formation;
            }

            if (control == null)
            {
                return;
            }

            control.CruiseSpeedOffset = 0;

            if (instance == null)
            {
                return;
            }

            if (instance.Update(control, input, time, world))
            {
                SetInstance(null);
                SetAutopilotStrafe(StrafeControls.None, Vector2.Zero);
            }
        }

        internal void SetAutopilotStrafe(StrafeControls strafe)
        {
            SetAutopilotStrafe(strafe, Vector2.Zero);
        }

        internal void SetAutopilotStrafe(StrafeControls strafe, Vector2 strafeVector)
        {
            AutopilotStrafe = strafe;
            AutopilotStrafeVector = strafeVector;
            if (Parent.TryGetComponent<ShipPhysicsComponent>(out var physics))
            {
                physics.CurrentStrafe = strafe;
                physics.AutopilotStrafeVector = strafeVector;
                physics.AutopilotCruiseStrafe = strafeVector != Vector2.Zero || strafe != StrafeControls.None;
            }
        }
    }
}
