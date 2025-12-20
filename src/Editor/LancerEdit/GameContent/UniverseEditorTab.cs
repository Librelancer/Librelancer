using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.Data;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Ini;
using LibreLancer.Graphics;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent;

public class EditorSystem(StarSystem system, Vector2 position)
{
    public StarSystem System = system;
    public Vector2 Position = position;
}

public class UniverseEditorTab : GameContentTab
{
    public bool Dirty = false;
    public GameDataContext Data;
    public List<EditorSystem> AllSystems;

    private MainWindow win;
    private Texture2D universeBackgroundTex;
    private ImTextureRef? universeBackgroundRegistered;
    private UniverseMap map;

    private PopupManager popups;

    private const string NAV_PRETTYMAP = "INTERFACE/NEURONET/NAVMAP/NEWNAVMAP/nav_prettymap.3db";
    private List<(EditorSystem, EditorSystem)> connections = new List<(EditorSystem, EditorSystem)>();


    public UniverseEditorTab(GameDataContext gameData, MainWindow win)
    {
        Title = "Universe Editor";
        this.win = win;
        this.Data = gameData;
        var pmap = this.Data.GameData.Items.DataPath(NAV_PRETTYMAP);
        if (gameData.GameData.VFS.FileExists(pmap))
            gameData.Resources.LoadResourceFile(pmap);
        universeBackgroundTex = (gameData.Resources.FindTexture("fancymap.tga") as Texture2D);
        if (universeBackgroundTex != null)
            universeBackgroundRegistered = ImGuiHelper.RegisterTexture(universeBackgroundTex);
        else
            universeBackgroundRegistered = null;
        popups = new PopupManager();
        map = new UniverseMap();
        map.OnChange += CalculateDirty;
        BuildSystemList();
        BuildConnections();
        SaveStrategy = new UniverseSaveStrategy() { Tab = this };
    }

    public void OnSaved() => win.OnSaved();

    public override void OnHotkey(Hotkeys hk, bool shiftPressed)
    {
        if (hk == Hotkeys.Undo && map.UndoBuffer.CanUndo)
        {
            map.UndoBuffer.Undo();
            CalculateDirty();
        }
        if (hk == Hotkeys.Redo && map.UndoBuffer.CanRedo)
        {
            map.UndoBuffer.Redo();
            CalculateDirty();
        }
    }

    void CalculateDirty()
    {
        Dirty = false;
        foreach (var s in AllSystems) {
            if (s.Position != s.System.UniversePosition)
            {
                Dirty = true;
                break;
            }
        }
    }

    void BuildSystemList()
    {
        AllSystems = Data.GameData.Items.Systems.OrderBy(x => x.Nickname).Select(x => new EditorSystem(
            x, x.UniversePosition)).ToList();
    }


    void BuildConnections()
    {
        HashSet<string> connected = new HashSet<string>();
        connections = new List<(EditorSystem, EditorSystem)>();
        foreach (var sys in AllSystems) {
            foreach (var obj in sys.System.Objects) {
                if(obj.Dock?.Kind == DockKinds.Jump &&
                   !obj.Dock.Target.Equals(sys.System.Nickname, StringComparison.OrdinalIgnoreCase))
                {
                    var conn = $"{sys.System.Nickname};{obj.Dock.Target}".ToLowerInvariant();
                    var connOther = $"{obj.Dock.Target};{sys.System.Nickname}".ToLowerInvariant();
                    if (!connected.Contains(conn) &&
                        !connected.Contains(connOther))
                    {
                        connected.Add(conn);
                        var other = AllSystems.FirstOrDefault(x =>
                            x.System.Nickname.Equals(obj.Dock.Target, StringComparison.OrdinalIgnoreCase));
                        if(other != null)
                            connections.Add((sys, other));
                    }
                }
            }
        }
    }

