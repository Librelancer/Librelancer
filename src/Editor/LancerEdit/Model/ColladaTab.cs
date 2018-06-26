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
using System.Text;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Vertices;
using LibreLancer.ImUI;

namespace LancerEdit
{
    public class ColladaTab : EditorTab
    {
        List<ColladaObject> objs;
        List<OutModel> output = new List<OutModel>();
        class OutModel
        {
            public string Name;
            ColladaObject def;
            public ColladaObject Def {
                get {
                    if (LODs.Count > 0) return LODs[0];
                    else return def;
                } set {
                    def = value;
                }
            }
            public bool ParentTransform = false;
            public bool Transform = false;
            public List<ColladaObject> LODs = new List<ColladaObject>();
            public List<OutModel> Children = new List<OutModel>();
        }

        MainWindow win;
        Viewport3D colladaViewport;
        Viewport3D flViewport;
        public ColladaTab(List<ColladaObject> objects, string fname, MainWindow win)
        {
            objs = objects;
            Autodetect();
            Title = string.Format("Collada Importer ({0})##{1}", fname,Unique);
            normalMaterial = new NormalDebugMaterial();
            this.win = win;
            colladaViewport = new Viewport3D(win.RenderState, win.Viewport);
            colladaViewport.MarginH = 15;
            flViewport = new Viewport3D(win.RenderState, win.Viewport);
            flViewport.MarginH = 15;
        }
        void Autodetect()
        {
            Dictionary<string, ColladaObject[]> autodetect = new Dictionary<string,ColladaObject[]>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var obj in objs)
                GetLods(obj, autodetect);
            foreach(var obj in objs) {
                AutodetectTree(obj, output, autodetect);
            }
        }
        void AutodetectTree(ColladaObject obj, List<OutModel> parent, Dictionary<string,ColladaObject[]> autodetect)
        {
            if (!obj.AutodetectInclude) return;
            var mdl = new OutModel();
            mdl.Name = obj.Name;
            if (obj.Name.EndsWith("_lod0", StringComparison.InvariantCultureIgnoreCase))
                mdl.Name = obj.Name.Remove(obj.Name.Length - 5, 5);
            var geometry = autodetect[mdl.Name];
            foreach (var g in geometry)
                if (g != null) mdl.LODs.Add(g);
            foreach(var child in obj.Children) {
                AutodetectTree(child, mdl.Children, autodetect);
            }
            parent.Add(mdl);
        }
        void GetLods(ColladaObject obj, Dictionary<string,ColladaObject[]> autodetect)
        {
            string objn;
            var num = LodNumber(obj, out objn);
            obj.AutodetectInclude = (num == 0);
            if(num != -1) {
               ColladaObject[] lods;
                if(!autodetect.TryGetValue(objn, out lods)) {
                    lods = new ColladaObject[10];
                    autodetect.Add(objn, lods);
                }
                lods[num] = obj;
            }
            foreach (var child in obj.Children)
                GetLods(child, autodetect);
        }
        //Autodetected LOD: object with geometry + suffix _lod[0-9]
        int LodNumber(ColladaObject obj, out string name)
        {
            name = obj.Name;
            if (obj.Geometry == null) return -1;
            if (obj.Name.Length < 6) return 0;
            if (!char.IsDigit(obj.Name, obj.Name.Length - 1)) return 0;
            if (!CheckSuffix("_lodX", obj.Name, 4)) return 0;
            name = obj.Name.Substring(0, obj.Name.Length - "_lodX".Length);
            return int.Parse(obj.Name[obj.Name.Length - 1] + "");
        }
        bool CheckSuffix(string postfixfmt, string src, int count)
        {
            for (int i = 0; i < count; i++)
                if (src[src.Length - postfixfmt.Length + i] != postfixfmt[i]) return false;
            return true;
        }
        const string EXPORTER_VERSION = "LancerEdit Collada Importer 2018";
        bool Finish(out EditableUtf result)
        {
            result = null;
            var utf = new EditableUtf();
            //Vanity
            var expv = new LUtfNode() { Name = "Exporter Version", Parent = utf.Root };
            expv.Data = System.Text.Encoding.UTF8.GetBytes(EXPORTER_VERSION);
            utf.Root.Children.Add(expv);
            //Actual stuff
            if (output.Count == 1)
            {
                if (output[0].Children.Count == 0)
                {
                    Export3DB(utf.Root, output[0]);
                } 
                else 
                {
                    return false; //TODO: CMP
                }
                if(generateMaterials) {
                    List<string> materials = new List<string>();
                    foreach (var mdl in output)
                        IterateMaterials(materials, mdl);
                    var mats = new LUtfNode() { Name = "material library", Parent = utf.Root };
                    mats.Children = new List<LUtfNode>();
                    int i = 0;
                    foreach (var mat in materials)
                        mats.Children.Add(DefaultMaterialNode(mats,mat,i++));
                    var txms = new LUtfNode() { Name = "texture library", Parent = utf.Root };
                    txms.Children = new List<LUtfNode>();
                    foreach (var mat in materials)
                        txms.Children.Add(DefaultTextureNode(txms,mat));
                    utf.Root.Children.Add(mats);
                    utf.Root.Children.Add(txms);
                }
                result = utf;
                return true;
            }
            else
                return false;
        }
        static readonly float[][] matColors =  {
            new float[]{ 1, 0, 0 },
            new float[]{ 0, 1, 0 },
            new float[]{ 0, 0, 1 },
            new float[]{ 1, 1, 1 },
            new float[]{ 1, 1, 0 },
            new float[]{ 0, 1, 1 }
        };
        static LUtfNode DefaultMaterialNode(LUtfNode parent, string name, int i)
        {
            var matnode = new LUtfNode() { Name = name, Parent = parent };
            matnode.Children = new List<LUtfNode>();
            matnode.Children.Add(new LUtfNode() { Name = "Type", Parent = matnode, Data = Encoding.ASCII.GetBytes("DcDt") });
            matnode.Children.Add(new LUtfNode() { Name = "Dc", Parent = matnode, Data = UnsafeHelpers.CastArray(matColors[i % matColors.Length]) });
            matnode.Children.Add(new LUtfNode() { Name = "Dt_name", Parent = matnode, Data = Encoding.ASCII.GetBytes(name + ".tex.dds") });
            matnode.Children.Add(new LUtfNode() { Name = "Dt_flags", Parent = matnode, Data = BitConverter.GetBytes(64) });
            return matnode;
        }
        static LUtfNode DefaultTextureNode(LUtfNode parent, string name)
        {
            var texnode = new LUtfNode() { Name = name + ".tex.dds", Parent = parent };
            texnode.Children = new List<LUtfNode>();
            var d = new byte[DefaultTexture.Data.Length];
            Buffer.BlockCopy(DefaultTexture.Data, 0, d, 0, DefaultTexture.Data.Length);
            texnode.Children.Add(new LUtfNode() { Name = "MIPS", Parent = texnode, Data = d });
            return texnode;
        }

