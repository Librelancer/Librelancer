using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using ImGuiNET;
using LancerEdit.GameContent.Lookups;
using LancerEdit.GameContent.Popups;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.Data;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.Archetypes;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema;
using LibreLancer.Dialogs;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.ImUI;
using LibreLancer.Infocards;
using LibreLancer.Physics;
using LibreLancer.Render;
using LibreLancer.Render.Cameras;
using LibreLancer.Resources;
using LibreLancer.World;
using Archetype = LibreLancer.Data.GameData.Archetype;
using ModelRenderer = LibreLancer.Render.ModelRenderer;
using DataEncounter = LibreLancer.Data.Schema.Universe.Encounter;
using DataFactionSpawn = LibreLancer.Data.Schema.Universe.FactionSpawn;
using DataDensityRestriction = LibreLancer.Data.Schema.Universe.DensityRestriction;

namespace LancerEdit.GameContent;

public class SystemEditorTab : GameContentTab
{
    private static WorldMatrixBuffer matrixBuffer = new WorldMatrixBuffer();

    //public fields
    public SystemEditData SystemData;
    public GameWorld World;
    public StarSystem CurrentSystem;
    public GameDataContext Data;
    public SunImmediateRenderer SunPreview;
    public SystemRenderer Renderer => renderer;

    private MainWindow win;
    private SystemRenderer renderer;
    private Viewport3D viewport;
    private LookAtCamera camera;
    private SystemMap systemMap = new SystemMap();
    public SystemObjectList ObjectsList;
    public LightSourceList LightsList;
    public ZoneList ZoneList;
    private AsteroidFieldEdit openField;

    private bool mapOpen = false;
    private Vector3 arcballTarget = Vector3.Zero;
    private int cameraMode = 0;
    private static readonly DropdownOption[] camModes =
    {
        new DropdownOption("Arcball", Icons.Globe, CameraModes.Arcball),
        new DropdownOption("Walkthrough", Icons.StreetView, CameraModes.Walkthrough)
    };

    public PopupManager Popups = new PopupManager();

    public EditorUndoBuffer UndoBuffer = new EditorUndoBuffer();

    private VerticalTabLayout layout;

    public SystemEditorTab(GameDataContext gameData, MainWindow mw, StarSystem system)
    {
        Title = "System Editor";
        SaveStrategy = new StarSystemSaveStrategy(this);
        this.Data = gameData;
        viewport = new Viewport3D(mw);
        viewport.EnableMSAA = false; //MSAA handled by SystemRenderer
        viewport.DefaultOffset = new Vector3(0, 0, 4);
        viewport.ModelScale = 1000f;
        cameraMode = mw.Config.DefaultSysEditCameraMode;
        viewport.Mode = (CameraModes)camModes[cameraMode].Tag;
        viewport.Background = new Vector4(0.12f, 0.12f, 0.12f, 1f);
        viewport.ResetControls();
        viewport.DoubleClicked += ViewportOnDoubleClicked;
        viewport.Draw3D = DrawGL;
        viewport.ClearArea = false;
        camera = new LookAtCamera()
        {
            GameFOV = true,
            ZRange = new Vector2(3f, 10000000f)
        };

        SunPreview = new(gameData.Resources);

        //Extract nav_prettymap texture
        string navPrettyMap = gameData.GameData.Items.DataPath("INTERFACE/NEURONET/NAVMAP/NEWNAVMAP/nav_prettymap.3db");

        if (gameData.GameData.VFS.FileExists(navPrettyMap))
        {
            gameData.Resources.LoadResourceFile(navPrettyMap);
        }

        systemMap.CreateContext(gameData, mw);
        this.win = mw;
        ObjectsList = new SystemObjectList(mw);
        LightsList = new LightSourceList(this);
        ObjectsList.OnSelectionChanged += OnObjectSelectionChanged;
        LightsList.OnSelectionChanged += OnLightSelectionChanged;
        ObjectsList.OnDelete += DeleteObject;

        LoadSystem(system);

        layout = new VerticalTabLayout(DrawLeft, DrawRight, DrawMiddle);
        layout.TabsLeft.Add(new(Icons.VectorSquare, "Zones", 0));
        layout.TabsLeft.Add(new(Icons.Cube,"Objects", 1));
        layout.TabsLeft.Add(new(Icons.Lightbulb,"Lights", 2));
        layout.TabsLeft.Add(new(Icons.Globe, "System", 3));
    }

    public void ForceSelectObject(GameObject obj)
    {
        layout.ActiveLeftTab = 1;
        ObjectsList.SelectSingle(obj);
        ObjectsList.ScrollToSelection();
    }

    public void ForceSelectLight(LightSource lt)
    {
        layout.ActiveLeftTab = 2;
        LightsList.Selected = lt;
    }

    public void OnSaved() => win.OnSaved();

    void DrawLeft(int tag)
    {
        switch (tag)
        {
            case 0:
                ZonesPanel();
                break;
            case 1:
                ObjectsPanel();
                break;
            case 2:
                LightsPanel();
                break;
            case 3:
                SystemPanel();
                break;
        }
    }

    void DrawToolbar()
    {
        ImGuiExt.ButtonDivided("##viewmode", "3D", "2D", ref render3d);
        if (render3d)
        {
            ImGui.SameLine();
            if (ImGuiExt.DropdownButton($"{Icons.Eye} View"))
            {
                ImGui.OpenPopup("viewpanel");
            }
            if (ImGui.BeginPopup("viewpanel"))
            {
                ViewPanel();
                ImGui.EndPopup();
            }
        }
        ImGui.SameLine();
        using (var tb = Toolbar.Begin("##toolbar", true))
        {
            if (render3d)
            {
                tb.DropdownButtonItem("Camera Mode", ref cameraMode, camModes);
            }
            else
            {
                tb.FloatSliderItem("Zoom", ref map2D.Zoom, 1, 10, "%.2fx");
            }
            tb.ToggleButtonItem("Map", ref mapOpen);
            tb.ToggleButtonItem("History", ref historyOpen);
            if (render3d)
            {
                tb.ToggleButtonItem("Camera Info", ref cameraOpen);
                tb.ToggleButtonItem("Zones", ref zonePosOpen);
            }
        }
    }
    void DrawMiddle()
    {
        if (render3d)
        {
            viewport.Mode = (CameraModes)camModes[cameraMode].Tag;
            ImGuiHelper.AnimatingElement();
            renderer.BackgroundOverride = SystemData.SpaceColor;
            renderer.SystemLighting.Ambient = new Color4(SystemData.Ambient, 1);
            renderer.SystemLighting.Lights = new List<DynamicLight>(LightsList.Sources.Count);
            for (int i = 0; i < LightsList.Sources.Count; i++)
            {
                renderer.SystemLighting.Lights.Add(new() { Active = LightsList.Visible[i], Light = LightsList.Sources[i].Light });
            }

            var cpos = ImGui.GetCursorPos();
            viewport.Draw();
            if (ManipulateObjects() || ManipulateZone() || ManipulateLight())
            {
                viewport.SetInputsEnabled(false);
            }
            else
            {
                viewport.SetInputsEnabled(true);
            }
            ImGui.SetCursorPos(cpos + ImGui.GetStyle().FramePadding);
            DrawToolbar();
        }
        else
        {
            DrawToolbar();
            map2D.Draw(SystemData, World, Data, this, win.RenderContext);
        }
        gizmoPreviews = [];
    }

    void DrawGL(int width, int height)
    {
        win.RenderContext.Wireframe = drawWireframe;
        renderer.Draw(width, height);
        win.RenderContext.Wireframe = false;
    }

    private void ViewportOnDoubleClicked(Vector2 pos)
    {
        if (layout.ActiveLeftTab == 1)
        {
            var sel = World.GetSelection(camera, null, pos.X, pos.Y, viewport.ControlWidth, viewport.ControlHeight);
            if (ShouldAddSecondary())
            {
                if (sel != null && !ObjectsList.Selection.Contains(sel))
                    ObjectsList.Selection.Add(sel);
            }
            else
            {
                ObjectsList.SelectSingle(sel);
                ObjectsList.ScrollToSelection();
            }
        }
        else if (layout.ActiveLeftTab == 2)
        {
            var cameraProjection = camera.Projection;
            var cameraView = camera.View;
            var vp = new Vector2(viewport.ControlWidth, viewport.ControlHeight);
            var start = Vector3Ex.UnProject(new Vector3(pos.X, pos.Y, 0f), cameraProjection, cameraView, vp);
            var end = Vector3Ex.UnProject(new Vector3(pos.X, pos.Y, 1f), cameraProjection, cameraView, vp);
            var dir = (end - start).Normalized() * 50000;
            var dist = float.MaxValue;
            LightSource r = null;
            foreach (var lt in LightsList.Sources)
            {
                var bb = DisplayMesh.Lightbulb.Bounds;
                bb.Min += lt.Light.Position;
                bb.Max += lt.Light.Position;
                var d = Vector3.DistanceSquared(lt.Light.Position, camera.Position);
                if (d < dist && bb.RayIntersect(ref start, ref dir))
                {
                    r = lt;
                    dist = d;
                }
            }

            LightsList.Selected = r;
        }
    }

