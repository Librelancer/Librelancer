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
using System.Linq;
using System.Collections.Generic;
namespace LibreLancer
{
    public class XmlUIPanel : XmlUIElement
    {
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
        protected Color4? modelColor;
        protected Vector3 modelRotate;
        MatrixCamera mcam = new MatrixCamera(Matrix4.Identity);
        protected List<TextElement> Texts = new List<TextElement>();
        public XmlUIPanel(XInt.Panel pnl, XInt.Style style, XmlUIManager manager) : this(style,manager)
        {
            Positioning = pnl;
            ID = pnl.ID;
        }

        public class PanelAPI : LuaAPI
        {
            XmlUIPanel p;
            public PanelAPI(XmlUIPanel pnl) : base(pnl)
            {
                p = pnl;
            }
            public TextElement.LuaAPI text(string id)
            {
                return p.Texts.Where((x) => x.ID == id).First().Lua;
            }

        }
        public XmlUIPanel(XInt.Style style, XmlUIManager manager) : base(manager)
        {
            Lua = new PanelAPI(this);
            Style = style;
            drawable = Manager.Game.ResourceManager.GetDrawable(
               Manager.Game.GameData.ResolveDataPath(style.Model.Path.Substring(2))
            );

            transform = Matrix4.CreateScale(style.Model.Transform[2], style.Model.Transform[3], 1) *
                              Matrix4.CreateTranslation(style.Model.Transform[0], style.Model.Transform[1], 0);
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
            if(Style.Texts != null) {
                foreach(var t in Style.Texts) {
                    Texts.Add(new TextElement(t));
                }
            }
        }

        public override Vector2 CalculateSize()
        {
            var h = Manager.Game.Height * Style.Size.Height;
            var w = h * Style.Size.Ratio;
            return new Vector2(w, h);
        }
        protected override void DrawInternal(TimeSpan delta)
        {
            var h = Manager.Game.Height * Style.Size.Height;
            var w = h * Style.Size.Ratio;
            var pos = CalculatePosition();
            int px = (int)pos.X, py = (int)pos.Y;
            if (Animation != null && (Animation.Remain || Animation.Running))
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
                var v = modelColor ?? Style.Model.Color.Value;
                for (int i = 0; i < materials.Count; i++)
                    materials[i].Mat.Dc = v;
            }
            mcam.CreateTransform(Manager.Game, r);
            drawable.Update(mcam, delta, TimeSpan.FromSeconds(Manager.Game.TotalTime));
            Matrix4 rot = Matrix4.Identity;
            if (modelRotate != Vector3.Zero)
                rot = Matrix4.CreateRotationX(modelRotate.X) *
                             Matrix4.CreateRotationY(modelRotate.Y) *
                             Matrix4.CreateRotationZ(modelRotate.Z);
            Manager.Game.RenderState.Cull = false;
            drawable.Draw(Manager.Game.RenderState, transform * rot, Lighting.Empty);
            Manager.Game.RenderState.Cull = true;
            if (Style.Model.Color != null)
            {
                for (int i = 0; i < materials.Count; i++)
                    materials[i].Mat.Dc = materials[i].Dc;
            }
            if(Texts.Count > 0) {
                Manager.Game.Renderer2D.Start(Manager.Game.Width, Manager.Game.Height);
                foreach (var t in Texts)
                    t.Draw(Manager, r);
                Manager.Game.Renderer2D.Finish();
            }
        }
    }
}
