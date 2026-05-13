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
using LibreLancer.Data.Schema.Solar;
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
    private Vector2 universeBackgroundUvMin = new(0, 1);
    private Vector2 universeBackgroundUvMax = new(1, 0);
    private UniverseMap map;

    private PopupManager popups;

    private const string NAV_PRETTYMAP = "INTERFACE/NEURONET/NAVMAP/NEWNAVMAP/nav_prettymap.3db";
    private List<UniverseMap.Connection> connections = new List<UniverseMap.Connection>();


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
        //if multiuniverse ini is not null, then its a multiuniverse mod.
        if (gameData.GameData.Items.Ini.MultiUniverse?.TryGetBackgroundUv(
                "galaxy",
                out var backgroundUvMin,
                out var backgroundUvMax) == true)
        {
            universeBackgroundUvMin = backgroundUvMin;
            universeBackgroundUvMax = backgroundUvMax;
        }
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
        Dictionary<string, UniverseMap.Connection> byPair = new(StringComparer.OrdinalIgnoreCase);
        foreach (var sys in AllSystems) {
            foreach (var obj in sys.System.Objects) {
                if(obj.Dock?.Kind == DockKinds.Jump &&
                   !obj.Dock.Target.Equals(sys.System.Nickname, StringComparison.OrdinalIgnoreCase))
                {
                    var other = AllSystems.FirstOrDefault(x =>
                        x.System.Nickname.Equals(obj.Dock.Target, StringComparison.OrdinalIgnoreCase));
                    if (other == null)
                        continue;

                    var key = PairKey(sys.System.Nickname, other.System.Nickname);
                    var legal = obj.Archetype?.Type == ArchetypeType.jump_gate;
                    if (byPair.TryGetValue(key, out var existing))
                    {
                        existing.Legal |= legal;
                    }
                    else
                    {
                        byPair[key] = new UniverseMap.Connection(sys, other, legal);
                    }
                }
            }
        }

        connections = byPair.Values
            .OrderBy(x => x.Source.System.Nickname)
            .ThenBy(x => x.Target.System.Nickname)
            .ToList();
    }

    private static string PairKey(string a, string b) =>
        string.Compare(a, b, StringComparison.OrdinalIgnoreCase) <= 0
            ? $"{a}\n{b}"
            : $"{b}\n{a}";

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
        string selected = null;
        map.Draw(
            AllSystems,
            Data.GameData,
            connections,
            [],
            new Vector2(size, size),
            new UniverseMap.ViewOptions
            {
                Id = "##universeeditmap",
                Background = universeBackgroundRegistered,
                BackgroundUvMin = universeBackgroundUvMin,
                BackgroundUvMax = universeBackgroundUvMax,
                EnablePanZoom = false,
                ShowHelpText = false,
                ShowLabels = false,
                FitToSystems = false,
                EditableSystems = true,
                ConnectionThickness = 4f,
                EditableNodeSize = 10f,
                HelpText = "Double-click to open. Click+drag to move. Shift to disable snapping",
                Tooltip = x => $"{Data.GameData.GetString(x.System.IdsName)} ({x.System.Nickname})",
                OnDoubleClick = x => selected = x.System.Nickname
            });
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
        foreach (var t in win.TabControl.Tabs.OfType<SystemEditorTab>())
        {
            if (t.OriginalSystem == sys)
            {
                win.TabControl.SetSelected(t);
                return;
            }
        }
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
