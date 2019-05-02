// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer
{
    public class XmlUIImage : XmlUIElement
    {
        Texture2D texture;

        public XmlUIImage(XInt.Image img, XmlUIScene scene) : base(scene)
        {
            texture = ImageLib.Generic.FromFile(scene.Manager.Game.GameData.ResolveDataPath(img.Path.Substring(2)));
        }

        protected override void DrawInternal(TimeSpan delta)
        {
            Scene.Renderer2D.Start(Scene.GWidth, Scene.GHeight);
            Scene.Renderer2D.DrawImageStretched(texture, new Rectangle(0, 0, Scene.GWidth, Scene.GHeight), Color4.White, true);
            Scene.Renderer2D.Finish();
            base.DrawInternal(delta);
        }
    }
}
