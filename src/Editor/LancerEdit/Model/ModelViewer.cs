// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer;
using LibreLancer.ImUI;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf;
using Anm = LibreLancer.Utf.Anm;
using DF = LibreLancer.Utf.Dfm;
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
        bool drawVMeshWire = false;
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

        FileDialogFilters SurFilters = new FileDialogFilters(
           new FileFilter("Sur Files", "sur")
        );

        Material wireframeMaterial3db;
        Material normalsDebugMaterial;
        Dictionary<int, Material> partMaterials = new Dictionary<int, Material>();
        List<HardpointGizmo> gizmos = new List<HardpointGizmo>();
        AnimationComponent animator;
        UtfTab parent;
        MainWindow _window;
        PopupManager popups;
        ModelNodes hprefs;
        TextBuffer filterText = new TextBuffer(128);
        Part cameraPart;
        bool doCockpitCam = false;
        bool doFilter = false;
        string currentFilter;
        bool hasVWire = false;
        public ModelViewer(string name, IDrawable drawable, MainWindow win, UtfTab parent, ModelNodes hprefs)
        {
            Title = string.Format("Model Viewer ({0})",name);
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
                    if (p.Camera != null) continue;
                    if (p.Model.VMeshWire != null) hasVWire = true;
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
                    if (p.Camera != null) continue;
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
                if (mdl.VMeshWire != null) hasVWire = true;
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
            popups.AddPopup("Confirm Delete", ConfirmDelete, ImGuiWindowFlags.AlwaysAutoResize);
            popups.AddPopup("Warning", MinMaxWarning, ImGuiWindowFlags.AlwaysAutoResize);
            popups.AddPopup("Apply Complete", (x) =>
            {
                ImGui.Text("Hardpoints successfully written");
                if (ImGui.Button("Ok")) ImGui.CloseCurrentPopup();
            },ImGuiWindowFlags.AlwaysAutoResize);
            popups.AddPopup("Apply Complete##Parts", (x) =>
            {
                ImGui.Text("Parts successfully written");
                if (ImGui.Button("Ok")) ImGui.CloseCurrentPopup();
            }, ImGuiWindowFlags.AlwaysAutoResize);
            popups.AddPopup("New Hardpoint", NewHardpoint, ImGuiWindowFlags.AlwaysAutoResize);
        }
        //For warnings
        bool _isDirtyHp = false;
        public void OnDirtyHp()
        {
            if (_isDirtyHp) return;
            _isDirtyHp = true;
            parent.DirtyCountHp++;
        }
        bool _isDirtyPart = false;
        public void OnDirtyPart()
        {
            if (_isDirtyPart) return;
            _isDirtyPart = true;
            parent.DirtyCountPart++;
        }

        public override void SetActiveTab(MainWindow win)
        {
            win.ActiveTab = parent;
        }
        ConstructNode GetNodeCmp(CmpFile c, AbstractConstruct con)
        {
            var node = new ConstructNode() { Con = con };
            foreach (var p in c.Parts)
                if (p.Construct == con) {
                    node.Camera = p.Camera;
                    if(node.Camera == null) node.Model = p.Model;
                    else if (p.ObjectName.Equals("cockpit cam", StringComparison.OrdinalIgnoreCase)) {
                        cameraPart = p;
                    }
                }
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
            if (skel != null) {
                skel.UpdateScripts(TimeSpan.FromSeconds(elapsed));
            }
            if (newErrorTimer > 0) newErrorTimer -= elapsed;
        }
        Vector2 rotation = Vector2.Zero;
        bool firstTab = true;
        Color4 background = Color4.CornflowerBlue * new Color4(0.3f, 0.3f, 0.3f, 1f);
        System.Numerics.Vector3 editCol;

        bool[] openTabs = new bool[] { false, false, false, false };
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
            if(!(drawable is DF.DfmFile))
                TabButton("Hierarchy", 0);
            if (drawable is CmpFile && ((CmpFile)drawable).Animation != null)
                TabButton("Animations", 1);
            if (drawable is DF.DfmFile)
                TabButton("Skeleton", 2);
            TabButton("Render", 3);
            ImGuiNative.igEndGroup();
            ImGui.SameLine();
        }

        public override void OnHotkey(Hotkeys hk)
        {
            if (hk == Hotkeys.Deselect) selectedNode = null;
            if (hk == Hotkeys.ResetViewport) modelViewport.ResetControls();
        }

        public override void Draw()
        {
            bool doTabs = false;
            popups.Run();
            HardpointEditor();
            PartEditor();
            foreach (var t in openTabs) if (t) { doTabs = true; break; }
            var contentw = ImGui.GetWindowContentRegionWidth();
            if (doTabs)
            {
                ImGui.Columns(2, "##panels", true);
                if (firstTab)
                {
                    ImGui.SetColumnWidth(0, contentw * 0.23f);
                    firstTab = false;
                }
                ImGui.BeginChild("##tabchild");
                if (openTabs[0]) HierarchyPanel();
                if (openTabs[1]) AnimationPanel();
                if (openTabs[2]) SkeletonPanel();
                if (openTabs[3]) RenderPanel();
                ImGui.EndChild();
                ImGui.NextColumn();
            }
            TabButtons();
            ImGui.BeginChild("##main");
            if (ImGui.ColorButton("Background Color", new Vector4(background.R, background.G, background.B, 1),
                                ImGuiColorEditFlags.NoAlpha, new Vector2(22, 22)))
            {
                ImGui.OpenPopup("Background Color###" + Unique);
                editCol = new System.Numerics.Vector3(background.R, background.G, background.B);
            }
            bool wOpen = true;
            if (ImGui.BeginPopupModal("Background Color###" + Unique, ref wOpen, ImGuiWindowFlags.AlwaysAutoResize))
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
            if (hasVWire)
            {
                ImGui.Checkbox("VMeshWire", ref drawVMeshWire);
                ImGui.SameLine();
            }
            if(cameraPart != null) {
                ImGui.Checkbox("Cockpit Cam", ref doCockpitCam);
                ImGui.SameLine();
            }
            ImGui.Checkbox("Wireframe", ref doWireframe);
            ImGui.SameLine();
            ImGui.Text("View Mode:");
            ImGui.SameLine();
            ImGui.PushItemWidth(-1);
            ImGui.Combo("##modes", ref viewMode, viewModes, viewModes.Length);
            ImGui.PopItemWidth();
            DoViewport();
            //
            if(ImGui.Button("Reset Camera (Ctrl+R)"))
            {
                ResetCamera();
            }
            ImGui.SameLine();
            //
            if (!(drawable is SphFile) && !(drawable is DF.DfmFile))
            {
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Level of Detail:");
                ImGui.SameLine();
                ImGui.Checkbox("Use Distance", ref useDistance);
                ImGui.SameLine();
                ImGui.PushItemWidth(-1);
                if (useDistance)
                {
                    ImGui.SliderFloat("Distance", ref levelDistance, 0, maxDistance, "%f", 1);
                }
                else
                {
                    ImGui.Combo("Level", ref level, levels, levels.Length);
                }
                ImGui.PopItemWidth();
            }
            ImGui.EndChild();

            if(_window.Config.ViewButtons)
            {
                ImGui.SetNextWindowPos(new Vector2(_window.Width - viewButtonsWidth, 90));
                ImGui.Begin("viewButtons#" + Unique, ImGuiWindowFlags.AlwaysAutoResize |
                ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove);
                ImGui.Dummy(new Vector2(120, 2));
                ImGui.Columns(2, "##border", false);
                if (ImGui.Button("Top", new Vector2(55, 0)))
                    modelViewport.GoTop();
                ImGui.NextColumn();
                if (ImGui.Button("Bottom", new Vector2(55, 0)))
                    modelViewport.GoBottom();
                ImGui.NextColumn();
                if (ImGui.Button("Left", new Vector2(55, 0)))
                    modelViewport.GoLeft();
                ImGui.NextColumn();
                if (ImGui.Button("Right", new Vector2(55, 0)))
                    modelViewport.GoRight();
                ImGui.NextColumn();
                if (ImGui.Button("Front", new Vector2(55, 0)))
                    modelViewport.GoFront();
                ImGui.NextColumn();
                if (ImGui.Button("Back", new Vector2(55, -1)))
                    modelViewport.GoBack();
                viewButtonsWidth = ImGui.GetWindowWidth() + 60;
                ImGui.End();
            }
        }
        float viewButtonsWidth = 100;




        class HardpointGizmo
        {
            public HardpointDefinition Definition;
            public AbstractConstruct Parent;
            public bool Enabled;
            public Matrix4? Override = null;
            public float EditingMin;
            public float EditingMax;
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
            public CmpCameraInfo Camera;
        }

        List<ConstructNode> cons = new List<ConstructNode>();
        ModelFile rootModel;
        List<AbstractConstruct> conOrphan = new List<AbstractConstruct>();
        ConstructNode selectedNode = null;
        public static string ConType(AbstractConstruct construct)
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
            var n = ImGuiExt.IDSafe(string.Format("{0} ({1})", cn.Con.ChildName, ConType(cn.Con)));
            var tflags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;
            if (selectedNode == cn) tflags |= ImGuiTreeNodeFlags.Selected;
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
            bool mdlVisible = true;
            if(cn.Model != null)
                mdlVisible = !hiddenModels.Contains(cn.Model);
            if (!mdlVisible)
            {
                var disabledColor = ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled];
                ImGui.PushStyleColor(ImGuiCol.Text, disabledColor);
            }
            if (ImGui.TreeNodeEx(ImGuiExt.Pad(n), tflags))
            {
                if (!mdlVisible) ImGui.PopStyleColor();
                if (ImGui.IsItemClicked(0))
                    selectedNode = cn;
                ConstructContext(cn, mdlVisible);
                Theme.RenderTreeIcon(n, icon, color);
                foreach (var child in cn.Nodes)
                    DoConstructNode(child);
                if (cn.Camera != null)
                    DoCamera(cn.Camera, cn.Con);
                else
                    DoModel(cn.Model, cn.Con);
                ImGui.TreePop();
            }
            else
            {
                if (!mdlVisible) ImGui.PopStyleColor();
                if (ImGui.IsItemClicked(0))
                    selectedNode = cn;
                ConstructContext(cn, mdlVisible);
                Theme.RenderTreeIcon(n, icon, color);
            }
        }

        void ConstructContext(ConstructNode con, bool mdlVisible)
        {
            if (ImGui.IsItemClicked(1))
                ImGui.OpenPopup(con.Con.ChildName + "_context");
            if(ImGui.BeginPopupContextItem(con.Con.ChildName + "_context")) {
                if (con.Model != null)
                {
                    //Visibility of model (this is bad)
                    bool visibleVar = mdlVisible;
                    Theme.IconMenuToggle("Visible", "eye", Color4.White, ref visibleVar, true);
                    if(visibleVar != mdlVisible)
                    {
                        if (visibleVar)
                            hiddenModels.Remove(con.Model);
                        else
                            hiddenModels.Add(con.Model);
                    }
                }

                if (Theme.BeginIconMenu("Change To","change",Color4.White)) {
                    var cmp = (CmpFile)drawable;
                    if(!(con.Con is FixConstruct) && Theme.IconMenuItem("Fix","fix",Color4.LightYellow,true)) {
                        var fix = new FixConstruct(cmp.Constructs)
                        {
                            ParentName = con.Con.ParentName,
                            ChildName = con.Con.ChildName,
                            Origin = con.Con.Origin,
                            Rotation = con.Con.Rotation
                        };
                        fix.Reset();
                        ReplaceConstruct(con, fix);
                        OnDirtyPart();
                    }
                    if(!(con.Con is RevConstruct) && Theme.IconMenuItem("Rev","rev",Color4.LightCoral,true)) {
                        var rev = new RevConstruct(cmp.Constructs)
                        {
                            ParentName = con.Con.ParentName,
                            ChildName = con.Con.ChildName,
                            Origin = con.Con.Origin,
                            Rotation = con.Con.Rotation
                        };
                        ReplaceConstruct(con, rev);
                        OnDirtyPart();
                    }
                    if(!(con.Con is PrisConstruct) && Theme.IconMenuItem("Pris","pris",Color4.LightPink,true)) {
                        var pris = new PrisConstruct(cmp.Constructs)
                        {
                            ParentName = con.Con.ParentName,
                            ChildName = con.Con.ChildName,
                            Origin = con.Con.Origin,
                            Rotation = con.Con.Rotation
                        };
                        ReplaceConstruct(con, pris);
                        OnDirtyPart();
                    }
                    if(!(con.Con is SphereConstruct) && Theme.IconMenuItem("Sphere","sphere",Color4.LightGreen,true)) {
                        var sphere = new SphereConstruct(cmp.Constructs)
                        {
                            ParentName = con.Con.ParentName,
                            ChildName = con.Con.ChildName,
                            Origin = con.Con.Origin,
                            Rotation = con.Con.Rotation
                        };
                        ReplaceConstruct(con, sphere);
                        OnDirtyPart();
                    }
                    ImGui.EndMenu();
                }
                if(Theme.IconMenuItem("Edit","edit",Color4.White,true)) {
                    AddPartEditor(con.Con);
                }
                ImGui.EndPopup();
            }
        }
        void DoCamera(CmpCameraInfo cam, AbstractConstruct con)
        {

        }
        List<ModelFile> hiddenModels = new List<ModelFile>();
        void DoModel(ModelFile mdl, AbstractConstruct con)
        {
            //Hardpoints
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
                List<Action> addActions = new List<Action>();
                foreach (var hp in mdl.Hardpoints)
                {
                    if(doFilter) {
                        if (hp.Name.IndexOf(currentFilter,StringComparison.OrdinalIgnoreCase) == -1) continue;
                    }
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
                    ImGui.Selectable(ImGuiExt.IDSafe(hp.Name));
                    var action = EditDeleteHpMenu(mdl.Path + hp.Name);
                    if (action == ContextActions.Delete)
                    {
                        hpDelete = hp;
                        hpDeleteFrom = mdl.Hardpoints;
                        popups.OpenPopup("Confirm Delete");
                    }
                    if (action == ContextActions.Edit) hpEditing = hp;
                    if(action == ContextActions.MirrorX) {
                        var newHp = MakeDuplicate(GetDupName(hp.Name), hp);
                        //do mirroring
                        newHp.Position.X = -newHp.Position.X;
                        newHp.Orientation *= new Matrix4(
                            -1, 0, 0, 0,
                            0, 1, 0, 0,
                            0, 0, 1, 0,
                            0, 0, 0, 1
                        );
                        //add
                        addActions.Add(() =>
                        {
                            mdl.Hardpoints.Add(newHp);
                            gizmos.Add(new HardpointGizmo(newHp, gz.Parent));
                            OnDirtyHp();
                        });
                    }
                    if(action == ContextActions.MirrorY) {
                        var newHp = MakeDuplicate(GetDupName(hp.Name), hp);
                        //do mirroring
                        newHp.Position.Y = -newHp.Position.Y;
                        newHp.Orientation *= new Matrix4(
                            1, 0, 0, 0,
                            0, -1, 0, 0,
                            0, 0, 1, 0,
                            0, 0, 0, 1
                        );
                        //add
                        addActions.Add(() =>
                        {
                            mdl.Hardpoints.Add(newHp);
                            gizmos.Add(new HardpointGizmo(newHp, gz.Parent));
                            OnDirtyHp();
                        });
                    }
                    if(action == ContextActions.MirrorZ) {
                        var newHp = MakeDuplicate(GetDupName(hp.Name), hp);
                        //do mirroring
                        newHp.Position.Z = -newHp.Position.Z;
                        newHp.Orientation *= new Matrix4(
                            1, 0, 0, 0,
                            0, 1, 0, 0,
                            0, 0, -1, 0,
                            0, 0, 0, 1
                        );
                        //add
                        addActions.Add(() =>
                        {
                            mdl.Hardpoints.Add(newHp);
                            gizmos.Add(new HardpointGizmo(newHp, gz.Parent));
                            OnDirtyHp();
                        });
                    }
                }
                foreach (var action in addActions) action();
                ImGui.TreePop();
            }
        }
        HardpointDefinition MakeDuplicate(string name, HardpointDefinition src)
        {
            if(src is FixedHardpointDefinition)
            {
                return new FixedHardpointDefinition(name) { Position = src.Position, Orientation = src.Orientation };
            }
            else if (src is RevoluteHardpointDefinition)
            {
                var revSrc = (RevoluteHardpointDefinition)src;
                return new RevoluteHardpointDefinition(name)
                {
                    Position = src.Position,
                    Orientation = src.Orientation,
                    Min = revSrc.Min,
                    Max = revSrc.Max
                };
            }
            return null;
        }
        enum ContextActions {
            None, NewFixed,NewRevolute,Edit,Delete,MirrorX,MirrorY, MirrorZ
        }
        ContextActions NewHpMenu(string n)
        {
            if(ImGui.IsItemClicked(1))
                ImGui.OpenPopup(n + "_HardpointContext");
            if(ImGui.BeginPopupContextItem(n + "_HardpointContext")) {
                if(Theme.BeginIconMenu("New","add",Color4.White)) {
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
            if(ImGui.IsItemClicked(1))
                ImGui.OpenPopup(n + "_HardpointEditCtx");
            if(ImGui.BeginPopupContextItem(n + "_HardpointEditCtx")) {
                if(Theme.IconMenuItem("Edit","edit",Color4.White,true)) return ContextActions.Edit;
                if(Theme.IconMenuItem("Delete","delete",Color4.White,true)) return ContextActions.Delete;
                if(Theme.BeginIconMenu("Duplicate", "duplicate", Color4.White))
                {
                    if (Theme.IconMenuItem("Mirror X", "axis_x", Color4.Red, true)) return ContextActions.MirrorX;
                    if (Theme.IconMenuItem("Mirror Y", "axis_y", Color4.LightGreen, true)) return ContextActions.MirrorY;
                    if (Theme.IconMenuItem("Mirror Z", "axis_z", Color4.LightBlue, true)) return ContextActions.MirrorZ;
                    ImGui.EndMenu();
                }
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

        string surname;
        bool surShowHull = true;
        bool surShowHps = true;
        void HierarchyPanel()
        {
            if(!(drawable is DF.DfmFile) && !(drawable is SphFile))
            {
                //Sur
                if(ImGui.Button("Open Sur"))
                {
                    var file = FileDialog.Open(SurFilters);
                    surname = System.IO.Path.GetFileName(file);
                    LibreLancer.Physics.Sur.SurFile sur;
                    try
                    {
                        using (var f = System.IO.File.OpenRead(file))
                        {
                            sur = new LibreLancer.Physics.Sur.SurFile(f);
                        }
                    }
                    catch (Exception)
                    {
                        sur = null;
                    }
                    if (sur != null) ProcessSur(sur);
                }
                if(surs != null)
                {
                    ImGui.Separator();
                    ImGui.Text("Sur: " + surname);
                    ImGui.Checkbox("Show Hull", ref surShowHull);
                    ImGui.Checkbox("Show Hardpoints", ref surShowHps);
                    ImGui.Separator();
                }
            }
            if (ImGuiExt.Button("Apply Hardpoints", _isDirtyHp))
            {
                if (drawable is CmpFile)
                {
                    var cmp = (CmpFile)drawable;
                    foreach (var kv in cmp.Models)
                    {
                        var node = hprefs.Nodes.Where((x) => x.Name == kv.Key).First();
                        node.HardpointsToNodes(kv.Value.Hardpoints);
                    }
                }
                else if (drawable is ModelFile)
                {
                    hprefs.Nodes[0].HardpointsToNodes(((ModelFile)drawable).Hardpoints);
                }
                if(_isDirtyHp)
                {
                    _isDirtyHp = false;
                    parent.DirtyCountHp--;
                }
                popups.OpenPopup("Apply Complete");
            }
            if ((drawable is CmpFile) && ((CmpFile)drawable).Parts.Count > 1 && ImGuiExt.Button("Apply Parts", _isDirtyPart))
            {
                WriteConstructs();
                if(_isDirtyPart)
                {
                    _isDirtyPart = false;
                    parent.DirtyCountPart--;
                }
                popups.OpenPopup("Apply Complete##Parts");
            }
            if (ImGuiExt.ToggleButton("Filter", doFilter)) doFilter = !doFilter;
            if (doFilter) {
                ImGui.InputText("##filter", filterText.Pointer, (uint)filterText.Size, ImGuiInputTextFlags.None, filterText.Callback);
                currentFilter = filterText.GetText();
            }
            else
                currentFilter = null;

            ImGui.Separator();
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

            var rootVisible = !hiddenModels.Contains(rootModel);
            if (!rootVisible)
            {
                var col = ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled];
                ImGui.PushStyleColor(ImGuiCol.Text, col);
            }
            if (ImGui.TreeNodeEx(ImGuiExt.Pad("Root"), ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (!rootVisible) ImGui.PopStyleColor();
                RootModelContext(rootVisible);
                Theme.RenderTreeIcon("Root", "tree", Color4.DarkGreen);
                foreach (var n in cons)
                    DoConstructNode(n);
                if (!(drawable is SphFile)) DoModel(rootModel, null);
                ImGui.TreePop();
            }
            else {
                if (!rootVisible) ImGui.PopStyleColor();
                RootModelContext(rootVisible);
                Theme.RenderTreeIcon("Root", "tree", Color4.DarkGreen);
            }
        }

        void RootModelContext(bool rootVisible)
        {
            if (rootModel != null && ImGui.IsItemClicked(1))
                ImGui.OpenPopup(Unique + "_mdl_rootpopup");
            if (ImGui.BeginPopupContextItem(Unique + "_mdl_rootpopup"))
            {
                bool visibleVar = rootVisible;
                Theme.IconMenuToggle("Visible", "eye", Color4.White, ref visibleVar, true);
                if (visibleVar != rootVisible)
                {
                    if (visibleVar)
                        hiddenModels.Remove(rootModel);
                    else
                        hiddenModels.Add(rootModel);
                }
                ImGui.EndPopup();
            }
        }
        void AnimationPanel()
        {
            var anm = ((CmpFile)drawable).Animation;
            int j = 0;
            foreach (var sc in anm.Scripts)
            {
                if (ImGui.Button(ImGuiExt.IDWithExtra(sc.Key, j++)))
                {
                    animator.StartAnimation(sc.Key, false);
                }
            }
            ImGui.Separator();
            if (ImGui.Button("Reset")) animator.ResetAnimations();
        }

        bool drawSkeleton = false;
        Anm.AnmFile anmFile;
        void SkeletonPanel()
        {
            ImGui.Checkbox("Draw Skeleton", ref drawSkeleton);
            if(ImGui.Button("Open Anm")) {
                var file = FileDialog.Open();
                if (file == null) return;
                anmFile = new Anm.AnmFile(file);
            }
            if(anmFile != null)
            {
                ImGui.Separator();
                foreach(var script in anmFile.Scripts)
                {
                    if (ImGui.Button(script.Key)) skel.StartScript(script.Value, 0, 1, 0);
                }
            }
        }

        int imageWidth = 256;
        int imageHeight = 256;
        bool renderBackground = false;
        FileDialogFilters pngFilters = new FileDialogFilters(new FileFilter("PNG Files", "png"));
        unsafe void RenderPanel()
        {
            ImGui.Text("Render to Image");
            ImGui.Checkbox("Background?", ref renderBackground);
            ImGui.InputInt("Width", ref imageWidth);
            ImGui.InputInt("Height", ref imageHeight);
            var w = Math.Max(imageWidth, 16);
            var h = Math.Max(imageHeight, 16);
            var rpanelWidth = ImGui.GetWindowWidth() - 15;
            int rpanelHeight = Math.Min((int)(rpanelWidth * ((float)h / (float)w)), 4096);
            DoPreview((int)rpanelWidth, rpanelHeight);
            if (ImGui.Button("Export"))
            {
                if(imageWidth < 16 || imageHeight < 16)
                {
                    FLLog.Error("Export", "Image minimum size is 16x16");
                }
                else
                {
                    string output;
                    if((output = FileDialog.Save(pngFilters)) != null)
                        RenderImage(output);
                }
            }
        }
        public override void DetectResources(List<MissingReference> missing, List<uint> matrefs, List<string> texrefs)
        {
            ResourceDetection.DetectDrawable(Name, drawable, res, missing, matrefs, texrefs);
        }

        public override void Dispose()
        {
            if (_isDirtyPart) parent.DirtyCountPart--;
            if (_isDirtyHp) parent.DirtyCountHp--;

            if (surs != null)
            {
                foreach(var s in surs)
                {
                    s.Vertices.Dispose();
                    s.Elements.Dispose();
                }
            }
            modelViewport.Dispose();
            imageViewport.Dispose();
            previewViewport.Dispose();
            newHpBuffer.Dispose();
        }
    }
}
