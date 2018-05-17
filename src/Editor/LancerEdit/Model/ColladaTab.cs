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
using ImGuiNET;
using LibreLancer;
using LibreLancer.Vertices;
namespace LancerEdit
{
    public class ColladaTab : DockTab
    {
        List<ColladaObject> objs;
        MainWindow win;
        Viewport3D colladaViewport;
        Viewport3D flViewport;
        public ColladaTab(List<ColladaObject> objects, string fname, MainWindow win)
        {
            objs = objects;
            Title = string.Format("Collada Importer ({0})",fname);
            normalMaterial = new NormalDebugMaterial();
            this.win = win;
            colladaViewport = new Viewport3D(win.RenderState, win.Viewport);
            colladaViewport.MarginH = 15;
            flViewport = new Viewport3D(win.RenderState, win.Viewport);
            flViewport.MarginH = 15;
        }

        VertexBuffer vbo;
        ElementBuffer ibo;
        NormalDebugMaterial normalMaterial;
        
        float collada_h1 = 200, collada_h2 = 200;
        public override bool Draw()
        {
            ImGui.Columns(2, "##columns", true);
            ImGui.Text("Collada");
            ImGui.NextColumn();
            ImGui.Text("UTF");
            ImGui.SameLine(ImGui.GetColumnWidth(1) - 60);
            if(ImGui.Button("Finish")) {
                
            }
            ImGui.NextColumn();
            ImGui.BeginChild("##collada");
            ColladaPane();
            ImGui.EndChild();
            ImGui.NextColumn();
            ImGui.BeginChild("##fl");
            FLPane();
            ImGui.EndChild();
            return true;
        }

        bool colladaPreview = true;
        void ColladaPane()
        {
            var totalH = ImGui.GetWindowHeight();
            ImGuiExt.SplitterV(2f, ref collada_h1, ref collada_h2, 8, 8, -1);
            collada_h1 = totalH - collada_h2 - 6f;
            ImGui.BeginChild("1", new Vector2(-1, collada_h1), false, WindowFlags.Default);
            ImGui.Separator();
            if (ImGui.TreeNode("Scene/"))
            {
                int i = 0;
                foreach (var obj in objs)
                    DoTree(obj, ref i);
                ImGui.TreePop();
            }
            CheckSelected();
            ImGui.EndChild();
            ImGui.BeginChild("2", new Vector2(-1, collada_h2), false, WindowFlags.Default);
            //Preview+Properties
            if(selected == null) {
                ImGui.Text("No node selected");
            } else {
                if (selected.Geometry != null)
                {
                    if (ImGuiExt.ToggleButton("Preview", colladaPreview)) colladaPreview = true;
                    ImGui.SameLine();
                    if (ImGuiExt.ToggleButton("Details", !colladaPreview)) colladaPreview = false;
                    ImGui.Separator();
                    if (colladaPreview)
                    {
                        ImGui.BeginChild("##colladapreview");
                        Render();
                        ImGui.EndChild();
                    }
                    else
                        ColladaDetails();
                }
                else
                    ColladaDetails();
            }
            //
            ImGui.EndChild();
        }

        void ColladaDetails()
        {
            ImGui.Text(selected.Name);
            ImGui.Text("ID: " + selected.ID);
            if(selected.Geometry != null) {
                ImGui.Text("Mesh: " + selected.Geometry.FVF.ToString());
                ImGui.Text("Materials:");
                foreach(var dc in selected.Geometry.Drawcalls) {
                    ImGui.Text(dc.Material);
                }
            }
        }

