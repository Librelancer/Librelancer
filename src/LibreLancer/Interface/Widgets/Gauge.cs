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

        public bool Reverse { get; set; }

        public override void Render(UiContext context, double delta, DrawList2D drawList)
        {
            if (!Visible) return;
            Background?.Draw(context, drawList, ClientRectangle);
            var fillRect = ClientRectangle;
            fillRect.Width *= PercentFilled;
            if (Reverse) {
                fillRect.X = ClientRectangle.X + Width - fillRect.Width;
            }
            Fill?.DrawWithClip(context, drawList, ClientRectangle, fillRect);
            Border?.Draw(context, drawList, ClientRectangle);
        }

    }
}
