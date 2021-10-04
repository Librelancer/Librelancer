// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using MoonSharp.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [MoonSharpUserData]
    public class CharacterView : UiWidget    
    {
        public string Costume { get; set; }
        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            if (!Visible) return;
            if (Width < 2 || Height < 2) return;
            var rect = GetMyRectangle(context, parentRectangle);
            Background?.Draw(context, rect);
            RenderCharacter(context, rect);
            Border?.Draw(context, rect);
        }
        
        void RenderCharacter(UiContext context, RectangleF myRect)
        {
            var px = context.PointsToPixels(myRect);
            //Get current state
            var rs = context.RenderContext;
            var cc = rs.ClearColor;
            var prevRt = rs.RenderTarget;
            //new state
            rs.DepthEnabled = true;
            var renderTarget = new RenderTarget2D(px.Width, px.Height);
            rs.RenderTarget = renderTarget;
            rs.ClearColor = Color4.Transparent;
            rs.ClearAll();
            //draw
            
            //restore state
            rs.RenderTarget = prevRt;
            rs.DepthEnabled = false;
            rs.ClearColor = cc;
            context.RenderContext.Renderer2D.Draw(renderTarget.Texture, new Rectangle(0,0,px.Width,px.Height), px, Color4.White);
            //Free
            renderTarget.Dispose();
        }
        RectangleF GetMyRectangle(UiContext context, RectangleF parentRectangle)
        {
            var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
            Update(context, myPos);
            myPos = AnimatedPosition(myPos);
            var myRect = new RectangleF(myPos.X,myPos.Y, Width, Height);
            return myRect;
        }
    }
}