        static void IterateMaterials(List<string> materials, OutModel mdl)
        {
            foreach (var lod in mdl.LODs)
                foreach (var dc in lod.Geometry.Drawcalls)
                    if (dc.Material != "NullMaterial" && !materials.Contains(dc.Material))
                        materials.Add(dc.Material);
            foreach (var child in mdl.Children)
                IterateMaterials(materials, child);
        }
        static void Export3DB(LUtfNode node3db, OutModel mdl)
        {
            var vms = new LUtfNode() { Name = "VMeshLibrary", Parent = node3db };
            vms.Children = new List<LUtfNode>();
            for (int i = 0; i < mdl.LODs.Count; i++)
            {
                var n = new LUtfNode() { Name = string.Format("{0}.level{1}.vms", mdl.Name, i), Parent = vms };
                n.Children = new List<LUtfNode>();
                n.Children.Add(new LUtfNode() { Name = "VMeshData", Parent = n, Data = mdl.LODs[i].Geometry.VMeshData() });
                vms.Children.Add(n);
            }
            node3db.Children.Add(vms);
            if (mdl.LODs.Count > 1)
            {
                var multilevel = new LUtfNode() { Name = "MultiLevel", Parent = node3db };
                multilevel.Children = new List<LUtfNode>();
                var switch2 = new LUtfNode() { Name = "Switch2", Parent = multilevel };
                switch2.Data = UnsafeHelpers.CastArray(new float[] { 0, 4000});
                multilevel.Children.Add(switch2);
                for (int i = 0; i < mdl.LODs.Count; i++)
                {
                    var n = new LUtfNode() { Name = "Level" + i, Parent = multilevel };
                    n.Children = new List<LUtfNode>();
                    n.Children.Add(new LUtfNode() { Name = "VMeshPart", Parent = n, Children = new List<LUtfNode>() });
                    n.Children[0].Children.Add(new LUtfNode()
                    {
                        Name = "VMeshRef",
                        Parent = n.Children[0],
                        Data = mdl.LODs[i].Geometry.VMeshRef(string.Format("{0}.level{1}.vms", mdl.Name, i))
                    });
                    multilevel.Children.Add(n);
                }
                node3db.Children.Add(multilevel);
            }
            else
            {
                var part = new LUtfNode() { Name = "VMeshPart", Parent = node3db };
                part.Children = new List<LUtfNode>();
                part.Children.Add(new LUtfNode()
                {
                    Name = "VMeshRef",
                    Parent = part,
                    Data = mdl.LODs[0].Geometry.VMeshRef(string.Format("{0}.level0.vms", mdl.Name))
                });
                node3db.Children.Add(part);
            }
        }
        VertexBuffer vbo;
        ElementBuffer ibo;
        NormalDebugMaterial normalMaterial;

