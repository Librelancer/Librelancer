// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using LibreLancer;
using LibreLancer.Graphics;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class DisplayColor : DisplayElement
    {
        public InterfaceColor? Color { get; set; }
        public override void Render(UiContext context, DrawList2D drawList, RectangleF clientRectangle, float alpha)
        {
            if(!Enabled)
            {
                return;
            }

            if (Color == null)
            {
                return;
            }

            var rect = context.PointsToPixels(clientRectangle);
            var c = Color.GetColor(context.GlobalTime);
            c.A *= alpha;
            drawList.FillRectangle(rect, c);
        }
    }
}
