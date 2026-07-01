// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer.World.Components;

internal static class FormationControl
{
    internal const float SeparationClearance = 15f;
    internal const float SeparationPredictionTime = 1.5f;
    internal const float SeparationHoldTime = 0.25f;
    internal const float ArrivalDampenDistance = 125f;
    internal const float ArrivalStopDistance = 100f;

    private const float PositionGain = 0.75f;
    private const float MaximumCorrectionSpeed = 120f;
    private const float ThrottleSpeedRange = 40f;
    private const float CruisePositionGain = 0.1f;
    private const float CruiseVelocityGain = 0.75f;
    private const float MaximumCruiseCorrection = 0.2f;
    internal readonly record struct Neighbor(Vector3 Position, Vector3 Velocity, float Radius, int StableId);
    internal readonly record struct Separation(Vector3 Direction, int NeighborId, float Brake = 0)
    {
        public bool Active => Direction.LengthSquared() > 0.0001f;
        public static Separation None => new(Vector3.Zero, 0);
    }

    internal static Vector3 SlotVelocity(Vector3 leaderVelocity, Vector3 leaderAngularVelocity,
        Vector3 worldOffset) => leaderVelocity + Vector3.Cross(leaderAngularVelocity, worldOffset);

    internal static Vector3 DesiredVelocity(Vector3 slotVelocity, Vector3 slotError) =>
        slotVelocity + ClampLength(slotError * PositionGain, MaximumCorrectionSpeed);

    internal static float StandardThrottle(float leaderThrottle, Vector3 desiredVelocity, Vector3 selfVelocity,
        Vector3 selfForward)
    {
        var speedCorrection = Vector3.Dot(desiredVelocity - selfVelocity, selfForward) / ThrottleSpeedRange;
        var throttle = MathHelper.Clamp(leaderThrottle + speedCorrection, 0, 1.2f);
        if (desiredVelocity.LengthSquared() < 1f)
        {
            return throttle;
        }

        var alignment = Vector3.Dot(selfForward, Vector3.Normalize(desiredVelocity));
        var alignmentScale = MathHelper.Clamp((alignment + 0.25f) / 1.25f, 0, 1);
        return throttle * alignmentScale;
    }

    internal static float ArrivalThrottle(float throttle, float distanceToSlot) =>
        throttle * MathHelper.Clamp(
            (distanceToSlot - ArrivalStopDistance) / (ArrivalDampenDistance - ArrivalStopDistance), 0, 1);

    internal static float CruiseSpeedOffset(Vector3 slotError, Vector3 selfVelocity, Vector3 slotVelocity,
        Vector3 travelDirection, float cruiseSpeed)
    {
        if (travelDirection.LengthSquared() < 0.0001f || cruiseSpeed <= 0)
        {
            return 0;
        }

        travelDirection = Vector3.Normalize(travelDirection);
        var positionError = Vector3.Dot(slotError, travelDirection);
        var relativeSpeed = Vector3.Dot(selfVelocity - slotVelocity, travelDirection);
        var correction = positionError * CruisePositionGain - relativeSpeed * CruiseVelocityGain;
        return MathHelper.Clamp(correction, -cruiseSpeed * MaximumCruiseCorrection,
            cruiseSpeed * MaximumCruiseCorrection);
    }

    internal static Separation CalculateSeparation(Vector3 selfPosition, Vector3 selfVelocity, Vector3 selfForward,
        float selfRadius, int selfId, ReadOnlySpan<Neighbor> neighbors)
    {
        var separation = Vector3.Zero;
        var strongestWeight = 0f;
        var strongestId = 0;
        var strongestDirection = Vector3.Zero;
        var brake = 0f;

        foreach (var neighbor in neighbors)
        {
            var relativePosition = neighbor.Position - selfPosition;
            var relativeVelocity = neighbor.Velocity - selfVelocity;
            var velocitySquared = relativeVelocity.LengthSquared();
            var closestTime = velocitySquared > 0.0001f
                ? MathHelper.Clamp(-Vector3.Dot(relativePosition, relativeVelocity) / velocitySquared,
                    0, SeparationPredictionTime)
                : 0;
            var predictedRelative = relativePosition + relativeVelocity * closestTime;
            var distance = predictedRelative.Length();
            var minimumDistance = MathF.Max(0, selfRadius) + MathF.Max(0, neighbor.Radius) + SeparationClearance;
            if (distance >= minimumDistance)
            {
                continue;
            }

            var away = distance > 0.0001f
                ? -predictedRelative / distance
                : StablePairDirection(selfId, neighbor.StableId);
            var weight = (minimumDistance - distance) / MathF.Max(minimumDistance, 1f);
            separation += away * weight;

            var currentDistance = relativePosition.Length();
            var currentDirection = currentDistance > 0.0001f
                ? relativePosition / currentDistance
                : -away;
            var ahead = MathF.Max(0, Vector3.Dot(currentDirection, selfForward));
            var closingSpeed = MathF.Max(0, -Vector3.Dot(relativeVelocity, currentDirection));
            brake = MathF.Max(brake,
                ahead * MathHelper.Clamp(weight + closingSpeed / 40f, 0, 1));
            if (weight > strongestWeight)
            {
                strongestWeight = weight;
                strongestId = neighbor.StableId;
                strongestDirection = away;
            }
        }

        if (separation.LengthSquared() <= 0.0001f && brake > 0)
            separation = strongestDirection;
        return separation.LengthSquared() > 0.0001f
            ? new Separation(Vector3.Normalize(separation), strongestId, brake)
            : Separation.None;
    }

    internal static Separation ApplySeparationHysteresis(Separation current, ref Vector3 heldDirection,
        ref int heldNeighbor, ref float holdTimer, float elapsed)
    {
        if (current.Active)
        {
            heldDirection = current.Direction;
            heldNeighbor = current.NeighborId;
            holdTimer = SeparationHoldTime;
            return current;
        }

        holdTimer -= elapsed;
        if (holdTimer > 0 && heldDirection.LengthSquared() > 0.0001f)
            return new Separation(heldDirection, heldNeighbor);

        heldDirection = Vector3.Zero;
        heldNeighbor = 0;
        holdTimer = 0;
        return Separation.None;
    }

    internal static Vector3 ClampLength(Vector3 value, float maximum)
    {
        var lengthSquared = value.LengthSquared();
        if (lengthSquared <= maximum * maximum)
        {
            return value;
        }
        return Vector3.Normalize(value) * maximum;
    }

    private static Vector3 StablePairDirection(int selfId, int neighborId)
    {
        var sign = selfId <= neighborId ? -1f : 1f;
        return Vector3.UnitX * sign;
    }
}
