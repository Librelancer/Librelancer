// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using LancerEdit.Materials;
using LibreLancer;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Primitives;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Render;
using LibreLancer.Render.Cameras;

namespace LancerEdit
{
    public class CubemapViewer: EditorTab
    {
        private QuadSphere sphere;
        private Viewport3D viewport;
        private CubemapMaterial material;
        private MainWindow mw;
        public CubemapViewer(string title, TextureCube texture, MainWindow mw)
        {
            Title = title;
            material = new CubemapMaterial(mw.Resources) {Texture = texture};
            sphere = new QuadSphere(mw.RenderContext, 32);
            viewport = new Viewport3D(mw);
            viewport.DefaultOffset = new Vector3(0, 0, 4);
            viewport.ModelScale = 0.01f;
            viewport.Mode = CameraModes.Arcball;
            viewport.Background =  new Vector4(0.12f,0.12f,0.12f, 1f);
            viewport.ResetControls();
            this.mw = mw;
        }
        public override void Draw(double elapsed)
        {
            if (viewport.Begin())
            {
                var cam = new LookAtCamera();
                Matrix4x4 rot = Matrix4x4.CreateRotationX(viewport.CameraRotation.Y) *
                                Matrix4x4.CreateRotationY(viewport.CameraRotation.X);
                var dir = Vector3.Transform(-Vector3.UnitZ, rot);
                var to = Vector3.Zero;
                cam.Update(viewport.RenderWidth, viewport.RenderHeight, viewport.CameraOffset, to, rot);
                mw.RenderContext.SetCamera(cam);
                material.Use(mw.RenderContext, new VertexPositionNormalTexture(), ref Lighting.Empty, 0);
                for (int i = 0; i < 6; i++)
                {
                    sphere.GetDrawParameters((CubeMapFace) i, out int start, out int count, out _);
                    sphere.VertexBuffer.Draw(PrimitiveTypes.TriangleList, 0, start, count);
                }

                viewport.End();
            }
        }

        public override void Dispose()
        {
            material.Texture.Dispose();
            viewport.Dispose();
            sphere.VertexBuffer.Dispose();
        }
    }
}