        float fl_h1 = 200, fl_h2 = 200;
        bool flPreview = true;
        void FLPane()
        {
            var totalH = ImGui.GetWindowHeight();
            ImGuiExt.SplitterV(2f, ref fl_h1, ref fl_h2, 8, 8, -1);
            fl_h1 = totalH - fl_h2 - 6f;
            ImGui.BeginChild("1", new Vector2(-1, fl_h1), false, WindowFlags.Default);
            ImGui.Separator();
            //3DB list
            if(ImGui.TreeNodeEx("Model/")) {
                
            }
            ImGui.EndChild();
            ImGui.BeginChild("2", new Vector2(-1, fl_h2), false, WindowFlags.Default);
            if (ImGuiExt.ToggleButton("Preview", flPreview)) flPreview = true;
            ImGui.SameLine();
            if (ImGuiExt.ToggleButton("Details", !flPreview)) flPreview = false;
            ImGui.Separator();
            if(flPreview) {
                
            } else {
                
            }
            ImGui.EndChild();
        }
        void Render()
        {
            colladaViewport.Begin();
            var cam = new LookAtCamera();
            cam.Update(colladaViewport.RenderWidth, colladaViewport.RenderHeight, new Vector3(colladaViewport.Zoom, 0, 0), Vector3.Zero);
            normalMaterial.World = Matrix4.CreateRotationX(colladaViewport.Rotation.Y) * Matrix4.CreateRotationY(colladaViewport.Rotation.X);
            normalMaterial.Camera = cam;
            normalMaterial.Use(win.RenderState, new VertexPositionNormalDiffuseTextureTwo(), ref Lighting.Empty);
            foreach(var drawcall in selected.Geometry.Drawcalls) {
                vbo.Draw(PrimitiveTypes.TriangleList, drawcall.Start, drawcall.TriCount);
            }
            colladaViewport.End();
        }
        void CheckSelected()
        {
            if (lastSelected != selected)
            {
                if (vbo != null) {
                    vbo.Dispose();
                    vbo = null;
                }
                if (ibo != null)  {
                    ibo.Dispose();
                    ibo = null;
                }
                if(selected.Geometry != null) {
                    vbo = new VertexBuffer(typeof(VertexPositionNormalDiffuseTextureTwo),
                                                    selected.Geometry.Vertices.Length);
                    vbo.SetData(selected.Geometry.Vertices);
                    ibo = new ElementBuffer(selected.Geometry.Indices.Length);
                    ibo.SetData(selected.Geometry.Indices);
                    vbo.SetElementBuffer(ibo);
                    colladaViewport.Zoom = selected.Geometry.Radius * 4f;
                    colladaViewport.ZoomStep = colladaViewport.Zoom / 3.76f;
                    colladaViewport.Rotation = Vector2.Zero;
                }

                lastSelected = selected;
            }
        }

        ColladaObject lastSelected = null;
        ColladaObject selected = null;
        void DoTree(ColladaObject obj,ref int i)
        {
            string tree_icon = "dummy";
            if (obj.Geometry != null) tree_icon = "fix";
            if (obj.Children != null)
            {
                var flags = TreeNodeFlags.OpenOnDoubleClick |
                                         TreeNodeFlags.DefaultOpen |
                                         TreeNodeFlags.OpenOnArrow;
                if (obj == selected) flags |= TreeNodeFlags.Selected;
                if (ImGui.TreeNodeEx(ImGuiExt.Pad(obj.Name + "##" + i++),flags))
                {
                    if (ImGuiNative.igIsItemClicked(0))
                        selected = obj;
                    Theme.RenderTreeIcon(obj.Name, tree_icon, Color4.White);
                    foreach (var child in obj.Children)
                        DoTree(child, ref i);
                    ImGui.TreePop();
                }
                i += 500;
            } else {
                if(ImGui.Selectable(ImGuiExt.Pad(obj.Name + "##" + i++), obj == selected)) {
                    selected = obj;
                }
                Theme.RenderTreeIcon(obj.Name, tree_icon, Color4.White);
            }
        }

        public override void Dispose()
        {
            colladaViewport.Dispose();
            flViewport.Dispose();
            if (vbo != null) vbo.Dispose();
            if (ibo != null) ibo.Dispose();
        }
    }
}
