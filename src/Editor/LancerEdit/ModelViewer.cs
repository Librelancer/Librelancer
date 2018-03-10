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
using LibreLancer;
using LibreLancer.Vertices;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Mat;
using ImGuiNET;
namespace LancerEdit
{
    public class ModelViewer : DockTab
    {
        RenderTarget2D renderTarget;
        int rw = -1, rh = -1;
        int rid = 0;
        bool open = true;
        Lighting lighting;
        IDrawable drawable;
        RenderState rstate;
        CommandBuffer buffer;
        ViewportManager vps;
        ResourceManager res;
        public string Name;
        int viewMode = 0;
        static readonly string[] viewModes = new string[] {
            "Textured",
            "Lit",
            "Flat",
            "Normals",
            "None"
        };
        bool doWireframe = false;
        const int M_TEXTURED = 0;
        const int M_LIT = 1;
        const int M_FLAT = 2;
        const int M_NORMALS = 3;
        const int M_NONE = 4;

        static readonly Color4[] initialCmpColors = new Color4[] {
            Color4.White,
            Color4.Red,
            Color4.LightGreen,
            Color4.Blue,
            Color4.Yellow,
            Color4.Magenta,
            Color4.DarkGreen,
            Color4.Cyan,
            Color4.Orange
        };

        class PartHps
        {
            public Part Part;
            public List<HardpointGizmo> Gizmos = new List<HardpointGizmo>();
        }
        Material wireframeMaterial3db;
        Material normalsDebugMaterial;
        Dictionary<int, Material> partMaterials = new Dictionary<int, Material>();
        List<PartHps> partlist = new List<PartHps>();
        AnimationComponent animator;
        public ModelViewer(string title, string name, IDrawable drawable, RenderState rstate, ViewportManager viewports, CommandBuffer commands, ResourceManager res)
        {
            Title = title;
            Name = name;
            this.drawable = drawable;
            this.rstate = rstate;
            this.vps = viewports;
            this.res = res;
            buffer = commands;
            wireframeMaterial3db = new Material(res);
            wireframeMaterial3db.Dc = Color4.White;
            wireframeMaterial3db.DtName = ResourceManager.WhiteTextureName;
            normalsDebugMaterial = new Material(res);
            normalsDebugMaterial.Type = "NormalDebugMaterial";
            lighting = Lighting.Create();
            lighting.Enabled = true;
            lighting.Ambient = Color4.Black;
            var src = new SystemLighting();
            src.Lights.Add(new DynamicLight()
            {
                Light = new RenderLight()
                {
                    Kind = LightKind.Directional,
                    Direction = new Vector3(0, -1, 0),
                    Color = Color4.White
                }
            });
            src.Lights.Add(new DynamicLight()
            {
                Light = new RenderLight()
                {
                    Kind = LightKind.Directional,
                    Direction = new Vector3(0, 0, 1),
                    Color = Color4.White
                }
            });
            lighting.Lights.SourceLighting = src;
            lighting.Lights.SourceEnabled[0] = true;
            lighting.Lights.SourceEnabled[1] = true;
            lighting.NumberOfTilesX = -1;
            GizmoRender.Init(res);
            zoom = drawable.GetRadius() * 2;
            if (drawable is CmpFile)
            {
                var cmp = (CmpFile)drawable;
                foreach (var p in cmp.Parts)
                {
                    var parentHp = p.Value.Construct != null ? p.Value.Construct.Transform : Matrix4.Identity;
                    var php = new PartHps() { Part = p.Value };
                    foreach (var hp in p.Value.Model.Hardpoints)
                    {
                        php.Gizmos.Add(new HardpointGizmo(hp, hp.Transform * parentHp));
                    }
                    partlist.Add(php);
                }
                if (cmp.Animation != null)
                    animator = new AnimationComponent(cmp.Constructs, cmp.Animation);
            }
            else if (drawable is ModelFile)
            {
                var php = new PartHps() { Part = null };
                foreach (var hp in ((ModelFile)drawable).Hardpoints)
                {
                    php.Gizmos.Add(new HardpointGizmo(hp, hp.Transform));
                }
                partlist.Add(php);
            }
        }

