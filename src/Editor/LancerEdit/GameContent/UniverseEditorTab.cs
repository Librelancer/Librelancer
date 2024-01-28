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
using LibreLancer.Graphics;
using LibreLancer.ImUI;

namespace LancerEdit;

public class UniverseEditorTab : EditorTab
{
    private MainWindow win;
    private GameDataContext gameData;
    private Texture2D universeBackgroundTex;
    private int universeBackgroundRegistered;
    private UniverseMap map;

    private PopupManager popups;

    private const string NAV_PRETTYMAP = "INTERFACE/NEURONET/NAVMAP/NEWNAVMAP/nav_prettymap.3db";
    private List<(Vector2, Vector2)> connections = new List<(Vector2, Vector2)>();


    public UniverseEditorTab(GameDataContext gameData, MainWindow win)
    {
        Title = "Universe Editor";
        this.win = win;
        this.gameData = gameData;
        var pmap = this.gameData.GameData.DataPath(NAV_PRETTYMAP);
        if (gameData.GameData.VFS.FileExists(pmap))
            gameData.Resources.LoadResourceFile(pmap);
        universeBackgroundTex = (gameData.Resources.FindTexture("fancymap.tga") as Texture2D);
        if (universeBackgroundTex != null)
            universeBackgroundRegistered = ImGuiHelper.RegisterTexture(universeBackgroundTex);
        else
            universeBackgroundRegistered = -1;
        popups = new PopupManager();
        BuildConnections();
        RefreshSystemList();
    }

    void RefreshSystemList()
    {
        allSystems = gameData.GameData.Systems.OrderBy(x => x.Nickname).Select(x => x.Nickname).ToArray();
    }

    void BuildConnections()
    {
        HashSet<string> connected = new HashSet<string>();
        connections = new List<(Vector2, Vector2)>();
        foreach (var sys in gameData.GameData.Systems) {
            foreach (var obj in sys.Objects) {
                if(obj.Dock?.Kind == DockKinds.Jump &&
                   !obj.Dock.Target.Equals(sys.Nickname, StringComparison.OrdinalIgnoreCase))
                {
                    var conn = $"{sys.Nickname};{obj.Dock.Target}".ToLowerInvariant();
                    var connOther = $"{obj.Dock.Target};{sys.Nickname}".ToLowerInvariant();
                    if (!connected.Contains(conn) &&
                        !connected.Contains(connOther))
                    {
                        connected.Add(conn);
                        var other = gameData.GameData.Systems.Get(obj.Dock.Target);
                        if(other != null)
                            connections.Add((sys.UniversePosition, other.UniversePosition));
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

    private string[] allSystems;


    private bool firstTab = true;
    void DrawSystems()
    {
        if (ImGui.Button("New System")) {
            popups.OpenPopup(new NameInputPopup(NameInputConfig.Nickname("New System", gameData.GameData.Systems.Contains), "", NewSystem));
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
        foreach (var sys in allSystems)
        {
            if (ImGui.Selectable(sys))
                OpenSystem(sys);
        }
        ImGui.EndChild();
        ImGui.NextColumn();
        var size = (int)Math.Min(ImGui.GetWindowHeight(), ImGui.GetColumnWidth()) - 10;
        var selected = UniverseMap.Draw(
            universeBackgroundRegistered,
            gameData.GameData, connections, size, size,
            25
        );
        if (!string.IsNullOrWhiteSpace(selected))
            OpenSystem(selected);
        ImGui.Columns(1);
    }

    void NewSystem(string nickname)
    {
        var systemsFolder = gameData.GameData.DataPath("UNIVERSE/SYSTEMS/");
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
            LocalFaction = gameData.GameData.Factions.First(),
            Preloads = Array.Empty<PreloadObject>(),
        };
        gameData.GameData.Systems.Add(system);
        var universePath = gameData.GameData.VFS.GetBackingFileName(gameData.GameData.Ini.Freelancer.UniversePath);
        File.WriteAllText(Path.Combine(newFolder, $"{nickname}.ini"), IniSerializer.SerializeStarSystem(system));
        File.WriteAllText(universePath, IniSerializer.SerializeUniverse(gameData.GameData.Systems, gameData.GameData.Bases));
        gameData.GameData.VFS.Refresh();
        win.AddTab(new SystemEditorTab(gameData, win, system));
        RefreshSystemList();
    }

    void OpenSystem(string nickname)
    {
        var sys = gameData.GameData.Systems.Get(nickname);
        win.AddTab(new SystemEditorTab(gameData, win, sys));
    }

    void DrawBases()
    {
        ImGui.Text("Base List");
        ImGui.Separator();
        ImGui.BeginChild("##baselist");
        foreach(var b in gameData.GameData.Bases)
            ImGui.Text(b.Nickname);
        ImGui.EndChild();
    }

    public override void Dispose()
    {
        if(universeBackgroundTex != null)
            ImGuiHelper.DeregisterTexture(universeBackgroundTex);
    }
}
