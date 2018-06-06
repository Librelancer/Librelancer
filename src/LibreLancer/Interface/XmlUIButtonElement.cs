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
using System.Collections.Generic;
namespace LibreLancer
{
    public class XmlUIButton : XmlUIElement
    {
        public XInt.Button Button;
        public XInt.Style Style;

        IDrawable drawable;
        Matrix4 transform;

        //Support color changes
        class ModifiedMaterial
        {
            public BasicMaterial Mat;
            public Color4 Dc;
        }
        List<ModifiedMaterial> materials = new List<ModifiedMaterial>();
        ModifiedStyle hoverStyle = new ModifiedStyle();
        Neo.IronLua.LuaChunk hoverChunk;
        class ModifiedStyle
        {
            public Color4? ModelColor;
            public Color4? TextColor;

            public void modelcolor(Color4 c) => ModelColor = c;
            public void textcolor(Color4 c) => TextColor = c;

            public void Reset()
            {
                ModelColor = null;
                TextColor = null;
            }
        }

        public XmlUIButton(XmlUIManager manager, XInt.Button button, XInt.Style style) : base(manager)
        {
            Button = button;
            Positioning = button;
            Style = style;
            if (Style.HoverStyle != null)
            {
                hoverChunk = LuaStyleEnvironment.L.CompileChunk(
                    style.HoverStyle, "buttonHover", new Neo.IronLua.LuaCompileOptions()
                );
            }
            drawable = Manager.Game.ResourceManager.GetDrawable(
                Manager.Game.GameData.ResolveDataPath(style.Model.Path.Substring(2))
            );
            transform = Matrix4.CreateScale(style.Model.Transform[2], style.Model.Transform[3], 1) *
                              Matrix4.CreateTranslation(style.Model.Transform[0], style.Model.Transform[1], 0);
            ID = button.ID;
            if (Style.Model.Color != null)
            { //Dc is modified
                var l0 = ((Utf.Cmp.ModelFile)drawable).Levels[0];
                var vms = l0.Mesh;
                //Save Mesh material state
                for (int i = l0.StartMesh; i < l0.StartMesh + l0.MeshCount; i++)
                {
                    var mat = (BasicMaterial)vms.Meshes[i].Material?.Render;
                    if (mat == null) continue;
                    bool found = false;
                    foreach (var m in materials)
                    {
                        if (m.Mat == mat)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found) continue;
                    materials.Add(new ModifiedMaterial() { Mat = mat, Dc = mat.Dc });
                }
            }
        }
        public override float CalculateWidth()
        {
            var h = Manager.Game.Height * Style.Size.Height;
            var w = h * Style.Size.Ratio;
            return w;
        }
        bool lastDown = false;
        protected override void UpdateInternal(TimeSpan delta)
        {
            base.UpdateInternal(delta);
            if (Animation != null && Animation.Running) return;

            var h = Manager.Game.Height * Style.Size.Height;
            var w = h * Style.Size.Ratio;
            var pos = CalculatePosition();
            var r = new Rectangle((int)pos.X, (int)pos.Y, (int)w, (int)h);
            hoverStyle.Reset();
            if (r.Contains(Manager.Game.Mouse.X, Manager.Game.Mouse.Y))
            {
                if (!lastDown && Manager.Game.Mouse.IsButtonDown(MouseButtons.Left))
                {
                    if (!string.IsNullOrEmpty(Button.OnClick))
                        Manager.Call(Button.OnClick);
                }
                if(hoverChunk != null)
                    LuaStyleEnvironment.Do(hoverChunk, hoverStyle, (float)Manager.Game.TotalTime);
            }
           
            lastDown = Manager.Game.Mouse.IsButtonDown(MouseButtons.Left);
        }
        protected override void DrawInternal(TimeSpan delta)
        {
            var h = Manager.Game.Height * Style.Size.Height;
            var w = h * Style.Size.Ratio;
            var pos = CalculatePosition();
            int px = (int)pos.X, py = (int)pos.Y;
            if (Animation != null && Animation.Running)
            {
                px = (int)Animation.CurrentPosition.X;
                py = (int)Animation.CurrentPosition.Y;
            }
            var r = new Rectangle(px, py, (int)w, (int)h);
            //Background (mostly for authoring purposes)
            if (Style.Background != null)
            {
                Manager.Game.Renderer2D.Start(Manager.Game.Width, Manager.Game.Height);
                Manager.Game.Renderer2D.FillRectangle(r, Style.Background.Color);
                Manager.Game.Renderer2D.Finish();
            }
            //Draw Model - TODO: Optional
            if (Style.Model.Color != null)
            {
                var v = hoverStyle.ModelColor ?? Style.Model.Color.Value;
                for (int i = 0; i < materials.Count; i++)
                    materials[i].Mat.Dc = v;
            }
            drawable.Update(new MatrixCamera(MatrixCamera.CreateTransform(Manager.Game, r)), delta, TimeSpan.FromSeconds(Manager.Game.TotalTime));
            drawable.Draw(Manager.Game.RenderState, transform, Lighting.Empty);
            if (Style.Model.Color != null)
            {
                for (int i = 0; i < materials.Count; i++)
                    materials[i].Mat.Dc = materials[i].Dc;
            }
            //Draw Text
            if (!string.IsNullOrEmpty(Button.Text) && Style.Text != null)
            {
                var t = Style.Text;
                var textR = new Rectangle(
                    (int)(r.X + r.Width * t.X),
                    (int)(r.Y + r.Height * t.Y),
                    (int)(r.Width * t.Width),
                    (int)(r.Height * t.Height)
                );
                Manager.Game.Renderer2D.Start(Manager.Game.Width, Manager.Game.Height);
                if (t.Background != null)
                {
                    Manager.Game.Renderer2D.FillRectangle(textR, t.Background.Value);
                }
                DrawTextCentered(Manager.Font, GetTextSize(textR.Height), Button.Text, textR, hoverStyle.TextColor ?? t.Color, t.Shadow);
                Manager.Game.Renderer2D.Finish();
            }
        }
    }
}
