// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using LibreLancer.Graphics;

namespace LibreLancer.Render;

public readonly struct RenderStateSnapshot
{
    private readonly RenderTarget renderTarget;
    private readonly Shader shader;
    private readonly bool depthEnabled;
    private readonly bool depthWrite;
    private readonly DepthFunction depthFunction;
    private readonly bool colorWrite;
    private readonly bool cullEnabled;
    private readonly CullFaces cullFace;
    private readonly ushort blendMode;
    private readonly Vector2 depthRange;

    public RenderStateSnapshot(RenderContext rstate)
    {
        renderTarget = rstate.RenderTarget;
        shader = rstate.Shader;
        depthEnabled = rstate.DepthEnabled;
        depthWrite = rstate.DepthWrite;
        depthFunction = rstate.DepthFunction;
        colorWrite = rstate.ColorWrite;
        cullEnabled = rstate.Cull;
        cullFace = rstate.CullFace;
        blendMode = rstate.BlendMode;
        depthRange = rstate.DepthRange;
    }

    public void Restore(RenderContext rstate)
    {
        rstate.RenderTarget = renderTarget;
        if (shader != null)
            rstate.Shader = shader;
        rstate.DepthEnabled = depthEnabled;
        rstate.DepthWrite = depthWrite;
        rstate.DepthFunction = depthFunction;
        rstate.ColorWrite = colorWrite;
        rstate.Cull = cullEnabled;
        rstate.CullFace = cullFace;
        rstate.BlendMode = blendMode;
        rstate.DepthRange = depthRange;
    }
}
