// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using LibreLancer.Data;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.Solar;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Text;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Shaders;
using WattleScript.Interpreter;

namespace LibreLancer.Interface;

public readonly record struct NavmapWaypoint(Vector3 Position, int Number);

[UiLoadable]
[WattleScriptUserData]
public partial class Navmap : UiWidget
{
    private const float GridSizeDefault = 240000;
    private const float MinimumObjectIconSize = 8f;
    private const float LabelCollisionPadding = 2f;
    private const float SelectorSize = 14f;
    private const float ZoomButtonOffset = 16f;
    private const float AddWaypointButtonOffset = 16f;
    private const float BestPathButtonOffset = 16f;
    private const float ZoomedScale = 4f;
    private const float ZoomAnimationDuration = 1.5f;
    private const float FadeOutDuration = 0.35f;
    private const float FadeInDuration = 0.35f;
    private const float DragStartDelay = 0.5f;
    private const float DragStartDistance = 3f;
    private const float SectorGridSize = 16f;
    private const float SectorGridCellCenter = 0.6f;
    private const float SectorStarBorderMargin = 0.1f;
    private const float SectorStarSmallSize = 5.4f;
    private const float SectorStarMediumSize = 7.2f;
    private const float SectorStarLargeSize = 8.0f;
    private const float SectorStarExtraShrinkMaximum = 0.08f;
    private const float SectorConnectionThickness = 4f;
    private const string SelectSound = "ui_item_select";
    private const string SelectAddSound = "ui_select_add";

    private static readonly string[] GRIDNUMBERS =
    [
        "1", "2", "3", "4", "5", "6", "7", "8"
    ];

    private readonly Button addWaypointButton = new();
    private readonly Button bestPathButton = new();

    private readonly string[] GRIDLETTERS =
    [
        "A", "B", "C", "D", "E", "F", "G", "H"
    ];

    private readonly List<(DrawObject Object, RectangleF Bounds)> labelCandidates = [];
    private readonly List<RectangleF> placedLabels = [];

    private readonly Button selectorButton = new();
    private readonly Dictionary<int, UiRenderable> userWaypointDigits = [];
    private readonly List<NavmapWaypoint> userWaypoints = [];
    private readonly Button zoomInButton = new();
    private readonly Button zoomOutButton = new();
    private Action<StarSystem, Vector3>? addWaypoint;
    private Func<StarSystem, Vector3, bool>? bestPath;
    private bool draggingMap;

    private Func<uint, bool> isVisited = _ => true;
    private Vector2 lastMousePosition;

    private readonly CachedRenderString[] letterCache = new CachedRenderString[16];
    private bool loadNewSystem = false;
    private bool mouseDownOnMap;
    private Vector2 mouseDownPosition;
    private double mouseDownTime;
    private float navmapscale;

    private NavmapStyle navStyle = new();
    private StarSystem? currentDisplaySystem;

    private List<DrawObject> objects = [];
    private CachedRenderString[] objectStrings = null!;
    public float OffsetX;
    public float OffsetY;
    private StarSystem? pendingSectorSystem;
    private Func<Vector3>? playerPositionProvider;
    private Func<uint>? playerSystemProvider;
    private UiRenderable? sectorBackground;

    private readonly List<SectorConnection> sectorConnections = [];
    private InterfaceModel? sectorStarModel;

    private readonly List<SectorStar> sectorStars = [];
    private readonly Dictionary<uint, StarSystem> universeSystems = [];
    private SectorStar? selectedSectorStar;
    private Vector2? selectorMapPosition;

    private readonly Panel selectorMenu = new();
    private Vector2 startOffset;
    private float startZoom = 1f;
    private string systemName = "";
    private CachedRenderString? systemNameCache;
    private Vector2 targetOffset;
    private float targetZoom = 1f;

    private List<Tradelanes> tradelanes = [];
    private UiRenderable? userWaypointDiamond;
    private Action<StarSystem, List<NavmapWaypoint>>? userWaypointProvider;

    private readonly ViewStateMachine<SectorViewState> viewState = new(SectorViewState.System);

    private List<DrawZone> zones = [];

    public float Zoom = 1f;
    private float zoomAnimationTime = ZoomAnimationDuration;
    private bool zoomed;

    public Navmap()
    {
        selectorMenu.Children.Add(addWaypointButton);
        selectorMenu.Children.Add(bestPathButton);
        selectorMenu.Children.Add(zoomInButton);
        selectorMenu.Children.Add(zoomOutButton);
        selectorMenu.Children.Add(selectorButton);

        zoomInButton.OnClick(OnZoomIn);
        zoomOutButton.OnClick(OnZoomOut);
        addWaypointButton.OnClick(OnAddWaypoint);
        bestPathButton.OnClick(OnBestPath);
    }

    public bool AcceptInput { get; set; } = true;
    public bool SectorViewActive => viewState.Active(SectorViewState.Sector);

    public bool LetterMargin { get; set; } = false;

    public bool MapBorder { get; set; } = false;

    public string ZoomInSound { get; set; } = "hud_zoom_in";
    public string ZoomOutSound { get; set; } = "hud_zoom_out";


    private Button ActiveZoomButton => zoomed ? zoomOutButton : zoomInButton;

