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
    public const float SlowdownStartDistance = 3000f;
    public const float ManualExitDuration = 0.75f;
    public const float ManualTurnDuration = 0.5f;
    public const float ManualTurnDegrees = 30f;

    public static float NormalThrottleSpeed(Ship ship, EngineEquipment engine)
    {
        var drag = ship.LinearDrag + engine.Def.LinearDrag;
        if (drag <= float.Epsilon || engine.Def.MaxForce <= 0)
        {
            return Speed;
        }

        return MathF.Max(0, engine.Def.MaxForce / drag);
    }

    public static float SlowdownSpeed(float progress, float normalThrottleSpeed)
    {
        var target = MathF.Min(Speed, MathF.Max(0, normalThrottleSpeed));
        var t = MathHelper.Clamp(progress, 0, 1);
        return Easing.Ease(EasingTypes.EaseOut, t, 0, 1, Speed, target);
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
        float distanceToNextRing, bool approachedFromFarEnough, bool nextIsPenultimate) =>
        approachedFromFarEnough &&
        nextIsPenultimate &&
        distanceToNextRing <= SlowdownStartDistance;

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
