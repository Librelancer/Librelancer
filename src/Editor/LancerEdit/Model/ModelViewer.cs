﻿// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using LibreLancer;
using LibreLancer.ImUI;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf;
using Anm = LibreLancer.Utf.Anm;
using DF = LibreLancer.Utf.Dfm;
using ImGuiNET;
using LibreLancer.Client.Components;
using LibreLancer.ContentEdit;
using LibreLancer.ContentEdit.Model;
using LibreLancer.Dialogs;
using LibreLancer.Render;
using LibreLancer.Sur;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LancerEdit
{
    public partial class ModelViewer : EditorTab
    {
        Lighting lighting;
        IDrawable drawable;
        RenderContext rstate;
        CommandBuffer buffer;
        ResourceManager res;
        public string Name;
        int viewMode = 0;
        public static readonly DropdownOption[] ViewModes = new[]
        {
            new DropdownOption("Textured", Icons.Image),
            new DropdownOption("Lit", Icons.Lightbulb),
            new DropdownOption("Flat", Icons.PenSquare),
            new DropdownOption("Normals", Icons.ArrowsAltH),
            new DropdownOption("None", Icons.EyeSlash)
        };
        private static readonly DropdownOption[] camModesNormal = new[]
        {
            new DropdownOption("Arcball", Icons.Globe, CameraModes.Arcball),
            new DropdownOption("Walkthrough", Icons.StreetView, CameraModes.Walkthrough),
            new DropdownOption("Starsphere", Icons.Star, CameraModes.Starsphere),
        };
        private static readonly DropdownOption[] camModesCockpit = new[]
        {
            new DropdownOption("Arcball", Icons.Globe, CameraModes.Arcball),
            new DropdownOption("Walkthrough", Icons.StreetView, CameraModes.Walkthrough),
            new DropdownOption("Cockpit", Icons.Video, CameraModes.Cockpit),
        };
        bool doBackground = true;
        bool doWireframe = false;
        bool drawNormals = false;
        bool blenderEnabled = false;
        private bool doBounds = false;
        bool drawVMeshWire = false;

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
        TextBuffer filterText = new TextBuffer(128);
        Part cameraPart;
        bool doFilter = false;
        string currentFilter;
        bool hasVWire = false;
        bool showWarnings = false;

        private RigidModel vmsModel;

        public ModelViewer(string name, IDrawable drawable, MainWindow win, UtfTab parent, ModelNodes hprefs)
        {
            blenderEnabled = Blender.BlenderPathValid(win.Config.BlenderPath);
            selectedCam = win.Config.DefaultCameraMode;
            viewMode = win.Config.DefaultRenderMode;
            Title = string.Format("Model Viewer ({0})",name);
            Name = name;
            this.drawable = drawable;
            this.parent = parent;
            this.TabColor = parent.TabColor;
            this.hprefs = hprefs;
            rstate = win.RenderContext;
            res = parent.DetachedResources ?? win.Resources;
            buffer = win.Commands;
            _window = win;
            SaveStrategy = parent.SaveStrategy;
            if (drawable is CmpFile)
            {
                //Setup Editor UI for constructs + hardpoints
                vmsModel = (drawable as CmpFile).CreateRigidModel(true, res);
                animator = new AnimationComponent(vmsModel, (drawable as CmpFile).Animation);
                int maxLevels = 0;
                foreach (var p in vmsModel.AllParts)
                {
                    if (p.Mesh != null && p.Mesh.Levels != null)
                    {
                        maxLevels = Math.Max(maxLevels, p.Mesh.Levels.Length);
                        if(p.Mesh.Switch2 != null)
                            for(int i = 0; i < p.Mesh.Switch2.Length - 1; i++)
                                maxDistance = Math.Max(p.Mesh.Switch2[i], maxDistance);
                    }

                }
                foreach (var cmpPart in (drawable as CmpFile).Parts) {
                    if (cmpPart.Camera != null) {
                        cameraPart = cmpPart;
                        break;
                    }
                }
                levels = new string[maxLevels];
                for (int i = 0; i < maxLevels; i++)
                    levels[i] = i.ToString();
            }
            else if (drawable is ModelFile)
            {
                vmsModel = (drawable as ModelFile).CreateRigidModel(true, res);
                levels = new string[vmsModel.AllParts[0].Mesh.Levels.Length];
                for (int i = 0; i < levels.Length; i++)
                    levels[i] = i.ToString();
                if (vmsModel.Root.Mesh.Switch2 != null)
                {
                    foreach (var d in vmsModel.Root.Mesh.Switch2)
                        maxDistance = Math.Max(d, maxDistance);
                }
            }
            else if (drawable is SphFile)
            {
                levels = new string[] {"0"};
                vmsModel = (drawable as SphFile).CreateRigidModel(true, res);
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

        public override void Update(double elapsed)
        {
            if (animator != null)
                animator.Update(elapsed);
            if (skel != null) {
                skel.UpdateScripts(elapsed);
            }
            if (newErrorTimer > 0) newErrorTimer -= elapsed;
        }
        Vector2 rotation = Vector2.Zero;
        bool firstTab = true;
        BitArray128 openTabs;
        void TabButton(string name, int idx)
        {
            if (TabHandler.VerticalTab($"{name}", openTabs[idx]))
            {
                if (!openTabs[idx])
                {
                    openTabs = new BitArray128();
                    openTabs[idx] = true;
                }
                else
                    openTabs = new BitArray128();
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
            if(drawable is CmpFile || drawable is ModelFile)
                TabButton("Export", 4);
            ImGuiNative.igEndGroup();
            ImGui.SameLine();
        }

        public override void OnHotkey(Hotkeys hk, bool shiftPressed)
        {
            if (hk == Hotkeys.Deselect) selectedNode = null;
            if (hk == Hotkeys.ResetViewport) modelViewport.ResetControls();
            if (hk == Hotkeys.ToggleGrid) showGrid = !showGrid;
        }
        int selectedCam = 0;

        public override void Draw(double elapsed)
        {
            popups.Run();
            HardpointEditor();
            PartEditor();
            var contentw = ImGui.GetContentRegionAvail().X;
            if (openTabs.Any())
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
                if (openTabs[4]) ExportPanel();
                ImGui.EndChild();
                ImGui.NextColumn();
            }
            TabButtons();
            ImGui.BeginChild("##main");
            var warnings = GetBrokenTextures();
            if (warnings.Length > 0)
            {
                if (ImGuiExt.ToggleButton(Icons.Warning.ToString(), showWarnings)) showWarnings = !showWarnings;
                ImGui.SameLine();
            }
            Controls.DropdownButton("View Mode", ref viewMode, ViewModes);
            ImGui.SameLine();
            using (var ct = Toolbar.Begin("#controls", true)) {
                ct.CheckItem("Background", ref doBackground);
                if (!(drawable is SphFile))
                    ct.CheckItem("Grid", ref showGrid);
                if(hasVWire)
                    ct.CheckItem("VMeshWire",  ref drawVMeshWire);
                ct.CheckItem("Bounds", ref doBounds);
                ct.CheckItem("Normals", ref drawNormals);
            }
            DoViewport();
            //
            var camModes = (cameraPart != null) ? camModesCockpit : camModesNormal;
            Controls.DropdownButton("Camera Mode", ref selectedCam, camModes);
            modelViewport.Mode = (CameraModes) (camModes[selectedCam].Tag);
            ImGui.SameLine();
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
                    ImGui.SliderFloat("Distance", ref levelDistance, 0, maxDistance, "%f");
                }
                else
                {
                    ImGui.Combo("Level", ref level, levels, levels.Length);
                }
                ImGui.PopItemWidth();
            }
            ImGui.EndChild();

            if (drawNormals)
            {
                ImGui.SetNextWindowPos(new Vector2(_window.Width - 135 * ImGuiHelper.Scale, _window.Height - 135 * ImGuiHelper.Scale));
                ImGui.Begin("normalScale#" + Unique, ImGuiWindowFlags.AlwaysAutoResize |
                                                     ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove);
                ImGui.Text("Normal Scale");
                ImGui.PushItemWidth(90 * ImGuiHelper.Scale);
                ImGui.SliderFloat("##normalScale", ref normalLength, 0.25f, 3f);
                ImGui.PopItemWidth();
                ImGui.End();
            }

            if (warnings.Length > 0 && showWarnings)
            {
                ImGui.SetNextWindowSize(new Vector2(500,220) * ImGuiHelper.Scale, ImGuiCond.FirstUseEver);
                ImGui.Begin("Warnings##" + Unique, ref showWarnings);
                bool first = true;
                foreach (var w in warnings)
                {
                    if(!first) ImGui.Separator();
                    if (w.Width != w.Height)
                    {
                        ImGui.TextUnformatted($"{w.Name}: Dimensions are not square ({w.Width} != {w.Height})");
                    }
                    if (!MathHelper.IsPowerOfTwo(w.Width) ||
                        !MathHelper.IsPowerOfTwo(w.Height))
                    {
                        ImGui.TextUnformatted($"{w.Name}: Dimensions are not powers of two ({w.Width}x{w.Height})");
                    }
                    first = false;
                }
                ImGui.End();
            }

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

        TextureReference[] GetBrokenTextures()
        {
            var missing = new List<MissingReference>();
            var matrefs = new List<uint>();
            var texrefs = new List<TextureReference>();
            DetectResources(missing, matrefs, texrefs);
            return texrefs.Where(x => x.Found && (
                (x.Width != x.Height) ||
                !MathHelper.IsPowerOfTwo(x.Width) ||
                !MathHelper.IsPowerOfTwo(x.Height)
                )).ToArray();
        }

        class HardpointGizmo
        {
            public Hardpoint Hardpoint;
            public RigidModelPart Parent;
            public bool Enabled;
            public Matrix4x4? Override = null;
            public float EditingMin;
            public float EditingMax;
            public HardpointGizmo(Hardpoint hp, RigidModelPart parent)
            {
                Hardpoint = hp;
                Parent = parent;
                Enabled = false;
            }

            public override string ToString()
            {
                return Hardpoint?.Name ?? "null hp";
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
            var icon = Icons.Cube_LightYellow;
            if (cn.Construct is PrisConstruct)
            {
                icon = Icons.Con_Pris;
            }
            if (cn.Construct is SphereConstruct)
            {
                icon = Icons.Con_Sph;
            }
            if (cn.Construct is RevConstruct)
            {
                icon = Icons.Rev_LightCoral;
            }
            bool mdlVisible = cn.Active;
            if (!mdlVisible)
            {
                var disabledColor = ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled];
                ImGui.PushStyleColor(ImGuiCol.Text, disabledColor);
            }
            if (Theme.IconTreeNode(icon, n, tflags))
            {
                if (!mdlVisible) ImGui.PopStyleColor();
                if (ImGui.IsItemClicked(0))
                    selectedNode = cn;
                ConstructContext(cn, mdlVisible);
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
            }
        }

        void ConstructContext(RigidModelPart con, bool mdlVisible)
        {
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                ImGui.OpenPopup(con.Construct.ChildName + "_context");
            if(ImGui.BeginPopupContextItem(con.Construct.ChildName + "_context")) {
                if (con.Mesh != null)
                {
                    //Visibility of model (this is bad)
                    bool visibleVar = mdlVisible;
                    Theme.IconMenuToggle(Icons.Eye, "Visible", ref visibleVar, true);
                    if(visibleVar != mdlVisible)
                    {
                        con.Active = visibleVar;
                    }
                }
                if (Theme.BeginIconMenu(Icons.Exchange, "Change To")) {
                    var cmp = (CmpFile)drawable;
                    if(!(con.Construct is FixConstruct) && Theme.IconMenuItem(Icons.Cube_LightYellow, "Fix",true)) {
                        var fix = new FixConstruct()
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
                    if(!(con.Construct is RevConstruct) && Theme.IconMenuItem(Icons.Rev_LightCoral, "Rev",true)) {
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
                    if(!(con.Construct is PrisConstruct) && Theme.IconMenuItem(Icons.Con_Pris, "Pris",true)) {
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
                    if(!(con.Construct is SphereConstruct) && Theme.IconMenuItem(Icons.Con_Sph, "Sphere",true)) {
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
                if(Theme.IconMenuItem(Icons.Edit, "Edit", true))
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
            bool open = Theme.IconTreeNode(Icons.Hardpoints, "Hardpoints");
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

                    if (gz == null)
                    {
                        throw new Exception("gizmo for hp not exist");
                    }
                    if (hp.Definition is RevoluteHardpointDefinition)
                    {
                        ImGui.Text(Icons.Rev_LightSeaGreen.ToString());
                    }
                    else
                    {
                        ImGui.Text(Icons.Cube_Purple.ToString());
                    }
                    ImGui.SameLine();
                    Controls.VisibleButton(hp.Name, ref gz.Enabled);
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
                        newHp.Definition.Orientation *= new Matrix4x4(
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
                        newHp.Definition.Orientation *= new Matrix4x4(
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
                        newHp.Definition.Orientation *= new Matrix4x4(
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
            var retval = ContextActions.None;
            if(ImGui.IsItemClicked(ImGuiMouseButton.Right))
                ImGui.OpenPopup(n + "_HardpointContext");
            if(ImGui.BeginPopupContextItem(n + "_HardpointContext")) {
                if(Theme.BeginIconMenu(Icons.PlusCircle, "New")) {
                    if (Theme.IconMenuItem(Icons.Cube_Purple, "Fixed Hardpoint",true)) retval = ContextActions.NewFixed;
                    if (Theme.IconMenuItem(Icons.Rev_LightSeaGreen, "Revolute Hardpoint",true)) retval = ContextActions.NewRevolute;
                    ImGui.EndMenu();
                }
                ImGui.EndPopup();
            }
            return retval;
        }
        ContextActions EditDeleteHpMenu(string n)
        {
            if(ImGui.IsItemClicked(ImGuiMouseButton.Right))
                ImGui.OpenPopup(n + "_HardpointEditCtx");
            if(ImGui.BeginPopupContextItem(n + "_HardpointEditCtx")) {
                if(Theme.IconMenuItem(Icons.Edit, "Edit",true)) return ContextActions.Edit;
                if(Theme.IconMenuItem(Icons.TrashAlt, "Delete",true)) return ContextActions.Delete;
                if(Theme.BeginIconMenu(Icons.Clone, "Duplicate"))
                {
                    if (ImGui.MenuItem("Mirror X")) return ContextActions.MirrorX;
                    if (ImGui.MenuItem("Mirror Y")) return ContextActions.MirrorY;
                    if (ImGui.MenuItem("Mirror Z")) return ContextActions.MirrorZ;
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
        int GetLevel(float[] switch2)
        {
            if (useDistance)
            {
                if (switch2 == null) return 0;
                for (int i = 0; i < (switch2.Length - 1); i++)
                {
                    if (levelDistance <= switch2[i + 1])
                        return i;
                }
                return int.MaxValue;
            }
            return level;
        }

        string surname;
        bool surShowHull = true;
        bool surShowHps = true;
        SurFile surFile = null;

        void OpenSur()
        {
            FileDialog.Open((file) =>
            {
                surname = System.IO.Path.GetFileName(file);
#if !DEBUG
            try
            {
#endif
                using (var f = System.IO.File.OpenRead(file))
                {
                    surFile = SurFile.Read(f);
                }
#if !DEBUG
            }
            catch (Exception e)
            {
                FLLog.Error("Sur", e.Message + "\n" + e.StackTrace);
                surFile = null;
            }
#endif
                if (surFile != null) ProcessSur(surFile);
            }, AppFilters.SurFilters);
        }
        void HierarchyPanel()
        {
            if(!(drawable is DF.DfmFile) && !(drawable is SphFile))
            {
                //Sur
                if (ImGui.Button("Open Sur"))
                    OpenSur();
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
                var euler = selectedNode.Construct.Rotation.GetEulerDegrees();
                ImGui.Text(string.Format("Rotation: (Pitch {0:0.000}, Yaw {1:0.000}, Roll {2:0.000})", euler.X, euler.Y, euler.Z));
                ImGui.Separator();
            }

            if (!vmsModel.Root.Active)
            {
                var col = ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled];
                ImGui.PushStyleColor(ImGuiCol.Text, col);
            }
            if (Theme.IconTreeNode(Icons.Tree_DarkGreen, "Root", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (!vmsModel.Root.Active) ImGui.PopStyleColor();
                RootModelContext(vmsModel.Root.Active);
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
            }
        }

        void RootModelContext(bool rootVisible)
        {
            if (vmsModel.Root != null && ImGui.IsItemClicked(ImGuiMouseButton.Right))
                ImGui.OpenPopup(Unique + "_mdl_rootpopup");
            if (ImGui.BeginPopupContextItem(Unique + "_mdl_rootpopup"))
            {
                bool visibleVar = rootVisible;
                Theme.IconMenuToggle(Icons.Eye, "Visible", ref visibleVar, true);
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
                var popup = $"{sc.Key}$Popup{j}";
                if (ImGui.Button(ImGuiExt.IDWithExtra(sc.Key, j++)))
                {
                    animator.StartAnimation(sc.Key, false);
                }
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    ImGui.OpenPopup(popup);
                if (ImGui.BeginPopupContextItem(popup))
                {
                    if(ImGui.MenuItem("Copy Nickname")) _window.SetClipboardText(sc.Key);
                    ImGui.EndPopup();
                }
            }
            ImGui.Separator();
            if (ImGui.Button("Reset")) animator.ResetAnimations();
        }

        bool drawSkeleton = false;
        Anm.AnmFile anmFile;
        private string skeletonHighlight;
        void SkeletonPanel()
        {
            ImGui.Checkbox("Draw Skeleton", ref drawSkeleton);
            if(ImGui.Button("Open Anm")) {
                FileDialog.Open((file) => anmFile = new Anm.AnmFile(file));
            }

            ImGui.Separator();

            var dfm = (DF.DfmFile)drawable;
            skeletonHighlight = null;
            if (ImGui.CollapsingHeader("Bones", ImGuiTreeNodeFlags.CollapsingHeader))
            {
                foreach(var inst in skel.BodySkinning.Bones)
                {
                    if(ImGui.Selectable(inst.Key))
                        popups.OpenPopup(new DfmBoneInfo(inst.Value));
                    if(ImGui.IsItemHovered())
                        skeletonHighlight = inst.Key;
                }
            }
            if (ImGui.CollapsingHeader("Hardpoints", ImGuiTreeNodeFlags.DefaultOpen))
            {
                foreach (var hp in dfm.GetHardpoints())
                {
                    if (ImGui.Selectable(hp.Hp.Name))
                        popups.OpenPopup(new DfmHpInfo(hp));
                    if(ImGui.IsItemHovered())
                        skeletonHighlight = hp.Hp.Name;
                }
            }

            if(anmFile != null)
            {
                ImGui.Separator();
                foreach(var script in anmFile.Scripts)
                {
                    var popup = $"{script.Key}Popup";
                    if (ImGui.Button(script.Key)) skel.StartScript(script.Value, 0, 1, 0);
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                        ImGui.OpenPopup(popup);
                    if (ImGui.BeginPopupContextItem(popup))
                    {
                        if(ImGui.MenuItem("Copy Nickname")) _window.SetClipboardText(script.Key);
                        ImGui.EndPopup();
                    }
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
            if (ImGui.Button("Export PNG"))
            {
                if(imageWidth < 16 || imageHeight < 16)
                {
                    FLLog.Error("Export", "Image minimum size is 16x16");
                }
                else
                {
                    FileDialog.Save(RenderImage, pngFilters);
                }
            }

            if (ImGui.Button("Create Icon"))
            {
                if(imageWidth < 16 || imageHeight < 16) {
                    FLLog.Error("Export", "Image minimum size is 16x16");
                }
                else
                {
                    var tmpFile = Path.GetTempFileName();
                    RenderImage(tmpFile);
                    _window.Make3dbDlg.Open(tmpFile, parent.DocumentName, true);
                }
            }
        }

        private ModelExporterSettings exportSettings = new ModelExporterSettings();

        void Export(SimpleMesh.ModelSaveFormat fmt, FileDialogFilters filters)
        {
            if (drawable == null) return;
            FileDialog.Save(output =>
            {
                SimpleMesh.Model exported = null;
                if (drawable is ModelFile mdl) {
                    exported = ModelExporter.Export(mdl, surFile, exportSettings, res);
                } else if (drawable is CmpFile cmp) {
                    exported = ModelExporter.Export(cmp, surFile, exportSettings, res);
                }
                if (exported != null)
                {
                    using var os = File.Create(output);
                    exported.SaveTo(os, fmt);
                }
            }, filters);
        }
        void ExportPanel()
        {
            ImGui.Checkbox("Include Hardpoints", ref exportSettings.IncludeHardpoints);
            ImGui.Checkbox("Include LODs", ref exportSettings.IncludeLods);
            if (surFile == null) {
                ImGui.TextDisabled("Sur file not loaded");
                if(ImGui.Button("Open Sur")) OpenSur();
            }
            else {
                ImGui.Checkbox("Include Hulls", ref exportSettings.IncludeHulls);
            }
            if (ImGui.Button("Export GLTF 2.0"))
                Export(SimpleMesh.ModelSaveFormat.GLTF2, AppFilters.GltfFilter);
            if (ImGui.Button("Export Collada"))
                Export(SimpleMesh.ModelSaveFormat.Collada, AppFilters.ColladaFilter);
            if (ImGuiExt.Button("Export .blend", blenderEnabled))
            {
                FileDialog.Save(output =>
                {
                    SimpleMesh.Model exported = null;
                    if (drawable is ModelFile mdl) {
                        exported = ModelExporter.Export(mdl, surFile, exportSettings, res);
                    } else if (drawable is CmpFile cmp) {
                        exported = ModelExporter.Export(cmp, surFile, exportSettings, res);
                    }
                    if (exported != null)
                    {
                        var result = Blender.ExportBlenderFile(exported, output, _window.Config.BlenderPath);
                        _window.ResultMessages(result);
                    }
                }, AppFilters.BlenderFilter);
            }
        }

        public override void DetectResources(List<MissingReference> missing, List<uint> matrefs, List<TextureReference> texrefs)
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
            parent?.DereferenceDetached();
        }
    }
}
