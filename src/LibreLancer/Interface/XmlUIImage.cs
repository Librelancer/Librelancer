/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
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