        bool _openError;
        string _errorText;
        void ErrorPopup(string text)
        {
            _openError = true;
            _errorText = text;
        }
        float collada_h1 = 200, collada_h2 = 200;
        public override void Draw()
        {
            ImGui.Columns(2, "##columns", true);
            ImGui.Text("Collada");
            ImGui.NextColumn();
            ImGui.Text("UTF");
            ImGui.SameLine(ImGui.GetColumnWidth(1) - 60);
            if (ImGui.Button("Finish"))
            {
                EditableUtf utf;
                if (Finish(out utf))
                    win.AddTab(new UtfTab(win, utf, "Untitled"));
                else {
                    ErrorPopup("Invalid UTF Structure:\nMore than one root node.");
                }
            }
            ImGui.NextColumn();
            ImGui.BeginChild("##collada");
            ColladaPane();
            ImGui.EndChild();
            ImGui.NextColumn();
            ImGui.BeginChild("##fl");
            FLPane();
            ImGui.EndChild();
            if (_openError) ImGui.OpenPopup("Error");
            if(ImGui.BeginPopupModal("Error")) {
                ImGui.Text(_errorText);
                if (ImGui.Button("Ok")) ImGui.CloseCurrentPopup();
                ImGui.EndPopup();
            }
            _openError = false;
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
                    ColladaTree(obj, ref i);
                ImGui.TreePop();
            }
            CheckSelected();
            ImGui.EndChild();
            ImGui.BeginChild("2", new Vector2(-1, collada_h2), false, WindowFlags.Default);
            //Preview+Properties
            if (selected == null)
            {
                ImGui.Text("No node selected");
            }
            else
            {
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
            if (selected.Geometry != null)
            {
                ImGui.Text("Mesh: " + selected.Geometry.FVF.ToString());
                ImGui.Text("Materials:");
                foreach (var dc in selected.Geometry.Drawcalls)
                {
                    ImGui.Text(dc.Material);
                }
            }
        }

