using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.GameData;
using LibreLancer.GameData.World;
using LibreLancer.ImUI;
using LibreLancer.Infocards;
using LibreLancer.Render;
using LibreLancer.Render.Cameras;
using LibreLancer.World;
using Microsoft.EntityFrameworkCore.Metadata;
using ModelRenderer = LibreLancer.Render.ModelRenderer;

namespace LancerEdit;

public class SystemEditorTab : GameContentTab
{
    private GameDataContext gameData;

    private MainWindow win;
    private SystemRenderer renderer;
    private Viewport3D viewport;
    private LookAtCamera camera;
    private GameWorld world;
    private SystemMap systemMap = new SystemMap();

    string[] systems;
    int sysIndex = 0;
    int sysIndexLoaded = -1;

    private StarSystem curSystem;

    private bool universeOpen = false;
    private bool infocardOpen = false;
    
    private Texture2D universeBackgroundTex;
    private int universeBackgroundRegistered;

    Infocard systemInfocard;
    private InfocardControl icard;

    private PopupManager popups = new PopupManager();
    public SystemEditorTab(GameDataContext gameData, MainWindow mw)
    {
        Title = "System Editor";
        
        this.gameData = gameData;
        viewport = new Viewport3D(mw);
        viewport.EnableMSAA = false; //MSAA handled by SystemRenderer
        viewport.DefaultOffset = new Vector3(0, 0, 4);
        viewport.ModelScale = 1000f;
        viewport.Mode = CameraModes.Walkthrough;
        viewport.Background =  new Vector4(0.12f,0.12f,0.12f, 1f);
        viewport.ResetControls();
        viewport.DoubleClicked += ViewportOnDoubleClicked;
        camera = new LookAtCamera()
        {
            GameFOV = true,
            ZRange = new Vector2(3f, 10000000f)
        };
        
        //Extract nav_prettymap texture
        string navPrettyMap;
        if ((navPrettyMap = gameData.GameData.VFS.Resolve(gameData.GameData.Ini.Freelancer.DataPath + "INTERFACE/NEURONET/NAVMAP/NEWNAVMAP/nav_prettymap.3db", false)) !=
            null)
        {
            gameData.Resources.LoadResourceFile(navPrettyMap);
        }
        universeBackgroundTex = (gameData.Resources.FindTexture("fancymap.tga") as Texture2D);
        if (universeBackgroundTex != null)
            universeBackgroundRegistered = ImGuiHelper.RegisterTexture(universeBackgroundTex);
        else
            universeBackgroundRegistered = -1;
        
        systemMap.CreateContext(gameData, mw);
        
        systems = gameData.GameData.ListSystems().OrderBy(x => x).ToArray();

        this.win = mw;
        
        ChangeSystem();
    }

    private bool scrollToSelection = false;

    private void ViewportOnDoubleClicked(Vector2 pos)
    {
        if (openTabs[1])
        {
            selectedObject =
                world.GetSelection(camera, null, pos.X, pos.Y, viewport.RenderWidth, viewport.RenderHeight);
            selectedTransform = selectedObject?.LocalTransform ?? Matrix4x4.Identity;
            scrollToSelection = true;
        }
    }

    void ChangeSystem()
    {
        if (sysIndex != sysIndexLoaded)
        {
            selectedObject = null;
            if (world != null)
            {
                world.Renderer.Dispose();
                world.Dispose();
            }
            //Load system
            renderer = new SystemRenderer(camera, gameData.Resources, win);
            world = new GameWorld(renderer, null);
            curSystem = gameData.GameData.GetSystem(systems[sysIndex]);
            systemInfocard = gameData.GameData.GetInfocard(curSystem.IdsInfo, gameData.Fonts);
            if (icard != null) icard.SetInfocard(systemInfocard);
            gameData.GameData.LoadAllSystem(curSystem);
            world.LoadSystem(curSystem, gameData.Resources, false);
            systemMap.SetObjects(curSystem);
            sysIndexLoaded = sysIndex;
            renderer.PhysicsHook = RenderZones;
            systemData = new SystemEditData(curSystem);
            //Setup UI
            InitZoneList();
            BuildObjectList();
        }
    }

