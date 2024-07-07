using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using ImGuiNET;
using LancerEdit.GameContent.Popups;
using LibreLancer;
using LibreLancer.Data;
using LibreLancer.GameData;
using LibreLancer.GameData.World;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.ImUI;
using LibreLancer.Infocards;
using LibreLancer.Physics;
using LibreLancer.Render;
using LibreLancer.Render.Cameras;
using LibreLancer.World;
using Archetype = LibreLancer.GameData.Archetype;
using ModelRenderer = LibreLancer.Render.ModelRenderer;

namespace LancerEdit.GameContent;

public class SystemEditorTab : GameContentTab
{
    private static WorldMatrixBuffer matrixBuffer = new WorldMatrixBuffer();

    //public fields
    public SystemEditData SystemData;
    public GameWorld World;
    public bool ObjectsDirty = false;
    public StarSystem CurrentSystem;
    public List<SystemObject> DeletedObjects = new();
    public GameDataContext Data;

    private MainWindow win;
    private SystemRenderer renderer;
    private Viewport3D viewport;
    private LookAtCamera camera;
    private SystemMap systemMap = new SystemMap();
    public SystemObjectList ObjectsList;
    public LightSourceList LightsList;
    public ZoneList ZoneList;

    private bool mapOpen = false;
    private bool infocardOpen = false;

    Infocard systemInfocard;
    private InfocardControl icard;

    public PopupManager Popups = new PopupManager();

    public EditorUndoBuffer UndoBuffer = new EditorUndoBuffer();

    public SystemEditorTab(GameDataContext gameData, MainWindow mw, StarSystem system)
    {
        Title = "System Editor";
        SaveStrategy = new StarSystemSaveStrategy(this);
        this.Data = gameData;
        viewport = new Viewport3D(mw);
        viewport.EnableMSAA = false; //MSAA handled by SystemRenderer
        viewport.DefaultOffset = new Vector3(0, 0, 4);
        viewport.ModelScale = 1000f;
        viewport.Mode = CameraModes.Walkthrough;
        viewport.Background = new Vector4(0.12f, 0.12f, 0.12f, 1f);
        viewport.ResetControls();
        viewport.DoubleClicked += ViewportOnDoubleClicked;
        camera = new LookAtCamera()
        {
            GameFOV = true,
            ZRange = new Vector2(3f, 10000000f)
        };

        //Extract nav_prettymap texture
        string navPrettyMap = gameData.GameData.DataPath("INTERFACE/NEURONET/NAVMAP/NEWNAVMAP/nav_prettymap.3db");

        if (gameData.GameData.VFS.FileExists(navPrettyMap))
        {
            gameData.Resources.LoadResourceFile(navPrettyMap);
        }

        systemMap.CreateContext(gameData, mw);
        this.win = mw;
        ObjectsList = new SystemObjectList(mw);
        LightsList = new LightSourceList(this);
        ObjectsList.OnMoveCamera += MoveCameraTo;
        LightsList.OnMoveCamera += MoveCameraToLight;
        ObjectsList.OnDelete += DeleteObject;

        LoadSystem(system);
    }


