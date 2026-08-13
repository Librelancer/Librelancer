using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.Effects;
using LibreLancer.Data.Schema.Solar;
using LibreLancer.Server;
using LibreLancer.Server.Components;
using LibreLancer.World;
using Xunit;
using RuntimeLayer = LibreLancer.Data.GameData.GateTunnelLayer;
using RuntimeTunnel = LibreLancer.Data.GameData.GateTunnel;

namespace LibreLancer.Tests;

public class JumpTunnelTests
{
    private const string GateIni = """
        [gate_tunnel]
        nickname = gate_tunnel_bretonia
        write_depth_buffer = 0
        num_spline_control_points = 9
        x_range = 500
        y_range = 500
        z_range = 20000
        min_radius = 50
        max_radius = 100
        far_radius_factor = 0
        min_speed = 0.003
        max_speed = 0.08
        time_to_max_speed = 5
        fade_distance = 0.1
        near_alpha = 1
        far_alpha = 1
        num_t_steps = 200
        num_s_steps = 12
        min_rotation = 3
        max_rotation = -3
        min_rgb = 0, 0, 30
        max_rgb = 0, 0, 100

        [layer]
        texture = jumptube
        color = 255, 128, 0
        near_alpha_factor = 1
        far_alpha_factor = 0.5
        radius_factor = 1.25
        u_offset = 0.1
        v_offset = 0.2
        du = 0.3
        dv = 0.4
        v_scale = 2
        """;

    private const string JumpIni = """
        [JumpShipEffect]
        jump_out_effect = jump_out
        jump_in_effect = jump_in

        [JumpGateEffect]
        nickname = jump_effect_bretonia
        glow_ring_effect = a, b, c
        glow_ring_hp = HpFX2, HpFX3, HpFX4
        glow_create_time = 1, 3, 5
        jump_out_time = 1.5
        jump_out_tunnel_time = 7
        jump_in_tunnel_time = 3
        jump_in_time = 1
        kill_time_before_done = 0.6
        jump_tunnel_effect = jump_tunnel_interior_player
        jump_tunnel = gate_tunnel_bretonia
        jump_ambient = 25, 25, 50
        jump_background_color = 255, 255, 255
        """;

    private static MemoryStream IniStream(string value) =>
        new(Encoding.UTF8.GetBytes(value));

    [Fact]
    public void ParsesBretoniaDefinitionsAndChildLayers()
    {
        var gates = new GateTunnelsIni();
        using (var stream = IniStream(GateIni))
            gates.ParseIni(stream, "gate_tunnel.ini");
        var jumps = new JumpEffectsIni();
        using (var stream = IniStream(JumpIni))
            jumps.ParseIni(stream, "jumpeffect.ini");

        var tunnel = Assert.Single(gates.Tunnels);
        Assert.Equal("gate_tunnel_bretonia", tunnel.Nickname);
        Assert.Equal(9, tunnel.NumSplineControlPoints);
        Assert.Equal(200, tunnel.NumTSteps);
        Assert.Equal(new Vector3(0, 0, 30), tunnel.MinRgb);
        var layer = Assert.Single(tunnel.Layers);
        Assert.Equal("jumptube", layer.Texture);
        Assert.Equal(1.25f, layer.RadiusFactor);
        Assert.Equal(0.3f, layer.Du);

        var ship = Assert.Single(jumps.ShipEffects);
        Assert.Equal("jump_out", ship.JumpOutEffect);
        var gate = Assert.Single(jumps.GateEffects);
        Assert.Equal(["a", "b", "c"], gate.GlowRingEffects);
        Assert.Equal(["HpFX2", "HpFX3", "HpFX4"], gate.GlowRingHardpoints);
        Assert.Equal([1f, 3f, 5f], gate.GlowCreateTimes);
        Assert.Equal(10f, gate.JumpOutTunnelTime + gate.JumpInTunnelTime);
        Assert.Equal(new Vector3(25, 25, 50), gate.JumpAmbient);
    }

    [Fact]
    public void ParsesJumpOutHardpointUsedForTunnelEntry()
    {
        const string source = """
            [Solar]
            nickname = jumpgate
            type = JUMP_GATE
            jump_out_hp = HpFX7
            docking_sphere = jump, HpDockMountA, 225
            """;
        var solars = new SolararchIni();
        using var stream = IniStream(source);
        solars.ParseIni(stream, "solararch.ini");

        var gate = Assert.Single(solars.Solars);
        Assert.Equal("HpFX7", gate.JumpOutHp);
        Assert.Equal("HpDockMountA", Assert.Single(gate.DockingSpheres).Hardpoint);
    }

