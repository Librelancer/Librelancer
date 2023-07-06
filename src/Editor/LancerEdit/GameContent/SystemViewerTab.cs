using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.GameData.World;
using LibreLancer.ImUI;
using LibreLancer.Infocards;
using LibreLancer.Render;
using LibreLancer.Render.Cameras;
using LibreLancer.World;
using ModelRenderer = LibreLancer.Render.ModelRenderer;

namespace LancerEdit;

public class SystemViewerTab : GameContentTab
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
    public SystemViewerTab(GameDataContext gameData, MainWindow mw)
    {
        Title = "System Viewer";
        
        this.gameData = gameData;
        
        viewport = new Viewport3D(mw);
        viewport.EnableMSAA = false; //MSAA handled by SystemRenderer
        viewport.DefaultOffset = new Vector3(0, 0, 4);
        viewport.ModelScale = 1000f;
        viewport.Mode = CameraModes.Walkthrough;
        viewport.Background =  new Vector4(0.12f,0.12f,0.12f, 1f);
        viewport.ResetControls();
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
            //Setup UI
            InitZoneList();
            
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
    
    void ObjectProperties(GameObject sel)
    {
        ImGui.BeginChild("##properties");
        ImGui.TextUnformatted($"Nickname: {sel.Nickname}");
        var ed = GetEditData(sel, false);
        //Name
        if(ed == null)
            ImGui.TextUnformatted($"Name: {sel.Name.GetName(gameData.GameData, camera.Position)}");
        else
            ImGui.TextUnformatted($"Name: {gameData.Infocards.GetStringResource(ed.IdsName)}");
        ImGui.SameLine();
        if (ImGui.Button($"{Icons.Edit}##name"))
        {
            popups.OpenPopup(IdsSearch.SearchStrings(gameData.Infocards, gameData.Fonts, newIds => {
                GetEditData(sel).IdsName = newIds;
            }));
        }
        //Position
        var pos = Vector3.Transform(Vector3.Zero, sel.LocalTransform);
        var rot = sel.LocalTransform.GetEulerDegrees();
        ImGui.TextUnformatted($"Position: {pos.X:0.00}, {pos.Y:0.00}, {pos.Z: 0.00}");
        ImGui.TextUnformatted($"Rotation: {rot.X: 0.00}, {rot.Y:0.00}, {rot.Z: 0.00}");
        ImGui.Selectable($"Archetype: {sel.SystemObject.Archetype?.Nickname}");
        if (ImGui.IsItemHovered())
        {
            var pv = gameData.GetArchetypePreview(sel.SystemObject.Archetype);
            if (pv != -1)
            {
                ImGui.BeginTooltip();
                ImGui.Image((IntPtr) pv, new Vector2(64) * ImGuiHelper.Scale, new Vector2(0, 1),
                    new Vector2(1, 0));
                ImGui.EndTooltip();
            }
        }
        ImGui.EndChild();
    }

    void ObjectsPanel()
    {
        var oprops = selectedObject;
        if (oprops != null)
        {
            ImGui.BeginChild("##objects", new Vector2(ImGui.GetWindowWidth(), ImGui.GetWindowHeight() / 2));
        }
        foreach (var obj in world.Objects.Where(x => x.SystemObject != null))
        {
            if (ImGui.Selectable(obj.Nickname, selectedObject == obj, ImGuiSelectableFlags.AllowDoubleClick))
            {
                selectedObject = obj;
                selectedTransform = obj.LocalTransform;
                if(ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                    MoveCameraTo(obj);
            }
        }
        if (oprops != null)
        {
            ImGui.EndChild();
            ImGui.Separator();
            ImGui.Text("Properties");
            ImGui.Separator();
            ObjectProperties(oprops);
        }
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
        world.RenderUpdate(elapsed);
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
        TabButton("View", 2);
        ImGui.EndGroup();
        ImGui.SameLine();
    }
    private bool firstTab = true;
    private Matrix4x4 selectedTransform;
    private GameObject selectedObject = null;
    private bool dirty = false;

    public override unsafe void Draw()
    {
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
            if(openTabs[2]) ViewPanel();
            ImGui.EndChild();
            ImGui.NextColumn();
        }
        TabButtons();
        ImGui.BeginChild("##main");
        using (var tb = Toolbar.Begin("##toolbar", false))
        {
            var curSysName = gameData.GameData.GetString(curSystem.IdsName);
            tb.TextItem($"Current System: {curSysName} ({curSystem.Nickname})");
            tb.ToggleButtonItem("Maps", ref universeOpen);
            tb.ToggleButtonItem("Infocard", ref infocardOpen);
            if (tb.ButtonItem("Change System (F6)")) {
                doChangeSystem = true;
            }
            if (tb.ButtonItem("Save", dirty))
            {
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
                dirty = false;
            }
        }
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