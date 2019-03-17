// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Collections.Generic;
namespace LibreLancer
{
    public class XmlUIPanel : XmlUIElement
    {
        public XInt.Style Style;
        public bool Enabled = true;
        List<ModelInfo> models = new List<ModelInfo>();

        class ModelInfo
        {
            public IDrawable Drawable;
            public Matrix4 Transform;
            public List<ModifiedMaterial> Materials = new List<ModifiedMaterial>();
        }
        //Support color changes
        class ModifiedMaterial
        {
            public BasicMaterial Mat;
            public Color4 Dc;
        }

        protected Color4? modelColor;
        protected Color4? borderColor;
        protected Vector3 modelRotate;
        protected bool renderText = true;
        protected int modelIndex = 0;
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
            public void disable() => p.Enabled = false;
            public void enable() => p.Enabled = true;
            public void modelindex(int index)
            {
                p.modelIndex = index;
            }
        }

        public XmlUIPanel(XInt.Style style, XmlUIManager manager, bool setLua = true) : base(manager)
        {
            if(setLua) Lua = new PanelAPI(this);
            Style = style;
            if (style.Models != null)
            {
                foreach (var model in style.Models)
                {
                    var res = new ModelInfo();
                    res.Drawable = Manager.Game.ResourceManager.GetDrawable(
                       Manager.Game.GameData.ResolveDataPath(model.Path.Substring(2))
                    );
                    res.Transform = Matrix4.CreateScale(model.Transform[2], model.Transform[3], 1) *
                                      Matrix4.CreateTranslation(model.Transform[0], model.Transform[1], 0);
                    if (model.Color != null)
                    { //Dc is modified
                        var l0 = ((Utf.Cmp.ModelFile)res.Drawable).Levels[0];
                        var vms = l0.Mesh;
                        //Save Mesh material state
                        for (int i = l0.StartMesh; i < l0.StartMesh + l0.MeshCount; i++)
                        {
                            var mat = (BasicMaterial)vms.Meshes[i].Material?.Render;
                            if (mat == null) continue;
                            bool found = false;
                            foreach (var m in res.Materials)
                            {
                                if (m.Mat == mat)
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (found) continue;
                            res.Materials.Add(new ModifiedMaterial() { Mat = mat, Dc = mat.Dc });
                        }
                    }
                    models.Add(res);
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
            if(Style.Scissor)
            {
                Manager.Game.RenderState.ScissorEnabled = true;
                Manager.Game.RenderState.ScissorRectangle = r;
            }
            //Background (mostly for authoring purposes)
            if (Style.Background != null || Style.Border != null)
            {
                Manager.Game.Renderer2D.Start(Manager.Game.Width, Manager.Game.Height);
                if(Style.Background != null)
                    Manager.Game.Renderer2D.FillRectangle(r, Style.Background.Color);
                if (Style.Border != null)
                    Manager.Game.Renderer2D.DrawRectangle(r, borderColor ?? Style.Border.Color, 1);
                Manager.Game.Renderer2D.Finish();
            }
             
            if (Style.Models != null && modelIndex >= 0 && modelIndex < Style.Models.Length)
            {
                var stl = Style.Models[modelIndex];
                var mdl = models[modelIndex];
                if (stl.Color != null)
                {
                    var v = modelColor ?? stl.Color.Value;
                    for (int i = 0; i < mdl.Materials.Count; i++)
                        mdl.Materials[i].Mat.Dc = v;
                }
                mcam.CreateTransform(Manager.Game, r);
                mdl.Drawable.Update(mcam, delta, TimeSpan.FromSeconds(Manager.Game.TotalTime));
                Matrix4 rot = Matrix4.Identity;
                if (modelRotate != Vector3.Zero)
                    rot = Matrix4.CreateRotationX(modelRotate.X) *
                                 Matrix4.CreateRotationY(modelRotate.Y) *
                                 Matrix4.CreateRotationZ(modelRotate.Z);
                Manager.Game.RenderState.Cull = false;
                mdl.Drawable.Draw(Manager.Game.RenderState, mdl.Transform * rot, Lighting.Empty);
                Manager.Game.RenderState.Cull = true;
                if (stl.Color != null)
                {
                    for (int i = 0; i < mdl.Materials.Count; i++)
                        mdl.Materials[i].Mat.Dc = mdl.Materials[i].Dc;
                }
            }
            if(renderText && Texts.Count > 0) {
                Manager.Game.Renderer2D.Start(Manager.Game.Width, Manager.Game.Height);
                foreach (var t in Texts)
                    t.Draw(Manager, r);
                Manager.Game.Renderer2D.Finish();
            }
            if(Style.Scissor)
            {
                Manager.Game.RenderState.ScissorEnabled = false;
            }
        }
    }
}