    [Fact]
    public void MissingOptionalAndMismatchedGlowDataStillParses()
    {
        const string source = """
            [JumpGateEffect]
            nickname = incomplete
            glow_ring_effect = one, two
            glow_ring_hp = HpFX
            """;
        var jumps = new JumpEffectsIni();
        using var stream = IniStream(source);
        jumps.ParseIni(stream, "incomplete.ini");
        var gate = Assert.Single(jumps.GateEffects);
        Assert.Equal(["one", "two"], gate.GlowRingEffects);
        Assert.Equal(["HpFX"], gate.GlowRingHardpoints);
        Assert.Empty(gate.GlowCreateTimes);
        Assert.Equal(7, gate.JumpOutTunnelTime);
        Assert.Equal(3, gate.JumpInTunnelTime);
    }

    [Fact]
    public void ConvertsAndClampsIniColors()
    {
        Assert.Equal(new Vector3(1, 0.5f, 0),
            JumpEffectColor.FromIni(new Vector3(255, 127.5f, 0)));
        Assert.Equal(new Vector3(0, 1, 1),
            JumpEffectColor.FromIni(new Vector3(-2, 300, 255)));
    }

    private static RuntimeTunnel Tunnel() => new()
    {
        Nickname = "test",
        NumSplineControlPoints = 9,
        XRange = 500,
        YRange = 500,
        ZRange = 20_000,
        MinRadius = 50,
        MaxRadius = 100,
        FarRadiusFactor = 0.5f,
        MinSpeed = 0.003f,
        MaxSpeed = 0.08f,
        TimeToMaxSpeed = 5,
        FadeDistance = 0.1f,
        NearAlpha = 1,
        FarAlpha = 0.75f,
        NumTSteps = 200,
        NumSSteps = 12,
        MinRotation = -3,
        MaxRotation = 3,
        MinColor = new Vector3(0, 0, 0.1f),
        MaxColor = new Vector3(0, 0, 0.5f),
        Layers =
        [
            new RuntimeLayer { Color = Vector3.One, RadiusFactor = 1, VScale = 1 },
            new RuntimeLayer { Color = new Vector3(0.5f), RadiusFactor = 2, VScale = 2 }
        ]
    };

    [Fact]
    public void GeneratorIsDeterministicFiniteAndIndexed()
    {
        var a = JumpTunnelGeometry.Generate(Tunnel(), 0x12345678);
        var b = JumpTunnelGeometry.Generate(Tunnel(), 0x12345678);

        Assert.Equal(201 * 13, a.VertexCount);
        Assert.Equal(200 * 12 * 6, a.Indices.Length);
        Assert.Equal(a.Indices, b.Indices);
        Assert.Equal(a.Centerline, b.Centerline);
        Assert.All(a.Centerline, sample =>
        {
            Assert.True(float.IsFinite(sample.Center.X));
            Assert.True(float.IsFinite(sample.Center.Y));
            Assert.True(float.IsFinite(sample.Center.Z));
            Assert.InRange(sample.Radius, 50, 150);
            Assert.InRange(sample.Alpha, 0, 1);
        });
        Assert.All(a.Indices, index => Assert.InRange(index, 0, a.VertexCount - 1));
    }

    [Fact]
    public void GeneratorSanitizesMalformedRanges()
    {
        var tunnel = Tunnel();
        tunnel.XRange = -500;
        tunnel.YRange = -500;
        tunnel.ZRange = -20_000;
        tunnel.MinRadius = 100;
        tunnel.MaxRadius = -50;
        tunnel.FarRadiusFactor = -2;
        tunnel.NumTSteps = 10_000;
        tunnel.NumSSteps = 10_000;

        var geometry = JumpTunnelGeometry.Generate(tunnel, 1);

        Assert.InRange(geometry.VertexCount, 1, ushort.MaxValue + 1);
        Assert.All(geometry.Centerline, sample =>
        {
            Assert.True(float.IsFinite(sample.Center.LengthSquared()));
            Assert.True(float.IsFinite(sample.Tangent.LengthSquared()));
            Assert.True(sample.Radius > 0);
        });
    }

