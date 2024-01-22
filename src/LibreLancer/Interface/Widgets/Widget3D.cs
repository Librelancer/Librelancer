// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Graphics;
using LibreLancer.Render.Cameras;

namespace LibreLancer.Interface
{
    public abstract class Widget3D : UiWidget
    {
        public bool CanRotate { get; set; } = true;

        protected virtual RectangleF GetMyRectangle(UiContext context, RectangleF parentRectangle)
        {
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
            Update(context, myPos);
            myPos = AnimatedPosition(myPos);
            var myRect = new RectangleF(myPos.X,myPos.Y, Width, Height);
            return myRect;
        }

        protected Vector2 OrbitPan;
        Vector2 dragStart = Vector2.Zero;
        private bool dragging = false;

        public override void OnMouseDown(UiContext context, RectangleF parentRectangle)
        {
            var myRect = GetMyRectangle(context, parentRectangle);
            if (CanRotate && myRect.Contains(context.MouseX, context.MouseY))
            {
                dragStart = new Vector2(context.MouseX, context.MouseY);
                dragging = true;
            }
        }

        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            if (!context.MouseLeftDown || !CanRotate)
            {
                dragging = false;
            }

            if (dragging)
            {
                var delta = new Vector2(context.MouseX, context.MouseY) - dragStart;
                OrbitPan += (delta / 75) * new Vector2(1, -1);
                dragStart = new Vector2(context.MouseX, context.MouseY);
            }
        }

        private LookAtCamera cam = new LookAtCamera();
        protected LookAtCamera GetCamera(float zoom, UiContext context, RectangleF rect)
        {
            var pxRect = context.PointsToPixels(rect);
            OrbitPan.Y = MathHelper.Clamp(OrbitPan.Y,-MathHelper.PiOver2 + 0.02f, MathHelper.PiOver2 - 0.02f);
            var mat = Matrix4x4.CreateFromYawPitchRoll(-OrbitPan.X, OrbitPan.Y, 0);
            var from = Vector3.Transform(new Vector3(0, 0, zoom), mat);
            cam.Update(pxRect.Width, pxRect.Height, from, Vector3.Zero);
            return cam;
        }

        protected void Draw3DViewport(UiContext context, RectangleF rect)
        {
            context.RenderContext.Flush();
            var pxRect = context.PointsToPixels(rect);
            if(pxRect.Width <= 0 || pxRect.Height <= 0) return;
            //setup
            var rTarget = new RenderTarget2D(context.RenderContext, pxRect.Width, pxRect.Height);
            var prevRt = context.RenderContext.RenderTarget;
            context.RenderContext.RenderTarget = rTarget;
            context.RenderContext.PushViewport(0,0, pxRect.Width, pxRect.Height);
            context.RenderContext.Cull = true;
            context.RenderContext.DepthEnabled = true;
            context.RenderContext.ClearColor = Color4.Transparent;
            context.RenderContext.ClearAll();
            //draw
            Draw3DContent(context, rect);
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

        protected abstract void Draw3DContent(UiContext context, RectangleF rect);
    }
}
