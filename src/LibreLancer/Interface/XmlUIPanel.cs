// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Collections.Generic;
using LibreLancer.XInt;

namespace LibreLancer
{
    public class XmlUIPanel : XmlUIElement
    {
        public XInt.Style Style;
        public bool Enabled = true;
        public abstract class RenderElement
        {
            public string ID;
            public bool Enabled;
            public abstract void Draw(ref bool is2d, TimeSpan delta, Rectangle bounds, Renderer2D render2d);
        }

        public class ModelRenderElement : RenderElement
        {
            public XmlUIPanel Panel;
            public IDrawable Drawable;
            public Matrix4 Transform;
            public Model Style;
            public override void Draw(ref bool is2d, TimeSpan delta, Rectangle bounds, Renderer2D render2d)
            {
                if (!Enabled) return;
                if (is2d)
                    render2d.Finish();
                is2d = false;
                if (Style.Color != null || Panel.modelColor != null)
                {
                    SetupModifiedMaterials();
                    Color4 color;
                    if (Panel.modelColor != null) color = Panel.modelColor.Value;
                    else color = Style.Color.Value;
                    for (int i = 0; i < Materials.Count; i++)
                        Materials[i].Mat.Dc = color;
                }
                Panel.mcam.CreateTransform(Panel.Scene.Manager.Game, bounds);
                Drawable.Update(Panel.mcam, delta, TimeSpan.FromSeconds(Panel.Scene.Manager.Game.TotalTime));
                Matrix4 rot = Matrix4.Identity;
                if (Panel.modelRotate != Vector3.Zero)
                    rot = Matrix4.CreateRotationX(Panel.modelRotate.X) *
                          Matrix4.CreateRotationY(Panel.modelRotate.Y) *
                          Matrix4.CreateRotationZ(Panel.modelRotate.Z);
                Panel.Scene.Manager.Game.RenderState.Cull = false;
                Drawable.Draw(Panel.Scene.Manager.Game.RenderState, Transform * rot, Lighting.Empty);
                Panel.Scene.Manager.Game.RenderState.Cull = true;
                if(Style.Color != null || Panel.modelColor != null)
                    for (int i = 0; i < Materials.Count; i++)
                        Materials[i].Mat.Dc = Materials[i].Dc;
            }
            
            //Support color changes
            class ModifiedMaterial
            {
                public BasicMaterial Mat;
                public Color4 Dc;
            }
            private List<ModifiedMaterial> Materials;
            void SetupModifiedMaterials()
            {
                if (Materials != null) return;
                Materials = new List<ModifiedMaterial>();
                var l0 = ((Utf.Cmp.ModelFile)Drawable).Levels[0];
                var vms = l0.Mesh;
                //Save Mesh material state
                for (int i = l0.StartMesh; i < l0.StartMesh + l0.MeshCount; i++)
                {
                    var mat = (BasicMaterial)vms.Meshes[i].Material?.Render;
                    if (mat == null) continue;
                    bool found = false;
                    if (Materials.Any(x => x.Mat == mat)) continue;
                    Materials.Add(new ModifiedMaterial() { Mat = mat, Dc = mat.Dc });
                }
            }
        }

        public class TextRenderElement : RenderElement
        {
            public XmlUIPanel Panel;
            public TextElement Text;
            public override void Draw(ref bool is2d, TimeSpan delta, Rectangle bounds, Renderer2D render2d)
            {
                if (!Enabled) return;
                if(!is2d)
                    render2d.Start(Panel.Scene.GWidth, Panel.Scene.GHeight);
                is2d = true;
                Text.Draw(Panel.Scene.Manager, bounds);
            }
        }
        public class RectangleElement : RenderElement
        {
            public XmlUIPanel Panel;
            public StyleRectangle Style;
            public override void Draw(ref bool is2d, TimeSpan delta, Rectangle bounds, Renderer2D render2d)
            {
                if (!Enabled) return;
                if (Style.Color == null && Style.BorderColor == null) return;
                if (!is2d)
                    render2d.Start(Panel.Scene.GWidth, Panel.Scene.GHeight);
                is2d = true;
                var r = GetRectangle(bounds);
                if(Style.Color != null)
                    render2d.FillRectangle(r, Style.Color.Value);
                if(Style.BorderColor != null)
                    render2d.DrawRectangle(r, Style.BorderColor.Value, 1);
            }
            public Rectangle GetRectangle(Rectangle r)
            {
                var textR = new Rectangle(
                    (int)(r.X + r.Width * Style.X),
                    (int)(r.Y + r.Height * Style.Y),
                    (int)(r.Width * Style.Width),
                    (int)(r.Height * Style.Height)
                );
                return textR;
            }
        }
        
        protected Color4? modelColor;
        protected Color4? borderColor;
        protected Vector3 modelRotate;
        protected bool renderText = true;
        MatrixCamera mcam = new MatrixCamera(Matrix4.Identity);
        protected List<TextElement> Texts = new List<TextElement>();
        
