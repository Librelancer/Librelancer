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
    public class Gauge : UiWidget
    {
        public UiRenderable? Fill { get; set; }
        public float PercentFilled { get; set; }
        public float PercentStart { get; set; }

        public bool Reverse { get; set; }

        public override void Render(UiContext context, double delta, DrawList2D drawList)
        {
            if (!Visible) return;
            Background?.Draw(context, drawList, ClientRectangle);
            var fillRect = ClientRectangle;
            var start = MathHelper.Clamp(PercentStart, 0, 1);
            var amount = MathHelper.Clamp(PercentFilled, 0, 1 - start);
            fillRect.X += ClientRectangle.Width * start;
            fillRect.Width = ClientRectangle.Width * amount;
            if (Reverse) {
                fillRect.X = ClientRectangle.X + Width - ClientRectangle.Width * start - fillRect.Width;
            }
            Fill?.DrawWithClip(context, drawList, ClientRectangle, fillRect);
            Border?.Draw(context, drawList, ClientRectangle);
        }

    }
}
