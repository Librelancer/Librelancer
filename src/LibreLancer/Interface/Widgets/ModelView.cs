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
    public class ModelView : UiWidget
    {
        RectangleF GetMyRectangle(UiContext context, RectangleF parentRectangle)
        {
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
            var myRectangle = new RectangleF(myPos.X, myPos.Y, Width, Height);
            return myRectangle;
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
            var rect = GetMyRectangle(context, parentRectangle);
            if (rect.Width <= 0 || rect.Height <= 0) return;
            Background?.Draw(context, rect);
            LoadModel(context);
            if (model != null) {
                DrawModel(context, rect);
            }
            Border?.Draw(context, rect);
        }


        void DrawModel(UiContext context, RectangleF rect)
        {
            context.RenderContext.Flush();
            var pxRect = context.PointsToPixels(rect);
            if(pxRect.Width <= 0 || pxRect.Height <= 0) return;
            //setup
            var rTarget = new RenderTarget2D(pxRect.Width, pxRect.Height);
            var prevRt = context.RenderContext.RenderTarget;
            context.RenderContext.RenderTarget = rTarget;
            context.RenderContext.PushViewport(0,0, pxRect.Width, pxRect.Height);
            context.RenderContext.Cull = true;
            context.RenderContext.DepthEnabled = true;
            context.RenderContext.ClearColor = Color4.Transparent;
            context.RenderContext.ClearAll();
            //draw
            context.CommandBuffer.StartFrame(context.RenderContext);
            var cam = new LookAtCamera();
            cam.Update(pxRect.Width, pxRect.Height, new Vector3(0, 0, model.GetRadius() * 3f),
                Vector3.Zero, Matrix4x4.CreateRotationX(1.5f));
            model.UpdateTransform();
            model.Update(cam, context.GlobalTime, context.Data.ResourceManager);
            model.DrawBuffer(0, context.CommandBuffer, context.Data.ResourceManager, Matrix4x4.Identity, ref Lighting.Empty);
            context.CommandBuffer.DrawOpaque(context.RenderContext);
            context.RenderContext.DepthWrite = false;
            context.CommandBuffer.DrawTransparent(context.RenderContext);
            context.RenderContext.DepthWrite = true;
            //blit to screen
            context.RenderContext.PopViewport();
            context.RenderContext.DepthEnabled = false;
            context.RenderContext.RenderTarget = prevRt;
            context.RenderContext.Renderer2D.Draw(rTarget.Texture, new Rectangle(0, 0, pxRect.Width, pxRect.Height),
                pxRect,
                Color4.White, BlendMode.Normal, true);
            context.RenderContext.Flush(); //need to flush before disposing RT
            rTarget.Dispose();
        }
    }
}