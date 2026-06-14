// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Graphics;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class ImageFile : UiWidget
    {
        public string Path { get; set; } = "";
        public bool Flip { get; set; } = true;
        public InterfaceColor? Tint { get; set; }
        public bool Fill { get; set; }
        private Texture2D? texture;
        private bool loaded = false;

        public override void Render(UiContext context, double delta, DrawList2D drawList)
        {
            if (!Visible)
            {
                return;
            }

            Background?.Draw(context, drawList, ClientRectangle);

            if (!loaded)
            {
                texture = context.Data.GetTextureFile(Path);
                loaded = true;
            }

            if (texture != null)
            {
                var color = (Tint ?? InterfaceColor.White).Color;
                drawList.DrawImageStretched(texture, context.PointsToPixels(ClientRectangle), color, Flip);
            }
        }
    }
}
