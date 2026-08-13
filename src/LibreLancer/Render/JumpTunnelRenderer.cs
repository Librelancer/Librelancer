// MIT License - Copyright (c) LibreLancer contributors
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Data.GameData;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Render.Materials;
using LibreLancer.Resources;

namespace LibreLancer.Render;

/// <summary>
/// Owns the GPU resources for all layers of one procedural jump tunnel.
/// </summary>
internal sealed class JumpTunnelRenderer : ObjectRenderer, IDisposable
{
    private readonly JumpTunnelGeometry geometry;
    private readonly VertexBuffer[] vertices;
    private readonly ElementBuffer indices;
    private readonly JumpTunnelMaterial[] materials;
    private float elapsed;
    private Matrix4x4 world = Matrix4x4.Identity;

    public JumpTunnelGeometry Geometry => geometry;

    public JumpTunnelRenderer(
        RenderContext context,
        ResourceManager resources,
        GateTunnel tunnel,
        uint seed)
    {
        geometry = JumpTunnelGeometry.Generate(tunnel, seed);
        var layers = tunnel.Layers.Length > 0
            ? tunnel.Layers
            : [new GateTunnelLayer()];
        vertices = new VertexBuffer[layers.Length];
        materials = new JumpTunnelMaterial[layers.Length];
        indices = new ElementBuffer(context, geometry.Indices.Length);
        indices.SetData(geometry.Indices);

        for (var i = 0; i < layers.Length; i++)
        {
            var cpuVertices = geometry.CreateLayerVertices(layers[i]);
            var gpuVertices = new VertexPositionColorTexture[cpuVertices.Length];
            for (var j = 0; j < cpuVertices.Length; j++)
            {
                var vertex = cpuVertices[j];
                gpuVertices[j] = new VertexPositionColorTexture(
                    vertex.Position,
                    new Color4(vertex.Color.X, vertex.Color.Y, vertex.Color.Z, vertex.Alpha),
                    vertex.TextureCoordinate);
            }

            vertices[i] = new VertexBuffer(
                context,
                typeof(VertexPositionColorTexture),
                gpuVertices.Length);
            vertices[i].SetData(gpuVertices);
            vertices[i].SetElementBuffer(indices);
            materials[i] = new JumpTunnelMaterial(resources)
            {
                Texture = layers[i].Texture,
                WriteDepth = tunnel.WriteDepthBuffer,
                Du = layers[i].Du,
                Dv = layers[i].Dv
            };
        }
    }

    public override void Update(double delta, Vector3 position, Matrix4x4 transform)
    {
        elapsed += (float)delta;
        world = transform;
        foreach (var material in materials)
            material.Elapsed = elapsed;
    }

    public override bool OutOfView(ICamera camera) => false;

    public override bool PrepareRender(
        ICamera camera,
        NebulaRenderer? nr,
        SystemRenderer sys,
        bool forceCull)
    {
        sys.AddObject(this);
        return true;
    }

    public override void Draw(
        ICamera camera,
        CommandBuffer commands,
        SystemLighting lights,
        NebulaRenderer nr)
    {
        var worldHandle = commands.WorldBuffer.SubmitMatrix(ref world);
        for (var i = 0; i < vertices.Length; i++)
        {
            commands.AddCommand(
                materials[i],
                null,
                worldHandle,
                Lighting.Empty,
                vertices[i],
                1,
                PrimitiveTypes.TriangleList,
                0,
                0,
                geometry.Indices.Length / 3,
                SortLayers.NEBULA_INSIDE,
                0,
                null,
                i);
        }
    }

    public void Dispose()
    {
        foreach (var vertexBuffer in vertices)
            vertexBuffer.Dispose();
        indices.Dispose();
    }
}