    public void SetVisitFunction(Func<uint, bool> isVisited)
    {
        this.isVisited = isVisited;
    }

    [WattleScriptHidden]
    public void SetUniverse(GameItemDb db)
    {
        universeSystems.Clear();
        sectorStars.Clear();
        sectorConnections.Clear();
        var visibleSystems = new Dictionary<string, StarSystem>(StringComparer.OrdinalIgnoreCase);
        foreach (var system in db.Systems)
        {
            if ((system.Visit & VisitFlags.Hidden) == VisitFlags.Hidden)
                continue;
            visibleSystems[system.Nickname] = system;
            universeSystems[system.CRC] = system;
            sectorStars.Add(CreateStar(system));
        }

        var visibleConnections = new Dictionary<string, SectorConnection>(StringComparer.OrdinalIgnoreCase);
        foreach (var sys in visibleSystems.Values)
        {
            foreach (var obj in sys.Objects)
            {
                if (obj.Dock?.Kind != DockKinds.Jump ||
                    string.IsNullOrWhiteSpace(obj.Dock.Target) ||
                    string.IsNullOrWhiteSpace(obj.Dock.Exit) ||
                    sys.Nickname.Equals(obj.Dock.Target, StringComparison.OrdinalIgnoreCase) ||
                    !visibleSystems.TryGetValue(obj.Dock.Target, out var other))
                    continue;

                var targetObject = other.Objects.FirstOrDefault(x =>
                    x.Nickname.Equals(obj.Dock.Exit, StringComparison.OrdinalIgnoreCase));
                if (targetObject == null)
                    continue;

                var key = string.Compare(sys.Nickname, other.Nickname, StringComparison.OrdinalIgnoreCase) <= 0
                    ? $"{sys.Nickname}\n{other.Nickname}"
                    : $"{other.Nickname}\n{sys.Nickname}";
                visibleConnections.TryAdd(key, new SectorConnection(
                    sys.UniversePosition,
                    other.UniversePosition,
                    sys.CRC,
                    other.CRC,
                    FLHash.CreateID(obj.Nickname),
                    FLHash.CreateID(targetObject.Nickname)));
            }
        }

        sectorConnections.AddRange(visibleConnections.Values);
    }

    [WattleScriptHidden]
    public void SetAddWaypointFunction(Action<StarSystem, Vector3>? addWaypoint)
    {
        this.addWaypoint = addWaypoint;
    }

    [WattleScriptHidden]
    public void SetBestPathFunction(Func<StarSystem, Vector3, bool>? bestPath)
    {
        this.bestPath = bestPath;
    }

    [WattleScriptHidden]
    public void SetPlayerPositionProvider(Func<Vector3>? playerPositionProvider)
    {
        this.playerPositionProvider = playerPositionProvider;
    }

    [WattleScriptHidden]
    public void SetPlayerSystemProvider(Func<uint>? playerSystemProvider)
    {
        this.playerSystemProvider = playerSystemProvider;
    }

    [WattleScriptHidden]
    public void SetUserWaypointProvider(Action<StarSystem, List<NavmapWaypoint>>? userWaypointProvider)
    {
        this.userWaypointProvider = userWaypointProvider;
        if (userWaypointProvider == null)
            userWaypoints.Clear();
    }

    private class DrawObject
    {
        public uint Hash;
        public int LabelPriority;
        public string? Name;
        public UiRenderable? Renderable;
        public float SolarRadius;
        public Vector2 XZ;
    }

    private class SectorStar
    {
        public float Phase;
        public Vector2 Position;
        public UiRenderable? Renderable;
        public float Size;
        public StarSystem System = null!;
        public InterfaceColor? Tint;
        public float TintOffset;
    }

    private readonly struct SectorConnection(
        Vector2 source,
        Vector2 target,
        uint sourceSystemHash,
        uint targetSystemHash,
        uint sourceObjectHash,
        uint targetObjectHash)
    {
        public readonly Vector2 Source = source;
        public readonly Vector2 Target = target;
        public readonly uint SourceSystemHash = sourceSystemHash;
        public readonly uint TargetSystemHash = targetSystemHash;
        public readonly uint SourceObjectHash = sourceObjectHash;
        public readonly uint TargetObjectHash = targetObjectHash;
    }

    private class DrawZone
    {
        public readonly uint Hash;
        public readonly float Sort;
        public readonly string Texture;
        public readonly Color4 Tint;
        public readonly Zone Zone;

        public DrawZone(Zone zone, Color4 tint, string texture, float sort)
        {
            Hash = FLHash.CreateID(zone.Nickname);
            Zone = zone;
            Tint = tint;
            Texture = texture;
            Sort = sort;
        }
    }

    private struct Tradelanes
    {
        public Vector2 StartXZ;
        public Vector2 EndXZ;
    }

    private enum SectorViewState
    {
        System,
        Sector
    }

    private struct ZoneVertex : IVertexType
    {
        public VertexDeclaration GetVertexDeclaration()
        {
            return new VertexDeclaration(8,
                new VertexElement(VertexSlots.Position, 2, VertexElementType.Float, false, 0));
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct NavmapParameters
    {
        public Vector4 Rectangle;
        public Vector2 Tiling;
    }
}
