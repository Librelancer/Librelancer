// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using LibreLancer;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class DisplayColor : DisplayElement
    {
        public InterfaceColor Color { get; set; }
        public override void Render(UiContext context, RectangleF clientRectangle)
        {
            if(!Enabled) return;
            if (Color == null) return;
            var rect = context.PointsToPixels(clientRectangle);
            context.RenderContext.Renderer2D.FillRectangle(rect, Color.GetColor(context.GlobalTime));
        }
    }
}
