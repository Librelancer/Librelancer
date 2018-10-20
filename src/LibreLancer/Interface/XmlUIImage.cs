// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer
{
    public class XmlUIImage : XmlUIElement
    {
        Texture2D texture;

        public XmlUIImage(XInt.Image img, XmlUIManager manager) : base(manager)
        {
            texture = ImageLib.Generic.FromFile(manager.Game.GameData.ResolveDataPath(img.Path.Substring(2)));
        }

        protected override void DrawInternal(TimeSpan delta)
        {
            Manager.Game.Renderer2D.Start(Manager.Game.Width, Manager.Game.Height);
            Manager.Game.Renderer2D.DrawImageStretched(texture, new Rectangle(0, 0, Manager.Game.Width, Manager.Game.Height), Color4.White, true);
            Manager.Game.Renderer2D.Finish();
            base.DrawInternal(delta);
        }
    }
}
