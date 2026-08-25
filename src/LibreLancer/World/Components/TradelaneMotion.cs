// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.Items;

namespace LibreLancer.World.Components;

public static class TradelaneMotion
{
    public const float Speed = 2500f;
    public const float SpeedupDuration = 8f;
    public const float SlowdownDuration = 7.5f;
    public const float ExitSpeed = 400f;
    public const float ManualExitDuration = 0.75f;
    public const float ManualTurnDuration = 0.5f;
    public const float ManualTurnDegrees = 30f;
    public const float EntryManeuverDistance = 500f;
    public const float EntryAlignmentDuration = 0.5f;

    public static float NormalThrottleSpeed(Ship ship, EngineEquipment engine)
    {
        var drag = ship.LinearDrag + engine.Def.LinearDrag;
        if (drag <= float.Epsilon || engine.Def.MaxForce <= 0)
        {
            return Speed;
        }

        return MathF.Max(0, engine.Def.MaxForce / drag);
    }

    public static float SlowdownSpeed(float elapsedTime)
    {
        var t = MathHelper.Clamp(elapsedTime / SlowdownDuration, 0, 1);
        return Easing.Ease(EasingTypes.EaseOut, t, 0, 1, Speed, ExitSpeed);
    }

    public static float SpeedupSpeed(float elapsedTime, float startingSpeed) =>
        Easing.Ease(
            EasingTypes.EaseInOut,
            MathHelper.Clamp(elapsedTime, 0, SpeedupDuration),
            0,
            SpeedupDuration,
            MathHelper.Clamp(startingSpeed, 0, Speed),
            Speed);

    public static Vector3 EntryPathPoint(
        Vector3 start,
        Vector3 startControl,
        Vector3 endControl,
        Vector3 hardpoint,
        float progress)
    {
        var t = MathHelper.Clamp(progress, 0, 1);
        var inverse = 1 - t;
        return inverse * inverse * inverse * start +
               3 * inverse * inverse * t * startControl +
               3 * inverse * t * t * endControl +
               t * t * t * hardpoint;
    }

    public static Vector3 EntryPathTangent(
        Vector3 start,
        Vector3 startControl,
        Vector3 endControl,
        Vector3 hardpoint,
        float progress)
    {
        var t = MathHelper.Clamp(progress, 0, 1);
        var inverse = 1 - t;
        return 3 * inverse * inverse * (startControl - start) +
               6 * inverse * t * (endControl - startControl) +
               3 * t * t * (hardpoint - endControl);
    }

    public static Quaternion EntryOrientation(
        Quaternion startOrientation,
        Quaternion targetOrientation,
        float elapsedTime)
    {
        var t = MathHelper.Clamp(elapsedTime / EntryAlignmentDuration, 0, 1);
        t *= t * (3 - 2 * t);
        return Quaternion.Normalize(Quaternion.Slerp(startOrientation, targetOrientation, t));
    }

    public static Vector3 EntryCapturePoint(
        Vector3 hardpointPosition,
        Vector3 laneDirection,
        float forwardTolerance)
    {
        if (laneDirection.LengthSquared() <= float.Epsilon)
        {
            return hardpointPosition;
        }

        return hardpointPosition -
               Vector3.Normalize(laneDirection) * MathF.Max(0, forwardTolerance);
    }

    public static bool HasCrossedEntryPlane(
        Vector3 shipPosition,
        Vector3 hardpointPosition,
        Vector3 laneDirection,
        float lateralRadius,
        float forwardTolerance)
    {
        if (laneDirection.LengthSquared() <= float.Epsilon)
        {
            return false;
        }

        var axis = Vector3.Normalize(laneDirection);
        var offset = shipPosition - hardpointPosition;
        var axialDistance = Vector3.Dot(offset, axis);
        if (axialDistance < -MathF.Max(0, forwardTolerance) ||
            axialDistance > EntryManeuverDistance)
        {
            return false;
        }

        var lateral = offset - axis * axialDistance;
        return lateral.Length() <= lateralRadius;
    }

    public static Quaternion OrientationForDirection(
        Vector3 direction,
        Quaternion referenceOrientation)
    {
        if (direction.LengthSquared() <= float.Epsilon)
        {
            return referenceOrientation;
        }

        direction = Vector3.Normalize(direction);
        var up = Vector3.Transform(Vector3.UnitY, referenceOrientation);
        up -= direction * Vector3.Dot(up, direction);

        if (up.LengthSquared() <= float.Epsilon)
        {
            up = Vector3.UnitY - direction * Vector3.Dot(Vector3.UnitY, direction);
        }

        if (up.LengthSquared() <= float.Epsilon)
        {
            up = Vector3.UnitX - direction * Vector3.Dot(Vector3.UnitX, direction);
        }

        return Quaternion.Normalize(QuaternionEx.LookRotation(-direction, Vector3.Normalize(up)));
    }

    public static Quaternion ManualExitOrientation(
        Quaternion startOrientation, Quaternion targetOrientation, float progress)
    {
        var t = MathHelper.Clamp(progress, 0, 1);
        t *= t * (3 - 2 * t);
        var startForward = Forward(startOrientation);
        var targetForward = Forward(targetOrientation);
        startForward.Y = 0;
        targetForward.Y = 0;

        if (startForward.LengthSquared() <= float.Epsilon || targetForward.LengthSquared() <= float.Epsilon)
        {
            var turned = Quaternion.Slerp(startOrientation, targetOrientation, t);
            return QuaternionEx.LookAt(Vector3.Zero, Forward(turned));
        }

        var forward = Vector3.Normalize(Vector3.Lerp(
            Vector3.Normalize(startForward), Vector3.Normalize(targetForward), t));

        // Keep the exit as a flat yaw turn
        return QuaternionEx.LookAt(Vector3.Zero, forward);
    }

    public static float ManualExitSpeed(float progress, float currentSpeed, float normalThrottleSpeed)
    {
        var target = MathF.Min(Speed, MathF.Max(0, normalThrottleSpeed));
        var t = MathHelper.Clamp(progress, 0, 1);
        t *= t * (3 - 2 * t);
        return MathHelper.Lerp(currentSpeed, target, t);
    }

    public static bool CanStartAutomaticSlowdown(
        bool nextIsFinal,
        bool enteredAtPenultimate = false) =>
        nextIsFinal && !enteredAtPenultimate;

    public static Vector3 Forward(Quaternion orientation) =>
        Vector3.Transform(-Vector3.UnitZ, orientation).Normalized();

    public static Quaternion TurnRight(Quaternion orientation, float degrees)
    {
        var up = Vector3.Transform(Vector3.UnitY, orientation).Normalized();
        var right = Vector3.Transform(Vector3.UnitX, orientation).Normalized();
        var angle = MathHelper.DegreesToRadians(degrees);
        var turned = Quaternion.Normalize(Quaternion.CreateFromAxisAngle(up, angle) * orientation);

        if (Vector3.Dot(Forward(turned), right) < 0)
        {
            turned = Quaternion.Normalize(Quaternion.CreateFromAxisAngle(up, -angle) * orientation);
        }

        return turned;
    }
}