    [Fact]
    public void LayersShareCenterlineAndCloseEverySeam()
    {
        var tunnel = Tunnel();
        var geometry = JumpTunnelGeometry.Generate(tunnel, 42);
        var inner = geometry.CreateLayerVertices(tunnel.Layers[0]);
        var outer = geometry.CreateLayerVertices(tunnel.Layers[1]);
        var stride = geometry.RadialSteps + 1;

        for (var ring = 0; ring <= geometry.LongitudinalSteps; ring++)
        {
            var first = ring * stride;
            var last = first + geometry.RadialSteps;
            Assert.Equal(inner[first].Position, inner[last].Position);
            var center = geometry.Centerline[ring].Center;
            Assert.True(Vector3.Distance(center, outer[first].Position) >
                        Vector3.Distance(center, inner[first].Position));
        }
        Assert.Equal(0, inner[0].Alpha);
        Assert.Equal(0, inner[^1].Alpha);
    }

    [Fact]
    public void LongTravelWrapsOnTheSameDeterministicPath()
    {
        var geometry = JumpTunnelGeometry.Generate(Tunnel(), 7);
        var a = geometry.SampleDistance(1234);
        var b = geometry.SampleDistance(1234 + geometry.Length);
        Assert.True(Vector3.Distance(a.Center, b.Center) < 0.01f);
        Assert.True(MathF.Abs(a.Tangent.Length() - 1) < 0.001f);
    }

    [Fact]
    public void TimelineUsesExactConfiguredBoundaries()
    {
        var effect = new LibreLancer.Data.GameData.JumpGateEffect
        {
            JumpOutTunnelTime = 7,
            JumpInTunnelTime = 3
        };
        var timeline = new JumpTransitionTimeline(effect);
        Assert.Equal(JumpTransitionPhase.TunnelOut, timeline.Phase);
        Assert.False(timeline.UpdateTunnelOut(6.999));
        Assert.Equal(JumpTransitionPhase.TunnelOut, timeline.Phase);
        Assert.True(timeline.UpdateTunnelOut(0.001));
        Assert.Equal(JumpTransitionPhase.Loading, timeline.Phase);
        Assert.Equal(7, timeline.Elapsed, 3);

        timeline.BeginTunnelIn();
        Assert.Equal(JumpTransitionPhase.TunnelIn, timeline.Phase);
        Assert.False(timeline.UpdateTunnelIn(2.999));
        Assert.Equal(JumpTransitionPhase.TunnelIn, timeline.Phase);
        Assert.True(timeline.UpdateTunnelIn(0.001));
        Assert.Equal(JumpTransitionPhase.Complete, timeline.Phase);
        Assert.Equal(10, timeline.Elapsed);

        var motionTunnel = Tunnel();
        Assert.Equal(
            0,
            JumpTunnelMotion.DistanceAt(0, motionTunnel),
            3);
        Assert.Equal(
            (motionTunnel.MinSpeed + motionTunnel.MaxSpeed) * 0.5f *
            motionTunnel.TimeToMaxSpeed * motionTunnel.ZRange,
            JumpTunnelMotion.DistanceAt(motionTunnel.TimeToMaxSpeed, motionTunnel),
            3);
    }

    [Fact]
    public void ReadinessOrderingAndAcknowledgementsAreExactlyOnce()
    {
        var clientFirst = new JumpTransferGuard();
        clientFirst.ClientReady();
        clientFirst.ClientReady();
        Assert.False(clientFirst.TryScheduleSpawn());
        clientFirst.DestinationReady();
        clientFirst.DestinationReady();
        Assert.True(clientFirst.TryScheduleSpawn());
        Assert.False(clientFirst.TryScheduleSpawn());
        Assert.True(clientFirst.MarkSpawned());
        Assert.False(clientFirst.MarkSpawned());
        Assert.True(clientFirst.TryComplete());
        Assert.False(clientFirst.TryComplete());

        var destinationFirst = new JumpTransferGuard();
        destinationFirst.DestinationReady();
        Assert.False(destinationFirst.TryScheduleSpawn());
        destinationFirst.ClientReady();
        Assert.True(destinationFirst.TryScheduleSpawn());

        var timeout = new JumpTransferGuard();
        timeout.DestinationReady();
        Assert.True(timeout.TryScheduleSpawn(true));
        Assert.True(timeout.MarkSpawned());
        Assert.True(timeout.TryComplete());
    }

