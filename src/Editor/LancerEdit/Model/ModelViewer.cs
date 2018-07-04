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
using System.Linq;
using LibreLancer;
using LibreLancer.ImUI;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf;
using ImGuiNET;
namespace LancerEdit
{
    public partial class ModelViewer : EditorTab
    {
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
        bool isStarsphere = false;
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

        Material wireframeMaterial3db;
        Material normalsDebugMaterial;
        Dictionary<int, Material> partMaterials = new Dictionary<int, Material>();
        List<HardpointGizmo> gizmos = new List<HardpointGizmo>();
        AnimationComponent animator;
        UtfTab parent;
        MainWindow _window;
        PopupManager popups;
        ModelNodes hprefs;
        public ModelViewer(string title, string name, IDrawable drawable, MainWindow win, UtfTab parent, ModelNodes hprefs)
        {
            Title = title;
            Name = name;
            this.drawable = drawable;
            this.parent = parent;
            this.hprefs = hprefs;
            rstate = win.RenderState;
            vps = win.Viewport;
            res = win.Resources;
            buffer = win.Commands;
            _window = win;
            SetupViewport();

            if (drawable is CmpFile)
            {
                //Setup Editor UI for constructs + hardpoints
                var cmp = (CmpFile)drawable;
                foreach (var p in cmp.Parts)
                {
                    foreach (var hp in p.Model.Hardpoints)
                    {
                        gizmos.Add(new HardpointGizmo(hp, p.Construct));
                    }
                }
                if (cmp.Animation != null)
                    animator = new AnimationComponent(cmp.Constructs, cmp.Animation);
                foreach (var p in cmp.Parts)
                {
                    if (p.Construct == null) rootModel = p.Model;
                }
                var q = new Queue<AbstractConstruct>();
                foreach (var c in cmp.Constructs)
                {
                    if (c.ParentName == "Root" || string.IsNullOrEmpty(c.ParentName))
                    {
                        cons.Add(GetNodeCmp(cmp, c));
                    }
                    else
                    {
                        if (cmp.Constructs.Find(c.ParentName) != null)
                            q.Enqueue(c);
                        else
                        {
                            conOrphan.Add(c);
                        }
                    }
                }
                while (q.Count > 0)
                {
                    var c = q.Dequeue();
                    if (!PlaceNode(cons, c))
                        q.Enqueue(c);
                }
                int maxLevels = 0;
                foreach (var p in cmp.Parts)
                {
                    maxLevels = Math.Max(maxLevels, p.Model.Levels.Length - 1);
                    if (p.Model.Switch2 != null)
                        for (int i = 0; i < p.Model.Switch2.Length - 1; i++)
                            maxDistance = Math.Max(maxDistance, p.Model.Switch2[i]);
                }
                levels = new string[maxLevels + 1];
                for (int i = 0; i <= maxLevels; i++)
                    levels[i] = i.ToString();
            }
            else if (drawable is ModelFile)
            {
                var mdl = (ModelFile)drawable;
                rootModel = mdl;
                foreach (var hp in mdl.Hardpoints)
                {
                    gizmos.Add(new HardpointGizmo(hp, null));
                }

                levels = new string[mdl.Levels.Length];
                for (int i = 0; i < mdl.Levels.Length; i++)
                    levels[i] = i.ToString();
                if (mdl.Switch2 != null)
                    for (int i = 0; i < mdl.Switch2.Length - 1; i++)
                        maxDistance = Math.Max(maxDistance, mdl.Switch2[i]);
            }
            maxDistance += 50;

            popups = new PopupManager();
            popups.AddPopup("Confirm Delete", ConfirmDelete, WindowFlags.AlwaysAutoResize);
            popups.AddPopup("Apply Complete", (x) =>
            {
                ImGui.Text("Hardpoints successfully written");
                if (ImGui.Button("Ok")) ImGui.CloseCurrentPopup();
            },WindowFlags.AlwaysAutoResize);
            popups.AddPopup("New Hardpoint", NewHardpoint, WindowFlags.AlwaysAutoResize);
        }
       