    private void ViewportOnDoubleClicked(Vector2 pos)
    {
        if (openTabs[1])
        {
            var sel = World.GetSelection(camera, null, pos.X, pos.Y, viewport.RenderWidth, viewport.RenderHeight);
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
        else if (openTabs[2])
        {
            var cameraProjection = camera.Projection;
            var cameraView = camera.View;
            var vp = new Vector2(viewport.RenderWidth, viewport.RenderHeight);
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
        systemInfocard = Data.GameData.GetInfocard(CurrentSystem.IdsInfo, Data.Fonts);
        if (icard != null) icard.SetInfocard(systemInfocard);
        Data.GameData.LoadAllSystem(CurrentSystem);
        World.LoadSystem(CurrentSystem, Data.Resources, false, false);
        World.Renderer.LoadLights(CurrentSystem);
        World.Renderer.LoadStarspheres(CurrentSystem);
        systemMap.SetObjects(CurrentSystem);
        renderer.PhysicsHook = RenderZones;
        renderer.OpaqueHook = RenderOpaque;
        SystemData = new SystemEditData(CurrentSystem);
        //Setup UI
        ZoneList = new ZoneList();
        ZoneList.SetZones(CurrentSystem.Zones, CurrentSystem.AsteroidFields, CurrentSystem.Nebulae);
        World.Renderer.LoadZones(ZoneList.AsteroidFields, ZoneList.Nebulae);
        ObjectsList.SetObjects(World);
        LightsList.SetLights(CurrentSystem.LightSources);
    }

    private bool renderGrid = false;

    void RenderGrid()
    {
        if (!renderGrid) return;
        var cpos = camera.Position;
        var y = Math.Abs(cpos.Y);
        if (y <= 100) y = 100;

        GridRender.Draw(win.RenderContext, GridRender.DistanceScale(y), win.Config.GridColor, camera.ZRange.X,
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

            ImGui.SameLine();
            if (ImGui.Button("Show All"))
                ZoneList.ShowAll();
            ImGui.SameLine();
            if (ImGui.Button("Hide All"))
                ZoneList.HideAll();
            ZoneList.Draw();
        }, ZoneProperties);
    }

    void ZoneProperties()
    {
        if (ZoneList.Selected == null)
        {
            ImGui.Text("No Zone Selected");
            return;
        }

        var sel = ZoneList.Selected.Current;
        var ez = ZoneList.Selected;

        Controls.BeginPropertyTable("properties", true, false, true);
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
            Popups.OpenPopup(IdsSearch.SearchStrings(Data.Infocards, Data.Fonts,
                newIds =>
                {
                    sel.IdsName = newIds;
                }));
        }

        //Position
        Controls.PropertyRow("Position", $"{sel.Position.X:0.00}, {sel.Position.Y:0.00}, {sel.Position.Z: 0.00}");
        if (ImGui.Button($"{Icons.Edit}##position"))
        {
            Popups.OpenPopup(new Vector3Popup("Position", sel.Position, (v, d) =>
            {
                sel.Position = v;
            }));
        }

        var rot = sel.RotationMatrix.GetEulerDegrees();
        Controls.PropertyRow("Rotation",
            $"{rot.X: 0.00}, {rot.Y:0.00}, {rot.Z: 0.00}");
        if (ImGui.Button("0##rot"))
        {
            UndoBuffer.Commit(SysZoneSetRotation.Create(sel, this, Matrix4x4.Identity));
        }

        ShapeProperties(sel);
        Controls.EndPropertyTable();

        //Comment
        Controls.BeginPropertyTable("comment", true, false, true);
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TextUnformatted("Comment");
        ImGui.TableNextColumn();
        Controls.TruncText(sel.Comment, 20);
        ImGui.TableNextColumn();
        if (ImGui.Button($"{Icons.Edit}##comment"))
            Popups.OpenPopup(new CommentPopup(sel.Comment,
                x => UndoBuffer.Commit(new SysZoneSetComment(sel, this, sel.Comment, x))));
        Controls.EndPropertyTable();
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
        ImGui.Checkbox("Nebulae", ref renderer.DrawNebulae);
        ImGui.Checkbox("Starspheres", ref renderer.DrawStarsphere);
        ImGui.BeginDisabled(!win.RenderContext.SupportsWireframe);
        ImGui.Checkbox("Wireframe", ref drawWireframe);
        ImGui.EndDisabled();
    }


    void MoveCameraTo(GameObject obj)
    {
        var r = (obj.RenderComponent as ModelRenderer)?.Model?.GetRadius() ?? 10f;
        viewport.CameraOffset = obj.LocalTransform.Position + new Vector3(0, 0, -r * 3.5f);
        viewport.CameraRotation = new Vector2(-MathF.PI, 0);
    }

    private void MoveCameraToLight(Vector3 pos)
    {
        viewport.CameraOffset = pos - new Vector3(0, 0, 12f);
        viewport.CameraRotation = new Vector2(-MathF.PI, 0);
    }

    ObjectEditData GetEditData(GameObject obj, bool create = true)
    {
        if (!obj.TryGetComponent<ObjectEditData>(out var d))
        {
            if (create)
            {
                d = new ObjectEditData(obj);
                ObjectsDirty = true;
                obj.AddComponent(d);
            }
        }

        return d;
    }


    public void SetArchetypeLoadout(GameObject obj, Archetype archetype, ObjectLoadout loadout)
    {
        var ed = GetEditData(obj);
        ed.Archetype = archetype;
        ed.Loadout = loadout;
        var tr = obj.LocalTransform;
        World.InitObject(obj, true, obj.SystemObject, Data.Resources, false, true, ed.Loadout, ed.Archetype);
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
            var sys = Data.GameData.Systems.Get(a.Target);
            var sname = sys == null ? "INVALID" : Data.Infocards.GetStringResource(sys.IdsName);
            sb.AppendLine($"{a.Target} ({sname})");
            sb.AppendLine($"{a.Exit}");
        }