    private bool showZones = false;
    private string hoveredZone = null;
    void ZonesPanel()
    {
        if (ImGui.Button("Show All"))
        {
            renderZones = new HashSet<string>();
            foreach (var z in curSystem.Zones)
                renderZones.Add(z.Nickname);
        }
        if (ImGui.Button("Hide All"))
        {
            renderZones = new HashSet<string>();
        }
        foreach (var z in curSystem.Zones)
        {
            var contains = renderZones.Contains(z.Nickname);
            var v = contains;
            ImGui.Checkbox(z.Nickname, ref v);
            if (ImGui.IsItemHovered())
                hoveredZone = z.Nickname;
            if (v != contains)
            {
                if (contains) renderZones.Remove(z.Nickname);
                else renderZones.Add(z.Nickname);
            }
        }
    }

    void ViewPanel()
    {
        ImGui.Checkbox("Nebulae", ref renderer.DrawNebulae);
        ImGui.Checkbox("Starspheres", ref renderer.DrawStarsphere);
    }
    

    void MoveCameraTo(GameObject obj)
    {
        var r = (obj.RenderComponent as ModelRenderer)?.Model?.GetRadius() ?? 10f;
        var pos = Vector3.Transform(Vector3.Zero, obj.LocalTransform);
        viewport.CameraOffset = pos + new Vector3(0, 0, -r * 3.5f);
        viewport.CameraRotation = new Vector2(-MathF.PI, 0);
    }

    ObjectEditData GetEditData(GameObject obj, bool create = true)
    {
        if (!obj.TryGetComponent<ObjectEditData>(out var d))
        {
            if (create) {
                d = new ObjectEditData(obj);
                dirty = true;
                obj.Components.Add(d);
            }
        }
        return d;
    }

    void ChangeLoadout(GameObject obj, ObjectLoadout loadout)
    {
        var ed = GetEditData(obj);
        ed.Loadout = loadout;

        var newObj = world.NewObject(obj.SystemObject, gameData.Resources, false,
            true, loadout, ed.Archetype);
        ed.Parent = newObj;
        newObj.Components.Add(ed);
        if (selectedObject == obj)
            selectedObject = newObj;
        newObj.SetLocalTransform(obj.LocalTransform);
        obj.Unregister(world.Physics);
        world.RemoveObject(obj);
        BuildObjectList();
    }

    void ChangeArchetype(GameObject obj, Archetype archetype)
    {
        var ed = GetEditData(obj);
        ed.Archetype = archetype;
        ed.Loadout = null;
        var newObj = world.NewObject(obj.SystemObject, gameData.Resources, false,
            true, null, ed.Archetype);
        ed.Parent = newObj;
        newObj.Components.Add(ed);
        if (selectedObject == obj)
            selectedObject = newObj;
        newObj.SetLocalTransform(obj.LocalTransform);
        obj.Unregister(world.Physics);
        world.RemoveObject(obj);
        BuildObjectList();
    }
    
