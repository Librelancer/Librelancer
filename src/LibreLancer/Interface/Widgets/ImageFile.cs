// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer;
using LibreLancer.Graphics;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class ImageFile : UiWidget
    {
       public string Path { get; set; }
       public bool Flip { get; set; } = true;
       public InterfaceColor Tint { get; set; }
       public bool Fill { get; set; }
       private Texture2D texture;
       private bool loaded = false;
       public override void Render(UiContext context, RectangleF parentRectangle)
       {
           if (!Visible) return;
           var myPos = context.AnchorPosition(parentRectangle, Anchor, X, Y, Width, Height);
           var myRectangle = new RectangleF(myPos.X, myPos.Y, Width, Height);
           if (Fill) myRectangle = parentRectangle;
           if(Background != null)
               Background.Draw(context, myRectangle);
           if (!loaded)
           {
               texture = context.Data.GetTextureFile(Path);
               loaded = true;
           }
           if (texture != null)
           {
               var color = (Tint ?? InterfaceColor.White).Color;
               context.RenderContext.Renderer2D.DrawImageStretched(texture, context.PointsToPixels(myRectangle), color, Flip);
           }
       }
    }
}
