// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Data.GameData;
using LibreLancer.Render;
using LibreLancer.Graphics;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class ModelView : Widget3D
    {
        public string ModelPath { get; set; } = null!;
        private string? _loadedPath;
        private RigidModel? model;

        private Lighting lighting;
        public ModelView()
        {
            lighting = Lighting.Create();
            lighting.Enabled = true;
            lighting.Ambient = Color3f.Black;
            var src = new SystemLighting();
            src.Lights.Add(new DynamicLight()
            {
                Light = new RenderLight()
                {
                    Kind = LightKind.Directional,
                    Direction = new Vector3(0, -1, 0),
                    Color = Color3f.White
                }
            });
            src.Lights.Add(new DynamicLight()
            {
                Light = new RenderLight()
                {
                    Kind = LightKind.Directional,
                    Direction = new Vector3(0, 0, 1),
                    Color = Color3f.White
                }
            });
            lighting.Lights.SourceLighting = src;
            lighting.Lights.SourceEnabled[0] = true;
            lighting.Lights.SourceEnabled[1] = true;
            lighting.NumberOfTilesX = -1;

            OrbitPan = new Vector2(-10.29f, -0.53f);
        }

        private void LoadModel(UiContext context)
        {
            if (string.IsNullOrWhiteSpace(ModelPath))
            {
                model = null;
                _loadedPath = null;
                return;
            }

            if (model == null || (_loadedPath != ModelPath))
            {
                _loadedPath = ModelPath;
                model = context.Data.GetModel(ModelPath);
            }
        }

        public override void Render(UiContext context, double delta, DrawList2D drawList)
        {
            if (ClientRectangle.Width <= 0 || ClientRectangle.Height <= 0) return;
            Background?.Draw(context, drawList, ClientRectangle);
            LoadModel(context);
            if (model != null) {
                drawList.AddCallback(_ => Draw3DViewport(context, ClientRectangle));
            }
            Border?.Draw(context, drawList, ClientRectangle);
        }

        protected override void Draw3DContent(UiContext context, RectangleF rect)
        {
            var cam = GetCamera(model!.GetRadius() * 2f, context, rect);
            context.RenderContext.SetCamera(cam);
            context.CommandBuffer.Camera = cam;
            context.CommandBuffer.StartFrame(context.RenderContext);
            model.UpdateTransform();
            model.Update(context.GlobalTime);
            model.DrawBuffer(0, context.CommandBuffer, context.Data.ResourceManager, Matrix4x4.Identity, ref lighting);
            context.CommandBuffer.DrawOpaque(context.RenderContext);
            context.RenderContext.DepthWrite = false;
            context.CommandBuffer.DrawTransparent(context.RenderContext);
            context.RenderContext.DepthWrite = true;
        }
    }
}
