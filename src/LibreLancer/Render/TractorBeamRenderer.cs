using System;
using System.Collections.Generic;
using System.Net;
using System.Numerics;
using LibreLancer.Client.Components;
using LibreLancer.Graphics;
using LibreLancer.Resources;

namespace LibreLancer.Render;

public partial class TractorBeamRenderer : ObjectRenderer
{
    public static void DrawTractorBeam(
        PolylineRender lines,
        ResourceManager resources,
        Vector3 cameraPosition,
        Vector3 p0, Vector3 p1,
        Color4 color,
        double totalTime,
        int offset = 0)
    {
        float lineLen = (p1 - p0).Length();
        if (lineLen < 1)
            return;
        int noiseOffset = offset + (int)MathHelper.Clamp((totalTime % 1.0) * 4095, 0, 4095);
        var tractorLine = resources.FindTexture("line") as Texture2D;
        lines.StartQuadLine(tractorLine, BlendMode.Additive);
        float segLength = 4;
        var segCount = (int)MathF.Ceiling(lineLen / segLength);
        var dir = (p1 - p0).Normalized();
        Vector3 up = MathF.Abs(Vector3.Dot(dir, Vector3.UnitY)) > 0.99f ? Vector3.UnitZ : Vector3.UnitY;
        var offsetDir = Vector3.Cross(dir, up);
        for (int i = 1; i <= segCount; i++)
        {
            var noise0 = SampleNoise(noiseOffset + i - 1);
            var noise1 = SampleNoise(noiseOffset + i);

            var offset0 = (-2.5f + (noise0) * 5f) * offsetDir;
            var offset1 = (-2.5f + (noise1) * 5f) * offsetDir;

            var point0 = p0 + (dir * segLength * (i - 1)) + offset0;
            var point1 = p0 + (dir * segLength * i) + offset1;

            lines.AddQuad(point0, point1, color);
        }
        var zVal = RenderHelpers.GetZ((p0 + p1) * 0.5f, cameraPosition);
        lines.FinishQuadLine(zVal);
    }

    const float CULL_DISTANCE = 20000;
    const float CULL = CULL_DISTANCE * CULL_DISTANCE;

    public Vector3 Origin;

    public RefList<VisibleBeam> TractorBeams = new();
    public Color3f Color;
    private SystemRenderer sysr;

    public override bool PrepareRender(ICamera camera, NebulaRenderer nr, SystemRenderer sys, bool forceCull)
    {
        if(Vector3.DistanceSquared(camera.Position, pos) < CULL)
        {
            sys.AddObject(this);
            sysr = sys;
            return true;
        }
        return false;
    }

    private Vector3 pos;

    public override void Update(double time, Vector3 position, Matrix4x4 transform)
    {
        pos = position;
    }

    public override void Draw(ICamera camera, CommandBuffer commands, SystemLighting lights, NebulaRenderer nr)
    {
        if (sysr == null)
        {
            return;
        }
        for (int i = 0; i < TractorBeams.Count; i++)
        {
            var beam = TractorBeams[i];
            var tgtPos = beam.Target.WorldTransform.Position;
            var len = (tgtPos - Origin).Length();
            var dir = (tgtPos - Origin).Normalized();
            if (beam.Distance < len)
            {
                len = beam.Distance;
            }

            DrawTractorBeam(sysr.Polyline, sysr.ResourceManager, camera.Position, Origin,
                Origin + (len * dir), new Color4(Color, 1), sysr.Game.TotalTime, beam.Seed << 4);
        }
    }
}
