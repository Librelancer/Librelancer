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
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf;
using ImGuiNET;
namespace LancerEdit
{
    public partial class ModelViewer : DockTab
    {
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
            SetupViewport();
            zoom = drawable.GetRadius() * 2;
            if (drawable is CmpFile)
            {
                //Setup Editor UI for constructs + hardpoints
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
                var q = new Queue<AbstractConstruct>();
                foreach (var c in cmp.Constructs)
                {
                    if (c.ParentName == "Root" || string.IsNullOrEmpty(c.ParentName))
                        cons.Add(new ConstructNode() { Con = c });
                    else
                    {
                        if(cmp.Constructs.Find(c.ParentName) != null)
                            q.Enqueue(c);
                        else {
                            conOrphan.Add(c);
                        }
                    }
                }
                while(q.Count > 0) {
                    var c = q.Dequeue();
                    if (!PlaceNode(cons, c))
                        q.Enqueue(c);
                }
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
        bool PlaceNode(List<ConstructNode> n, AbstractConstruct con)
        {
            foreach(var node in n) {
                if(node.Con.ChildName == con.ParentName) {
                    node.Nodes.Add(new ConstructNode() { Con = con });
                    return true;
                }
                if (PlaceNode(node.Nodes, con))
                    return true;
            }
            return false;
        }
        public override void Update(double elapsed)
        {
            if (animator != null)
                animator.Update(TimeSpan.FromSeconds(elapsed));
        }
        Vector2 rotation = Vector2.Zero;
        bool firstTab = true;
        float zoom = 0;
        Color4 background = Color4.CornflowerBlue * new Color4(0.3f, 0.3f, 0.3f, 1f);
        System.Numerics.Vector3 editCol;

        bool[] openTabs = new bool[] { false, false, false };
        void TabButton(string name, int idx)
        {
            if(TabHandler.VerticalTab(name, openTabs[idx])) {
                if (!openTabs[idx])
                {
                    for (int i = 0; i < openTabs.Length; i++) openTabs[i] = false;
                    openTabs[idx] = true;
                }
                else
                    openTabs[idx] = false;
            }
        }
        void TabButtons()
        {
            ImGuiNative.igBeginGroup();
            TabButton("Hardpoints", 0);
            if (drawable is CmpFile)
                TabButton("Constructs", 1);
            if (drawable is CmpFile && ((CmpFile)drawable).Animation != null)
                TabButton("Animations", 2);
            ImGuiNative.igEndGroup();
            ImGui.SameLine();
        }

        public override bool Draw()
        {
            bool doTabs = false;
            foreach (var t in openTabs) if (t) { doTabs = true; break; }
            var contentw = ImGui.GetContentRegionAvailableWidth();
            if (doTabs)
            {
                ImGui.Columns(2, "##panels", true);
                if(firstTab) {
                    ImGui.SetColumnWidth(0, contentw * 0.23f);
                    firstTab = false;
                }
                ImGui.BeginChild("##tabchild");
                if (openTabs[0]) HardpointsPanel();
                if (openTabs[1]) ConstructsPanel();
                if (openTabs[2]) AnimationPanel();
                ImGui.EndChild();
                ImGui.NextColumn();
            }
            TabButtons();
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
            DoViewport();
            ImGui.EndChild();
            return true;
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

        void DoChecks(List<HardpointGizmo> gizmos)
        {
            int j = 0;
            foreach (var gz in gizmos)
            {
                ImGui.Checkbox(gz.Definition.Name + "##" + j++, ref gz.Enabled);
            }
        }

        void HardpointsPanel()
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

        class ConstructNode
        {
            public AbstractConstruct Con;
            public List<ConstructNode> Nodes = new List<ConstructNode>();
        }
        List<ConstructNode> cons = new List<ConstructNode>();
        List<AbstractConstruct> conOrphan = new List<AbstractConstruct>();

        void DoConstructNode(ConstructNode cn)
        {
            var n = string.Format("{0} ({1})", cn.Con.ChildName, cn.Con.GetType().Name);
            if (cn.Nodes.Count > 0)
            {
                if (ImGui.TreeNode(n))
                {
                    foreach (var child in cn.Nodes)
                        DoConstructNode(child);
                    ImGui.TreePop();
                }
            }
            else
            {
                ImGui.BulletText(n);
            }
        }

        void ConstructsPanel()
        {
            if (ImGui.TreeNodeEx("Root", TreeNodeFlags.DefaultOpen))
            {
                foreach (var n in cons)
                    DoConstructNode(n);
                ImGui.TreePop();
            }
        }

        void AnimationPanel()
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

        public override void DetectResources(List<MissingReference> missing, List<uint> matrefs, List<string> texrefs)
        {
            ResourceDetection.DetectDrawable(Name, drawable, res, missing, matrefs, texrefs);
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
