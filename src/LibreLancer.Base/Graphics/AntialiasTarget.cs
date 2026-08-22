// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Graphics.Backends;

namespace LibreLancer.Graphics;

public class AntialiasTarget : RenderTarget
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public AntialiasMode Mode { get; private set; }

    private IMultisampleTarget? msaa;
    private RenderTarget2D? smaa;
    private RenderTarget2D? smaaEdges;
    private RenderTarget2D? smaaBlend;
    private DepthBuffer? depthStencil;
    private RenderContext renderContext;

    public AntialiasTarget(RenderContext renderContext, int width, int height, AntialiasMode mode)
    {
        this.renderContext = renderContext;
        Width = width;
        Height = height;
        if (mode >= AntialiasMode.MSAA2x)
        {
            msaa = renderContext.Backend.CreateMultisampleTarget(width, height, mode switch
            {
                AntialiasMode.MSAA4x => 4,
                AntialiasMode.MSAA8x => 8,
                _ => 2
            });
            Target = msaa;
        }
        else
        {
            smaa = new(renderContext, width, height);
            depthStencil = new(renderContext, width, height, true);
            smaaEdges = new(renderContext, width, height, depthStencil, false);
            smaaBlend = new(renderContext, width, height, depthStencil, false);
            Target = smaa.Target;
        }
    }

    void SMAABlit(RenderTarget? tgt, Point offset)
    {
        if (smaa == null ||
            smaaEdges == null ||
            smaaBlend == null)
            throw new InvalidOperationException();
        // Save state
        var t0 = renderContext.Textures[0];
        var t1 = renderContext.Textures[1];
        var t2 = renderContext.Textures[2];
        var s0 = renderContext.Samplers[0];
        var s1 = renderContext.Samplers[1];
        var sTgt = renderContext.RenderTarget;
        var shdr = renderContext.Shader;
        var srcBlend = renderContext.BlendMode;
        var stencil = renderContext.Stencil;
        var stencilEnabled = renderContext.StencilEnabled;
        bool depth = renderContext.DepthEnabled;
        bool cull = renderContext.Cull;
        renderContext.PushViewport(0, 0, Width, Height);
        renderContext.PushScissor(new(0, 0, Width, Height), false);
        // Fetch shaders and set RT size
        uint level = (uint)Mode;
        var rtMetrics = new Vector4(1.0f / Width, 1.0f / Height, Width, Height);
        var edgeDetection = renderContext.SMAAEdgeDetection.Get(level);
        edgeDetection.SetUniformBlock(3, ref rtMetrics);
        var blendingWeightCalculation = renderContext.SMAABlendingWeightCalculation.Get(level);
        blendingWeightCalculation.SetUniformBlock(3, ref rtMetrics);
        var neighborhoodBlending = renderContext.SMAANeighborhoodBlending.Get(level);
        neighborhoodBlending.SetUniformBlock(3, ref rtMetrics);
        // SMAA global samplers
        renderContext.Cull = false;
        renderContext.Samplers[0] = new(TextureFiltering.Linear, WrapMode.ClampToEdge, WrapMode.ClampToEdge);
        renderContext.Samplers[1] = new (TextureFiltering.Nearest, WrapMode.ClampToEdge, WrapMode.ClampToEdge);
        // Edge detection pass
        renderContext.RenderTarget = smaaEdges;
        renderContext.Textures[0] = smaa.Texture;
        renderContext.BlendMode = BlendMode.Opaque;
        renderContext.DepthEnabled = false;
        renderContext.Shader = edgeDetection;
        renderContext.StencilEnabled = true;
        renderContext.Stencil = new(StencilFunction.Always, StencilOperation.Replace, StencilOperation.Replace,
            StencilOperation.Replace, 1, 0xFFFFFFFF);
        renderContext.ClearDepth(); // clear stencil buffer
        renderContext.FullScreenTriangle.Draw(PrimitiveTypes.TriangleList, 1);
        // Blending weight pass
        renderContext.RenderTarget = smaaBlend;
        renderContext.Textures[0] = smaaEdges.Texture;
        renderContext.Textures[1] = renderContext.SMAAAreaTex;
        renderContext.Textures[2] = renderContext.SMAASearchTex;
        renderContext.Stencil = new(StencilFunction.Equal, StencilOperation.Keep, StencilOperation.Keep,
            StencilOperation.Keep, 1, 0xFFFFFFFF);
        renderContext.Shader = blendingWeightCalculation;
        renderContext.FullScreenTriangle.Draw(PrimitiveTypes.TriangleList, 1);
        //Neighborhood blending pass
        renderContext.StencilEnabled = false;

        renderContext.RenderTarget = tgt;
        renderContext.Textures[0] = smaa.Texture;
        renderContext.Textures[1] = smaaBlend.Texture;
        renderContext.Shader = neighborhoodBlending;
        if (offset != Point.Zero)
        {
            renderContext.PushViewport(offset.X, offset.Y, Width, Height);
            renderContext.PushScissor(new(offset.X, offset.Y, Width, Height), false);
        }
        renderContext.FullScreenTriangle.Draw(PrimitiveTypes.TriangleList, 1);
        if (offset != Point.Zero)
        {
            renderContext.PopScissor();
            renderContext.PopViewport();
        }
        // Restore state
        renderContext.Textures[0] = t0;
        renderContext.Textures[1] = t1;
        renderContext.Textures[2] = t2;
        renderContext.Samplers[0] = s0;
        renderContext.Samplers[1] = s1;
        renderContext.BlendMode = srcBlend;
        renderContext.RenderTarget = sTgt;
        renderContext.Shader = shdr;
        renderContext.DepthEnabled = depth;
        renderContext.Cull = cull;
        renderContext.Stencil = stencil;
        renderContext.StencilEnabled = stencilEnabled;
        renderContext.PopScissor();
        renderContext.PopViewport();
    }

    public void BlitToScreen(Point offset)
    {
        if (msaa != null)
        {
            msaa.BlitToScreen(offset);
        }
        else
        {
            SMAABlit(null, offset);
        }
    }

    public void BlitToRenderTarget(RenderTarget2D rTarget)
    {
        if (msaa != null)
        {
            msaa.BlitToRenderTarget(rTarget.Backing);
        }
        else
        {
            SMAABlit(rTarget, Point.Zero);
        }
    }

    public override void Dispose()
    {
        msaa?.Dispose();
        smaa?.Dispose();
        smaaEdges?.Dispose();
        smaaBlend?.Dispose();
        depthStencil?.Dispose();
    }
}