    [Fact]
    public void ExitPathUsesArcLengthAndHitsBothEndpoints()
    {
        Vector3[] path =
        [
            Vector3.Zero,
            new Vector3(0, 0, 10),
            new Vector3(0, 10, 10)
        ];
        var start = JumpTunnelMotion.SamplePath(path, 0);
        var halfway = JumpTunnelMotion.SamplePath(path, 0.5f);
        var end = JumpTunnelMotion.SamplePath(path, 1);
        Assert.Equal(Vector3.Zero, start.Position);
        Assert.Equal(new Vector3(0, 0, 10), halfway.Position);
        Assert.Equal(path[^1], end.Position);
        Assert.Equal(Vector3.UnitY, end.Direction);
    }

    [Fact]
    public void JumpExitTraversesTwoThousandUnitsToRandomPointOnExitCircumference()
    {
        var hardpoints = new[]
        {
            Vector3.Zero,
            new Vector3(0, 0, -10)
        };
        var a = JumpTunnelMotion.BuildJumpExitPath(
            Vector3.Zero,
            Quaternion.Identity,
            hardpoints,
            42);
        var b = JumpTunnelMotion.BuildJumpExitPath(
            Vector3.Zero,
            Quaternion.Identity,
            hardpoints,
            42);

        Assert.Equal(a, b);
        Assert.Equal(2, a.Length);
        Assert.Equal(
            JumpTunnelMotion.GateExitBehindDistance,
            -a[1].Z,
            3);
        Assert.InRange(
            MathF.Sqrt((a[1].X * a[1].X) + (a[1].Y * a[1].Y)),
            JumpTunnelMotion.JumpExitLateralAdjustment - 0.001f,
            JumpTunnelMotion.JumpExitLateralAdjustment + 0.001f);
        Assert.Equal(a[0].X, a[1].X, 4);
        Assert.Equal(a[0].Y, a[1].Y, 4);
        Assert.Equal(
            JumpTunnelMotion.JumpArrivalTravelDistance,
            Vector3.Distance(a[0], a[1]),
            3);
        Assert.True(Vector3.Distance(
            Vector3.Normalize(a[1] - a[0]),
            -Vector3.UnitZ) < 0.0001f);
        var parallelGateAxis = JumpTunnelMotion.BuildJumpExitPath(
            Vector3.Zero,
            Quaternion.Identity,
            [Vector3.Zero, Vector3.UnitX],
            42);
        Assert.All(parallelGateAxis,
            point => Assert.True(float.IsFinite(point.LengthSquared())));
        Assert.Equal(
            0.8f,
            JumpTunnelMotion.JumpArrivalDuration,
            3);
    }

    [Fact]
    public void JumpGateAnimationStartsFiveHundredUnitsBeforeFirstApproachHardpoint()
    {
        Assert.True(SDockableComponent.IsWithinAnimationTrigger(
            DockKinds.Jump,
            499.99f,
            80));
        Assert.True(SDockableComponent.IsWithinAnimationTrigger(
            DockKinds.Jump,
            SDockableComponent.JumpAnimationApproachDistance,
            0));
        Assert.False(SDockableComponent.IsWithinAnimationTrigger(
            DockKinds.Jump,
            500.01f,
            80));
    }

    [Fact]
    public void DestroyedNpcJumpArrivalReportsCancellationWithoutCompleting()
    {
        var obj = new GameObject();
        bool? arrived = null;
        var callbackCount = 0;
        var component = new SJumpInComponent(
            obj,
            [Vector3.Zero, -Vector3.UnitZ],
            1,
            value =>
            {
                arrived = value;
                callbackCount++;
            });

        component.Unregister(null!);
        component.Unregister(null!);

        Assert.False(arrived);
        Assert.Equal(1, callbackCount);
    }

    [Fact]
    public void FinishedNpcJumpArrivalReportsSuccessfulCompletion()
    {
        var obj = new GameObject();
        bool? arrived = null;
        var component = new SJumpInComponent(
            obj,
            [Vector3.Zero, -Vector3.UnitZ],
            1,
            value => arrived = value);

        component.Update(1, null!);

        Assert.True(arrived);
        Assert.Equal(-Vector3.UnitZ, obj.LocalTransform.Position);
    }
}