        public override void Update(double elapsed)
        {
            if (animator != null)
                animator.Update(TimeSpan.FromSeconds(elapsed));
        }
        Vector2 rotation = Vector2.Zero;
        bool hpsopen = false;
        bool animsopen = false;
        bool firstTab = true;
        float zoom = 0;
        Color4 background = Color4.CornflowerBlue * new Color4(0.3f, 0.3f, 0.3f, 1f);
        System.Numerics.Vector3 editCol;
        public override bool Draw()
        {
            bool doTabs = hpsopen || animsopen;
            var contentw = ImGui.GetContentRegionAvailableWidth();
            if (doTabs)
            {
                ImGui.Columns(2, "##panels", true);
                if(firstTab) {
                    ImGui.SetColumnWidth(0, contentw * 0.23f);
                    firstTab = false;
                }
                ImGui.BeginChild("##tabchild");
                if (hpsopen)
                {
                    if (partlist.Count == 1 && partlist[0].Part == null)
                    {
                        DoChecks(partlist[0].Gizmos);
                    }
                    else if (partlist.Count > 0)
                    {
                        int j = 0;
                        foreach (var pl in partlist)
                        {
                            if (pl.Gizmos.Count == 0) continue;
                            if (ImGui.CollapsingHeader(pl.Part.ObjectName,
                                                      pl.Part.ObjectName + "_" + j++,
                                                      false, true))
                            {
                                DoChecks(pl.Gizmos);
                            }
                        }
                    }
                }
                if (animsopen)
                {
                    var anm = ((CmpFile)drawable).Animation;
                    int j = 0;
                    foreach (var sc in anm.Scripts)
                    {
                        if (ImGui.Button(sc.Key + "###" + j++))
                        {
                            animator.StartAnimation(sc.Key, false);
                        }
                    }
                }
                ImGui.EndChild();
                ImGui.NextColumn();
            }
            ImGuiNative.igBeginGroup();
            if (TabHandler.VerticalTab("Hardpoints", hpsopen))
            {
                if (!hpsopen)
                {
                    hpsopen = true;
                    animsopen = false;
                }
                else
                    hpsopen = false;
            }
            if (drawable is CmpFile && ((CmpFile)drawable).Animation != null)
            {
                if (TabHandler.VerticalTab("Animations", animsopen))
                {
                    if (!animsopen)
                    {
                        animsopen = true;
                        hpsopen = false;
                    }
                    else
                        animsopen = false;
                }
            }
            ImGuiNative.igEndGroup();
            ImGui.SameLine();
            ImGui.BeginChild("##main");
            if (ImGui.ColorButton("Background Color", new Vector4(background.R, background.G, background.B, 1),
                                ColorEditFlags.NoAlpha, new Vector2(22, 22)))
            {
                ImGui.OpenPopup("Background Color###" + Unique);
                editCol = new System.Numerics.Vector3(background.R, background.G, background.B);
            }
            if (ImGui.BeginPopupModal("Background Color###" + Unique, WindowFlags.AlwaysAutoResize))
            {
                ImGui.ColorPicker3("###a", ref editCol);
                if (ImGui.Button("OK"))
                {
                    background = new Color4(editCol.X, editCol.Y, editCol.Z, 1);
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button("Default"))
                {
                    var def = Color4.CornflowerBlue * new Color4(0.3f, 0.3f, 0.3f, 1f);
                    editCol = new System.Numerics.Vector3(def.R, def.G, def.B);
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel")) ImGui.CloseCurrentPopup();
                ImGui.EndPopup();
            }
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Background");
            ImGui.SameLine();
            ImGui.Checkbox("Wireframe", ref doWireframe);
            ImGui.SameLine();
            ImGui.Text("View Mode:");
            ImGui.SameLine();
            ImGui.PushItemWidth(-1);
            ImGui.Combo("##modes", ref viewMode, viewModes);
            ImGui.PopItemWidth();
            var renderWidth = Math.Max(120, (int)ImGui.GetWindowWidth() - 15);
            var renderHeight = Math.Max(120, (int)ImGui.GetWindowHeight() - 40);
            //Generate render target
            if (rh != renderHeight || rw != renderWidth)
            {
                if (renderTarget != null)
                {
                    ImGuiHelper.DeregisterTexture(renderTarget);
                    renderTarget.Dispose();
                }
                renderTarget = new RenderTarget2D(renderWidth, renderHeight);
                rid = ImGuiHelper.RegisterTexture(renderTarget);
                rw = renderWidth;
                rh = renderHeight;
            }
            DrawGL(renderWidth, renderHeight);
            ImGui.ImageButton((IntPtr)rid, new Vector2(renderWidth, renderHeight),
                              Vector2.Zero, Vector2.One,
                              0,
                              Vector4.One, Vector4.One);
            if (ImGui.IsItemHovered(HoveredFlags.Default))
            {
                if (ImGui.IsMouseDragging(0, 1f))
                {
                    var delta = (Vector2)ImGui.GetMouseDragDelta(0, 1f);
                    rotation -= (delta / 64);
                    ImGui.ResetMouseDragDelta(0);
                }
                float wheel = ImGui.GetIO().MouseWheel;
                if (ImGui.GetIO().ShiftPressed)
                    zoom -= wheel * 10;
                else
                    zoom -= wheel * 40;
                if (zoom < 0) zoom = 0;
            }

            ImGui.EndChild();
            return true;
        }

        void DoChecks(List<HardpointGizmo> gizmos)
        {
            int j = 0;
            foreach (var gz in gizmos)
            {
                ImGui.Checkbox(gz.Definition.Name + "##" + j++, ref gz.Enabled);
            }
        }

        public override void DetectResources(List<MissingReference> missing, List<uint> matrefs, List<string> texrefs)
        {
            ResourceDetection.DetectDrawable(Name, drawable, res, missing, matrefs, texrefs);
        }

        void DrawGL(int renderWidth, int renderHeight)
        {
            //Set state
            renderTarget.BindFramebuffer();
            rstate.Cull = true;
            var cc = rstate.ClearColor;
            rstate.DepthEnabled = true;
            rstate.ClearColor = background;
            rstate.ClearAll();
            vps.Push(0, 0, renderWidth, renderHeight);
            //Draw Model
            var cam = new LookAtCamera();
            cam.Update(renderWidth, renderHeight, new Vector3(zoom, 0, 0), Vector3.Zero);
            drawable.Update(cam, TimeSpan.Zero, TimeSpan.Zero);
            if (viewMode != M_NONE)
            {
                buffer.StartFrame(rstate);
                if (drawable is CmpFile)
                    DrawCmp(cam, false);
                else
                    DrawSimple(cam, false);
                buffer.DrawOpaque(rstate);
                rstate.DepthWrite = false;
                buffer.DrawTransparent(rstate);
                rstate.DepthWrite = true;
            }
            if (doWireframe)
            {
                buffer.StartFrame(rstate);
                GL.PolygonOffset(1, 1);
                rstate.Wireframe = true;
                if (drawable is CmpFile)
                    DrawCmp(cam, true);
                else
                    DrawSimple(cam, false);
                GL.PolygonOffset(0, 0);
                buffer.DrawOpaque(rstate);
                rstate.Wireframe = false;
            }
            //Draw hardpoints
            DrawHardpoints(cam);
            //Restore state
            rstate.Cull = false;
            rstate.BlendMode = BlendMode.Normal;
            rstate.DepthEnabled = false;
            rstate.ClearColor = cc;
            RenderTarget2D.ClearBinding();
            vps.Pop();
        }

        class HardpointGizmo
        {
            public HardpointDefinition Definition;
            public Matrix4 Transform;
            public bool Enabled;
            public HardpointGizmo(HardpointDefinition def, Matrix4 tr)
            {
                Definition = def;
                Transform = tr;
                Enabled = false;
            }
        }

        void DrawHardpoints(ICamera cam)
        {
            var matrix = Matrix4.CreateRotationX(rotation.Y) * Matrix4.CreateRotationY(rotation.X);
            GizmoRender.Begin();
            foreach (var pl in partlist)
            {
                foreach (var tr in pl.Gizmos)
                {
                    if (tr.Enabled)
                        GizmoRender.AddGizmo(tr.Transform * matrix);
                }
            }
            GizmoRender.RenderGizmos(cam, rstate);
        }

        void DrawSimple(ICamera cam, bool wireFrame)
        {
            Material mat = null;
            if (wireFrame || viewMode == M_FLAT)
            {
                mat = wireframeMaterial3db;
                mat.Update(cam);
            }
            else if (viewMode == M_NORMALS)
            {
                mat = normalsDebugMaterial;
                mat.Update(cam);
            }
            var matrix = Matrix4.CreateRotationX(rotation.Y) * Matrix4.CreateRotationY(rotation.X);
            if (viewMode == M_LIT)
                drawable.DrawBuffer(buffer, matrix, ref lighting, mat);
            else
                drawable.DrawBuffer(buffer, matrix, ref Lighting.Empty, mat);
        }

        int jColors = 0;
        void DrawCmp(ICamera cam, bool wireFrame)
        {
            var matrix = Matrix4.CreateRotationX(rotation.Y) * Matrix4.CreateRotationY(rotation.X);
            if (wireFrame || viewMode == M_FLAT)
            {
                var cmp = (CmpFile)drawable;
                foreach (var part in cmp.Parts)
                {
                    Material mat;
                    if (!partMaterials.TryGetValue(part.Key, out mat))
                    {
                        mat = new Material(res);
                        mat.DtName = ResourceManager.WhiteTextureName;
                        mat.Dc = initialCmpColors[jColors++];
                        if (jColors >= initialCmpColors.Length) jColors = 0;
                        partMaterials.Add(part.Key, mat);
                    }
                    mat.Update(cam);
                    part.Value.DrawBuffer(buffer, matrix, ref Lighting.Empty, mat);
                }
            }
            else if (viewMode == M_TEXTURED || viewMode == M_LIT)
            {
                if (viewMode == M_LIT)
                    drawable.DrawBuffer(buffer, matrix, ref lighting);
                else
                    drawable.DrawBuffer(buffer, matrix, ref Lighting.Empty);
            }
            else
            {
                normalsDebugMaterial.Update(cam);
                drawable.DrawBuffer(buffer, matrix, ref Lighting.Empty, normalsDebugMaterial);
            }
        }

        public override void Dispose()
        {
            if (renderTarget != null)
            {
                ImGuiHelper.DeregisterTexture(renderTarget);
                renderTarget.Dispose();
            }
        }
    }
}
