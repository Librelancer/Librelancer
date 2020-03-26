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

        private RigidModel vmsModel;
        
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
            if (drawable is CmpFile)
            {
                //Setup Editor UI for constructs + hardpoints
                vmsModel = (drawable as CmpFile).CreateRigidModel(true);
                animator = new AnimationComponent(vmsModel, (drawable as CmpFile).Animation);
                int maxLevels = 0;
                foreach (var p in vmsModel.AllParts)
                {
                    if (p.Mesh != null && p.Mesh.Levels != null)
                        maxLevels = Math.Max(maxLevels, p.Mesh.Levels.Length);
                }
                levels = new string[maxLevels + 1];
                for (int i = 0; i <= maxLevels; i++)
                    levels[i] = i.ToString();
            }
            else if (drawable is ModelFile)
            {
                vmsModel = (drawable as ModelFile).CreateRigidModel(true);
                levels = new string[vmsModel.AllParts[0].Mesh.Levels.Length];
                for (int i = 0; i < levels.Length; i++)
                    levels[i] = i.ToString();
            }
            else if (drawable is SphFile)
            {
                levels = new string[] {"0"};
                vmsModel = (drawable as SphFile).CreateRigidModel(true);
            }
            if (vmsModel != null)
            {
                foreach (var p in vmsModel.AllParts)
                {
                    foreach (var hp in p.Hardpoints)
                    {
                        gizmos.Add(new HardpointGizmo(hp, p));
                    }
                    if(p.Wireframe != null) hasVWire = true;
                }
            }
            SetupViewport();
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
        System.Numerics.Vector3 editCol;
        System.Numerics.Vector3 editCol2;
        private bool editGrad;
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
            if(drawable is CmpFile || drawable is ModelFile)
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
            if (ViewerControls.GradientButton("Background", backgroundTop, backgroundBottom, new Vector2(22), _window.Viewport, gradientBackground))
            {
                ImGui.OpenPopup("Background Color###" + Unique);
                editCol = new System.Numerics.Vector3(backgroundTop.R, backgroundTop.G, backgroundTop.B);
                editCol2 = new System.Numerics.Vector3(backgroundBottom.R, backgroundBottom.G, backgroundBottom.B);
                editGrad = gradientBackground;
            }
            bool wOpen = true;
            if (ImGui.BeginPopupModal("Background Color###" + Unique, ref wOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Checkbox("Gradient", ref editGrad);
                
                ImGui.ColorPicker3(editGrad ? "Top###a" : "###a", ref editCol);
                if (editGrad)
                {
                    ImGui.SameLine();
                    ImGui.ColorPicker3("Bottom###b", ref editCol2);
                }
                if (ImGui.Button("OK"))
                {
                    backgroundTop = new Color4(editCol.X, editCol.Y, editCol.Z, 1);
                    backgroundBottom = new Color4(editCol2.X, editCol2.Y, editCol2.Z, 1);
                    gradientBackground = editGrad;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button("Default"))
                {
                    var def = Color4.CornflowerBlue * new Color4(0.3f, 0.3f, 0.3f, 1f);
                    editCol = new System.Numerics.Vector3(def.R, def.G, def.B);
                    editGrad = false;
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel")) ImGui.CloseCurrentPopup();
                ImGui.EndPopup();
            }
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Background");
            ImGui.SameLine();
            if (vmsModel != null)
            {
                ImGui.Checkbox("Starsphere", ref isStarsphere);
                ImGui.SameLine();
            }
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
            public Hardpoint Hardpoint;
            public RigidModelPart Parent;
            public bool Enabled;
            public Matrix4? Override = null;
            public float EditingMin;
            public float EditingMax;
            public HardpointGizmo(Hardpoint hp, RigidModelPart parent)
            {
                Hardpoint = hp;
                Parent = parent;
                Enabled = false;
            }
        }

        RigidModelPart selectedNode = null;
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
        void DoConstructNode(RigidModelPart cn)
        {
            var n = ImGuiExt.IDSafe(string.Format("{0} ({1})", cn.Construct.ChildName, ConType(cn.Construct)));
            var tflags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;
            if (selectedNode == cn) tflags |= ImGuiTreeNodeFlags.Selected;
            var icon = "fix";
            var color = Color4.LightYellow;
            if (cn.Construct is PrisConstruct)
            {
                icon = "pris";
                color = Color4.LightPink;
            }
            if (cn.Construct is SphereConstruct)
            {
                icon = "sphere";
                color = Color4.LightGreen;
            }
            if (cn.Construct is RevConstruct)
            {
                icon = "rev";
                color = Color4.LightCoral;
            }
            bool mdlVisible = cn.Active;
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
                if (cn.Children != null)
                {
                    foreach (var child in cn.Children)
                        DoConstructNode(child);
                }

                DoModel(cn);
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

        void ConstructContext(RigidModelPart con, bool mdlVisible)
        {
            if (ImGui.IsItemClicked(1))
                ImGui.OpenPopup(con.Construct.ChildName + "_context");
            if(ImGui.BeginPopupContextItem(con.Construct.ChildName + "_context")) {
                if (con.Mesh != null)
                {
                    //Visibility of model (this is bad)
                    bool visibleVar = mdlVisible;
                    Theme.IconMenuToggle("Visible", "eye", Color4.White, ref visibleVar, true);
                    if(visibleVar != mdlVisible)
                    {
                        con.Active = visibleVar;
                    }
                }
                if (Theme.BeginIconMenu("Change To","change",Color4.White)) {
                    var cmp = (CmpFile)drawable;
                    if(!(con.Construct is FixConstruct) && Theme.IconMenuItem("Fix","fix",Color4.LightYellow,true)) {
                        var fix = new FixConstruct(cmp.Constructs)
                        {
                            ParentName = con.Construct.ParentName,
                            ChildName = con.Construct.ChildName,
                            Origin = con.Construct.Origin,
                            Rotation = con.Construct.Rotation
                        };
                        fix.Reset();
                        con.Construct = fix;
                        OnDirtyPart();
                    }
                    if(!(con.Construct is RevConstruct) && Theme.IconMenuItem("Rev","rev",Color4.LightCoral,true)) {
                        var rev = new RevConstruct()
                        {
                            ParentName = con.Construct.ParentName,
                            ChildName = con.Construct.ChildName,
                            Origin = con.Construct.Origin,
                            Rotation = con.Construct.Rotation
                        };
                        con.Construct = rev;
                        OnDirtyPart();
                    }
                    if(!(con.Construct is PrisConstruct) && Theme.IconMenuItem("Pris","pris",Color4.LightPink,true)) {
                        var pris = new PrisConstruct()
                        {
                            ParentName = con.Construct.ParentName,
                            ChildName = con.Construct.ChildName,
                            Origin = con.Construct.Origin,
                            Rotation = con.Construct.Rotation
                        };
                        con.Construct = pris;
                        OnDirtyPart();
                    }
                    if(!(con.Construct is SphereConstruct) && Theme.IconMenuItem("Sphere","sphere",Color4.LightGreen,true)) {
                        var sphere = new SphereConstruct()
                        {
                            ParentName = con.Construct.ParentName,
                            ChildName = con.Construct.ChildName,
                            Origin = con.Construct.Origin,
                            Rotation = con.Construct.Rotation
                        };
                        con.Construct = sphere;
                        OnDirtyPart();
                    }
                    ImGui.EndMenu();
                }
                if(Theme.IconMenuItem("Edit","edit",Color4.White,true))
                {
                    AddPartEditor(con.Construct);
                }
                ImGui.EndPopup();
            }
        }
        void DoCamera(CmpCameraInfo cam, AbstractConstruct con)
        {

        }
        
        void DoModel(RigidModelPart part)
        {
            //Hardpoints
            bool open = ImGui.TreeNode(ImGuiExt.Pad("Hardpoints"));
            var act = NewHpMenu(part.Path);
            switch(act) {
                case ContextActions.NewFixed:
                case ContextActions.NewRevolute:
                    newIsFixed = act == ContextActions.NewFixed;
                    addTo = part;
                    newHpBuffer.Clear();
                    popups.OpenPopup("New Hardpoint");
                    break;
            }
            Theme.RenderTreeIcon("Hardpoints", "hardpoint", Color4.CornflowerBlue);
            if (open)
            {
                List<Action> addActions = new List<Action>();
                foreach (var hp in part.Hardpoints)
                {
                    if(doFilter) {
                        if (hp.Name.IndexOf(currentFilter,StringComparison.OrdinalIgnoreCase) == -1) continue;
                    }
                    HardpointGizmo gz = null;
                    foreach (var gizmo in gizmos)
                    {
                        if (gizmo.Hardpoint == hp)
                        {
                            gz = gizmo;
                            break;
                        }
                    }
                    if (hp.Definition is RevoluteHardpointDefinition)
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
                    var action = EditDeleteHpMenu(part.Path + hp.Name);
                    if (action == ContextActions.Delete)
                    {
                        hpDelete = hp;
                        hpDeleteFrom = part.Hardpoints;
                        popups.OpenPopup("Confirm Delete");
                    }
                    if (action == ContextActions.Edit) hpEditing = hp;
                    if(action == ContextActions.MirrorX) {
                        var newHp = MakeDuplicate(GetDupName(hp.Name), hp);
                        //do mirroring
                        newHp.Definition.Position.X = -newHp.Definition.Position.X;
                        newHp.Definition.Orientation *= new Matrix4(
                            -1, 0, 0, 0,
                            0, 1, 0, 0,
                            0, 0, 1, 0,
                            0, 0, 0, 1
                        );
                        //add
                        addActions.Add(() =>
                        {
                            part.Hardpoints.Add(newHp);
                            gizmos.Add(new HardpointGizmo(newHp, gz.Parent));
                            OnDirtyHp();
                        });
                    }
                    if(action == ContextActions.MirrorY) {
                        var newHp = MakeDuplicate(GetDupName(hp.Name), hp);
                        //do mirroring
                        newHp.Definition.Position.Y = -newHp.Definition.Position.Y;
                        newHp.Definition.Orientation *= new Matrix4(
                            1, 0, 0, 0,
                            0, -1, 0, 0,
                            0, 0, 1, 0,
                            0, 0, 0, 1
                        );
                        //add
                        addActions.Add(() =>
                        {
                            part.Hardpoints.Add(newHp);
                            gizmos.Add(new HardpointGizmo(newHp, gz.Parent));
                            OnDirtyHp();
                        });
                    }
                    if(action == ContextActions.MirrorZ) {
                        var newHp = MakeDuplicate(GetDupName(hp.Name), hp);
                        //do mirroring
                        newHp.Definition.Position.Z = -newHp.Definition.Position.Z;
                        newHp.Definition.Orientation *= new Matrix4(
                            1, 0, 0, 0,
                            0, 1, 0, 0,
                            0, 0, -1, 0,
                            0, 0, 0, 1
                        );
                        //add
                        addActions.Add(() =>
                        {
                            part.Hardpoints.Add(newHp);
                            gizmos.Add(new HardpointGizmo(newHp, gz.Parent));
                            OnDirtyHp();
                        });
                    }
                }
                foreach (var action in addActions) action();
                ImGui.TreePop();
            }
        }

        Hardpoint MakeDuplicate(string name, Hardpoint src)
        {
            return new Hardpoint(DupDef(name, src.Definition), src.Parent);
        }
        HardpointDefinition DupDef(string name, HardpointDefinition src)
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
                if (drawable is ModelFile)
                {
                    hprefs.Nodes[0].HardpointsToNodes(vmsModel.Root.Hardpoints);
                } 
                else if (drawable is CmpFile)
                {
                    foreach (var mdl in vmsModel.AllParts)
                    {
                        var node = hprefs.Nodes.First((x) => x.Name == mdl.Path);
                        node.HardpointsToNodes(mdl.Hardpoints);
                    }
                }
                if(_isDirtyHp)
                {
                    _isDirtyHp = false;
                    parent.DirtyCountHp--;
                }
                popups.OpenPopup("Apply Complete");
            }
            if (vmsModel.AllParts.Length > 1 && ImGuiExt.Button("Apply Parts", _isDirtyPart))
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
                ImGui.Text(selectedNode.Construct.ChildName);
                ImGui.Text(selectedNode.Construct.GetType().Name);
                ImGui.Text("Origin: " + selectedNode.Construct.Origin.ToString());
                var euler = selectedNode.Construct.Rotation.GetEuler();
                ImGui.Text(string.Format("Rotation: (Pitch {0:0.000}, Yaw {1:0.000}, Roll {2:0.000})",
                                        MathHelper.RadiansToDegrees(euler.X),
                                        MathHelper.RadiansToDegrees(euler.Y),
                                         MathHelper.RadiansToDegrees(euler.Z)));
                ImGui.Separator();
            }

            if (!vmsModel.Root.Active)
            {
                var col = ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled];
                ImGui.PushStyleColor(ImGuiCol.Text, col);
            }
            if (ImGui.TreeNodeEx(ImGuiExt.Pad("Root"), ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (!vmsModel.Root.Active) ImGui.PopStyleColor();
                RootModelContext(vmsModel.Root.Active);
                Theme.RenderTreeIcon("Root", "tree", Color4.DarkGreen);
                if (vmsModel.Root.Children != null)
                {
                    foreach (var n in vmsModel.Root.Children)
                        DoConstructNode(n);
                }

                DoModel(vmsModel.Root);
                //if (!(drawable is SphFile)) DoModel(rootModel, null);
                ImGui.TreePop();
            }
            else {
                if (!vmsModel.Root.Active) ImGui.PopStyleColor();
                RootModelContext(vmsModel.Root.Active);
                Theme.RenderTreeIcon("Root", "tree", Color4.DarkGreen);
            }
        }

        void RootModelContext(bool rootVisible)
        {
            if (vmsModel.Root != null && ImGui.IsItemClicked(1))
                ImGui.OpenPopup(Unique + "_mdl_rootpopup");
            if (ImGui.BeginPopupContextItem(Unique + "_mdl_rootpopup"))
            {
                bool visibleVar = rootVisible;
                Theme.IconMenuToggle("Visible", "eye", Color4.White, ref visibleVar, true);
                if (visibleVar != rootVisible) vmsModel.Root.Active = visibleVar;
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
