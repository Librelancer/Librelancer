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
    public class TextureImage : UiWidget
    {
        private string? _name;
        public string? Name
        {
            get { return _name;}
            set
            {
                loaded = false;
                _name = value;
            }
        }

        public bool Flip { get; set; } = true;
        public InterfaceColor Tint { get; set; }
        public bool Fill { get; set; }
        private Texture2D? texture;
        private bool loaded = false;

        public override void OnLayout(UiContext context, Layout layout, double delta)
        {
            base.OnLayout(context, layout, delta);
            if (Fill)
                ClientRectangle = layout.Fill();
        }

        public override void Render(UiContext context, double delta, DrawList2D drawList)
        {
            if (!Visible)
            {
                return;
            }

            Background?.Draw(context, drawList, ClientRectangle);

            if (!string.IsNullOrEmpty(Name) && (!loaded || texture == null || texture.IsDisposed))
            {
                texture = (context.Data.ResourceManager.FindTexture(Name) as Texture2D)!;
                loaded = true;
            }

            if (texture != null)
            {
                var color = (Tint ?? InterfaceColor.White).Color;
               drawList.DrawImageStretched(texture, context.PointsToPixels(ClientRectangle), color, Flip);
            }

            Border?.Draw(context, drawList, ClientRectangle);
        }
    }
}
