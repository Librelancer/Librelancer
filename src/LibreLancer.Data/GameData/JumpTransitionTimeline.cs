// MIT License - Copyright (c) LibreLancer contributors
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer.Data.GameData;

public enum JumpTransitionPhase
{
    TunnelOut,
    Loading,
    TunnelIn,
    Complete
}

public sealed class JumpTransitionTimeline
{
    public float TunnelOutTime { get; }
    public float TunnelInTime { get; }
    public double Elapsed { get; private set; }
    public double PhaseElapsed { get; private set; }
    public JumpTransitionPhase Phase { get; private set; }

    public JumpTransitionTimeline(JumpGateEffect? effect)
    {
        TunnelOutTime = MathF.Max(0, effect?.JumpOutTunnelTime ?? 0);
        TunnelInTime = MathF.Max(0, effect?.JumpInTunnelTime ?? 0);
    }

    public double TunnelDuration => TunnelOutTime + TunnelInTime;

    public bool UpdateTunnelOut(double delta)
    {
        if (Phase != JumpTransitionPhase.TunnelOut)
            return true;
        return Advance(delta, TunnelOutTime, 0, JumpTransitionPhase.Loading);
    }

    public void BeginTunnelIn()
    {
        if (Phase != JumpTransitionPhase.Loading)
            return;
        Phase = JumpTransitionPhase.TunnelIn;
        PhaseElapsed = 0;
        Elapsed = TunnelOutTime;
    }

    public bool UpdateTunnelIn(double delta)
    {
        if (Phase == JumpTransitionPhase.Loading)
            return false;
        if (Phase != JumpTransitionPhase.TunnelIn)
            return true;
        return Advance(delta, TunnelInTime, TunnelOutTime, JumpTransitionPhase.Complete);
    }

    private bool Advance(
        double delta,
        double duration,
        double offset,
        JumpTransitionPhase next)
    {
        var advance = Math.Max(0, delta);
        PhaseElapsed = Math.Min(duration, PhaseElapsed + advance);
        Elapsed = offset + PhaseElapsed;
        if (PhaseElapsed < duration)
            return false;
        Phase = next;
        return true;
    }
}

public static class JumpTunnelMotion
{
    public const float GateExitBehindDistance = 350;
    public const float JumpExitLateralAdjustment = 500;
    public const float JumpArrivalTravelDistance = 2000;
    public const float JumpArrivalSpeed = 2500;
    public const float JumpArrivalDuration =
        JumpArrivalTravelDistance / JumpArrivalSpeed;

    public static float DistanceAt(float time, GateTunnel tunnel)
    {
        time = MathF.Max(0, time);
        if (tunnel.TimeToMaxSpeed <= 0)
            return tunnel.MaxSpeed * tunnel.ZRange * time;
        var rampTime = MathF.Min(time, tunnel.TimeToMaxSpeed);
        var rampDistance = (tunnel.MinSpeed * rampTime) +
                           (0.5f * (tunnel.MaxSpeed - tunnel.MinSpeed) *
                            rampTime * rampTime / tunnel.TimeToMaxSpeed);
        var maxSpeedTime = MathF.Max(0, time - tunnel.TimeToMaxSpeed);
        return (rampDistance + (tunnel.MaxSpeed * maxSpeedTime)) * tunnel.ZRange;
    }

    public static JumpPathSample SamplePath(Vector3[] points, float progress)
    {
        if (points.Length == 0)
            return new JumpPathSample(Vector3.Zero, Vector3.UnitZ);
        if (points.Length == 1)
            return new JumpPathSample(points[0], Vector3.UnitZ);
        progress = Math.Clamp(progress, 0, 1);
        var total = 0f;
        for (var i = 1; i < points.Length; i++)
            total += Vector3.Distance(points[i - 1], points[i]);
        if (total <= float.Epsilon)
            return new JumpPathSample(points[^1], Vector3.UnitZ);
        var target = progress * total;
        var accumulated = 0f;
        for (var i = 1; i < points.Length; i++)
        {
            var delta = points[i] - points[i - 1];
            var length = delta.Length();
            if (accumulated + length >= target)
            {
                var t = length <= 0 ? 0 : (target - accumulated) / length;
                return new JumpPathSample(
                    Vector3.Lerp(points[i - 1], points[i], t),
                    length <= 0 ? Vector3.UnitZ : delta / length);
            }
            accumulated += length;
        }
        var direction = Vector3.Normalize(points[^1] - points[^2]);
        return new JumpPathSample(points[^1],
            float.IsFinite(direction.X) ? direction : Vector3.UnitZ);
    }

    public static Vector3[] BuildJumpExitPath(
        Vector3 gateCenter,
        Quaternion gateOrientation,
        Vector3[] hardpointPath,
        uint seed)
    {
        var outward = hardpointPath.Length >= 2
            ? hardpointPath[^1] - hardpointPath[0]
            : Vector3.Transform(-Vector3.UnitZ, gateOrientation);
        if (outward.LengthSquared() < 0.000001f)
            outward = Vector3.Transform(-Vector3.UnitZ, gateOrientation);
        outward = Vector3.Normalize(outward);

        var right = Vector3.Transform(Vector3.UnitX, gateOrientation);
        right -= outward * Vector3.Dot(right, outward);
        if (right.LengthSquared() < 0.000001f)
            right = Vector3.Cross(
                MathF.Abs(Vector3.Dot(outward, Vector3.UnitY)) > 0.95f
                    ? Vector3.UnitX
                    : Vector3.UnitY,
                outward);
        right = Vector3.Normalize(right);
        var up = Vector3.Normalize(Vector3.Cross(outward, right));

        // Stable across client and server, on the border of the lateral circle.
        var rng = new JumpRandom(seed);
        var angle = rng.Next() * MathF.Tau;
        var x = MathF.Cos(angle) * JumpExitLateralAdjustment;
        var y = MathF.Sin(angle) * JumpExitLateralAdjustment;
        var lateral = (right * x) + (up * y);
        var arrival = gateCenter +
                      (outward * GateExitBehindDistance) +
                      lateral;

        return
        [
            arrival - (outward * JumpArrivalTravelDistance),
            arrival
        ];
    }
}

public readonly record struct JumpPathSample(Vector3 Position, Vector3 Direction);