    void LoadSystem(StarSystem system)
    {
        ObjectsList.SelectSingle(null);
        if (World != null)
        {
            World.Renderer.Dispose();
            World.Dispose();
        }

        //Load system
        renderer = new SystemRenderer(camera, Data.Resources, win);
        World = new GameWorld(renderer, Data.Resources, null, true);
        CurrentSystem = system;
        Data.GameData.LoadAllSystem(CurrentSystem);
        World.LoadSystem(CurrentSystem, Data.Resources, null, false, false);
        World.Renderer.LoadLights(CurrentSystem);
        World.Renderer.LoadStarspheres(CurrentSystem);
        systemMap.SetObjects(CurrentSystem);
        renderer.PhysicsHook = RenderEditorObjects;
        renderer.OpaqueHook = RenderOpaque;
        SystemData = new SystemEditData(CurrentSystem);
        //Setup UI
        ZoneList = new ZoneList();
        ZoneList.SetZones(CurrentSystem.Zones, CurrentSystem.AsteroidFields, CurrentSystem.Nebulae);
        World.Renderer.LoadZones(ZoneList.AsteroidFields.Fields, ZoneList.Nebulae);
        ObjectsList.SetObjects(World);
        LightsList.SetLights(CurrentSystem.LightSources);
    }

    public void ReloadFieldRenderers()
    {
        World.Renderer.LoadZones(ZoneList.AsteroidFields.Fields, ZoneList.Nebulae);
    }

    private bool renderGrid = false;

    void RenderGrid()
    {
        if (!renderGrid)
        {
            return;
        }

        var distance = Math.Abs(viewport.CameraOffset.Y);
        GridRender.Draw(win.RenderContext, camera, GridRender.DistanceScale(distance), win.Config.GridColor, camera.ZRange.X,
            camera.ZRange.Y);
    }