    public override void Draw(double elapsed)
    {
        ImGui.BeginTabBar("##universetabs");
        if (ImGui.BeginTabItem("Systems"))
        {
            DrawSystems();
            ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem("Bases"))
        {
            DrawBases();
            ImGui.EndTabItem();
        }
        ImGui.EndTabBar();
        popups.Run();
    }



    private bool firstTab = true;
    void DrawSystems()
    {
        if (ImGui.Button("New System")) {
            popups.OpenPopup(new NameInputPopup(NameInputConfig.Nickname("New System", Data.GameData.Items.Systems.Contains), "",
                x => NewSystem(x, Vector2.Zero)));
        }
        ImGui.SameLine();
        if(ImGui.Button("Refresh Connections"))
            BuildConnections();
        ImGui.Separator();
        ImGui.Columns(2);
        if (firstTab) {
            ImGui.SetColumnWidth(0, ImGui.GetContentRegionAvail().X * 0.35f);
            firstTab = false;
        }
        ImGui.BeginChild("##systems");
        foreach (var sys in AllSystems)
        {
            if (ImGui.Selectable(sys.System.Nickname))
                OpenSystem(sys.System.Nickname);
        }
        ImGui.EndChild();
        ImGui.NextColumn();
        var size = (int)Math.Min(ImGui.GetWindowHeight(), ImGui.GetColumnWidth()) - 10;
        var selected = map.Draw(
            universeBackgroundRegistered, AllSystems,
            Data.GameData, connections, size, size,
            25
        );
        if (!string.IsNullOrWhiteSpace(selected))
            OpenSystem(selected);
        ImGui.Columns(1);
    }

    void NewSystem(string nickname, Vector2 position)
    {
        var systemsFolder =
            Data.GameData.VFS.GetBackingFileName(Data.GameData.Items.Ini.Freelancer.DataPath + "/UNIVERSE/SYSTEMS");
        var newFolder = Path.Combine(systemsFolder, nickname);
        Directory.CreateDirectory(newFolder);
        var system = new StarSystem()
        {
            Nickname = nickname,
            SourceFile = $"systems\\{nickname}\\{nickname}.ini",
            CRC = CrcTool.FLModelCrc(nickname),
            BackgroundColor = Color4.Black,
            FarClip = 20000,
            NavMapScale = 1f,
            LocalFaction = Data.GameData.Items.Factions.First(),
            Preloads = Array.Empty<PreloadObject>(),
            UniversePosition = position,
        };
        Data.GameData.Items.Systems.Add(system);
        var universePath = Data.GameData.VFS.GetBackingFileName(Data.GameData.Items.Ini.Freelancer.UniversePath);
        using (var stream = File.Create(Path.Combine(newFolder, $"{nickname}.ini")))
        {
            var sections = IniSerializer.SerializeStarSystem(system);
            IniWriter.WriteIni(stream, sections);
        }
        IniWriter.WriteIniFile(universePath, IniSerializer.SerializeUniverse(Data.GameData.Items.Systems, Data.GameData.Items.Bases));
        Data.GameData.VFS.Refresh();
        win.AddTab(new SystemEditorTab(Data, win, system));
        AllSystems.Add(new EditorSystem(system, system.UniversePosition));
        AllSystems.Sort((x, y) => string.Compare(x.System.Nickname, y.System.Nickname, StringComparison.Ordinal));
    }

    void OpenSystem(string nickname)
    {
        var sys = Data.GameData.Items.Systems.Get(nickname);
        win.AddTab(new SystemEditorTab(Data, win, sys));
    }

    void DrawBases()
    {
        ImGui.Text("Base List");
        ImGui.Separator();
        ImGui.BeginChild("##baselist");
        foreach(var b in Data.GameData.Items.Bases)
            ImGui.Text(b.Nickname);
        ImGui.EndChild();
    }

    public override void Dispose()
    {
        if(universeBackgroundTex != null)
            ImGuiHelper.DeregisterTexture(universeBackgroundTex);
    }
}
