using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
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
        
        systems = gameData.GameData.Systems.Select(x => x.Nickname).OrderBy(x => x).ToArray();

        this.win = mw;
        
        ChangeSystem();
    }

    private bool scrollToSelection = false;

    private void ViewportOnDoubleClicked(Vector2 pos)
    {
        if (openTabs[1])
        {
            var sel = world.GetSelection(camera, null, pos.X, pos.Y, viewport.RenderWidth, viewport.RenderHeight);
            if (ShouldAddSecondary())
            {
                if(sel != null && !selection.Contains(sel))
                    selection.Add(sel);
            }
            else
            {
                SelectSingle(sel);
                scrollToSelection = true;
            }
        }
    }

    void ChangeSystem()
    {
        if (sysIndex != sysIndexLoaded)
        {
            SelectSingle(null);
            if (world != null)
            {
                world.Renderer.Dispose();
                world.Dispose();
            }
            //Load system
            renderer = new SystemRenderer(camera, gameData.Resources, win);
            world = new GameWorld(renderer, null);
            curSystem = gameData.GameData.Systems.Get(systems[sysIndex]);
            systemInfocard = gameData.GameData.GetInfocard(curSystem.IdsInfo, gameData.Fonts);
            if (icard != null) icard.SetInfocard(systemInfocard);
            gameData.GameData.LoadAllSystem(curSystem);
            world.LoadSystem(curSystem, gameData.Resources, false);
            systemMap.SetObjects(curSystem);
            sysIndexLoaded = sysIndex;
            renderer.PhysicsHook = RenderZones;
            renderer.GridHook = RenderGrid;
            systemData = new SystemEditData(curSystem);
            //Setup UI
            InitZoneList();
            BuildObjectList();
        }
    }

    private bool renderGrid = false;
    private void RenderGrid()
    {
        if (!renderGrid) return;
        var cpos = camera.Position;
        var y = Math.Abs(cpos.Y);
        if (y <= 100) y = 100;
        
        GridRender.Draw(win.RenderContext, GridRender.DistanceScale(y), win.Config.GridColor,camera.ZRange.X, camera.ZRange.Y);
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

    bool IsPrimarySelection(GameObject obj) =>
        selection.Count > 0 && selection[0] == obj;
    
    void ChangeLoadout(GameObject obj, ObjectLoadout loadout)
    {
        var ed = GetEditData(obj);
        ed.Loadout = loadout;

        var newObj = world.NewObject(obj.SystemObject, gameData.Resources, false,
            true, loadout, ed.Archetype);
        ed.Parent = newObj;
        newObj.Components.Add(ed);
        if (IsPrimarySelection(obj))
            SelectSingle(newObj);
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
        if (IsPrimarySelection(obj))
            SelectSingle(newObj);
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

    void FactionRow(string name, Faction f, Action<Faction> onSet)
    {
        PropertyRow(name, f == null ? "(none)" : $"{f.Nickname} ({gameData.Infocards.GetStringResource(f.IdsName)})");
        if (ImGui.Button($"{Icons.Edit}##{name}"))
            popups.OpenPopup(new FactionSelection(onSet, name, f, gameData));
    }
    
    void BaseRow(string name, Base f, Action<Base> onSet, string message = null)
    {
        PropertyRow(name, f == null ? "(none)" : $"{f.Nickname} ({gameData.Infocards.GetStringResource(f.IdsName)})");
        if (ImGui.Button($"{Icons.Edit}##{name}"))
            popups.OpenPopup(new BaseSelection(onSet, name, message, f, gameData));
    }

    string DockDescription(DockAction a)
    {
        if (a == null) return "(none)";
        var sb = new StringBuilder();
        sb.AppendLine(a.Kind.ToString());
        if (a.Kind == DockKinds.Jump)
        {
            var sys = gameData.GameData.Systems.Get(a.Target);
            var sname = sys == null ? "INVALID" : gameData.Infocards.GetStringResource(sys.IdsName);
            sb.AppendLine($"{a.Target} ({sname})");
            sb.AppendLine($"{a.Exit}");
        }
        if (a.Kind == DockKinds.Base)
        {
            var b = gameData.GameData.Bases.Get(a.Target);
            var bname = b == null ? "INVALID" : gameData.Infocards.GetStringResource(b.IdsName);
            sb.AppendLine($"{a.Target} ({bname})");
        }
        return sb.ToString();
    }
    void DockRow(DockAction act, Archetype a, Action<DockAction> onSet)
    {
        PropertyRow("Dock", DockDescription(act));
        if (ImGui.Button($"{Icons.Edit}##dock"))
        {
            popups.OpenPopup(
                new DockActionSelection(
                    onSet, act,  a,
                    objectList.Select(x => x.SystemObject.Nickname).ToArray(),
                    gameData
                )
            );
        }
    }
    
    void ObjectProperties(GameObject sel)
    {
        ImGui.BeginChild("##properties");
        var ed = GetEditData(sel, false);
        if (ImGuiExt.Button("Reset", ed != null && !ed.IsNewObject)) {
            sel.Unregister(world.Physics);
            world.RemoveObject(sel);
            sel = world.NewObject(sel.SystemObject, gameData.Resources, false);
            selectedTransform = sel.LocalTransform;
            SelectSingle(sel);
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
        FactionRow("Reputation", ed == null ? sel.SystemObject.Reputation : ed.Reputation, x => GetEditData(sel).Reputation = x);
        Controls.EndPropertyTable();
        Controls.BeginPropertyTable("base and docking",true,false,true);
        BaseRow(
            "Base", 
            ed == null ? sel.SystemObject.Base : ed.Base, 
            x => GetEditData(sel).Base = x,
            "This is the base entry used when linking infocards.\nDocking requires setting the dock action"
            );
        DockRow(
            ed == null ? sel.SystemObject.Dock : ed.Dock, 
            ed == null ? sel.SystemObject.Archetype : ed.Archetype,
            x => GetEditData(sel).Dock = x
            );
        Controls.EndPropertyTable();
        ImGui.EndChild();
    }

    void CreateObject(string nickname, Archetype archetype)
    {
        var rot = Matrix4x4.CreateRotationX(viewport.CameraRotation.Y) *
                  Matrix4x4.CreateRotationY(viewport.CameraRotation.X);
        var dir = Vector3.Transform(-Vector3.UnitZ,rot);
        var to = viewport.CameraOffset + (dir * 50);
        var sysobj = new SystemObject()
        {
            Nickname = nickname,
            IdsInfo = Array.Empty<int>(),
            Archetype = archetype,
            Position = to,
        };
        var o = world.NewObject(sysobj, gameData.Resources, false);
        GetEditData(o).IsNewObject = true;
        SelectSingle(o);
        BuildObjectList();
        scrollToSelection = true;
        dirty = true;
    }


    private List<SystemObject> toRemove = new List<SystemObject>();
    void DeleteObject(GameObject go)
    {
        if (selection.Contains(go))
            selection.Remove(go);
        var ed = GetEditData(go);
        if(ed == null || !ed.IsNewObject)
            toRemove.Add(go.SystemObject);
        go.Unregister(world.Physics);
        world.RemoveObject(go);
        BuildObjectList();
    }
    void RestoreObject(SystemObject o)
    {
        toRemove.Remove(o);
        world.NewObject(o, gameData.Resources, false);
        BuildObjectList();
    }
    
    bool ShouldAddSecondary() => selection.Count > 0 && (win.Keyboard.IsKeyDown(Keys.LeftShift) ||
                                                         win.Keyboard.IsKeyDown(Keys.RightShift));
    private float h1 = 200, h2 = 200;
    void ObjectsPanel()
    {
        var totalH = ImGui.GetWindowHeight();
        ImGuiExt.SplitterV(2f, ref h1, ref h2, 15 * ImGuiHelper.Scale, 60 * ImGuiHelper.Scale, -1);
        h1 = totalH - h2 - 24f * ImGuiHelper.Scale;
        ImGui.BeginChild("##objects", new Vector2(ImGui.GetWindowWidth(), h1));
        if (ImGui.Button("New Object"))
        {
            popups.OpenPopup(new NewObjectPopup(gameData, world, CreateObject));
        }
        ImGui.SameLine();
        if (ImGuiExt.Button("Restore Object", toRemove.Count > 0))
            ImGui.OpenPopup("Restore");
        if (ImGui.BeginPopupContextItem("Restore"))
        {
            for (int i = 0; i < toRemove.Count; i++)
                if (ImGui.MenuItem(toRemove[i].Nickname))
                    RestoreObject(toRemove[i]);
            ImGui.EndPopup();
        }
        ImGui.BeginChild("objscroll");
        foreach (var obj in objectList)
        {
            bool isPrimary = IsPrimarySelection(obj);
            bool isSelected = selection.Contains(obj);
            bool addSecondary = ShouldAddSecondary();
            if (scrollToSelection && isPrimary)
                ImGui.SetScrollHereY();
            if (isSelected && !isPrimary)
                ImGui.PushStyleColor(ImGuiCol.Header, new Color4(120, 83, 101, 255));
            if(addSecondary && !isPrimary)
                ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Color4(156, 107, 131, 255));
            if (ImGui.Selectable(obj.Nickname, isSelected, ImGuiSelectableFlags.AllowDoubleClick))
            {
                if (!selection.Contains(obj)) {
                    if(selection.Count > 0 && (win.Keyboard.IsKeyDown(Keys.LeftShift) ||
                       win.Keyboard.IsKeyDown(Keys.RightShift)))
                        selection.Add(obj);
                    else
                        SelectSingle(obj);
                }
                if(ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                    MoveCameraTo(obj);
            }
            if(isSelected && !isPrimary)
                ImGui.PopStyleColor();
            if(addSecondary && !isPrimary)
                ImGui.PopStyleColor();
            if (ImGui.BeginPopupContextItem(obj.Nickname))
            {
                if (ImGui.MenuItem("Select with Children"))
                {
                    SelectSingle(obj);
                    foreach (var c in objectList)
                    {
                        if (obj.Nickname.Equals(c.SystemObject.Parent, StringComparison.OrdinalIgnoreCase))
                            selection.Add(c);
                    }
                }
                if(ImGui.MenuItem("Delete"))
                    DeleteObject(obj);
                ImGui.EndPopup();
            }
        }
        scrollToSelection = false;
        ImGui.EndChild();
        ImGui.EndChild();
        ImGui.BeginChild("##properties", new Vector2(ImGui.GetWindowWidth(), h2));
        ImGui.Text("Properties");
        ImGui.Separator();
        if(selection.Count == 1)
            ObjectProperties(selection[0]);
        else if (selection.Count > 0) {
            ImGui.Text("Multiple objects selected");
            bool canReset = false;
            foreach (var obj in selection) {
                if (GetEditData(obj, false) != null) {
                    canReset = true;
                    break;
                }
            }
            if (ImGuiExt.Button("Reset All", canReset)) {
                for (int i = 0; i < selection.Count; i++)
                {
                    var sel = selection[i];
                    sel.Unregister(world.Physics);
                    world.RemoveObject(sel);
                    sel = world.NewObject(sel.SystemObject, gameData.Resources, false);
                    if(i == 0)
                        selectedTransform = sel.LocalTransform;
                    selection[i] = sel;
                }
                BuildObjectList();
            }
        }
        else
            ImGui.Text("No Object Selected");
        ImGui.EndChild();
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
        PropertyRow("Name", gameData.Infocards.GetStringResource(systemData.IdsName));
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
    private List<GameObject> selection = new List<GameObject>();
    private bool dirty = false;

    void SelectSingle(GameObject obj)
    {
        selectedTransform = obj?.LocalTransform ?? Matrix4x4.Identity;
        if (selection.Count > 0)
            selection = new List<GameObject>();
        if(obj != null)
            selection.Add(obj);
    }
    
    private GameObject[] objectList;
    void BuildObjectList()
    {
        objectList = world.Objects.Where(x => x.SystemObject != null)
            .OrderBy(x => x.SystemObject.Nickname).ToArray();
    }

    void SaveSystem()
    {
        bool writeUniverse = systemData.IsUniverseDirty();
        systemData.Apply();
        foreach (var item in world.Objects.Where(x => x.SystemObject != null))
        {
            if (item.TryGetComponent<ObjectEditData>(out var dat))
            {
                dat.Apply();
                if (dat.IsNewObject) {
                    curSystem.Objects.Add(item.SystemObject);
                }
                item.Components.Remove(dat);
            }
        }
        foreach (var o in toRemove)
            curSystem.Objects.Remove(o);
        toRemove = new List<SystemObject>();
        var resolved = gameData.GameData.ResolveDataPath("universe/" + curSystem.SourceFile);
        File.WriteAllText(resolved, IniSerializer.SerializeStarSystem(curSystem));
        if (writeUniverse)
        {
            var path = gameData.GameData.VFS.Resolve(gameData.GameData.Ini.Freelancer.UniversePath);
            File.WriteAllText(path, IniSerializer.SerializeUniverse(gameData.GameData.Systems, gameData.GameData.Bases));
        }
        dirty = false;
    }
    

    public override unsafe void Draw(double elapsed)
    {
        var curSysName = gameData.Infocards.GetStringResource(systemData.IdsName);
        Title = $"{curSysName} ({curSystem.Nickname})";
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
        ImGui.SameLine();
        using (var tb = Toolbar.Begin("##toolbar", false))
        {
            tb.CheckItem("Grid", ref renderGrid);
            tb.TextItem($"Camera Position: {camera.Position}");
            tb.ToggleButtonItem("Maps", ref universeOpen);
            tb.ToggleButtonItem("Infocard", ref infocardOpen);
            if (tb.ButtonItem("Change System (F6)")) {
                doChangeSystem = true;
            }
            if (tb.ButtonItem("Save", dirty || systemData.IsDirty()))
                SaveSystem();
        }
        renderer.BackgroundOverride = systemData.SpaceColor;
        renderer.SystemLighting.Ambient = systemData.Ambient;
        viewport.Begin();
        renderer.Draw(viewport.RenderWidth, viewport.RenderHeight);
        viewport.End();
        if (selection.Count > 0)
        {
            var v = camera.View;
            var p = camera.Projection;
            fixed (Matrix4x4* tr = &selectedTransform)
            {
                Matrix4x4 delta;
                if (ImGuizmo.Manipulate(ref v, ref p, GuizmoOperation.TRANSLATE | GuizmoOperation.ROTATE_AXIS,
                        GuizmoMode.WORLD,
                        tr, &delta))
                {
                    dirty = true;
                    GetEditData(selection[0]);
                    for (int i = 1; i < selection.Count; i++)
                    {
                        selection[i].SetLocalTransform(selection[i].LocalTransform * delta);
                        GetEditData(selection[i]);
                    }
                }
            }
            selection[0].SetLocalTransform(selectedTransform);
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