        float fl_h1 = 200, fl_h2 = 200;
        bool flPreview = false;
        bool generateMaterials = true;
        void FLPane()
        {
            var totalH = ImGui.GetWindowHeight();
            ImGuiExt.SplitterV(2f, ref fl_h1, ref fl_h2, 8, 8, -1);
            fl_h1 = totalH - fl_h2 - 6f;
            ImGui.BeginChild("1", new Vector2(-1, fl_h1), false, WindowFlags.Default);
            ImGui.Separator();
            //3DB list
            if (ImGui.TreeNodeEx("Model/"))
            {
                int i = 0;
                foreach (var mdl in output)
                {
                    FLTree(mdl, ref i);
                }
            }
            ImGui.EndChild();
            ImGui.BeginChild("2", new Vector2(-1, fl_h2), false, WindowFlags.Default);
            if (ImGuiExt.ToggleButton("Options", !flPreview)) flPreview = false;
            ImGui.SameLine();
            if (ImGuiExt.ToggleButton("Preview", flPreview)) flPreview = true;
            ImGui.Separator();
            if (flPreview)
            {

            }
            else
            {
                ImGui.Checkbox("Default Materials", ref generateMaterials);
            }
            ImGui.EndChild();
        }
        void FLTree(OutModel mdl, ref int i)
        {
            var flags = TreeNodeFlags.OpenOnDoubleClick |
                                         TreeNodeFlags.DefaultOpen |
                                         TreeNodeFlags.OpenOnArrow;
            //if (obj == selected) flags |= TreeNodeFlags.Selected;
            var open = ImGui.TreeNodeEx(ImGuiExt.Pad(mdl.Name + "##" + i++), flags);
            //if (ImGuiNative.igIsItemClicked(0))
            //selected = obj;
            //ColladaContextMenu();
            Theme.RenderTreeIcon(mdl.Name, "fix", Color4.White);
            if(open)
            {
                if(ImGui.TreeNode("LODs")) {
                    for (int j = 0; j < mdl.LODs.Count; j++)
                        ImGui.Selectable(string.Format("{0}: {1}", j, mdl.LODs[j].Name));
                    ImGui.TreePop();
                }
                foreach (var child in mdl.Children)
                    FLTree(child, ref i);
                ImGui.TreePop();
            }
            i += 500;
        }
        void Render()
        {
            colladaViewport.Begin();
            var cam = new LookAtCamera();
            cam.Update(colladaViewport.RenderWidth, colladaViewport.RenderHeight, new Vector3(colladaViewport.Zoom, 0, 0), Vector3.Zero);
            normalMaterial.World = Matrix4.CreateRotationX(colladaViewport.Rotation.Y) * Matrix4.CreateRotationY(colladaViewport.Rotation.X);
            normalMaterial.Camera = cam;
            normalMaterial.Use(win.RenderState, new VertexPositionNormalDiffuseTextureTwo(), ref Lighting.Empty);
            foreach (var drawcall in selected.Geometry.Drawcalls)
            {
                vbo.Draw(PrimitiveTypes.TriangleList, drawcall.StartVertex, drawcall.StartIndex, drawcall.TriCount);
            }
            colladaViewport.End();
        }
        void CheckSelected()
        {
            if (lastSelected != selected)
            {
                if (vbo != null)
                {
                    vbo.Dispose();
                    vbo = null;
                }
                if (ibo != null)
                {
                    ibo.Dispose();
                    ibo = null;
                }
                if (selected.Geometry != null)
                {
                    vbo = new VertexBuffer(typeof(VertexPositionNormalDiffuseTextureTwo),
                                                    selected.Geometry.Vertices.Length);
                    vbo.SetData(selected.Geometry.Vertices);
                    ibo = new ElementBuffer(selected.Geometry.Indices.Length);
                    ibo.SetData(selected.Geometry.Indices);
                    vbo.SetElementBuffer(ibo);
                    colladaViewport.Zoom = selected.Geometry.Radius * 2;
                    colladaViewport.ZoomStep = colladaViewport.Zoom / 3.76f;
                    colladaViewport.Rotation = Vector2.Zero;
                }

                lastSelected = selected;
            }
        }

        ColladaObject lastSelected = null;
        ColladaObject selected = null;
        void ColladaTree(ColladaObject obj, ref int i)
        {
            string tree_icon = "dummy";
            if (obj.Geometry != null) tree_icon = "fix";
            if (obj.Children.Count > 0)
            {
                var flags = TreeNodeFlags.OpenOnDoubleClick |
                                         TreeNodeFlags.DefaultOpen |
                                         TreeNodeFlags.OpenOnArrow;
                if (obj == selected) flags |= TreeNodeFlags.Selected;
                var open = ImGui.TreeNodeEx(ImGuiExt.Pad(obj.Name + "##" + i++), flags);
                if (ImGuiNative.igIsItemClicked(0))
                    selected = obj;
                ColladaContextMenu();
                Theme.RenderTreeIcon(obj.Name, tree_icon, Color4.White);
                if(open)
                {
                    foreach (var child in obj.Children)
                        ColladaTree(child, ref i);
                    ImGui.TreePop();
                }
                i += 500;
            }
            else
            {
                if (ImGui.Selectable(ImGuiExt.Pad(obj.Name + "##" + i++), obj == selected))
                {
                    selected = obj;
                }
                ColladaContextMenu();
                Theme.RenderTreeIcon(obj.Name, tree_icon, Color4.White);
            }

        }
        void ColladaContextMenu()
        {
            if (ImGuiNative.igIsItemClicked(1))
            {
                ImGui.OpenPopup("colladanodeclick");
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