        protected List<RenderElement> RenderElements = new List<RenderElement>();
        
        public XmlUIPanel(XInt.Panel pnl, XInt.Style style, XmlUIScene scene) : this(style, scene)
        {
            Positioning = pnl;
            ID = pnl.ID;
            if(pnl.Text != null)
            {
                foreach(var t in pnl.Text)
                {
                    var f = Texts.FirstOrDefault(x => x.ID == t.Item);
                    if (f != null)
                    {
                        var txt = scene.Manager.GetString(t.Strid, t.InfocardId, t.Value);
                        if (txt != null) f.Text = txt;
                    }
                }
            }
        }

        public class PanelAPI : LuaAPI
        {
            XmlUIPanel p;
            public PanelAPI(XmlUIPanel pnl) : base(pnl)
            {
                p = pnl;
            }
            public void disable() => p.Enabled = false;
            public void enable() => p.Enabled = true;
            
            public PartAccessor parts(string id) => new PartAccessor(p.RenderElements.Where(x => x.ID == id));

        }

        public class PartAccessor
        {
            IEnumerable<RenderElement> parts; 
            public PartAccessor(IEnumerable<RenderElement> parts) => this.parts = parts;
            public void hide()
            {
                foreach (var p in parts) p.Enabled = false;
            }

            public void show()
            {
                foreach (var p in parts) p.Enabled = true;
            }

            public void value(string str)
            {
                foreach (var t in parts.OfType<TextRenderElement>()) t.Text.Text = str;
            }

            public void color(Color4 c)
            {
                foreach (var t in parts.OfType<TextRenderElement>()) t.Text.ColorOverride = c;
            }
            
        }
        public XmlUIPanel(XInt.Style style, XmlUIScene scene, bool setLua = true) : base(scene)
        {
            if (setLua) Lua = new PanelAPI(this);
            Style = style;
            if (style.DrawElements != null)
            {
                foreach (var e in style.DrawElements)
                {
                    switch (e)
                    {
                        case Model mdl:
                            RenderElements.Add(new ModelRenderElement()
                            {
                                Panel = this, Enabled = e.Enabled, ID = e.ID,
                                Drawable = Scene.Manager.Game.ResourceManager.GetDrawable(
                                    Scene.Manager.Game.GameData.ResolveDataPath(mdl.Path.Substring(2))
                                ),
                                Transform = Matrix4.CreateScale(mdl.Transform[2], mdl.Transform[3], 1) *
                                            Matrix4.CreateTranslation(mdl.Transform[0], mdl.Transform[1], 0),
                                Style = mdl
                            });
                            break;
                        case StyleText txt:
                            var telem = new TextElement(txt);
                            Texts.Add(telem);
                            RenderElements.Add(new TextRenderElement
                            {
                                Text = telem, Panel = this, Enabled = txt.Enabled,
                                ID = txt.ID
                            });
                            break;
                        case StyleRectangle rect:
                            RenderElements.Add(new RectangleElement()
                            {
                                Panel = this, Enabled = e.Enabled, ID = e.ID,
                                Style = rect
                            });
                            break;
                    }
                }
            }
        }

        public override Vector2 CalculateSize()
        {
            var h = Scene.GHeight * Style.Size.Height;
            var w = h * Style.Size.Ratio;
            if (Style.Size.WidthText != null)
                w = Scene.GWidth * Style.Size.Width;
            return new Vector2(w, h);
        }

        protected override void DrawInternal(TimeSpan delta)
        {
            var cz = CalculateSize();
            var h = cz.Y;
            var w = cz.X;

            var pos = CalculatePosition();
            int px = (int)pos.X, py = (int)pos.Y;
            if (Animation != null && (Animation.Remain || Animation.Running))
            {
                px = (int)Animation.CurrentPosition.X;
                py = (int)Animation.CurrentPosition.Y;
            }
            var r = new Rectangle(px, py, (int)w, (int)h);
            if (Style.Scissor)
            {
                Scene.Manager.Game.RenderState.ScissorEnabled = true;
                Scene.Manager.Game.RenderState.ScissorRectangle = r;
            }
            bool is2d = false;
            //Background (mostly for authoring purposes)
            if (Style.Background != null || Style.Border != null)
            {
                is2d = true;
                Scene.Renderer2D.Start(Scene.GWidth, Scene.GHeight);
                if (Style.Background != null)
                    Scene.Renderer2D.FillRectangle(r, Style.Background.Color);
                if (Style.Border != null)
                    Scene.Renderer2D.DrawRectangle(r, borderColor ?? Style.Border.Color, 1);
            }
            foreach (var element in RenderElements)
                element.Draw(ref is2d, delta, r, Scene.Manager.Game.Renderer2D);
            if(is2d)
                Scene.Manager.Game.Renderer2D.Finish();
            if (Style.Scissor)
            {
                Scene.Manager.Game.RenderState.ScissorEnabled = false;
            }
        }
    }
}