    void PropertyRow(string name, string value)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(name);
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(value);
        ImGui.TableNextColumn();
    }
    
    void ObjectProperties(GameObject sel)
    {
        ImGui.BeginChild("##properties");
        var ed = GetEditData(sel, false);
        if (ed != null && ImGui.Button("Reset")) {
            sel.Unregister(world.Physics);
            world.RemoveObject(sel);
            sel = world.NewObject(sel.SystemObject, gameData.Resources, false);
            selectedTransform = sel.LocalTransform;
            selectedObject = sel;
            BuildObjectList();
        }
        Controls.BeginPropertyTable("properties", true, false, true);
        PropertyRow("Nickname", sel.Nickname);
        PropertyRow("Name", ed == null 
            ? sel.Name.GetName(gameData.GameData, camera.Position)
            : gameData.Infocards.GetStringResource(ed.IdsName));
        if (ImGui.Button($"{Icons.Edit}##name"))
        {
            popups.OpenPopup(IdsSearch.SearchStrings(gameData.Infocards, gameData.Fonts, newIds => {
                GetEditData(sel).IdsName = newIds;
            }));
        }
        //Position
        var pos = Vector3.Transform(Vector3.Zero, sel.LocalTransform);
        var rot = sel.LocalTransform.GetEulerDegrees();
        PropertyRow("Position", $"{pos.X:0.00}, {pos.Y:0.00}, {pos.Z: 0.00}");
        PropertyRow("Rotation", $"{rot.X: 0.00}, {rot.Y:0.00}, {rot.Z: 0.00}");
        if (ImGui.Button("0##rot"))
        {
            //Create data if needed
            GetEditData(sel);
            sel.SetLocalTransform(Matrix4x4.CreateTranslation(pos));
            selectedTransform = sel.LocalTransform;
        }
        //Archetype
        PropertyRow("Archetype", ed == null
            ? (sel.SystemObject.Archetype?.Nickname ?? "(none)")
            : (ed.Archetype?.Nickname ?? "(none)"));
        if (ImGui.Button($"{Icons.Edit}##archetype"))
        {
            popups.OpenPopup(new ArchetypeSelection(x =>
            {
                ChangeArchetype(sel, x);
            }, ed?.Archetype ?? sel.SystemObject.Archetype, gameData));
        }
        //Loadout
        PropertyRow("Loadout", ed == null
            ? (sel.SystemObject.Loadout?.Nickname ?? "(none)")
            : (ed.Loadout?.Nickname ?? "(none)"));
        if (ImGui.Button($"{Icons.Edit}##loadout"))
        {
            popups.OpenPopup(new LoadoutSelection(x =>
            {
                ChangeLoadout(sel, x);
            }, ed != null ? ed.Loadout : sel.SystemObject.Loadout, gameData));
        }
        //Visit
        PropertyRow("Visit", VisitFlagEditor.FlagsString(ed?.Visit ?? sel.SystemObject.Visit));
        if(ImGui.Button($"{Icons.Edit}##visit"))
            popups.OpenPopup(new VisitFlagEditor(ed?.Visit ?? sel.SystemObject.Visit, x => GetEditData(sel).Visit = x));
        Controls.EndPropertyTable();
        ImGui.EndChild();
    }

    void ObjectsPanel()
    {
        ImGui.BeginChild("##objects", new Vector2(ImGui.GetWindowWidth(), ImGui.GetWindowHeight() / 2));
        foreach (var obj in objectList)
        {
            if (scrollToSelection && selectedObject == obj)
                ImGui.SetScrollHereY();
            if (ImGui.Selectable(obj.Nickname, selectedObject == obj, ImGuiSelectableFlags.AllowDoubleClick))
            {
                selectedObject = obj;
                selectedTransform = obj.LocalTransform;
                if(ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                    MoveCameraTo(obj);
            }
        }
        scrollToSelection = false;
        ImGui.EndChild();
        ImGui.Separator();
        ImGui.Text("Properties");
        ImGui.Separator();
        if(selectedObject != null)
            ObjectProperties(selectedObject);
        else
            ImGui.Text("No Object Selected");
    }

    private SystemEditData systemData;

    void MusicProp(string name, string arg, Action<string> onSet)
    {
        PropertyRow(name, arg ?? "(none)");
        if(Controls.Music(name, win, !string.IsNullOrEmpty(arg)))
            gameData.Sounds.PlayMusic(arg, 0, true);
        ImGui.TableNextColumn();
        if (ImGui.Button($"{Icons.Edit}##{name}"))
            popups.OpenPopup(new MusicSelection(onSet, name, arg, gameData, win));
        ImGui.TableNextColumn();
        if (ImGuiExt.Button($"{Icons.TrashAlt}##{name}", !string.IsNullOrEmpty(arg)))
            onSet(null);
    }

    void StarsphereProp(string name, ResolvedModel arg, Action<ResolvedModel> onSet)
    {
        var modelName = arg?.ModelFile;
        if (modelName != null)
            modelName = Path.GetFileName(modelName);
        PropertyRow(name, modelName ?? "(none)");
        if (ImGui.Button($"{Icons.Edit}##{name}"))
        {
            popups.OpenPopup(new FileSelector(name, gameData.GetDataFolder(), file =>
            {
                var modelFile = gameData.GameData.ResolveDataPath(file);
                var sourcePath = file.Replace('/', '\\');
                onSet(new ResolvedModel() {ModelFile = modelFile, SourcePath = sourcePath});
                ReloadStarspheres();
            }, FileSelector.MakeFilter(".cmp")));
        }
        ImGui.TableNextColumn();
        if (ImGuiExt.Button($"{Icons.TrashAlt}##{name}", arg != null))
        {
            onSet(null);
            ReloadStarspheres();
        }
    }

    void ReloadStarspheres()
    {
        var models = new List<RigidModel>();
        if (systemData.StarsBasic != null)
        {
            var mdl = systemData.StarsBasic.LoadFile(gameData.Resources);
            if(mdl is IRigidModelFile rm)
                models.Add(rm.CreateRigidModel(true));
        }
        if (systemData.StarsComplex != null)
        {
            var mdl = systemData.StarsComplex.LoadFile(gameData.Resources);
            if(mdl is IRigidModelFile rm)
                models.Add(rm.CreateRigidModel(true));
        }
        if (systemData.StarsNebula != null)
        {
            var mdl = systemData.StarsNebula.LoadFile(gameData.Resources);
            if(mdl is IRigidModelFile rm)
                models.Add(rm.CreateRigidModel(true));
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
            popups.OpenPopup(new ColorPicker(name, color, onSet));
        }
        ImGui.TableNextColumn();
        if(ImGui.Button($"{Icons.Edit}##{name}"))
            popups.OpenPopup(new ColorPicker(name, color, onSet));
        ImGui.PopItemWidth();
    }
    
    void SystemPanel()
    {
        if (ImGuiExt.Button("Reset All", systemData.IsDirty())) {
            systemData = new SystemEditData(curSystem);
            ReloadStarspheres();
        }
        ImGui.Separator();
        Controls.BeginPropertyTable("Props", true, false, true);
        PropertyRow("Name", gameData.GameData.GetString(systemData.IdsName));
        if (ImGui.Button($"{Icons.Edit}##name"))
        {
            popups.OpenPopup(IdsSearch.SearchStrings(gameData.Infocards, gameData.Fonts, newIds => {
               systemData.IdsName = newIds;
            }));
        }
        ColorProperty("Space Color", systemData.SpaceColor, x => systemData.SpaceColor = x);
        ColorProperty("Ambient Color", systemData.Ambient, x => systemData.Ambient = x);
        Controls.EndPropertyTable();
        ImGui.Separator();
        ImGui.Text("Music");
        Controls.BeginPropertyTable("Music", true, false, true, true, true);
        MusicProp("Space", systemData.MusicSpace, x => systemData.MusicSpace = x);
        MusicProp("Battle", systemData.MusicBattle, x => systemData.MusicBattle = x);
        MusicProp("Danger", systemData.MusicDanger, x => systemData.MusicDanger = x);
        Controls.EndPropertyTable();
        ImGui.Separator();
        ImGui.Text("Stars");
        Controls.BeginPropertyTable("Stars", true, false, true, true);
        StarsphereProp("Layer 1", systemData.StarsBasic, x => systemData.StarsBasic = x);
        StarsphereProp("Layer 2", systemData.StarsComplex, x => systemData.StarsComplex = x);
        StarsphereProp("Layer 3", systemData.StarsNebula, x => systemData.StarsNebula = x);
        Controls.EndPropertyTable();
    }
    

    private Dictionary<string, ZoneDisplayKind> zoneTypes = new Dictionary<string, ZoneDisplayKind>();
    private HashSet<string> renderZones = new HashSet<string>();
    enum ZoneDisplayKind
    {
        Normal,
        ExclusionZone,
        AsteroidField,
        Nebula
    }

    private static readonly Color4[] zoneColors = new Color4[]
    {
        Color4.White,
        Color4.Teal,
        Color4.Coral,
        Color4.LimeGreen,
    };
    
    void InitZoneList()
    {
        renderZones = new HashSet<string>();
        zoneTypes = new Dictionary<string, ZoneDisplayKind>();
        //asteroid fields
        foreach (var ast in curSystem.AsteroidFields)
        {
            zoneTypes[ast.Zone.Nickname] = ZoneDisplayKind.AsteroidField;
            foreach (var ex in ast.ExclusionZones)
            {
                zoneTypes[ex.Zone.Nickname] = ZoneDisplayKind.ExclusionZone;
            }
        }
        //nebulae
        foreach (var neb in curSystem.Nebulae)
        {
            zoneTypes[neb.Zone.Nickname] = ZoneDisplayKind.Nebula;
            foreach (var ex in neb.ExclusionZones)
            {
                zoneTypes[ex.Zone.Nickname] = ZoneDisplayKind.ExclusionZone;
            }
        }
    }
    

    private void RenderZones()
    {
        ZoneRenderer.Begin(win.RenderContext, camera);
        foreach (var z in curSystem.Zones)
        {
            if (z.Nickname != hoveredZone && !renderZones.Contains(z.Nickname)) continue;
            var zoneColor = zoneColors[(int) zoneTypes.GetValueOrDefault(z.Nickname, ZoneDisplayKind.Normal)]
                .ChangeAlpha(0.5f);
            bool inZone = z.Shape.ContainsPoint(camera.Position);
            switch (z.Shape)
            {
                case ZoneSphere sph:
                    ZoneRenderer.DrawSphere(z.Position, sph.Radius, z.RotationMatrix, zoneColor, inZone);
                    break;
                case ZoneEllipsoid epl:
                    ZoneRenderer.DrawEllipsoid(z.Position, epl.Size, z.RotationMatrix, zoneColor, inZone);
                    break;
                case ZoneCylinder cyl:
                    ZoneRenderer.DrawCylinder(z.Position, cyl.Radius, cyl.Height, z.RotationMatrix, zoneColor, inZone);
                    break;
                case ZoneRing ring:
                    ZoneRenderer.DrawRing(z.Position, ring.InnerRadius, ring.OuterRadius, ring.Height, z.RotationMatrix, zoneColor, inZone);
                    break;
                case ZoneBox box:
                    ZoneRenderer.DrawCube(z.Position, box.Size, z.RotationMatrix, zoneColor, inZone);
                    break;
            }
        }
        hoveredZone = null;
        ZoneRenderer.Finish(gameData.Resources);
    }

    public override void Update(double elapsed)
    {
        var rot = Matrix4x4.CreateRotationX(viewport.CameraRotation.Y) *
                  Matrix4x4.CreateRotationY(viewport.CameraRotation.X);
        var dir = Vector3.Transform(-Vector3.UnitZ,rot);
        var to = viewport.CameraOffset + (dir * 10);
        camera.Update(viewport.RenderWidth, viewport.RenderHeight, viewport.CameraOffset, to, rot);
        world.Update(elapsed);
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
        if (universeOpen)
        {
            ImGui.SetNextWindowSize(new Vector2(300, 300), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("Map", ref universeOpen))
            {
                ImGui.BeginTabBar("##maptabs");
                if (ImGui.BeginTabItem("Universe"))
                {
                    var szX = Math.Max(20, ImGui.GetWindowWidth());
                    var szY = Math.Max(20, ImGui.GetWindowHeight() - 50);
                    string result = UniverseMap.Draw(universeBackgroundRegistered, gameData.GameData, (int) szX,
                        (int) szY, 20);
                    if (result != null)
                    {
                        for (int i = 0; i < systems.Length; i++)
                        {
                            if (result.Equals(systems[i], StringComparison.OrdinalIgnoreCase))
                            {
                                sysIndex = i;
                                ChangeSystem();
                                break;
                            }
                        }
                    }
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("System"))
                {
                    var szX = Math.Max(20, ImGui.GetWindowWidth());
                    var szY = Math.Max(20, ImGui.GetWindowHeight() - 70);
                    systemMap.Draw((int) szX, (int) szY, 1 / 60.0f);
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
                ImGui.End();
            }
        }
    }

    private bool doChangeSystem = false;

    public override void OnHotkey(Hotkeys hk)
    {
        if (hk == Hotkeys.ChangeSystem)
            doChangeSystem = true;
    }

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
        ImGui.BeginGroup();
        TabButton("Zones", 0);
        TabButton("Objects", 1);
        TabButton("System", 2);
        TabButton("View", 3);
        
        ImGui.EndGroup();
        ImGui.SameLine();
    }
    private bool firstTab = true;
    private Matrix4x4 selectedTransform;
    private GameObject selectedObject = null;
    private bool dirty = false;

    private GameObject[] objectList;
    void BuildObjectList()
    {
        objectList = world.Objects.Where(x => x.SystemObject != null)
            .OrderBy(x => x.SystemObject.Nickname).ToArray();
    }

    public override unsafe void Draw(double elapsed)
    {
        world.RenderUpdate(elapsed);
        ImGuiHelper.AnimatingElement();
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
            if(openTabs[1]) ObjectsPanel();
            if (openTabs[2]) SystemPanel();
            if(openTabs[3]) ViewPanel();
            ImGui.EndChild();
            ImGui.NextColumn();
        }
        TabButtons();
        ImGui.BeginChild("##main");
        using (var tb = Toolbar.Begin("##toolbar", false))
        {
            var curSysName = gameData.GameData.GetString(systemData.IdsName);
            tb.TextItem($"Current System: {curSysName} ({curSystem.Nickname})");
            tb.ToggleButtonItem("Maps", ref universeOpen);
            tb.ToggleButtonItem("Infocard", ref infocardOpen);
            if (tb.ButtonItem("Change System (F6)")) {
                doChangeSystem = true;
            }
            if (tb.ButtonItem("Save", dirty || systemData.IsDirty()))
            {
                bool writeUniverse = systemData.IsUniverseDirty();
                systemData.Apply();
                foreach (var item in world.Objects.Where(x => x.SystemObject != null))
                {
                    if (item.TryGetComponent<ObjectEditData>(out var dat))
                    {
                        dat.Apply();
                        item.Components.Remove(dat);
                    }
                }
                var resolved = gameData.GameData.ResolveDataPath("universe/" + curSystem.SourceFile);
                File.WriteAllText(resolved, IniSerializer.SerializeStarSystem(curSystem));
                if (writeUniverse)
                {
                    var path = gameData.GameData.VFS.Resolve(gameData.GameData.Ini.Freelancer.UniversePath);
                    File.WriteAllText(path, IniSerializer.SerializeUniverse(gameData.GameData.AllSystems, gameData.GameData.AllBases));
                }
                dirty = false;
            }
        }
        renderer.BackgroundOverride = systemData.SpaceColor;
        renderer.SystemLighting.Ambient = systemData.Ambient;
        viewport.Begin();
        renderer.Draw(viewport.RenderWidth, viewport.RenderHeight);
        viewport.End();
        if (selectedObject != null)
        {
            var v = camera.View;
            var p = camera.Projection;
            fixed (Matrix4x4* tr = &selectedTransform)
            {
                if (ImGuizmo.Manipulate(ref v, ref p, GuizmoOperation.TRANSLATE | GuizmoOperation.ROTATE_AXIS,
                        GuizmoMode.WORLD,
                        tr))
                {
                    dirty = true;
                    if(!selectedObject.TryGetComponent<ObjectEditData>(out var _))
                        selectedObject.Components.Add(new ObjectEditData(selectedObject));
                }
            }
            selectedObject.SetLocalTransform(selectedTransform);
            viewport.SetInputsEnabled(!ImGuizmo.IsOver() && !ImGuizmo.IsUsing());
        }
        else
        {
            viewport.SetInputsEnabled(true);
        }
        ImGui.EndChild();
        DrawMaps();
        DrawInfocard();
        if(doChangeSystem) {
            ImGui.OpenPopup("Change System##" + Unique);
            sysIndex = sysIndexLoaded;
            doChangeSystem = false;
        }
        bool popupopen = true;
        if(ImGui.BeginPopupModal("Change System##" + Unique,ref popupopen, ImGuiWindowFlags.AlwaysAutoResize)) {
            ImGui.Combo("System", ref sysIndex, systems, systems.Length);
            if(ImGui.Button("Ok")) {
                ChangeSystem();
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if(ImGui.Button("Cancel")) {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
        popups.Run();
    }

    public override void Dispose()
    { 
        ImGuiHelper.DeregisterTexture(universeBackgroundTex);
        world.Dispose();
        renderer.Dispose();
        viewport.Dispose();
    }
}