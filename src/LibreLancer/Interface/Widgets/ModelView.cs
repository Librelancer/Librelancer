// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Data.GameData;
using LibreLancer.Render;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class ModelView : Widget3D
    {
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

        public string ModelPath { get; set; }
        private string _loadedPath;
        private RigidModel model;

        void LoadModel(UiContext context)
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

        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            base.Render(context, parentRectangle);
            var rect = GetMyRectangle(context, parentRectangle);
            if (rect.Width <= 0 || rect.Height <= 0) return;
            Background?.Draw(context, rect);
            LoadModel(context);
            if (model != null) {
                Draw3DViewport(context, rect);
            }
            Border?.Draw(context, rect);
        }

        protected override void Draw3DContent(UiContext context, RectangleF rect)
        {
            var cam = GetCamera(model.GetRadius() * 2f, context, rect);
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
