// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using MoonSharp.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [MoonSharpUserData]
    public class ModelView : Widget3D
    {
        public ModelView()
        {
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
            context.CommandBuffer.StartFrame(context.RenderContext);
            model.UpdateTransform();
            model.Update(cam, context.GlobalTime, context.Data.ResourceManager);
            model.DrawBuffer(0, context.CommandBuffer, context.Data.ResourceManager, Matrix4x4.Identity, ref Lighting.Empty);
            context.CommandBuffer.DrawOpaque(context.RenderContext);
            context.RenderContext.DepthWrite = false;
            context.CommandBuffer.DrawTransparent(context.RenderContext);
            context.RenderContext.DepthWrite = true;
        }
    }
}