    void RenderLights()
    {
        win.RenderContext.Cull = true;
        win.RenderContext.BlendMode = BlendMode.Opaque;
        var mats = new LibreLancer.Utf.Mat.Material[DisplayMesh.Lightbulb.Drawcalls.Length];
        for (int i = 0; i < mats.Length; i++)
        {
            mats[i] = new(win.Resources);
            mats[i].Name = "FLAT PART MATERIAL";
            mats[i].DtName = ResourceManager.WhiteTextureName;
            mats[i].Dc = DisplayMesh.Lightbulb.Drawcalls[i].Color;
            mats[i].Initialize(win.Resources);
        }

        foreach (var light in LightsList.Sources)
        {
            var w = Matrix4x4.CreateTranslation(light.Light.Position);
            var mb = matrixBuffer.SubmitMatrix(ref w);
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i].Render.World = mb;
                mats[i].Render.Use(win.RenderContext, new VertexPositionNormal(), ref Lighting.Empty, 0);
                var dc = DisplayMesh.Lightbulb.Drawcalls[i];
                DisplayMesh.Lightbulb.VertexBuffer.Draw(PrimitiveTypes.TriangleList, dc.BaseVertex, dc.Start, dc.Count);
            }
        }

        matrixBuffer.Reset();
    }

    private void RenderOpaque()
    {
        RenderLights();
        RenderGrid();
    }

    private bool showZones = false;
    private string hoveredZone = null;

    void ZonesPanel()
    {
        PanelWithProperties("##zones", () =>
        {
            if (ImGui.Button("New Zone"))
            {
                Popups.OpenPopup(new NameInputPopup(NameInputConfig.Nickname("New Zone", ZoneList.ZoneExists), "", n =>
                {
                    var rot = Matrix4x4.CreateRotationX(viewport.CameraRotation.Y) *
                              Matrix4x4.CreateRotationY(viewport.CameraRotation.X);
                    var dir = Vector3.Transform(-Vector3.UnitZ, rot);
                    var to = viewport.CameraOffset + (dir * 50);
                    var c = new SysZoneCreate(this, n, to);
                    UndoBuffer.Commit(c);
                    ZoneList.Selected = c.Zone;
                }));
            }
            ImGui.Separator();
            if (ImGui.Button("Show All"))
                ZoneList.ShowAll();
            ImGui.SameLine();
            if (ImGui.Button("Hide All"))
                ZoneList.HideAll();
            ZoneList.Draw();
        }, ZoneProperties);
    }

    void InfocardRow(int idsInfo, Action<int> change)
    {
        //Infocard
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Infocard");
        ImGui.TableNextColumn();
        Controls.TruncText(InfocardPreview(idsInfo), 20);
        ImGui.TableNextColumn();
        if (ImGui.Button($"{Icons.Edit}##infocard"))
        {
            Popups.OpenPopup(new InfocardSelection(idsInfo, win, Data.Infocards, Data.Fonts, change));
        }
    }

    void ZoneProperties()
    {
        if (ZoneList.Selected == null)
        {
            ImGui.Text("No Zone Selected");
            return;
        }

        var sel = ZoneList.Selected.Current;

        if (ImGui.Button("Get Ini"))
        {
            var ib = new IniBuilder();
            IniSerializer.SerializeZone(sel, ib);
            IniWindow(ib, "zone.txt");
        }

        if (!Controls.BeginPropertyTable("properties", true, false, true))
            return;
        Controls.PropertyRow("Nickname", sel.Nickname);
        if (ImGui.Button($"{Icons.Edit}##nickname"))
        {
            Popups.OpenPopup(new NameInputPopup(
                NameInputConfig.Nickname("Rename", x => ZoneList.HasZone(x)),
                sel.Nickname,
                x => UndoBuffer.Commit(new SysZoneSetNickname(sel, this, sel.Nickname, x))
            ));
        }

        Controls.PropertyRow("Name", Data.Infocards.GetStringResource(sel.IdsName));
        if (ImGui.Button($"{Icons.Edit}##name"))
        {
            Popups.OpenPopup(new StringSelection(sel.IdsName, Data.Infocards,
                newIds =>
                {
                   UndoBuffer.Commit(new SysZoneSetIdsName(sel, this, sel.IdsName, newIds));
                }));
        }

        //Infocard
        InfocardRow(sel.IdsInfo, x => UndoBuffer.Commit(new SysZoneSetIdsInfo(sel, this, sel.IdsInfo, x)));

        //Position
        Controls.PropertyRow("Position", $"{sel.Position.X:0.00}, {sel.Position.Y:0.00}, {sel.Position.Z: 0.00}");
        if (ImGui.Button($"{Icons.Edit}##position"))
        {
            var origPosition = sel.Position;
            Popups.OpenPopup(new Vector3Popup("Position", false, origPosition, (value, kind) =>
            {
                if (kind == SetActionKind.Commit)
                    UndoBuffer.Commit(new SysZoneSetPosition(sel, this, origPosition, value));
                else
                    sel.Position = value;
            }));
        }

        var rot = sel.RotationMatrix.GetEulerDegrees();
        Controls.PropertyRow("Rotation",
            $"{rot.X: 0.00}, {rot.Y:0.00}, {rot.Z: 0.00}");
        if (ImGui.Button($"{Icons.Edit}##rotation"))
        {
            var origMatrix = sel.RotationMatrix;
            var origAngles = sel.RotationAngles;
            Popups.OpenPopup(new Vector3Popup("Rotation", false, rot, (value, kind) =>
            {
                var mat = MathHelper.MatrixFromEulerDegrees(value);
                if (kind == SetActionKind.Commit)
                {
                    UndoBuffer.Commit(SysZoneSetRotation.Create(sel, this, mat));
                }
                else if (kind == SetActionKind.Revert)
                {
                    sel.RotationMatrix = origMatrix;
                    sel.RotationAngles = origAngles;
                }
                else
                {
                    sel.RotationMatrix = mat;
                    sel.RotationAngles = new Vector3(
                        MathHelper.DegreesToRadians(value.X),
                        MathHelper.DegreesToRadians(value.Y),
                        MathHelper.DegreesToRadians(value.Z));
                }
            }));
        }

        if (ImGui.Button("0##rot"))
        {
            UndoBuffer.Commit(SysZoneSetRotation.Create(sel, this, Matrix4x4.Identity));
        }

        ShapeProperties(sel);
        Controls.EndPropertyTable();

        //Special
        var ast = ZoneList.AsteroidFields.Fields.FirstOrDefault(x => x.Zone == sel);
        if (ast != null && ImGui.Button("Edit Asteroids"))
        {
            closeField = false;
            openField = new AsteroidFieldEdit(ast, win, this);
        }

        //Comment
        Controls.BeginPropertyTable("comment", true, false, true);
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Comment");
        ImGui.TableNextColumn();
        Controls.TruncText(sel.Comment, 20);
        ImGui.TableNextColumn();
        if (ImGui.Button($"{Icons.Edit}##comment"))
            Popups.OpenPopup(new CommentPopup(sel.Comment,
                x => UndoBuffer.Commit(new SysZoneSetComment(sel, this, sel.Comment, x))));
        Controls.EndPropertyTable();
    }

    private bool closeField = false;
    internal void AsteroidFieldClose()
    {
        closeField = true;
    }

    void ShapeChangeButton(Zone z)
    {
        if (ImGui.Button($"{Icons.Edit}##shape"))
            ImGui.OpenPopup("shapeitem");
        bool changed = false;
        if (ImGui.BeginPopup("shapeitem"))
        {
            if (ImGui.MenuItem("Sphere", (z.Shape != ShapeKind.Sphere)))
            {
                z.ChangeShape(ShapeKind.Sphere, this);
            }
            if (ImGui.MenuItem("Ellipsoid", (z.Shape != ShapeKind.Ellipsoid)))
            {
                z.ChangeShape(ShapeKind.Ellipsoid, this);
            }
            if (ImGui.MenuItem("Box", (z.Shape != ShapeKind.Box)))
            {
               z.ChangeShape(ShapeKind.Box, this);
            }
            if (ImGui.MenuItem("Cylinder", (z.Shape != ShapeKind.Cylinder)))
            {
                z.ChangeShape(ShapeKind.Cylinder, this);
            }
            if (ImGui.MenuItem("Ring", (z.Shape != ShapeKind.Ring)))
            {
                z.ChangeShape(ShapeKind.Ring, this);
            }
            ImGui.EndPopup();
        }
    }

    void FloatRow(string name, float size, Action<float> preview, Action<float,float> set)
    {
        Controls.PropertyRow(name, size.ToString("0.####"));
        if (ImGui.Button($"{Icons.Edit}##{name}"))
        {
            Popups.OpenPopup(new FloatPopup(name, size, (old, updated) => set(old,updated), v =>
            {
                preview(v);
            }, 1f));
        }
    }

    private static Dictionary<ShapeKind, string[]> shapeLabels = new()
    {
        { ShapeKind.Sphere, new []{ "Radius", null, null }},
        { ShapeKind.Box, new[] {"Size X", "Size Y", "Size Z"}},
        { ShapeKind.Ellipsoid, new[] { "Size X", "Size Y", "Size Z"}},
        { ShapeKind.Cylinder, new[] { "Radius", "Height", null }},
        { ShapeKind.Ring, new[] { "Inner Radius", "Height", "Outer Radius" }}
    };

    void ShapeProperties(Zone zone)
    {
        Controls.PropertyRow("Shape", zone.Shape.ToString());
        ShapeChangeButton(zone);
        var labels = shapeLabels[zone.Shape];
        if (!string.IsNullOrWhiteSpace(labels[0]))
        {
            FloatRow(labels[0], zone.Size.X,
                e => zone.Size = zone.Size with { X = e },
                (o,u) => UndoBuffer.Commit(new SysZoneSetSizeX(zone, this, o,u )));
        }
        if (!string.IsNullOrWhiteSpace(labels[1]))
        {
            FloatRow(labels[1], zone.Size.Y,
                e => zone.Size = zone.Size with { Y = e },
                (o,u) => UndoBuffer.Commit(new SysZoneSetSizeY(zone, this, o,u )));
        }
        if (!string.IsNullOrWhiteSpace(labels[2]))
        {
            FloatRow(labels[2], zone.Size.Z,
                e => zone.Size = zone.Size with { Z = e },
                (o,u) => UndoBuffer.Commit(new SysZoneSetSizeZ(zone, this, o,u )));
        }
    }


    private bool drawWireframe = false;
    void ViewPanel()
    {
        if (!ImGui.BeginTable("##opts", 2))
            return;
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Checkbox("Nebulae", ref renderer.DrawNebulae);
        ImGui.Checkbox("Starspheres", ref renderer.DrawStarsphere);
        ImGui.BeginDisabled(!win.RenderContext.SupportsWireframe);
        ImGui.Checkbox("Wireframe", ref drawWireframe);
        ImGui.EndDisabled();
        ImGui.TableNextColumn();
        ImGui.Checkbox("Grid", ref renderGrid);
        ImGui.EndTable();
    }

    private void OnObjectSelectionChanged(GameObject obj)
    {
        arcballTarget = obj?.LocalTransform.Position ?? Vector3.Zero;

        var r = (obj!.RenderComponent as ModelRenderer)?.Model?.GetRadius() ?? 10f;
        viewport.CameraOffset = obj.LocalTransform.Position + new Vector3(0, 0, -r * 3.5f);
        viewport.CameraRotation = new Vector2(-MathF.PI, 0);
    }

    private void OnLightSelectionChanged(Vector3 pos)
    {
        viewport.CameraOffset = pos - new Vector3(0, 0, 12f);
        viewport.CameraRotation = new Vector2(-MathF.PI, 0);
    }


    public void SetArchetypeLoadout(GameObject obj, Archetype archetype, ObjectLoadout loadout, Sun star)
    {
        var ed = obj.GetEditData();
        ed.Archetype = archetype;
        ed.Loadout = loadout;
        ed.Star = star;
        var tr = obj.LocalTransform;
        World.InitObject(obj, true, obj.SystemObject, Data.Resources, null, false, true, ed.Loadout, ed.Archetype, (OptionalArgument<Sun>)ed.Star);
        obj.AddComponent(ed);
        obj.SetLocalTransform(tr);
    }

    void FactionRow(string name, Faction f, Action<Faction> onSet)
    {
        Controls.PropertyRow(name,
            f == null ? "(none)" : $"{f.Nickname} ({Data.Infocards.GetStringResource(f.IdsName)})");
        if (ImGui.Button($"{Icons.Edit}##{name}"))
            Popups.OpenPopup(new FactionSelection(onSet, name, f, Data));
    }

    void BaseRow(string name, Base f, Action<Base> onSet, string message = null)
    {
        Controls.PropertyRow(name,
            f == null ? "(none)" : $"{f.Nickname} ({Data.Infocards.GetStringResource(f.IdsName)})");
        if (ImGui.Button($"{Icons.Edit}##{name}"))
            Popups.OpenPopup(new BaseSelection(onSet, name, message, f, Data));
    }

    string DockDescription(DockAction a)
    {
        if (a == null) return "(none)";
        var sb = new StringBuilder();
        sb.AppendLine(a.Kind.ToString());
        if (a.Kind == DockKinds.Jump)
        {
            var sys = Data.GameData.Items.Systems.Get(a.Target);
            var sname = sys == null ? "INVALID" : Data.Infocards.GetStringResource(sys.IdsName);
            sb.AppendLine($"{a.Target} ({sname})");
            sb.AppendLine($"{a.Exit}");
        }

        if (a.Kind == DockKinds.Base)
        {
            var b = Data.GameData.Items.Bases.Get(a.Target);
            var bname = b == null ? "INVALID" : Data.Infocards.GetStringResource(b.IdsName);
            sb.AppendLine($"{a.Target} ({bname})");
        }

        return sb.ToString();
    }

    void DockRow(DockAction act, Archetype a, Action<DockAction> onSet)
    {
        Controls.PropertyRow("Dock", DockDescription(act));
        if (ImGui.Button($"{Icons.Edit}##dock"))
        {
            Popups.OpenPopup(
                new DockActionSelection(
                    onSet, act, a,
                    ObjectsList.Objects.Select(x => x.SystemObject.Nickname).ToArray(),
                    Data
                )
            );
        }
    }

    private string lastPreview = null;
    private int lastPreviewId = -1;
    string InfocardPreview(int id)
    {
        if (id <= 0)
        {
            return "";
        }
        if (id != lastPreviewId)
        {
            lastPreviewId = id;
            var infocard = Data.Infocards.GetXmlResource(id);
            var parsed = RDLParse.Parse(infocard, Data.Fonts);
            lastPreview = parsed.ExtractText();
        }

        return lastPreview;
    }

    void ObjectProperties(GameObject sel)
    {
        var ed = sel.GetEditData(false);
        var gc = sel.Content();
        if (ImGui.Button("Get Ini"))
        {
            SystemObject obj;
            if (sel.TryGetComponent<ObjectEditData>(out var oed)) {
                obj = oed.MakeCopy().SystemObject;
            } else {
                obj = sel.SystemObject;
            }
            var ib = new IniBuilder();
            IniSerializer.SerializeSystemObject(obj, ib);
            IniWindow(ib, "object.txt");
        }

        if (!Controls.BeginPropertyTable("properties", true, false, true))
            return;
        Controls.PropertyRow("Nickname", sel.Nickname);
        if (ImGui.Button($"{Icons.Edit}##nickname"))
        {
            Popups.OpenPopup(new NameInputPopup(NameInputConfig.Nickname("Rename", n => World.GetObject(n) != null),
                sel.Nickname, x => UndoBuffer.Commit(new ObjectSetNickname(sel, ObjectsList, sel.Nickname, x))));
        }

        Controls.PropertyRow("Name", ed == null
            ? sel.Name.GetName(Data.GameData, camera.Position)
            : ed.GetName(Data.GameData, camera.Position));
        if (ImGui.Button($"{Icons.Edit}##name"))
        {
            var oldName = ed?.IdsName ?? sel.SystemObject.IdsName;
            Popups.OpenPopup(new StringSelection(oldName, Data.Infocards,
                newIds => UndoBuffer.Commit(new ObjectSetIdsName(sel, ObjectsList, oldName, newIds))));
        }
        //Infocard
        InfocardRow(gc.IdsInfo, x => UndoBuffer.Commit(new ObjectSetIdsInfo(sel, ObjectsList, gc.IdsInfo, x)));
        //Position
        var pos = sel.LocalTransform.Position;
        var rot = sel.LocalTransform.GetEulerDegrees();
        Controls.PropertyRow("Position", $"{pos.X:0.00}, {pos.Y:0.00}, {pos.Z: 0.00}");
        if (ImGui.Button($"{Icons.Edit}##position"))
        {
            var oldTr = sel.LocalTransform;
            Popups.OpenPopup(new Vector3Popup("Position", false, oldTr.Position, (value, kind) =>
            {
                if (kind == SetActionKind.Commit)
                {
                    UndoBuffer.Commit(new ObjectSetTransform(sel, ObjectsList, oldTr, oldTr with { Position = value }));
                }
                else if (kind == SetActionKind.Revert)
                {
                    sel.SetLocalTransform(oldTr);
                }
                else
                {
                    sel.SetLocalTransform(oldTr with { Position = value });
                }
            }));
        }
        Controls.PropertyRow("Rotation", $"{rot.X: 0.00}, {rot.Y:0.00}, {rot.Z: 0.00}");
        if (ImGui.Button($"{Icons.Edit}##rotation"))
        {
            var oldTr = sel.LocalTransform;
            var angles = oldTr.Orientation.GetEulerDegrees();
            Popups.OpenPopup(new Vector3Popup("Position", true, angles, (value, kind) =>
            {
                var newOrient = MathHelper.QuatFromEulerDegrees(value);
                if (kind == SetActionKind.Commit)
                {
                    UndoBuffer.Commit(new ObjectSetTransform(sel, ObjectsList, oldTr, oldTr with { Orientation = newOrient }));
                }
                else if (kind == SetActionKind.Revert)
                {
                    sel.SetLocalTransform(oldTr);
                }
                else
                {
                    sel.SetLocalTransform(oldTr with { Orientation = newOrient });
                }
            }));
        }

        var oldArchetype = ed?.Archetype ?? sel.SystemObject.Archetype;
        var oldLoadout = ed != null ? ed.Loadout : sel.SystemObject.Loadout;
        var oldStar = ed != null ? ed.Star : sel.SystemObject.Star;

        //Archetype
        Controls.PropertyRow("Archetype", gc.Archetype?.Nickname ?? "(none)");
        if (ImGui.Button($"{Icons.Edit}##archetype"))
        {
            Popups.OpenPopup(new ArchetypeSelection(
                x => UndoBuffer.Commit(new ObjectSetArchetypeLoadoutStar(
                    sel, this, oldArchetype, oldLoadout,oldStar, x, null, null)),
                oldArchetype,
                Data));
        }
        //Star
        Controls.PropertyRow("Star", gc.Star?.Nickname ?? "(none)");
        if (ImGui.Button($"{Icons.Edit}##star"))
        {
            Popups.OpenPopup(new StarSelection(
                x => UndoBuffer.Commit(new ObjectSetArchetypeLoadoutStar(
                    sel, this, oldArchetype, oldLoadout,oldStar, oldArchetype, oldLoadout, x)),
                oldStar,
                SunPreview, Data, win.RenderContext));
        }

        //Loadout
        Controls.PropertyRow("Loadout", gc.Loadout?.Nickname ?? "(none)");
        if (ImGui.Button($"{Icons.Edit}##loadout"))
        {
            Popups.OpenPopup(new LoadoutSelection(
                x => UndoBuffer.Commit(new ObjectSetArchetypeLoadoutStar(
                    sel, this, oldArchetype, oldLoadout, oldStar, oldArchetype, x, oldStar)),
                oldLoadout,
                sel.GetHardpoints().Select(x => x.Name).ToArray(),
                Data));
        }

        Controls.PropertyRow("Parent", gc.ParentObject ?? "(none)");
        if (ImGui.Button($"{Icons.Edit}##parent"))
        {
            Popups.OpenPopup(new ParentSelectPopup(
                ObjectsList.Objects.Where(x => x != sel),
                Data, World.GetObject(gc.ParentObject),
                x => UndoBuffer.Commit(new ObjectSetParent(sel, ObjectsList, gc.ParentObject, x?.Nickname))
                ));
        }

        //Visit
        Controls.PropertyRow("Visit", VisitFlagEditor.FlagsString(gc.Visit));
        if (ImGui.Button($"{Icons.Edit}##visit"))
            Popups.OpenPopup(
                new VisitFlagEditor(gc.Visit, x => UndoBuffer.Commit(new ObjectSetVisit(sel, ObjectsList, gc.Visit, x))));
        FactionRow("Reputation", gc.Reputation, x => UndoBuffer.Commit(new ObjectSetReputation(sel, ObjectsList, gc.Reputation, x)));
        Controls.EndPropertyTable();
        if (!Controls.BeginPropertyTable("base and docking", true, false, true))
            return;
        BaseRow(
            "Base",
            gc.Base,
            x => UndoBuffer.Commit(new ObjectSetBase(sel, ObjectsList, gc.Base, x)),
            "This is the base entry used when linking infocards.\nDocking requires setting the dock action"
        );
        DockRow(gc.Dock, gc.Archetype, x => UndoBuffer.Commit(new ObjectSetDock(sel, ObjectsList, gc.Dock, x)));
        Controls.EndPropertyTable();

        //Comment
        if(!Controls.BeginPropertyTable("comment", true, false, true))
            return;
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Comment");
        ImGui.TableNextColumn();
        Controls.TruncText(gc.Comment, 20);
        ImGui.TableNextColumn();
        if (ImGui.Button($"{Icons.Edit}##comment"))
            Popups.OpenPopup(new CommentPopup(gc.Comment,
                x => UndoBuffer.Commit(new ObjectSetComment(sel, ObjectsList, gc.Comment, x))));
        Controls.EndPropertyTable();
    }

    public void CreateObject(string nickname, Archetype archetype, Vector3? pos = null)
    {
        var rot = Matrix4x4.CreateRotationX(viewport.CameraRotation.Y) *
                  Matrix4x4.CreateRotationY(viewport.CameraRotation.X);
        var dir = Vector3.Transform(-Vector3.UnitZ, rot);
        var to = viewport.CameraOffset + (dir * 50);
        var sysobj = new SystemObject()
        {
            Nickname = nickname,
            Archetype = archetype,
            Position = pos ?? to,
        };
        var obj = new SysCreateObject(this, sysobj);
        UndoBuffer.Commit(obj);
        ObjectsList.SelectSingle(obj.Object);
        ObjectsList.ScrollToSelection();
    }

    public void RefreshObjects()
    {
        ObjectsList.SetObjects(World);
    }

    public void OnRemoved(GameObject go)
    {
        if (ObjectsList.Selection.Contains(go))
            ObjectsList.Selection.Remove(go);
        ObjectsList.SetObjects(World);
    }

    void DeleteObject(GameObject go)
    {
        UndoBuffer.Commit(new SysDeleteObject(this, go));
    }

    bool ShouldAddSecondary() => ObjectsList.Selection.Count > 0 && (win.Keyboard.IsKeyDown(Keys.LeftShift) ||
                                                                     win.Keyboard.IsKeyDown(Keys.RightShift));

    private float h1 = 150, h2 = 200;

    private Action propertiesPanel;
    private int propertiesMode = 0;

    void PanelWithProperties(string id, Action panel, Action properties)
    {
        if (propertiesMode == 0)
        {
            var totalH = ImGui.GetWindowHeight();
            ImGuiExt.SplitterV(2f, ref h1, ref h2, 15 * ImGuiHelper.Scale, 60 * ImGuiHelper.Scale, -1);
            h1 = totalH - h2 - 24f * ImGuiHelper.Scale;
            ImGui.BeginChild(id, new Vector2(ImGui.GetWindowWidth(), h1));
        }
        else
        {
            ImGui.BeginChild(id);
        }

        panel();
        ImGui.EndChild();
        if (propertiesMode == 2)
        {
            ImGui.SetNextWindowSize(new Vector2(400, 300) * ImGuiHelper.Scale, ImGuiCond.FirstUseEver);
            if (ImGui.Begin("Properties"))
            {
                ImGuiExt.UseTitlebar(out var restoreX, out var restoreY);
                var titleWidth = ImGui.CalcTextSize("Properties").X;
                ImGui.SetCursorPos(new Vector2(titleWidth + 40 * ImGuiHelper.Scale, 0));
                if (ImGui.Button($"{Icons.ArrowLeft}")) {
                    propertiesMode = 0;
                }
                ImGui.SetItemTooltip("Dock Left");
                ImGui.SameLine();
                if (ImGui.Button($"{Icons.ArrowRight}")) {
                    propertiesMode = 1;
                }
                ImGui.SetItemTooltip("Dock Right");
                ImGui.PopClipRect();
                ImGui.SetCursorPos(new Vector2(restoreX, restoreY));
                properties();
            }
            ImGui.End();
        }
        else if (propertiesMode == 1)
        {
            propertiesPanel = properties;
        }
        else if (propertiesMode == 0)
        {
            ImGui.BeginChild("##properties", new Vector2(ImGui.GetWindowWidth(), h2));
            ImGui.Text("Properties");
            ImGui.SameLine();
            if (Controls.SmallButton($"{Icons.UpRightFromSquare}"))
                propertiesMode = 2;
            ImGui.Separator();
            properties();
            ImGui.EndChild();
        }
    }

    void DrawRight(int tag)
    {
        if (tag == 0)
        {
            if (Controls.SmallButton($"{Icons.UpRightFromSquare}"))
                propertiesMode = 2;
            ImGui.Separator();
            propertiesPanel?.Invoke();
            propertiesPanel = null;
        }
    }

    void ObjectsPanel()
    {
        PanelWithProperties("##objects", () =>
        {
            if (ImGui.Button("New Object"))
            {
                Popups.OpenPopup(new NewObjectPopup(Data, World, null, CreateObject));
            }

            ObjectsList.Draw();
        }, () =>
        {
            if (ObjectsList.Selection.Count == 1)
                ObjectProperties(ObjectsList.Selection[0]);
            else if (ObjectsList.Selection.Count > 0)
            {
                ImGui.Text("Multiple objects selected");
                bool canReset = false;
                foreach (var obj in ObjectsList.Selection)
                {
                    if (obj.GetEditData(false) != null)
                    {
                        canReset = true;
                        break;
                    }
                }

                if (ImGuiExt.Button("Reset All", canReset))
                {
                    for (int i = 0; i < ObjectsList.Selection.Count; i++)
                    {
                        var sel = ObjectsList.Selection[i];
                        sel.Unregister(World.Physics);
                        World.RemoveObject(sel);
                        sel = World.NewObject(sel.SystemObject, Data.Resources, null, false);
                        if (i == 0)
                            ObjectsList.SelectedTransform = sel.LocalTransform.Matrix();
                        ObjectsList.Selection[i] = sel;
                    }

                    ObjectsList.SetObjects(World);
                }
                if (ObjectsList.Selection.Count == 2)
                {
                    if (ImGui.Button("Join Objects"))
                    {
                        Popups.OpenPopup(new HardpointJoinPopup(ObjectsList.Selection[0], ObjectsList.Selection[1],
                            (hp, childHp, setParent) =>
                            {
                                var child = childHp == null
                                    ? Transform3D.Identity
                                    : childHp.Transform.Inverse();
                                var attachment = hp?.Transform ?? Transform3D.Identity;
                                var tr = child * attachment * ObjectsList.Selection[0].LocalTransform;
                                var setTr = new ObjectSetTransform(
                                    ObjectsList.Selection[1], ObjectsList, ObjectsList.Selection[1].LocalTransform,
                                    tr);
                                if (setParent)
                                {
                                    UndoBuffer.Commit(EditorAggregateAction.Create([
                                        new ObjectSetParent(ObjectsList.Selection[1], ObjectsList,
                                            ObjectsList.Selection[1].Content().ParentObject,
                                            ObjectsList.Selection[0].Nickname),
                                        setTr
                                    ]));
                                }
                                else
                                {
                                    UndoBuffer.Commit(setTr);
                                }
                            }, PreviewGizmos));
                    }
                }
            }
            else
                ImGui.Text("No Object Selected");
        });
    }

    private HpGizmoData[] gizmoPreviews = [];
    void PreviewGizmos(HpGizmoData[] previews)
    {
        gizmoPreviews = previews;
    }

    void UpdateAttenuation(LightSource light, string curveName, Vector3 attenuation)
    {
        if (curveName != null && light.Light.Kind != LightKind.PointAttenCurve)
        {
            UndoBuffer.Commit(EditorAggregateAction.Create([
                new SysLightSetKind(light, light.Light.Kind, LightKind.PointAttenCurve, LightsList),
                new SysLightSetAttenuation(light, light.AttenuationCurveName, light.Light.Attenuation, curveName, attenuation, LightsList)
            ]));
        }
        else if (curveName == null && light.Light.Kind == LightKind.PointAttenCurve)
        {
            UndoBuffer.Commit(EditorAggregateAction.Create([
                new SysLightSetKind(light, light.Light.Kind, LightKind.Point, LightsList),
                new SysLightSetAttenuation(light, light.AttenuationCurveName, light.Light.Attenuation, curveName, attenuation, LightsList)
            ]));
        }
        else
        {
            UndoBuffer.Commit(new SysLightSetAttenuation(light, light.AttenuationCurveName, light.Light.Attenuation, curveName, attenuation, LightsList));
        }
    }

    void UpdateLightKind(LightSource light, LightKind newKind)
    {
        if (light.Light.Kind == LightKind.PointAttenCurve) {
            UndoBuffer.Commit(EditorAggregateAction.Create(new EditorAction[]
            {
                new SysLightSetKind(light, light.Light.Kind, newKind, LightsList),
                new SysLightSetAttenuation(light, light.AttenuationCurveName, light.Light.Attenuation, null, new Vector3(1,0,0), LightsList)
            }));
        }
        else
        {
            UndoBuffer.Commit(new SysLightSetKind(light, light.Light.Kind, newKind, LightsList));
        }
    }

    void LightsPanel()
    {
        PanelWithProperties("##lights", () =>
        {
            if (ImGui.Button("New Light"))
            {
                Popups.OpenPopup(new NameInputPopup(NameInputConfig.Nickname("New Light", LightsList.HasLight), "", n =>
                {
                    var rot = Matrix4x4.CreateRotationX(viewport.CameraRotation.Y) *
                              Matrix4x4.CreateRotationY(viewport.CameraRotation.X);
                    var dir = Vector3.Transform(-Vector3.UnitZ, rot);
                    var to = viewport.CameraOffset + (dir * 50);
                    var lt = new LightSource();
                    lt.Nickname = n;
                    lt.Light.Attenuation = Vector3.UnitX;
                    lt.Light.Range = 5000;
                    lt.Light.Color = Color3f.White;
                    lt.Light.Position = to;
                    lt.Light.Kind = LightKind.Point;
                    UndoBuffer.Commit(new SysLightCreate(lt, this));
                }));
            }
            LightsList.Draw();
        }, () =>
        {
            if (LightsList.Selected == null)
            {
                ImGui.Text("No light selected");
                return;
            }
            var sel = LightsList.Selected;
            if (ImGui.Button("Get Ini"))
            {
                var ib = new IniBuilder();
                IniSerializer.SerializeLightSource(sel, ib);
                IniWindow(ib, "LightSource.txt");
            }
            if (!Controls.BeginPropertyTable("Props", true, false, true))
                return;
            Controls.PropertyRow("Nickname", sel.Nickname);
            if (ImGui.Button($"{Icons.Edit}##nickname"))
            {
                Popups.OpenPopup(new NameInputPopup(
                    NameInputConfig.Nickname("Rename", x => LightsList.HasLight(x)),
                    sel.Nickname,
                    x => UndoBuffer.Commit(new SysLightSetNickname(sel, sel.Nickname, x, LightsList))
                ));
            }
            var pos = sel.Light.Position;
            Controls.PropertyRow("Position", $"{pos.X:0.00}, {pos.Y:0.00}, {pos.Z: 0.00}");
            if (ImGui.Button($"{Icons.Edit}##position"))
            {
                var origPosition = sel.Light.Position;
                Popups.OpenPopup(new Vector3Popup("Position", false, origPosition, (value, kind) =>
                {
                    if (kind == SetActionKind.Commit)
                        UndoBuffer.Commit(new SysLightSetPosition(sel, origPosition, value, LightsList));
                    else
                        sel.Light.Position = value;
                }));
            }
            ColorProperty("Color", new Color4(sel.Light.Color, 1), x =>
                UndoBuffer.Commit(new SysLightSetColor(sel, sel.Light.Color, x.Rgb, LightsList)));
            Controls.PropertyRow("Type", sel.Light.Kind == LightKind.Directional ? "Directional" : "Point");
            if (sel.Light.Kind == LightKind.Directional)
            {
                var dir = sel.Light.Direction;
                Controls.PropertyRow("Direction", $"{dir.X:0.00000}, {dir.Y:0.0000}, {dir.Z:0.0000}");
            }
            Controls.PropertyRow("Range", $"{sel.Light.Range:0.00}");
            if (ImGui.Button($"{Icons.Edit}##range"))
            {
                Popups.OpenPopup(new FloatPopup("Range", sel.Light.Range,
                    (old, updated) => UndoBuffer.Commit(new SysLightSetRange(sel, old, updated, LightsList)),
                    v => sel.Light.Range = v, 1));
            }
            Vector3 atten3 = sel.Light.Attenuation;
            var attenPreview = string.IsNullOrWhiteSpace(sel.AttenuationCurveName)
                ? $"{atten3.X:0.00000000}, {atten3.Y:0.00000000}, {atten3.Z:0.00000000}"
                : sel.AttenuationCurveName;
            Controls.PropertyRow("Attenuation", attenPreview);
            if (ImGui.Button($"{Icons.Edit}##atten"))
            {
                FloatGraph graph = null;
                Vector3 attenuation = sel.Light.Attenuation;
                bool isGraph = sel.Light.Kind == LightKind.PointAttenCurve;
                bool canGraph = sel.Light.Kind == LightKind.PointAttenCurve || sel.Light.Kind == LightKind.Directional;
                if (sel.Light.Kind == LightKind.PointAttenCurve)
                {
                    graph = Data.GameData.Items.Ini.Graphs.FindFloatGraph(sel.AttenuationCurveName);
                }
                else
                {
                    attenuation = sel.Light.Attenuation;
                }
                Popups.OpenPopup(new AttenuationPopup(
                    graph,
                    attenuation,
                    isGraph,
                    canGraph,
                    sel.Light.Range,
                    (curve, atten) => UpdateAttenuation(sel, curve, atten),
                    Data.GameData));
            }

            Controls.EndPropertyTable();
        });
    }

    void IniWindow(IniBuilder ini, string name)
    {
        var os = new MemoryStream();
        IniWriter.WriteIni(os, ini.Sections);
        os.Position = 0;
        var sr = new StreamReader(os);
        win.TextWindows.Add(new TextDisplayWindow(sr.ReadToEnd(), name, win));
    }

    void MusicProp(string name, string arg, Action<string> onSet)
    {
        Controls.PropertyRow(name, arg ?? "(none)");
        if (Controls.Music(name, win, !string.IsNullOrEmpty(arg)))
            Data.Sounds.PlayMusic(arg, 0, true);
        ImGui.TableNextColumn();
        if (ImGui.Button($"{Icons.Edit}##{name}"))
            Popups.OpenPopup(new MusicSelection(onSet, name, arg, Data, win));
        ImGui.TableNextColumn();
        if (ImGuiExt.Button($"{Icons.TrashAlt}##{name}", !string.IsNullOrEmpty(arg)))
            onSet(null);
    }

    void StarsphereProp(string name, ResolvedModel arg, Action<ResolvedModel> onSet)
    {
        var modelName = arg?.ModelFile;
        if (modelName != null)
            modelName = Path.GetFileName(modelName);
        Controls.PropertyRow(name, modelName ?? "(none)");
        if (ImGui.Button($"{Icons.Edit}##{name}"))
        {
            Popups.OpenPopup(new VfsFileSelector(name, Data.GameData.VFS, Data.GameData.Items.Ini.Freelancer.DataPath, file =>
            {
                var modelFile = Data.GameData.Items.DataPath(file);
                var sourcePath = file.Replace('/', '\\');
                onSet(new ResolvedModel() { ModelFile = modelFile, SourcePath = sourcePath });
            }, VfsFileSelector.MakeFilter(".cmp")));
        }

        ImGui.TableNextColumn();
        if (ImGuiExt.Button($"{Icons.TrashAlt}##{name}", arg != null))
        {
            onSet(null);
        }
    }

    public void ReloadStarspheres()
    {
        var models = new List<RigidModel>();
        if (SystemData.StarsBasic != null)
        {
            var mdl = SystemData.StarsBasic.LoadFile(Data.Resources);
            if (mdl?.Drawable is IRigidModelFile rm)
                models.Add(rm.CreateRigidModel(true, Data.Resources));
        }

        if (SystemData.StarsComplex != null)
        {
            var mdl = SystemData.StarsComplex.LoadFile(Data.Resources);
            if (mdl?.Drawable is IRigidModelFile rm)
                models.Add(rm.CreateRigidModel(true, Data.Resources));
        }

        if (SystemData.StarsNebula != null)
        {
            var mdl = SystemData.StarsNebula.LoadFile(Data.Resources);
            if (mdl?.Drawable is IRigidModelFile rm)
                models.Add(rm.CreateRigidModel(true, Data.Resources));
        }

        renderer.StarSphereModels = models.ToArray();
    }

    void ColorProperty(string name, Color4 color, Action<Color4> onSet)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text(name);
        ImGui.TableNextColumn();
        ImGui.PushItemWidth(-1);
        if (ImGui.ColorButton(name, color, ImGuiColorEditFlags.NoAlpha,
                new Vector2(ImGui.CalcItemWidth(), ImGui.GetFrameHeight())))
        {
            Popups.OpenPopup(new ColorPicker(name, color, onSet));
        }

        ImGui.TableNextColumn();
        if (ImGui.Button($"{Icons.Edit}##{name}"))
            Popups.OpenPopup(new ColorPicker(name, color, onSet));
        ImGui.PopItemWidth();
    }

    void ColorProperty(string name, Color3f color, Action<Color3f> onSet)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text(name);
        ImGui.TableNextColumn();
        ImGui.PushItemWidth(-1);
        if (ImGui.ColorButton(name, new Color4(color, 1), ImGuiColorEditFlags.NoAlpha,
                new Vector2(ImGui.CalcItemWidth(), ImGui.GetFrameHeight())))
        {
            Popups.OpenPopup(new ColorPicker(name, color, onSet));
        }

        ImGui.TableNextColumn();
        if (ImGui.Button($"{Icons.Edit}##{name}"))
            Popups.OpenPopup(new ColorPicker(name, color, onSet));
        ImGui.PopItemWidth();
    }

    void SystemPanel()
    {
        if (!Controls.BeginPropertyTable("Props", true, false, true))
            return;
        Controls.PropertyRow("Name", Data.Infocards.GetStringResource(SystemData.IdsName));
        if (ImGui.Button($"{Icons.Edit}##name"))
        {
            Popups.OpenPopup(new StringSelection(SystemData.IdsName, Data.Infocards,
                newIds => UndoBuffer.Commit(new SysDataSetIdsName(SystemData, SystemData.IdsName, newIds))));
        }
        InfocardRow(SystemData.IdsInfo,
            x => UndoBuffer.Commit(new SysDataSetIdsInfo(SystemData, SystemData.IdsInfo, x)));
        ColorProperty("Space Color", SystemData.SpaceColor, x =>
            UndoBuffer.Commit(new SysDataSetSpaceColor(SystemData, SystemData.SpaceColor, x)));
        ColorProperty("Ambient Color", SystemData.Ambient, x =>
            UndoBuffer.Commit(new SysDataSetAmbient(SystemData, SystemData.Ambient, x)));
        Controls.EndPropertyTable();
        ImGui.Separator();
        ImGui.Text("Music");
        if (!Controls.BeginPropertyTable("Music", true, false, true, true, true))
            return;
        MusicProp("Space", SystemData.MusicSpace, x =>
            UndoBuffer.Commit(new SysDataSetMusic(SystemData, SystemData.MusicSpace, x, "Space")));
        MusicProp("Battle", SystemData.MusicBattle, x =>
            UndoBuffer.Commit(new SysDataSetMusic(SystemData, SystemData.MusicBattle, x, "Battle")));
        MusicProp("Danger", SystemData.MusicDanger, x =>
            UndoBuffer.Commit(new SysDataSetMusic(SystemData, SystemData.MusicDanger, x, "Danger")));
        Controls.EndPropertyTable();
        ImGui.Separator();
        ImGui.Text("Stars");
        if (!Controls.BeginPropertyTable("Stars", true, false, true, true))
            return;
        StarsphereProp("Layer 1", SystemData.StarsBasic, x =>
            UndoBuffer.Commit(new SysDataSetStars(SystemData, SystemData.StarsBasic, x, "Basic", this)));
        StarsphereProp("Layer 2", SystemData.StarsComplex, x =>
            UndoBuffer.Commit(new SysDataSetStars(SystemData, SystemData.StarsComplex, x, "Complex", this)));
        StarsphereProp("Layer 3", SystemData.StarsNebula, x =>
            UndoBuffer.Commit(new SysDataSetStars(SystemData, SystemData.StarsNebula, x, "Nebula", this)));
        Controls.EndPropertyTable();
    }

    private static readonly Color4[] zoneColors = new Color4[]
    {
        Color4.White,
        Color4.Teal,
        Color4.Coral,
        Color4.LimeGreen,
    };

    private void RenderEditorObjects()
    {
        ZoneRenderer.Begin(win.RenderContext, camera);
        foreach (var ez in ZoneList.Zones)
        {
            if (!ez.Visible &&
                ez != ZoneList.HoveredZone &&
                ez != ZoneList.Selected)
                continue;
            var z = ez.Current;
            var zoneColor = zoneColors[(int)ZoneList.GetZoneType(z.Nickname)]
                .ChangeAlpha(0.5f);
            bool inZone = z.ContainsPoint(camera.Position);
            switch (z.Shape)
            {
                case ShapeKind.Sphere:
                    ZoneRenderer.DrawSphere(z.Position, z.Size.X, z.RotationMatrix, zoneColor, inZone);
                    break;
                case ShapeKind.Ellipsoid:
                    ZoneRenderer.DrawEllipsoid(z.Position, z.Size, z.RotationMatrix, zoneColor, inZone);
                    break;
                case ShapeKind.Cylinder:
                    ZoneRenderer.DrawCylinder(z.Position, z.Size.X, z.Size.Y, z.RotationMatrix, zoneColor, inZone);
                    break;
                case ShapeKind.Ring:
                    ZoneRenderer.DrawRing(z.Position, z.Size.X, z.Size.Z, z.Size.Y, z.RotationMatrix,
                        zoneColor, inZone);
                    break;
                case ShapeKind.Box:
                    ZoneRenderer.DrawCube(z.Position, z.Size, z.RotationMatrix, zoneColor, inZone);
                    break;
            }
        }

        hoveredZone = null;
        ZoneRenderer.Finish(Data.Resources);

        foreach (var obj in ObjectsList.Selection)
        {
            var rc = obj.RenderComponent as ModelRenderer;
            if (rc == null) continue;
            var bbox = rc.Model.GetBoundingBox();
            EditorPrimitives.DrawBox(renderer.DebugRenderer, bbox, obj.LocalTransform.Matrix(), Color4.White);
        }

        if (LightsList.Selected != null && LightsMode)
        {
            var tr = Matrix4x4.CreateTranslation(LightsList.Selected.Light.Position);
            EditorPrimitives.DrawBox(renderer.DebugRenderer, DisplayMesh.Lightbulb.Bounds, tr, Color4.White);
            if (LightsList.Selected.Light.Kind != LightKind.Directional)
            {
                EditorPrimitives.DrawBox(renderer.DebugRenderer, new BoundingBox(
                    new Vector3(-LightsList.Selected.Light.Range),
                    new Vector3(LightsList.Selected.Light.Range)), tr, Color4.Yellow);
            }
        }

        foreach (var p in gizmoPreviews)
        {
            GizmoRender.AddGizmo(renderer.DebugRenderer, p.Scale, p.Transform.Matrix(), Color4.Yellow);
        }
    }

    public override void Update(double elapsed)
    {
        var rot = Matrix4x4.CreateRotationX(viewport.CameraRotation.Y) *
                  Matrix4x4.CreateRotationY(viewport.CameraRotation.X);
        var dir = Vector3.Transform(-Vector3.UnitZ, rot);
        var to = viewport.CameraOffset + (dir * 10);

        var from = viewport.CameraOffset;
        if (viewport.Mode == CameraModes.Arcball)
        {
            to = arcballTarget;
            from += to;
        }
        if ((from - to).LengthSquared() < 0.0001f)
        {
            from -= dir; //Disable zero rotation
        }
        camera.Update(viewport.ControlWidth, viewport.ControlHeight, from, to, rot);
        World.Update(elapsed);
    }

    void DrawCamera()
    {
        if (render3d && cameraOpen)
        {
            ImGui.SetNextWindowSize(new Vector2(360, 210) * ImGuiHelper.Scale, ImGuiCond.FirstUseEver);
            if (ImGui.Begin("Camera", ref cameraOpen))
            {
                string pos =
                    $"{camera.Position.X.ToStringInvariant()}, {camera.Position.Y.ToStringInvariant()}, {camera.Position.Z.ToStringInvariant()}";
                string rotWXYZ =
                    $"{camera.Rotation.W.ToStringInvariant()}, {camera.Rotation.X.ToStringInvariant()}, {camera.Rotation.Y.ToStringInvariant()}, {camera.Rotation.Z.ToStringInvariant()}";
                string rotEuler =
                    $"{camera.RotationEuler.X.ToStringInvariant()}, {camera.RotationEuler.Y.ToStringInvariant()}, {camera.RotationEuler.Z.ToStringInvariant()}";
                ImGui.SeparatorText("Position");
                if (ImGui.Button($"{Icons.Copy}##pos")) {
                    win.SetClipboardText(pos);
                }
                ImGui.SameLine();
                ImGui.AlignTextToFramePadding();
                ImGui.Text(pos);
                ImGui.SeparatorText("WXYZ Rotation");
                if (ImGui.Button($"{Icons.Copy}##wxyz")) {
                    win.SetClipboardText(rotWXYZ);
                }
                ImGui.SameLine();
                ImGui.AlignTextToFramePadding();
                ImGui.Text(rotWXYZ);
                ImGui.SeparatorText("Euler Rotation");
                if (ImGui.Button($"{Icons.Copy}##euler")) {
                    win.SetClipboardText(rotEuler);
                }
                ImGui.SameLine();
                ImGui.AlignTextToFramePadding();
                ImGui.Text(rotEuler);
            }

            ImGui.End();
        }
    }

    void DrawMaps()
    {
        if (mapOpen)
        {
            ImGui.SetNextWindowSize(new Vector2(300) * ImGuiHelper.Scale, ImGuiCond.FirstUseEver);
            if (ImGui.Begin("Map", ref mapOpen))
            {
                ImGui.SliderFloat("Zoom", ref systemMap.Control.Zoom, 1, 10);
                var szX = Math.Max(20, ImGui.GetWindowWidth());
                var szY = Math.Max(20, ImGui.GetWindowHeight() - 90 * ImGuiHelper.Scale);
                systemMap.Draw((int)szX, (int)szY, 1 / 60.0f);
            }
            ImGui.End();
        }
    }

    private static readonly Regex endNumbers = new Regex(@"(\d+)$", RegexOptions.Compiled);

    static string MakeCopyNickname(string nickname)
    {
        var m = endNumbers.Match(nickname);
        if (m.Success)
        {
            var newNumber = (int.Parse(m.Groups[1].Value) + 1)
                .ToString()
                .PadLeft(m.Groups[1].Value.Length, '0');
            return nickname.Substring(0, m.Groups[1].Index) + newNumber;
        }

        return m + "_01";
    }

    record ObjectClipboardItem(ObjectEditData EditData, Transform3D Transform)
    {
        //MakeCopy() after new object data to clone the SystemObject - detaches from world state
        public static ObjectClipboardItem Create(GameObject obj)
            => new((obj.TryGetComponent<ObjectEditData>(out var ed) ?
                ed : new ObjectEditData(obj)).MakeCopy(), obj.LocalTransform);
    }

    public override void OnHotkey(Hotkeys hk, bool shiftPressed)
    {
        if (hk == Hotkeys.Deselect)
        {
            ObjectsList.SelectSingle(null);
            ZoneList.Selected = null;
        }

        if (hk == Hotkeys.Undo && UndoBuffer.CanUndo)
            UndoBuffer.Undo();
        if (hk == Hotkeys.Redo && UndoBuffer.CanRedo)
            UndoBuffer.Redo();

        if (hk == Hotkeys.ToggleGrid)
            renderGrid = !renderGrid;

        if (hk == Hotkeys.ResetViewport)
            viewport.ResetControls();

        if (hk == Hotkeys.Copy && ObjectMode && ObjectsList.Selection.Count > 0)
        {
            win.SystemEditCopy(ObjectsList.Selection
                .Select(ObjectClipboardItem.Create)
                .ToArray());
        }

        if (hk == Hotkeys.Paste && ObjectMode && win.SystemEditClipboard is ObjectClipboardItem[] objs)
        {
            var sel = new List<SysCreateObject>();
            HashSet<string> generatedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var clip in objs)
            {
                string n = clip.EditData.SystemObject.Nickname;
                while (World.GetObject(n) != null ||
                       generatedNames.Contains(n))
                {
                    n = MakeCopyNickname(n);
                }
                generatedNames.Add(n);
                var newData = clip.EditData.MakeCopy();
                newData.Apply();
                newData.ApplyTransform(clip.Transform);
                newData.SystemObject.Nickname = n;
                sel.Add(new SysCreateObject(this, newData.SystemObject));
            }
            UndoBuffer.Commit(EditorAggregateAction.Create(sel.ToArray()));
            ObjectsList.SetObjects(World);
            ObjectsList.Selection = sel.Select(x => x.Object).ToList();
            ObjectsList.SelectedTransform = ObjectsList.Selection[0].LocalTransform.Matrix();
        }

        if (hk == Hotkeys.Copy && LightsMode && LightsList.Selected != null)
        {
            win.SystemEditCopy(LightsList.Selected.Clone());
        }

        if (hk == Hotkeys.Paste && LightsMode && win.SystemEditClipboard is LightSource pasteSource)
        {
            var toCreate = pasteSource.Clone();
            while (LightsList.HasLight(toCreate.Nickname))
            {
                toCreate.Nickname = MakeCopyNickname(toCreate.Nickname);
            }
            UndoBuffer.Commit(new SysLightCreate(toCreate, this));
        }

        if (hk == Hotkeys.Copy && ZonesMode && ZoneList.Selected != null)
        {
            win.SystemEditCopy(ZoneList.Selected.Current.Clone());
        }

        if (hk == Hotkeys.Paste && ZonesMode && win.SystemEditClipboard is Zone pasteZone)
        {
            var toCreate = pasteZone.Clone();
            while (ZoneList.HasZone(toCreate.Nickname))
            {
                toCreate.Nickname = MakeCopyNickname(toCreate.Nickname);
            }
            UndoBuffer.Commit(new SysZoneCreate(this, toCreate));
        }
    }

    private bool firstTab = true;
    bool render3d = true;
    private bool historyOpen = false;
    private bool cameraOpen = false;
    private bool zonePosOpen = false;
    EditMap2D map2D = new();
    List<VerticalTab> blankTabs = new();
    private List<VerticalTab> propertiesTab = new() { new(Icons.PenSquare, "Properties", 0) };

    public override unsafe void Draw(double elapsed)
    {
        if (openField != null)
        {
            if (closeField)
            {
                openField.Closed();
                openField = null;
            }
            else
            {
                openField.Update(elapsed);
                openField.Draw();
                Popups.Run();
                return;
            }
        }
        var curSysName = Data.Infocards.GetStringResource(SystemData.IdsName);
        Title = $"{curSysName} ({CurrentSystem.Nickname})";
        World.RenderUpdate(elapsed);
        if (propertiesMode == 1 &&
            layout.ActiveLeftTab >= 0 &&
            layout.ActiveLeftTab <= 2)
        {
            layout.TabsRight = propertiesTab;
        }
        else
        {
            layout.TabsRight = blankTabs;
            layout.ActiveRightTab = -1;
        }

        layout.Draw((VerticalTabStyle)win.Config.TabStyle);
        DrawMaps();
        DrawCamera();
        DrawZoneInfo();
        if (historyOpen)
            UndoBuffer.DisplayStack();
        Popups.Run();
    }

    void DrawZoneInfo()
    {
        if (!render3d || !zonePosOpen)
            return;
        if (ImGui.Begin("Zones", ref zonePosOpen))
        {
            ImGui.Text($"Zones at position {camera.Position}:");
            ZoneList.ZonesByPosition.ZonesAtPosition(camera.Position, z =>
            {
                ImGui.Text(z.Nickname);
            });
        }
        ImGui.End();
    }

    private bool ZonesMode => layout.ActiveLeftTab == 0;
    private bool ObjectMode => layout.ActiveLeftTab == 1;
    private bool LightsMode => layout.ActiveLeftTab == 2;

    private List<(GameObject Object, Transform3D Transform)> originalObjTransforms =
        new List<(GameObject Object, Transform3D Transform)>();

    private bool manipulatingObjects = false;

    unsafe bool ManipulateObjects()
    {
        if (ObjectMode && ObjectsList.Selection.Count > 0)
        {
            var v = camera.View;
            var p = camera.Projection;
            var mode = ImGui.GetIO().KeyCtrl ? GuizmoMode.WORLD : GuizmoMode.LOCAL;
            Matrix4x4 delta = Matrix4x4.Identity;
            GuizmoOp op;
            if ((op = ImGuizmo.Manipulate(ref v, ref p, GuizmoOperation.TRANSLATE | GuizmoOperation.ROTATE_AXIS, mode,
                    ref ObjectsList.SelectedTransform, out delta)) != GuizmoOp.Nothing && !delta.IsIdentity)
            {
                if (!manipulatingObjects)
                {
                    foreach (var go in ObjectsList.Selection)
                        originalObjTransforms.Add((go, go.LocalTransform));
                    manipulatingObjects = true;
                }

                //GetEditData(objectList.Selection[0]);
                for (int i = 0; i < ObjectsList.Selection.Count; i++)
                {
                    ObjectsList.Selection[i]
                        .SetLocalTransform(Transform3D.FromMatrix(ImGuizmo.ApplyDelta(ObjectsList.Selection[i].LocalTransform.Matrix(), delta, op)));
                    ObjectsList.Selection[i].GetEditData();
                }
            }

            //Insert undo
            if (!ImGuizmo.IsUsing() && manipulatingObjects)
            {
                var actions = originalObjTransforms.Select(x => (EditorAction)new
                    ObjectSetTransform(x.Object, ObjectsList, x.Transform, x.Object.LocalTransform)).ToArray();
                UndoBuffer.Push(EditorAggregateAction.Create(actions));
                manipulatingObjects = false;
                originalObjTransforms = new();
            }

            return ImGuizmo.IsOver() || ImGuizmo.IsUsing();
        }

        return false;
    }

    private bool manipulatingLight = false;
    private Vector3 originalLightPos = Vector3.Zero;

    unsafe bool ManipulateLight()
    {
        if (LightsMode && LightsList.Selected != null)
        {
            var v = camera.View;
            var p = camera.Projection;
            var tr = Matrix4x4.CreateTranslation(LightsList.Selected.Light.Position);
            var mode = ImGui.GetIO().KeyCtrl ? GuizmoMode.WORLD : GuizmoMode.LOCAL;
            if (ImGuizmo.Manipulate(ref v, ref p, GuizmoOperation.TRANSLATE, mode,  ref tr, out var delta) != GuizmoOp.Nothing
                && !delta.IsIdentity)
            {
                if (!manipulatingLight)
                {
                    originalLightPos = LightsList.Selected.Light.Position;
                    manipulatingLight = true;
                }

                var dpos = Vector3.Transform(Vector3.Zero, delta);
                LightsList.Selected.Light.Position += dpos;
            }

            if (!ImGuizmo.IsUsing() && manipulatingLight)
            {
                UndoBuffer.Push(new SysLightSetPosition(LightsList.Selected, originalLightPos,
                    LightsList.Selected.Light.Position, LightsList));
                manipulatingLight = false;
            }

            return ImGuizmo.IsOver() || ImGuizmo.IsUsing();
        }

        return false;
    }

    private bool zoneChanging;
    private Matrix4x4 oldZoneRotation;
    private Vector3 oldRotAngles;
    private Vector3 oldZonePosition;
    private Vector3 oldZoneSize;

    unsafe bool ManipulateZone()
    {
        if (ZonesMode && ZoneList.Selected != null)
        {
            var v = camera.View;
            var p = camera.Projection;
            var zs = ZoneList.Selected;
            var (zsize, zsizemode) = zs.Current.GetSizeModify();
            var mode = GuizmoMode.LOCAL;
            //Cannot combine scale with WORLD rotations
            if (ImGui.GetIO().KeyCtrl)
            {
                zsizemode = 0;
                mode = GuizmoMode.WORLD;
            }

            var tr = Matrix4x4.CreateScale(zsize) * zs.Current.RotationMatrix *
                     Matrix4x4.CreateTranslation(zs.Current.Position);
            if (ImGuizmo.Manipulate(ref v, ref p, GuizmoOperation.TRANSLATE | GuizmoOperation.ROTATE_AXIS | zsizemode,
                    mode, ref tr, out _) != GuizmoOp.Nothing)
            {
                if (!zoneChanging)
                {
                    zoneChanging = true;
                    oldZonePosition = zs.Current.Position;
                    oldZoneRotation = zs.Current.RotationMatrix;
                    oldRotAngles = zs.Current.RotationAngles;
                    oldZoneSize = zs.Current.Size;
                }
                Matrix4x4.Decompose(tr, out var newScale, out var rot, out _);
                zs.Current.Position = Vector3.Transform(Vector3.Zero, tr);
                zs.Current.RotationMatrix = Matrix4x4.CreateFromQuaternion(rot);
                zs.Current.Size = newScale;
                zs.Current.RotationAngles = zs.Current.RotationMatrix.GetEulerDegrees();
                World.Renderer.ZoneVersion++;
            }
            if (!ImGuizmo.IsUsing() && zoneChanging)
            {
                var changes = new List<EditorAction>();
                if (oldZonePosition != zs.Current.Position)
                {
                    changes.Add(new SysZoneSetPosition(zs.Current, this, oldZonePosition, zs.Current.Position));
                }
                if (oldZoneRotation != zs.Current.RotationMatrix)
                {
                    changes.Add(new SysZoneSetRotation(zs.Current, this, oldZoneRotation, oldRotAngles, zs.Current.RotationMatrix, zs.Current.RotationAngles));
                }
                if (oldZoneSize != zs.Current.Size)
                {
                    changes.Add(new SysZoneSetShape(zs.Current, this, zs.Current.Shape, oldZoneSize, zs.Current.Shape, zs.Current.Size));
                }
                if(changes.Count > 0)
                    UndoBuffer.Commit(EditorAggregateAction.Create(changes.ToArray()));
                zoneChanging = false;
            }
            return ImGuizmo.IsOver() || ImGuizmo.IsUsing();
        }

        return false;
    }

    // SYSTEM_Trade_Lane_Ring_X
    // Creates a tradelane with default configuration for vanilla
    void CreateTradelane(TradelaneAddCommand tl)
    {
        // Clear tool state
        map2D.CreationTools.Tradelane.Cancel();
        // Create objects
        var tlNick = $"{SystemData.Nickname}_Trade_Lane_Ring_1";
        HashSet<string> nicks = new();
        List<SystemObject> createdTradelanes = new();
        var offset = (tl.End - tl.Start) / (tl.Count - 1);
        var face = QuaternionEx.LookAt(tl.Start, tl.End);
        Data.GameData.Items.TryGetLoadout("trade_lane_ring_li_01", out var loadout);
        for (int i = 0; i < tl.Count; i++)
        {
            var pos = tl.Start + offset * i;
            while (World.GetObject(tlNick) != null ||
                   nicks.Contains(tlNick))
            {
                tlNick = MakeCopyNickname(tlNick);
            }
            var newTl = new SystemObject()
            {
                Nickname = tlNick,
                Position = pos,
                Rotation = face,
                IdsName = 260923, // Vanilla empty string - make configurable
                IdsInfo = 66170, // Vanilla tradelane infocard - make configurable
                IdsLeft = tl.IdsLeft, // Internal property
                IdsRight = tl.IdsRight, // Internal property
                Archetype = tl.Archetype,
                Behavior = "NOTHING",
                Reputation = tl.Reputation,
                DifficultyLevel = 3,
                Pilot = Data.GameData.Items.GetPilot("pilot_solar_easiest"), // From vanilla - make configurable
                Loadout = loadout
            };
            createdTradelanes.Add(newTl);
            nicks.Add(tlNick);
        }

        for (int i = 0; i < createdTradelanes.Count; i++)
        {
            createdTradelanes[i].Dock = new()
            {
                Kind = DockKinds.Tradelane,
                Target = i + 1 < createdTradelanes.Count ? createdTradelanes[i + 1].Nickname : null,
                TargetLeft = i - 1 >= 0 ? createdTradelanes[i - 1].Nickname : null
            };
        }
        createdTradelanes[0].TradelaneSpaceName = tl.IdsRight; //TradelaneSpaceName = destination?
        createdTradelanes[^1].TradelaneSpaceName = tl.IdsLeft;
        UndoBuffer.Commit(EditorAggregateAction.Create(
            createdTradelanes.Select(x => new SysCreateObject(this, x)).ToArray<EditorAction>()));
    }

    public void OnCreateTradelane(Vector3 start, Vector3 end)
    {
        Popups.OpenPopup(new TradelaneAddPopup(start, end, Data, ObjectsList.Objects,
            CreateTradelane,
            () => map2D.CreationTools.Tradelane.Cancel()));
    }

    public void FinishPatrolRoute()
    {
        var points = map2D.CreationTools.Patrol.Finish();
        Popups.OpenPopup(new PatrolRouteDialog(points, config =>
        {
            CreatePatrolRoute(points, config);
        }, () => {
            // Cancel callback - nothing to do, state already reset
        }, Data, CurrentSystem));
    }

    private void CreatePatrolRoute(List<Vector3> points, PatrolRouteConfig config)
    {
        var actions = new List<EditorAction>();

        foreach (var encounter in config.Encounters)
        {
            if (!CurrentSystem.EncounterParameters.Any(x =>
                    x.Nickname.Equals(encounter.Archetype, StringComparison.OrdinalIgnoreCase)))
            {
                actions.Add(new SysAddEncounterParameter(CurrentSystem, new EncounterParameters()
                {
                    Nickname = encounter.Archetype,
                    SourceFile = $"missions\\encounters\\{encounter.Archetype}.ini"
                }));
            }
        }

        for (int i = 0; i < points.Count - 1; i++)
        {
            var p1 = points[i];
            var p2 = points[i + 1];

            var center = (p1 + p2) / 2;
            var height = Vector3.Distance(p1, p2);

            if (height < 0.1f) continue;

            var direction = (p2 - p1).Normalized();

            Matrix4x4 rotation;
            // Align cylinder's local Y with `direction`
            var startVec = Vector3.UnitY;
            var dot = Vector3.Dot(startVec, direction);

            if (Math.Abs(dot) > 0.99999f) // Parallel vectors
            {
                rotation = dot > 0 ? Matrix4x4.Identity : Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, MathF.PI);
            }
            else
            {
                var rotAxis = Vector3.Cross(startVec, direction);
                rotAxis = Vector3.Normalize(rotAxis);
                var angle = MathF.Acos(dot);
                rotation = Matrix4x4.CreateFromAxisAngle(rotAxis, angle);
            }

            string baseName = $"{CurrentSystem.Nickname}_path_{config.PathLabel}";
            string zoneName = $"{baseName}_{i + 1}";
            int count = 1;
            while(ZoneList.ZoneExists(zoneName)) {
                zoneName = $"{baseName}_{i + 1}_{count++}";
            }

            var zone = new Zone()
            {
                Nickname = zoneName,
                Position = center,
                RotationMatrix = rotation,
                Shape = ShapeKind.Cylinder,
                Size = new Vector3(500, height, 0), //Radius, Height
                Sort = config.Sort,
                Toughness = config.Toughness,
                Density = config.Density,
                RepopTime = config.RepopTime,
                MaxBattleSize = config.MaxBattleSize,
                ReliefTime = config.ReliefTime,
                PopType = new[] { "attack_patrol" },
                PathLabel = new[] { config.PathLabel, (i + 1).ToString() },
                Usage = new[] { "patrol" },
                Encounters = config.Encounters.Select(e => new DataEncounter
                {
                    Archetype = e.Archetype,
                    Difficulty = e.Difficulty,
                    Chance = e.Chance,
                    FactionSpawns = e.Factions.Select(f => new DataFactionSpawn
                    {
                        Faction = f.Faction.Nickname,
                        Chance = f.Chance
                    }).ToList()
                }).ToArray(),
                DensityRestrictions = Array.Empty<DataDensityRestriction>()
            };
            actions.Add(new SysAddZoneAction(this, zone));
        }

        if(actions.Count > 0)
            UndoBuffer.Commit(EditorAggregateAction.Create(actions.ToArray()));

        map2D.CreationTools.Patrol.Cancel();
    }


    public override void Dispose()
    {
        World.Dispose();
        renderer.Dispose();
        viewport.Dispose();
        openField?.Closed();
        ZoneList.Dispose();
        systemMap.Dispose();
    }
}