        if (a.Kind == DockKinds.Base)
        {
            var b = Data.GameData.Bases.Get(a.Target);
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

    void ObjectProperties(GameObject sel)
    {
        var ed = GetEditData(sel, false);
        var gc = sel.Content();
        if (ImGuiExt.Button("Reset", ed != null && !ed.IsNewObject))
        {
            sel.Unregister(World.Physics);
            World.RemoveObject(sel);
            sel = World.NewObject(sel.SystemObject, Data.Resources, false);
            ObjectsList.SelectedTransform = sel.LocalTransform.Matrix();
            ObjectsList.SelectSingle(sel);
            ObjectsList.SetObjects(World);
        }

        Controls.BeginPropertyTable("properties", true, false, true);
        Controls.PropertyRow("Nickname", sel.Nickname);
        if (ImGui.Button($"{Icons.Edit}##nickname"))
        {
            Popups.OpenPopup(new NameInputPopup(NameInputConfig.Nickname("Rename", n => World.GetObject(n) != null),
                sel.Nickname, x => UndoBuffer.Commit(new ObjectSetNickname(sel, sel.Nickname, x, ObjectsList))));
        }

        Controls.PropertyRow("Name", ed == null
            ? sel.Name.GetName(Data.GameData, camera.Position)
            : Data.Infocards.GetStringResource(ed.IdsName));
        if (ImGui.Button($"{Icons.Edit}##name"))
        {
            var oldName = ed?.IdsName ?? sel.SystemObject.IdsName;
            Popups.OpenPopup(IdsSearch.SearchStrings(Data.Infocards, Data.Fonts,
                newIds => UndoBuffer.Commit(new ObjectSetIdsName(sel, oldName, newIds))));
        }

        //Position
        var pos = sel.LocalTransform.Position;
        var rot = sel.LocalTransform.GetEulerDegrees();
        Controls.PropertyRow("Position", $"{pos.X:0.00}, {pos.Y:0.00}, {pos.Z: 0.00}");
        Controls.PropertyRow("Rotation", $"{rot.X: 0.00}, {rot.Y:0.00}, {rot.Z: 0.00}");
        if (ImGui.Button("0##rot"))
        {
            UndoBuffer.Commit(new ObjectSetTransform(sel, sel.LocalTransform, new Transform3D(pos, Quaternion.Identity),
                ObjectsList));
        }

        var oldArchetype = ed?.Archetype ?? sel.SystemObject.Archetype;
        var oldLoadout = ed != null ? ed.Loadout : sel.SystemObject.Loadout;

        //Archetype
        Controls.PropertyRow("Archetype", gc.Archetype?.Nickname ?? "(none)");
        if (ImGui.Button($"{Icons.Edit}##archetype"))
        {
            Popups.OpenPopup(new ArchetypeSelection(
                x => UndoBuffer.Commit(new ObjectSetArchetypeLoadout(
                    sel, this, oldArchetype, oldLoadout, x, null)),
                oldArchetype,
                Data));
        }

        //Loadout
        Controls.PropertyRow("Loadout", gc.Loadout?.Nickname ?? "(none)");
        if (ImGui.Button($"{Icons.Edit}##loadout"))
        {
            Popups.OpenPopup(new LoadoutSelection(
                x => UndoBuffer.Commit(new ObjectSetArchetypeLoadout(
                    sel, this, oldArchetype, oldLoadout, oldArchetype, x)),
                oldLoadout,
                sel.GetHardpoints().Select(x => x.Name).ToArray(),
                Data));
        }

        //Visit
        Controls.PropertyRow("Visit", VisitFlagEditor.FlagsString(gc.Visit));
        if (ImGui.Button($"{Icons.Edit}##visit"))
            Popups.OpenPopup(
                new VisitFlagEditor(gc.Visit, x => UndoBuffer.Commit(new ObjectSetVisit(sel, gc.Visit, x))));
        FactionRow("Reputation", gc.Reputation, x => UndoBuffer.Commit(new ObjectSetReputation(sel, gc.Reputation, x)));
        Controls.EndPropertyTable();
        Controls.BeginPropertyTable("base and docking", true, false, true);
        BaseRow(
            "Base",
            gc.Base,
            x => UndoBuffer.Commit(new ObjectSetBase(sel, gc.Base, x)),
            "This is the base entry used when linking infocards.\nDocking requires setting the dock action"
        );
        DockRow(gc.Dock, gc.Archetype, x => UndoBuffer.Commit(new ObjectSetDock(sel, gc.Dock, x)));
        Controls.EndPropertyTable();

        //Comment
        Controls.BeginPropertyTable("comment", true, false, true);
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TextUnformatted("Comment");
        ImGui.TableNextColumn();
        Controls.TruncText(gc.Comment, 20);
        ImGui.TableNextColumn();
        if (ImGui.Button($"{Icons.Edit}##comment"))
            Popups.OpenPopup(new CommentPopup(gc.Comment,
                x => UndoBuffer.Commit(new ObjectSetComment(sel, gc.Comment, x))));
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
            IdsInfo = Array.Empty<int>(),
            Archetype = archetype,
            Position = pos ?? to,
        };
        var obj = new SysCreateObject(this, sysobj);
        UndoBuffer.Commit(obj);
        ObjectsList.SelectSingle(obj.Object);
        ObjectsList.ScrollToSelection();
        ObjectsDirty = true;
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

    private bool propertiesWindow = false;

    void PanelWithProperties(string id, Action panel, Action properties)
    {
        if (!propertiesWindow)
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
        if (propertiesWindow)
        {
            ImGui.SetNextWindowSize(new Vector2(400, 300) * ImGuiHelper.Scale, ImGuiCond.FirstUseEver);
            if (ImGui.Begin("Properties", ref propertiesWindow))
            {
                properties();
                ImGui.End();
            }
        }
        else
        {
            ImGui.BeginChild("##properties", new Vector2(ImGui.GetWindowWidth(), h2));
            ImGui.Text("Properties");
            ImGui.SameLine();
            if (Controls.SmallButton($"{Icons.UpRightFromSquare}"))
                propertiesWindow = true;
            ImGui.Separator();
            properties();
            ImGui.EndChild();
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
                    if (GetEditData(obj, false) != null)
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
                        sel = World.NewObject(sel.SystemObject, Data.Resources, false);
                        if (i == 0)
                            ObjectsList.SelectedTransform = sel.LocalTransform.Matrix();
                        ObjectsList.Selection[i] = sel;
                    }

                    ObjectsList.SetObjects(World);
                }
            }
            else
                ImGui.Text("No Object Selected");
        });
    }

    void UpdateAttenuation(LightSource light, string curveName, Vector3 attenuation)
    {
        if (curveName != null && light.Light.Kind != LightKind.PointAttenCurve)
        {
            UndoBuffer.Commit(EditorAggregateAction.Create([
                new SysLightSetKind(light, light.Light.Kind, LightKind.PointAttenCurve),
                new SysLightSetAttenuation(light, light.AttenuationCurveName, light.Light.Attenuation, curveName, attenuation)
            ]));
        }
        else if (curveName == null && light.Light.Kind == LightKind.PointAttenCurve)
        {
            UndoBuffer.Commit(EditorAggregateAction.Create([
                new SysLightSetKind(light, light.Light.Kind, LightKind.Point),
                new SysLightSetAttenuation(light, light.AttenuationCurveName, light.Light.Attenuation, curveName, attenuation)
            ]));
        }
        else
        {
            UndoBuffer.Commit(new SysLightSetAttenuation(light, light.AttenuationCurveName, light.Light.Attenuation, curveName, attenuation));
        }
    }

    void UpdateLightKind(LightSource light, LightKind newKind)
    {
        if (light.Light.Kind == LightKind.PointAttenCurve) {
            UndoBuffer.Commit(EditorAggregateAction.Create(new EditorAction[]
            {
                new SysLightSetKind(light, light.Light.Kind, newKind),
                new SysLightSetAttenuation(light, light.AttenuationCurveName, light.Light.Attenuation, null, new Vector3(1,0,0))
            }));
        }
        else
        {
            UndoBuffer.Commit(new SysLightSetKind(light, light.Light.Kind, newKind));
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
            Controls.BeginPropertyTable("Props", true, false, true);
            Controls.PropertyRow("Nickname", sel.Nickname);
            if (ImGui.Button($"{Icons.Edit}##nickname"))
            {
                Popups.OpenPopup(new NameInputPopup(
                    NameInputConfig.Nickname("Rename", x => LightsList.HasLight(x)),
                    sel.Nickname,
                    x => UndoBuffer.Commit(new SysLightSetNickname(sel, sel.Nickname, x))
                ));
            }
            var pos = sel.Light.Position;
            Controls.PropertyRow("Position", $"{pos.X:0.00}, {pos.Y:0.00}, {pos.Z: 0.00}");
            ColorProperty("Color", new Color4(sel.Light.Color, 1), x =>
                UndoBuffer.Commit(new SysLightSetColor(sel, sel.Light.Color, x.Rgb)));
            Controls.PropertyRow("Type", sel.Light.Kind == LightKind.Directional ? "Directional" : "Point");
            if (sel.Light.Kind == LightKind.Directional)
            {
                var dir = sel.Light.Direction;
                Controls.PropertyRow("Direction", $"{dir.X:0.00000}, {dir.Y:0.0000}, {dir.Z: 0.0000}");
            }
            Controls.PropertyRow("Range", $"{sel.Light.Range:0.00}");
            if (ImGui.Button($"{Icons.Edit}##range"))
            {
                Popups.OpenPopup(new FloatPopup("Range", sel.Light.Range,
                    (old, updated) => UndoBuffer.Commit(new SysLightSetRange(sel, old, updated)),
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
                    graph = Data.GameData.Ini.Graphs.FindFloatGraph(sel.AttenuationCurveName);
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
            Popups.OpenPopup(new VfsFileSelector(name, Data.GameData.VFS, Data.GameData.Ini.Freelancer.DataPath, file =>
            {
                var modelFile = Data.GameData.DataPath(file);
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

    void SystemPanel()
    {
        if (ImGuiExt.Button("Reset All", SystemData.IsDirty()))
        {
            SystemData = new SystemEditData(CurrentSystem);
            ReloadStarspheres();
        }

        ImGui.Separator();
        Controls.BeginPropertyTable("Props", true, false, true);
        Controls.PropertyRow("Name", Data.Infocards.GetStringResource(SystemData.IdsName));
        if (ImGui.Button($"{Icons.Edit}##name"))
        {
            Popups.OpenPopup(IdsSearch.SearchStrings(Data.Infocards, Data.Fonts,
                newIds => UndoBuffer.Commit(new SysDataSetIdsName(SystemData, SystemData.IdsName, newIds))));
        }

        ColorProperty("Space Color", SystemData.SpaceColor, x =>
            UndoBuffer.Commit(new SysDataSetSpaceColor(SystemData, SystemData.SpaceColor, x)));
        ColorProperty("Ambient Color", SystemData.Ambient, x =>
            UndoBuffer.Commit(new SysDataSetAmbient(SystemData, SystemData.Ambient, x)));
        Controls.EndPropertyTable();
        ImGui.Separator();
        ImGui.Text("Music");
        Controls.BeginPropertyTable("Music", true, false, true, true, true);
        MusicProp("Space", SystemData.MusicSpace, x =>
            UndoBuffer.Commit(new SysDataSetMusic(SystemData, SystemData.MusicSpace, x, "Space")));
        MusicProp("Battle", SystemData.MusicBattle, x =>
            UndoBuffer.Commit(new SysDataSetMusic(SystemData, SystemData.MusicBattle, x, "Battle")));
        MusicProp("Danger", SystemData.MusicDanger, x =>
            UndoBuffer.Commit(new SysDataSetMusic(SystemData, SystemData.MusicDanger, x, "Danger")));
        Controls.EndPropertyTable();
        ImGui.Separator();
        ImGui.Text("Stars");
        Controls.BeginPropertyTable("Stars", true, false, true, true);
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

    private void RenderZones()
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
    }

    public override void Update(double elapsed)
    {
        var rot = Matrix4x4.CreateRotationX(viewport.CameraRotation.Y) *
                  Matrix4x4.CreateRotationY(viewport.CameraRotation.X);
        var dir = Vector3.Transform(-Vector3.UnitZ, rot);
        var to = viewport.CameraOffset + (dir * 10);
        camera.Update(viewport.RenderWidth, viewport.RenderHeight, viewport.CameraOffset, to, rot);
        World.Update(elapsed);
    }

    void DrawInfocard()
    {
        if (infocardOpen)
        {
            ImGui.SetNextWindowSize(new Vector2(300, 300), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("Infocard", ref infocardOpen))
            {
                var szX = Math.Max(20, ImGui.GetWindowWidth());
                var szY = Math.Max(20, ImGui.GetWindowHeight());
                if (icard == null)
                {
                    icard = new InfocardControl(win, systemInfocard, szX);
                }

                icard.Draw(szX);
                ImGui.End();
            }
        }
    }

    void DrawMaps()
    {
        if (mapOpen)
        {
            ImGui.SetNextWindowSize(new Vector2(300) * ImGuiHelper.Scale, ImGuiCond.FirstUseEver);
            if (ImGui.Begin("Map", ref mapOpen))
            {
                var szX = Math.Max(20, ImGui.GetWindowWidth());
                var szY = Math.Max(20, ImGui.GetWindowHeight() - 37 * ImGuiHelper.Scale);
                systemMap.Draw((int)szX, (int)szY, 1 / 60.0f);
                ImGui.End();
            }
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
    }

    BitArray128 openTabs;

    void TabButtons()
    {
        ImGui.BeginGroup();
        TabHandler.TabButton("Zones", 0, ref openTabs);
        TabHandler.TabButton("Objects", 1, ref openTabs);
        TabHandler.TabButton("Lights", 2, ref openTabs);
        TabHandler.TabButton("System", 3, ref openTabs);
        TabHandler.TabButton("View", 4, ref openTabs);
        ImGui.EndGroup();
        ImGui.SameLine();
    }

    private bool firstTab = true;
    bool render3d = true;
    private bool historyOpen = false;
    EditMap2D map2D = new();

    public override unsafe void Draw(double elapsed)
    {
        var curSysName = Data.Infocards.GetStringResource(SystemData.IdsName);
        Title = $"{curSysName} ({CurrentSystem.Nickname})";
        World.RenderUpdate(elapsed);
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
            if (openTabs[0]) ZonesPanel();
            if (openTabs[1]) ObjectsPanel();
            if (openTabs[2]) LightsPanel();
            if (openTabs[3]) SystemPanel();
            if (openTabs[4]) ViewPanel();
            ImGui.EndChild();
            ImGui.NextColumn();
        }

        TabButtons();
        ImGui.BeginChild("##main");
        ImGui.SameLine();
        using (var tb = Toolbar.Begin("##toolbar", false))
        {
            tb.CheckItem("3D", ref render3d);
            if (render3d)
            {
                tb.CheckItem("Grid", ref renderGrid);
                tb.TextItem($"Camera Position: {camera.Position}");
            }
            else
            {
                tb.FloatSliderItem("Zoom", ref map2D.Zoom, 1, 10, "%.2fx");
            }

            tb.ToggleButtonItem("Map", ref mapOpen);
            tb.ToggleButtonItem("Infocard", ref infocardOpen);
            tb.ToggleButtonItem("History", ref historyOpen);
        }

        if (render3d)
        {
            ImGuiHelper.AnimatingElement();
            renderer.BackgroundOverride = SystemData.SpaceColor;
            renderer.SystemLighting.Ambient = SystemData.Ambient;
            renderer.SystemLighting.Lights =
                LightsList.Sources.Select(x => new DynamicLight() { Light = x.Light }).ToList();
            if (viewport.Begin())
            {
                win.RenderContext.Wireframe = drawWireframe;
                renderer.Draw(viewport.RenderWidth, viewport.RenderHeight);
                viewport.End();
                win.RenderContext.Wireframe = false;
            }

            if (ManipulateObjects() || ManipulateZone() || ManipulateLight())
            {
                viewport.SetInputsEnabled(false);
            }
            else
            {
                viewport.SetInputsEnabled(true);
            }
        }
        else
        {
            map2D.Draw(SystemData, World, Data, this);
        }

        ImGui.EndChild();
        DrawMaps();
        DrawInfocard();
        if (historyOpen)
            UndoBuffer.DisplayStack();
        Popups.Run();
    }

    private bool ZonesMode => openTabs[0];
    private bool ObjectMode => openTabs[1];
    private bool LightsMode => openTabs[2];

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

                ObjectsDirty = true;
                //GetEditData(objectList.Selection[0]);
                for (int i = 0; i < ObjectsList.Selection.Count; i++)
                {
                    ObjectsList.Selection[i]
                        .SetLocalTransform(Transform3D.FromMatrix(ImGuizmo.ApplyDelta(ObjectsList.Selection[i].LocalTransform.Matrix(), delta, op)));
                    GetEditData(ObjectsList.Selection[i]);
                }
            }

            //Insert undo
            if (!ImGuizmo.IsUsing() && manipulatingObjects)
            {
                var actions = originalObjTransforms.Select(x => (EditorAction)new
                    ObjectSetTransform(x.Object, x.Transform, x.Object.LocalTransform, ObjectsList)).ToArray();
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
                    LightsList.Selected.Light.Position));
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


    public override void Dispose()
    {
        World.Dispose();
        renderer.Dispose();
        viewport.Dispose();
    }
}