        public override void SetActiveTab(MainWindow win)
        {
            win.ActiveTab = parent;
        }
        ConstructNode GetNodeCmp(CmpFile c, AbstractConstruct con)
        {
            var node = new ConstructNode() { Con = con };
            foreach (var p in c.Parts)
                if (p.Construct == con)
                    node.Model = p.Model;
            return node;
        }
        bool PlaceNode(List<ConstructNode> n, AbstractConstruct con)
        {
            foreach (var node in n)
            {
                if (node.Con.ChildName == con.ParentName)
                {
                    node.Nodes.Add(GetNodeCmp((CmpFile)drawable, con));
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
        Color4 background = Color4.CornflowerBlue * new Color4(0.3f, 0.3f, 0.3f, 1f);
        System.Numerics.Vector3 editCol;

        bool[] openTabs = new bool[] { false, false };
        void TabButton(string name, int idx)
        {
            if (TabHandler.VerticalTab(name, openTabs[idx]))
            {
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
            TabButton("Hierachy", 0);
            if (drawable is CmpFile && ((CmpFile)drawable).Animation != null)
                TabButton("Animations", 1);
            ImGuiNative.igEndGroup();
            ImGui.SameLine();
        }

        public override void Draw()
        {
            bool doTabs = false;
            popups.Run();
            HardpointEditor();
            foreach (var t in openTabs) if (t) { doTabs = true; break; }
            var contentw = ImGui.GetContentRegionAvailableWidth();
            if (doTabs)
            {
                ImGui.Columns(2, "##panels", true);
                if (firstTab)
                {
                    ImGui.SetColumnWidth(0, contentw * 0.23f);
                    firstTab = false;
                }
                ImGui.BeginChild("##tabchild");
                if (openTabs[0]) HierachyPanel();
                if (openTabs[1]) AnimationPanel();
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
            ImGui.Checkbox("Starsphere", ref isStarsphere);
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
        }

        class HardpointGizmo
        {
            public HardpointDefinition Definition;
            public AbstractConstruct Parent;
            public bool Enabled;
            public Matrix4? Override = null;
            public HardpointGizmo(HardpointDefinition def, AbstractConstruct parent)
            {
                Definition = def;
                Parent = parent;
                Enabled = false;
            }
        }

        class ConstructNode
        {
            public AbstractConstruct Con;
            public List<ConstructNode> Nodes = new List<ConstructNode>();
            public ModelFile Model;
        }
        List<ConstructNode> cons = new List<ConstructNode>();
        ModelFile rootModel;
        List<AbstractConstruct> conOrphan = new List<AbstractConstruct>();
        ConstructNode selectedNode = null;
        string ConType(AbstractConstruct construct)
        {
            var type = "???";
            if (construct is FixConstruct) type = "Fix";
            if (construct is RevConstruct) type = "Rev";
            if (construct is LooseConstruct) type = "Loose";
            if (construct is PrisConstruct) type = "Pris";
            if (construct is SphereConstruct) type = "Sphere";
            return type;
        }
        void DoConstructNode(ConstructNode cn)
        {
            var n = string.Format("{0} ({1})", cn.Con.ChildName, ConType(cn.Con));
            var tflags = TreeNodeFlags.OpenOnArrow | TreeNodeFlags.OpenOnDoubleClick;
            if (selectedNode == cn) tflags |= TreeNodeFlags.Selected;
            var icon = "fix";
            var color = Color4.LightYellow;
            if (cn.Con is PrisConstruct)
            {
                icon = "pris";
                color = Color4.LightPink;
            }
            if (cn.Con is SphereConstruct)
            {
                icon = "sphere";
                color = Color4.LightGreen;
            }
            if (cn.Con is RevConstruct)
            {
                icon = "rev";
                color = Color4.LightCoral;
            }
            if (ImGui.TreeNodeEx(ImGuiExt.Pad(n), tflags))
            {
                Theme.RenderTreeIcon(n, icon, color);
                if (ImGuiNative.igIsItemClicked(0))
                    selectedNode = cn;
                foreach (var child in cn.Nodes)
                    DoConstructNode(child);
                DoModel(cn.Model, cn.Con);
                ImGui.TreePop();
            }
            else
            {
                Theme.RenderTreeIcon(n, icon, color);
                if (ImGuiNative.igIsItemClicked(0))
                    selectedNode = cn;
            }
        }
        HardpointDefinition hpEditing;
        HardpointDefinition hpDelete;
        List<HardpointDefinition> hpDeleteFrom;
        void ConfirmDelete(PopupData data)
        {
            ImGui.Text(string.Format("Are you sure you wish to delete '{0}'?", hpDelete.Name));
            if(ImGui.Button("Yes")) {
                hpDeleteFrom.Remove(hpDelete);
                var gz = gizmos.Where((x) => x.Definition == hpDelete).First();
                if (hpDelete == hpEditing) hpEditing = null;
                gizmos.Remove(gz);
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if(ImGui.Button("No")) {
                ImGui.CloseCurrentPopup();
            }
        }
        TextBuffer newHpBuffer = new TextBuffer(256);
        bool newIsFixed = false;
        List<HardpointDefinition> addTo;
        AbstractConstruct addConstruct;
        void NewHardpoint(PopupData data)
        {
            ImGui.Text("Name: ");
            ImGui.SameLine();
            ImGui.InputText("##hpname", newHpBuffer.Pointer, (uint)newHpBuffer.Size, InputTextFlags.Default, newHpBuffer.Callback);
            ImGui.Text("Type: " + (newIsFixed ? "Fixed" : "Revolute"));
            if(ImGui.Button("Ok")) {
                var txt = newHpBuffer.GetText();
                if(txt.Length == 0) {
                    return;
                }
                HardpointDefinition def;
                if (newIsFixed) def = new FixedHardpointDefinition(txt);
                else def = new RevoluteHardpointDefinition(txt);
                gizmos.Add(new HardpointGizmo(def, addConstruct));
                addTo.Add(def);
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if(ImGui.Button("Cancel")) {
                ImGui.CloseCurrentPopup();
            }
        }
        bool hpEditOpen = false;
        HardpointGizmo editingGizmo;
        float HPpitch, HPyaw, HProll;
        float HPx, HPy, HPz;
        float HPaxisX, HPaxisY, HPaxisZ;
        float HPmin, HPmax;
        void SetHardpointValues()
        {
            HPx = hpEditing.Position.X;
            HPy = hpEditing.Position.Y;
            HPz = hpEditing.Position.Z;
            var euler = hpEditing.Orientation.GetEuler();
            HPpitch = euler.X; HPyaw = euler.Y; HProll = euler.Z;
            if (hpEditing is RevoluteHardpointDefinition)
            {
                var rev = (RevoluteHardpointDefinition)hpEditing;
                HPmin = rev.Min; HPmax = rev.Max;
                HPaxisX = rev.Axis.X;
                HPaxisY = rev.Axis.Y;
                HPaxisZ = rev.Axis.Z;
            }
        }
        unsafe void HardpointEditor()
        {
            if(hpEditing == null) {
                hpEditOpen = false;
                return;
            }
            if(hpEditing != null && hpEditOpen == false) {
                editingGizmo = gizmos.First((x) => x.Definition == hpEditing);
                hpEditOpen = true;
                SetHardpointValues();
            }
            if(ImGui.BeginWindow("Hardpoint Editor##" + Unique,ref hpEditOpen, WindowFlags.Default)) {
                ImGui.Text(hpEditing.Name);
                bool isFix = hpEditing is FixedHardpointDefinition;
                ImGui.Text("Type: " + (isFix ? "Fixed" : "Revolute"));
                if (ImGui.Button("Reset")) SetHardpointValues();
                ImGui.Separator();
                ImGui.Text("Position");
                fixed (float* hpx = &HPx)
                    ImGuiNative.igInputFloat("X##posX", hpx, 0.01f, 0.25f, 5, InputTextFlags.CharsDecimal);
                fixed (float* hpy = &HPy)
                    ImGuiNative.igInputFloat("Y##posY", hpy, 0.01f, 0.25f, 5, InputTextFlags.CharsDecimal);
                fixed (float* hpz = &HPz)
                    ImGuiNative.igInputFloat("Z##posZ", hpz, 0.01f, 0.25f, 5, InputTextFlags.CharsDecimal);
                ImGui.Separator();
                ImGui.Text("Rotation");
                if (ImGui.Button("0##0pitch")) HPpitch = 0; ImGui.SameLine();
                ImGui.SliderFloat("Pitch", ref HPpitch, -MathHelper.Pi, MathHelper.Pi, "%f", 1);
                if (ImGui.Button("0##0yaw")) HPyaw = 0; ImGui.SameLine();
                ImGui.SliderFloat("Yaw", ref HPyaw, -MathHelper.Pi, MathHelper.Pi, "%f", 1);
                if (ImGui.Button("0##0roll")) HProll = 0; ImGui.SameLine();
                ImGui.SliderFloat("Roll", ref HProll, -MathHelper.Pi, MathHelper.Pi, "%f", 1);
                ImGui.Separator();
                if(!isFix) {
                    ImGui.Text("Axis");
                    fixed (float* axx = &HPaxisX)
                        ImGuiNative.igInputFloat("X##axisX", axx, 0.01f, 0.25f, 5, InputTextFlags.CharsDecimal);
                    fixed (float* axy = &HPaxisY)
                        ImGuiNative.igInputFloat("Y##axisY", axy, 0.01f, 0.25f, 5, InputTextFlags.CharsDecimal);
                    fixed (float* axz = &HPaxisZ)
                        ImGuiNative.igInputFloat("Z##axisZ", axz, 0.01f, 0.25f, 5, InputTextFlags.CharsDecimal);
                    if (ImGui.Button("0##0min")) HPmin = 0; ImGui.SameLine();
                    ImGui.SliderFloat("Min", ref HPmin, -MathHelper.Pi, MathHelper.Pi, "%f", 1);
                    if (ImGui.Button("0##0max")) HPmax = 0; ImGui.SameLine();
                    ImGui.SliderFloat("Max", ref HPmax, -MathHelper.Pi, MathHelper.Pi, "%f", 1);
                    ImGui.Separator();
                }
                if(ImGui.Button("Apply")) {
                    hpEditing.Position = new Vector3(HPx, HPy, HPz);
                    hpEditing.Orientation = Matrix4.CreateFromQuaternion(Quaternion.FromEulerAngles(HPpitch, HPyaw, HProll));
                    if(!isFix) {
                        var rev = (RevoluteHardpointDefinition)hpEditing;
                        rev.Min = HPmin;
                        rev.Max = HPmax;
                        rev.Axis = new Vector3(HPaxisX, HPaxisY, HPaxisZ);
                    }
                    hpEditOpen = false;
                }
                ImGui.SameLine();
                if(ImGui.Button("Cancel")) {
                    hpEditOpen = false;
                }
                editingGizmo.Override =
                    Matrix4.CreateFromQuaternion(Quaternion.FromEulerAngles(HPpitch, HPyaw, HProll)) *
                                Matrix4.CreateTranslation(HPx, HPy, HPz);
                ImGui.EndWindow();
            }
            if (hpEditOpen == false)
            {
                hpEditing = null;
                editingGizmo.Override = null;
                editingGizmo = null;
            }
        }
        void DoModel(ModelFile mdl, AbstractConstruct con)
        {
            bool open = ImGui.TreeNode(ImGuiExt.Pad("Hardpoints"));
            var act = NewHpMenu(mdl.Path);
            switch(act) {
                case ContextActions.NewFixed:
                case ContextActions.NewRevolute:
                    newIsFixed = act == ContextActions.NewFixed;
                    addTo = mdl.Hardpoints;
                    addConstruct = con;
                    newHpBuffer.Clear();
                    popups.OpenPopup("New Hardpoint");
                    break;
            }
            Theme.RenderTreeIcon("Hardpoints", "hardpoint", Color4.CornflowerBlue);
            if (open)
            {
                foreach (var hp in mdl.Hardpoints)
                {
                    HardpointGizmo gz = null;
                    foreach (var gizmo in gizmos)
                    {
                        if (gizmo.Definition == hp)
                        {
                            gz = gizmo;
                            break;
                        }
                    }
                    if (hp is RevoluteHardpointDefinition)
                    {
                        Theme.Icon("rev", Color4.LightSeaGreen);
                    }
                    else
                    {
                        Theme.Icon("fix", Color4.Purple);
                    }
                    ImGui.SameLine();
                    if (Theme.IconButton("visible$" + hp.Name, "eye", gz.Enabled ? Color4.White : Color4.Gray))
                    {
                        gz.Enabled = !gz.Enabled;
                    }
                    ImGui.SameLine();
                    ImGui.Selectable(hp.Name);
                    var action = EditDeleteHpMenu(mdl.Path + hp.Name);
                    if (action == ContextActions.Delete)
                    {
                        hpDelete = hp;
                        hpDeleteFrom = mdl.Hardpoints;
                        popups.OpenPopup("Confirm Delete");
                    }
                    if (action == ContextActions.Edit) hpEditing = hp;
                }
                ImGui.TreePop();
            }
        }
        enum ContextActions {
            None, NewFixed,NewRevolute,Edit,Delete
        }
        ContextActions NewHpMenu(string n)
        {
            if(ImGuiNative.igIsItemClicked(1))
                ImGui.OpenPopup(n + "_HardpointContext");
            if(ImGui.BeginPopupContextItem(n + "_HardpointContext")) {
                if(ImGui.BeginMenu("New")) {
                    if (Theme.IconMenuItem("Fixed Hardpoint","fix",Color4.Purple,true)) return ContextActions.NewFixed;
                    if (Theme.IconMenuItem("Revolute Hardpoint","rev",Color4.LightSeaGreen,true)) return ContextActions.NewRevolute;
                    ImGui.EndMenu();
                }
                ImGui.EndPopup();
            }
            return ContextActions.None;
        }
        ContextActions EditDeleteHpMenu(string n)
        {
            if(ImGuiNative.igIsItemClicked(1))
                ImGui.OpenPopup(n + "_HardpointEditCtx");
            if(ImGui.BeginPopupContextItem(n + "_HardpointEditCtx")) {
                if(ImGui.MenuItem("Edit")) return ContextActions.Edit;
                if(ImGui.MenuItem("Delete")) return ContextActions.Delete;
                ImGui.EndPopup();
            }
            return ContextActions.None;
        }
        int level = 0;
        string[] levels;
        float levelDistance = 0;
        float maxDistance;
        bool useDistance = false;
        int GetLevel(float[] switch2, int maxLevel)
        {
            if (useDistance)
            {
                if (switch2 == null) return 0;
                for (int i = 0; i < switch2.Length; i++)
                {
                    if (levelDistance <= switch2[i])
                        return Math.Min(i, maxLevel);
                }
                return maxLevel;
            }
            else
            {
                return Math.Min(level, maxLevel);
            }
        }
        void HierachyPanel()
        {
            if (!(drawable is SphFile))
            {
                ImGui.Text("Level of Detail");
                ImGui.Checkbox("Use Distance", ref useDistance);
                if (useDistance)
                {
                    ImGui.SliderFloat("Distance", ref levelDistance, 0, maxDistance, "%f", 1);
                }
                else
                {
                    ImGui.Combo("Level", ref level, levels);
                }
                ImGui.Separator();
            }

            if (selectedNode != null)
            {
                ImGui.Text(selectedNode.Con.ChildName);
                ImGui.Text(selectedNode.Con.GetType().Name);
                ImGui.Text("Origin: " + selectedNode.Con.Origin.ToString());
                var euler = selectedNode.Con.Rotation.GetEuler();
                ImGui.Text(string.Format("Rotation: (Pitch {0:0.000}, Yaw {1:0.000}, Roll {2:0.000})",
                                        MathHelper.RadiansToDegrees(euler.X),
                                        MathHelper.RadiansToDegrees(euler.Y),
                                         MathHelper.RadiansToDegrees(euler.Z)));
                ImGui.Separator();
            }
            if(ImGui.Button("Apply Hardpoints")) {
                if(drawable is CmpFile) {
                    var cmp = (CmpFile)drawable;
                    foreach(var kv in cmp.Models) {
                        var node = hprefs.Nodes.Where((x) => x.Name == kv.Key).First();
                        node.HardpointsToNodes(kv.Value.Hardpoints);
                    }
                } else if (drawable is ModelFile) {
                    hprefs.Nodes[0].HardpointsToNodes(((ModelFile)drawable).Hardpoints);
                }
                popups.OpenPopup("Apply Complete");
            }

            if (ImGui.TreeNodeEx(ImGuiExt.Pad("Root"), TreeNodeFlags.DefaultOpen))
            {
                Theme.RenderTreeIcon("Root", "tree", Color4.DarkGreen);
                foreach (var n in cons)
                    DoConstructNode(n);
                if (!(drawable is SphFile)) DoModel(rootModel,null);
                ImGui.TreePop();
            }
            else
                Theme.RenderTreeIcon("Root", "tree", Color4.DarkGreen);
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
            modelViewport.Dispose();
            newHpBuffer.Dispose();
        }
    }
}
