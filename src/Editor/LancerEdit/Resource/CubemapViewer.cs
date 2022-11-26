// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using LibreLancer;
using LibreLancer.Primitives;
using LibreLancer.Render.Cameras;
using LibreLancer.Shaders;

namespace LancerEdit
{
    public class CubemapViewer: EditorTab
    {
        private ShaderVariables shader;
        private int cubemapIndex;
        private int cameraPositionIndex;
        private TextureCube tex;
        private QuadSphere sphere;
        private Viewport3D viewport;
        public CubemapViewer(string title, TextureCube texture, MainWindow mw)
        {
            Title = title;
            EnvMapTest.Compile();
            shader = EnvMapTest.Get();
            tex = texture;
            sphere = new QuadSphere(32);
            viewport = new Viewport3D(mw);
            viewport.DefaultOffset = new Vector3(0, 0, 4);
            viewport.ModelScale = 0.01f;
            viewport.Mode = CameraModes.Arcball;
            viewport.Background =  new Vector4(0.12f,0.12f,0.12f, 1f);
            viewport.ResetControls();
            cubemapIndex = shader.Shader.GetLocation("Cubemap");
            shader.Shader.SetInteger(cubemapIndex, 0);
            cameraPositionIndex = shader.Shader.GetLocation("CameraPosition");
        }
        public override void Draw()
        {
            viewport.Begin();
            var cam = new LookAtCamera();
            Matrix4x4 rot = Matrix4x4.CreateRotationX(viewport.CameraRotation.Y) *
                            Matrix4x4.CreateRotationY(viewport.CameraRotation.X);
            var dir = Vector3.Transform(-Vector3.UnitZ, rot);
            var to = Vector3.Zero;
            cam.Update(viewport.RenderWidth, viewport.RenderHeight, viewport.CameraOffset, to, rot);
            shader.SetViewProjection(cam);
            var w = Matrix4x4.Identity;
            var n = Matrix4x4.Identity;
            shader.SetWorld(ref w, ref n);
            shader.Shader.SetVector3(cameraPositionIndex, cam.Position);
            tex.BindTo(0);
            shader.UseProgram();
            for (int i = 0; i < 6; i++)
            {
                sphere.GetDrawParameters((CubeMapFace) i, out int start, out int count, out _);
                sphere.VertexBuffer.Draw(PrimitiveTypes.TriangleList, 0, start, count);
            }
            viewport.End();
        }

        public override void Dispose()
        {
            tex.Dispose();
            viewport.Dispose();
            sphere.VertexBuffer.Dispose();
        }
    }
}