using System;
using System.Collections.Generic;
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
        ZoneRenderer.Begin(win.RenderContext, gameData.Resources, camera);
        foreach (var z in curSystem.Zones)
        {
            if (z.Nickname != hoveredZone && !renderZones.Contains(z.Nickname)) continue;
            var zoneColor = zoneColors[(int) zoneTypes.GetValueOrDefault(z.Nickname, ZoneDisplayKind.Normal)]
                .ChangeAlpha(0.5f);
            switch (z.Shape)
            {
                case ZoneSphere sph:
                    ZoneRenderer.DrawSphere(z.Position, sph.Radius, z.RotationMatrix, zoneColor);
                    break;
                case ZoneEllipsoid epl:
                    ZoneRenderer.DrawEllipsoid(z.Position, epl.Size, z.RotationMatrix, zoneColor);
                    break;
                case ZoneCylinder cyl:
                    ZoneRenderer.DrawCylinder(z.Position, cyl.Radius, cyl.Height, z.RotationMatrix, zoneColor);
                    break;
                case ZoneBox box:
                    ZoneRenderer.DrawCube(z.Position, box.Size, z.RotationMatrix, zoneColor);
                    break;
            }
        }
        hoveredZone = null;
        ZoneRenderer.Finish();
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
        ImGui.EndGroup();
        ImGui.SameLine();
    }
    private bool firstTab = true;

    public override void Draw()
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
            tb.CheckItem("Nebulae", ref renderer.DrawNebulae);
            tb.CheckItem("Starspheres", ref renderer.DrawStarsphere);
            if (tb.ButtonItem("To Text")) {
                win.TextWindows.Add(new TextDisplayWindow(IniSerializer.SerializeStarSystem(curSystem), $"{curSystem.Nickname}.ini"));
            }
        }
        viewport.Begin();
        renderer.Draw(viewport.RenderWidth, viewport.RenderHeight);
        viewport.End();
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
    }

    public override void Dispose()
    { 
        ImGuiHelper.DeregisterTexture(universeBackgroundTex);
        world.Dispose();
        renderer.Dispose();
        viewport.Dispose();
    }
}