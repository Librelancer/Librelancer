// MIT License - Copyright (c) LibreLancer contributors
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer.Data.GameData;

public readonly record struct JumpTunnelSample(
    Vector3 Center,
    Vector3 Tangent,
    Vector3 Normal,
    Vector3 Binormal,
    float Radius,
    Vector3 Color,
    float Alpha);

public readonly record struct JumpTunnelVertex(
    Vector3 Position,
    Vector3 Color,
    float Alpha,
    Vector2 TextureCoordinate);

public sealed class JumpTunnelGeometry
{
    public JumpTunnelSample[] Centerline { get; }
    public ushort[] Indices { get; }
    public int LongitudinalSteps { get; }
    public int RadialSteps { get; }

    public int VertexCount => (LongitudinalSteps + 1) * (RadialSteps + 1);
    public float Length { get; }

    private JumpTunnelGeometry(
        JumpTunnelSample[] centerline,
        ushort[] indices,
        int longitudinalSteps,
        int radialSteps)
    {
        Centerline = centerline;
        Indices = indices;
        LongitudinalSteps = longitudinalSteps;
        RadialSteps = radialSteps;
        var length = 0f;
        for (var i = 1; i < centerline.Length; i++)
            length += Vector3.Distance(centerline[i - 1].Center, centerline[i].Center);
        Length = length;
    }

    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        var t2 = t * t;
        var t3 = t2 * t;
        return 0.5f * ((2 * p1) +
                       ((-p0 + p2) * t) +
                       (((2 * p0) - (5 * p1) + (4 * p2) - p3) * t2) +
                       ((-p0 + (3 * p1) - (3 * p2) + p3) * t3));
    }

    private static Vector3 SampleSpline(Vector3[] controlPoints, float t)
    {
        var segments = controlPoints.Length - 3;
        var scaled = Math.Clamp(t, 0, 1) * segments;
        var segment = Math.Min((int)scaled, segments - 1);
        return CatmullRom(
            controlPoints[segment],
            controlPoints[segment + 1],
            controlPoints[segment + 2],
            controlPoints[segment + 3],
            segment == segments - 1 && t >= 1 ? 1 : scaled - segment);
    }

    private static float Smooth(float value) =>
        value * value * (3 - (2 * value));

    private static float SampleScalar(float[] values, float t)
    {
        var scaled = Math.Clamp(t, 0, 1) * (values.Length - 1);
        var index = Math.Min((int)scaled, values.Length - 2);
        return MathHelper.Lerp(
            values[index],
            values[index + 1],
            Smooth(scaled - index));
    }

    private static Vector3 SampleColor(Vector3[] values, float t)
    {
        var scaled = Math.Clamp(t, 0, 1) * (values.Length - 1);
        var index = Math.Min((int)scaled, values.Length - 2);
        return Vector3.Lerp(
            values[index],
            values[index + 1],
            Smooth(scaled - index));
    }

    private static Vector3 Normalize(Vector3 value, Vector3 fallback)
    {
        var lengthSquared = value.LengthSquared();
        return float.IsFinite(lengthSquared) && lengthSquared > 0.000001f
            ? value / MathF.Sqrt(lengthSquared)
            : fallback;
    }

    public static JumpTunnelGeometry Generate(GateTunnel tunnel, uint seed)
    {
        ArgumentNullException.ThrowIfNull(tunnel);
        var pointCount = Math.Max(4, tunnel.NumSplineControlPoints);
        var tSteps = Math.Clamp(tunnel.NumTSteps, 2, 10_000);
        var sSteps = Math.Clamp(tunnel.NumSSteps, 3, 1_024);
        var maxRadialSteps = ((ushort.MaxValue + 1) / (tSteps + 1)) - 1;
        sSteps = Math.Min(sSteps, maxRadialSteps);

        var rng = new JumpRandom(seed);
        var controls = new Vector3[pointCount];
        var radii = new float[pointCount];
        var colors = new Vector3[pointCount];
        var segmentLength = MathF.Max(1, MathF.Abs(tunnel.ZRange)) / (pointCount - 3);
        // Freelancer's x/y ranges describe the full random span. Treating
        // them as both the positive and negative extent doubles the intended
        // curvature and produces very sharp camera turns.
        var halfXRange = MathF.Abs(tunnel.XRange) * 0.5f;
        var halfYRange = MathF.Abs(tunnel.YRange) * 0.5f;
        var minRadius = MathF.Max(0.01f, MathF.Min(tunnel.MinRadius, tunnel.MaxRadius));
        var maxRadius = MathF.Max(minRadius, MathF.Max(tunnel.MinRadius, tunnel.MaxRadius));
        for (var i = 0; i < pointCount; i++)
        {
            var endpoint = i <= 1 || i >= pointCount - 2;
            controls[i] = new Vector3(
                endpoint ? 0 : rng.Range(-halfXRange, halfXRange),
                endpoint ? 0 : rng.Range(-halfYRange, halfYRange),
                (i - 1) * segmentLength);
            radii[i] = rng.Range(minRadius, maxRadius);
            colors[i] = new Vector3(
                rng.Range(tunnel.MinColor.X, tunnel.MaxColor.X),
                rng.Range(tunnel.MinColor.Y, tunnel.MaxColor.Y),
                rng.Range(tunnel.MinColor.Z, tunnel.MaxColor.Z));
        }

        var centers = new Vector3[tSteps + 1];
        var tangents = new Vector3[tSteps + 1];
        for (var i = 0; i <= tSteps; i++)
            centers[i] = SampleSpline(controls, i / (float)tSteps);
        for (var i = 0; i <= tSteps; i++)
        {
            var previous = centers[Math.Max(0, i - 1)];
            var next = centers[Math.Min(tSteps, i + 1)];
            tangents[i] = Normalize(next - previous, Vector3.UnitZ);
        }

        var samples = new JumpTunnelSample[tSteps + 1];
        var normal = Normalize(Vector3.Cross(
            MathF.Abs(Vector3.Dot(tangents[0], Vector3.UnitY)) > 0.95f
                ? Vector3.UnitX
                : Vector3.UnitY,
            tangents[0]), Vector3.UnitY);
        var roll = 0f;
        var rollStep = MathF.PI / 180f * rng.Range(tunnel.MinRotation, tunnel.MaxRotation) /
                       Math.Max(1, tSteps);
        for (var i = 0; i <= tSteps; i++)
        {
            var tangent = tangents[i];
            normal -= tangent * Vector3.Dot(normal, tangent);
            if (normal.LengthSquared() < 0.000001f)
                normal = Normalize(Vector3.Cross(
                    MathF.Abs(Vector3.Dot(tangent, Vector3.UnitY)) > 0.95f
                        ? Vector3.UnitX
                        : Vector3.UnitY,
                    tangent), Vector3.UnitY);
            else
                normal = Normalize(normal, Vector3.UnitY);
            var binormal = Normalize(Vector3.Cross(tangent, normal), Vector3.UnitX);
            if (roll != 0)
            {
                var rotation = Quaternion.CreateFromAxisAngle(tangent, roll);
                normal = Vector3.Transform(normal, rotation);
                binormal = Vector3.Transform(binormal, rotation);
            }

            var t = i / (float)tSteps;
            var radius = SampleScalar(radii, t) *
                         MathHelper.Lerp(
                             1,
                             MathF.Max(0.001f, 1 + tunnel.FarRadiusFactor),
                             t);
            var alpha = MathHelper.Lerp(tunnel.NearAlpha, tunnel.FarAlpha, t);
            if (tunnel.FadeDistance > 0)
            {
                var edge = MathF.Min(
                    Math.Clamp(t / tunnel.FadeDistance, 0, 1),
                    Math.Clamp((1 - t) / tunnel.FadeDistance, 0, 1));
                alpha *= edge;
            }
            samples[i] = new JumpTunnelSample(
                centers[i], tangent, normal, binormal, radius,
                SampleColor(colors, t), alpha);
            roll += rollStep;
        }

        var indices = new ushort[tSteps * sSteps * 6];
        var idx = 0;
        var stride = sSteps + 1;
        for (var t = 0; t < tSteps; t++)
        {
            for (var s = 0; s < sSteps; s++)
            {
                var a = (ushort)((t * stride) + s);
                var b = (ushort)(a + 1);
                var c = (ushort)(((t + 1) * stride) + s);
                var d = (ushort)(c + 1);
                // Wound toward the center of the tube.
                indices[idx++] = a;
                indices[idx++] = d;
                indices[idx++] = b;
                indices[idx++] = a;
                indices[idx++] = c;
                indices[idx++] = d;
            }
        }
        return new JumpTunnelGeometry(samples, indices, tSteps, sSteps);
    }

    public JumpTunnelVertex[] CreateLayerVertices(GateTunnelLayer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);
        var result = new JumpTunnelVertex[VertexCount];
        var stride = RadialSteps + 1;
        for (var t = 0; t <= LongitudinalSteps; t++)
        {
            var sample = Centerline[t];
            var longitudinal = t / (float)LongitudinalSteps;
            var layerAlpha = sample.Alpha * MathHelper.Lerp(
                layer.NearAlphaFactor,
                layer.FarAlphaFactor,
                longitudinal);
            for (var s = 0; s <= RadialSteps; s++)
            {
                var radial = s / (float)RadialSteps;
                var direction = s == RadialSteps
                    ? sample.Normal
                    : (sample.Normal * MathF.Cos(radial * MathF.Tau)) +
                      (sample.Binormal * MathF.Sin(radial * MathF.Tau));
                result[(t * stride) + s] = new JumpTunnelVertex(
                    sample.Center + (direction * sample.Radius * layer.RadiusFactor),
                    sample.Color * layer.Color,
                    layerAlpha,
                    new Vector2(
                        layer.UOffset + radial,
                        layer.VOffset + (longitudinal * layer.VScale)));
            }
        }
        return result;
    }

    public JumpTunnelSample SampleDistance(float distance)
    {
        if (Length <= 0)
            return Centerline[0];
        distance %= Length;
        if (distance < 0)
            distance += Length;
        var accumulated = 0f;
        for (var i = 1; i < Centerline.Length; i++)
        {
            var segment = Vector3.Distance(Centerline[i - 1].Center, Centerline[i].Center);
            if (accumulated + segment >= distance)
            {
                var t = segment <= 0 ? 0 : (distance - accumulated) / segment;
                var a = Centerline[i - 1];
                var b = Centerline[i];
                return new JumpTunnelSample(
                    Vector3.Lerp(a.Center, b.Center, t),
                    Normalize(Vector3.Lerp(a.Tangent, b.Tangent, t), a.Tangent),
                    Normalize(Vector3.Lerp(a.Normal, b.Normal, t), a.Normal),
                    Normalize(Vector3.Lerp(a.Binormal, b.Binormal, t), a.Binormal),
                    MathHelper.Lerp(a.Radius, b.Radius, t),
                    Vector3.Lerp(a.Color, b.Color, t),
                    MathHelper.Lerp(a.Alpha, b.Alpha, t));
            }
            accumulated += segment;
        }
        return Centerline[^1];
    }
}
