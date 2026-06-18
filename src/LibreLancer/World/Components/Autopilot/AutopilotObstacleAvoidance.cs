// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Client.Components;
using LibreLancer.Physics;
using LibreLancer.Render;

namespace LibreLancer.World.Components
{
    internal sealed class AutopilotObstacleAvoidance
    {
        private StrafeControls avoidanceStrafe = StrafeControls.None;
        private Vector2 avoidanceVector = Vector2.Zero;
        private float avoidanceClearTimer = 0;
        private const float AvoidanceClearDelay = 1.0f;

        internal readonly struct AvoidancePlan(bool active, StrafeControls strafe, Vector2 strafeVector)
        {
            public readonly bool Active = active;
            public readonly StrafeControls Strafe = strafe;
            public readonly Vector2 StrafeVector = strafeVector;

            public static AvoidancePlan None => new(false, StrafeControls.None, Vector2.Zero);
        }

        public AvoidancePlan GetPlan(
            GameWorld world,
            GameObject parent,
            GameObject? targetObject,
            Vector3 targetPoint,
            float destinationRadius,
            double time)
        {
            var body = parent.PhysicsComponent?.Body;
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
            foreach (var other in world.SpatialLookup.GetNearbyObjects(parent, body.Position, queryRange))
            {
                if (!ShouldAvoid(parent, targetObject, other))
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
                    parent,
                    body,
                    pathDirection,
                    right,
                    up,
                    candidates,
                    scores,
                    probeLength,
                    halfWidth,
                    halfHeight,
                    targetObject);
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
                return ClearAvoidance();
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
            GameObject parent,
            PhysicsObject body,
            Vector3 pathDirection,
            Vector3 right,
            Vector3 up,
            (StrafeControls Strafe, Vector2 Direction, Vector2 Offset)[] candidates,
            Span<float> scores,
            float probeLength,
            float halfWidth,
            float halfHeight,
            GameObject? targetObject)
        {
            var pathBlocked = RaycastBlocked(world, parent, targetObject, body, body.Position, pathDirection, probeLength,
                out _);
            if (RaycastBlocked(world, parent, targetObject, body, body.Position + right * halfWidth, pathDirection,
                    probeLength, out _))
            {
                pathBlocked = true;
                PenalizeDirection(candidates, scores, new Vector2(1, 0), 3.5f);
            }
            if (RaycastBlocked(world, parent, targetObject, body, body.Position - right * halfWidth, pathDirection,
                    probeLength, out _))
            {
                pathBlocked = true;
                PenalizeDirection(candidates, scores, new Vector2(-1, 0), 3.5f);
            }
            if (RaycastBlocked(world, parent, targetObject, body, body.Position + up * halfHeight, pathDirection,
                    probeLength, out _))
            {
                pathBlocked = true;
                PenalizeDirection(candidates, scores, new Vector2(0, 1), 3.5f);
            }
            if (RaycastBlocked(world, parent, targetObject, body, body.Position - up * halfHeight, pathDirection,
                    probeLength, out _))
            {
                pathBlocked = true;
                PenalizeDirection(candidates, scores, new Vector2(0, -1), 3.5f);
            }
            for (int i = 0; i < candidates.Length; i++)
            {
                var origin = body.Position + right * candidates[i].Offset.X + up * candidates[i].Offset.Y;
                if (RaycastBlocked(world, parent, targetObject, body, origin, pathDirection, probeLength, out var hitDistance))
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
            GameObject parent,
            GameObject? targetObject,
            PhysicsObject body,
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

            if (hitObject?.Tag is GameObject hitGameObject && !ShouldAvoid(parent, targetObject, hitGameObject))
            {
                return false;
            }

            hitDistance = Vector3.Distance(origin, contactPoint);
            return true;
        }

        private static bool ShouldAvoid(GameObject parent, GameObject? targetObject, GameObject other)
        {
            if (!other.Flags.HasFlag(GameObjectFlags.Exists) ||
                other == targetObject ||
                other.Kind is GameObjectKind.Waypoint or GameObjectKind.Missile or GameObjectKind.Loot)
            {
                return false;
            }

            var formationLead = parent.Formation?.LeadShip;
            if (formationLead != null && other == formationLead)
            {
                return false;
            }

            return other.PhysicsComponent?.Body != null;
        }
    }
}
