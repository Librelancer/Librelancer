#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.Goods;
using LibreLancer.Data.Schema.Solar;
using LibreLancer.ImUI;
using Base = LibreLancer.Data.GameData.World.Base;
using StarSystem = LibreLancer.Data.GameData.World.StarSystem;

namespace LancerEdit.GameContent;

public partial class TradingPlannerTab : GameContentTab
{
    private readonly GameDataContext data;
    private readonly MainWindow window;
    private readonly Dictionary<string, PlannerSystem> systems = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> anyJumpGraph = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> legalJumpGraph = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<PlannerConnection> connections = [];
    private readonly List<TradeMarketEntry> marketEntries = [];
    private readonly List<Base> allBases;
    private readonly List<ResolvedGood> commodityGoods;
    private readonly HashSet<string> dirtyMarketBases = new(StringComparer.OrdinalIgnoreCase);
    private readonly EditorUndoBuffer undoBuffer = new();
    private readonly UniverseMap tradeMap = new();
    private List<TradeCommodity> commodities = [];

    private string commodityFilter = "";
    private string routeFilter = "";
    private string mapSystemExclusion = "";
    private string commoditySaveStatus = "";
    private string? selectedCommodity;
    private ResolvedGood? newMarketGood;
    private PlannerSystem? selectedSystem;
    private Base? selectedBase;
    private TradeRoute? selectedRoute;

    private float buyerMultiplierMinimum = 3.0f;
    private float sellerMultiplierMaximum = 1.0f;
    private float rangeMultiplierMinimum = 0.0f;
    private float rangeMultiplierMaximum = 10.0f;
    private bool filterBuyerMultiplier = false;
    private bool filterSellerMultiplier = false;
    private bool filterRangeMultiplier = false;
    private bool includeJumpHoles = true;
    private int maxRoutesShown = 500;
    private int maxMapRoutes = 80;
    private float mapSystemSpacing = 40.0f;
    private float mapSectorSystemSpacing = 30.0f;
    private float mapHeight = 0;

    public TradingPlannerTab(GameDataContext gameData, MainWindow mainWindow)
    {
        Title = "Trading Planner";
        data = gameData;
        window = mainWindow;
        SaveStrategy = new TradingPlannerSaveStrategy(this);
        allBases = data.GameData.Items.Bases.OrderBy(x => x.Nickname).ToList();
        commodityGoods = data.GameData.Items.Goods
            .Where(x => x.Ini.Category == GoodCategory.Commodity && x.Ini.Price > 0)
            .OrderBy(x => x.Nickname)
            .ToList();
        newMarketGood = commodityGoods.FirstOrDefault();
        LoadMapOptions();

        BuildSystems();
        BuildConnections();
        BuildMarketEntries();
        RecalculateRoutes();
        selectedCommodity = commodities.FirstOrDefault(x => x.Routes.Count > 0)?.Nickname ??
                            commodities.FirstOrDefault()?.Nickname;
        selectedBase = allBases.FirstOrDefault();
    }

    public override void Draw(double elapsed)
    {
        DrawToolbar();

        var size = ImGui.GetContentRegionAvail();
        if (size.X <= 0 || size.Y <= 0)
            return;

        if (ImGui.BeginTable("##tradingplannerlayout", 2,
                ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable | ImGuiTableFlags.NoHostExtendY,
                size))
        {
            ImGui.TableSetupColumn("##commodities", ImGuiTableColumnFlags.WidthFixed, 280 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("##main", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            DrawCommodityList();

            ImGui.TableNextColumn();
            DrawRoutesView();

            ImGui.EndTable();
        }
    }

    public override void OnHotkey(Hotkeys hk, bool shiftPressed)
    {
        if (hk == Hotkeys.Undo && undoBuffer.CanUndo)
            undoBuffer.Undo();
        if (hk == Hotkeys.Redo && undoBuffer.CanRedo)
            undoBuffer.Redo();
    }

    private void DrawToolbar()
    {
        ImGui.Text($"{systems.Count} systems, {connections.Count} jump links, {marketEntries.Count} market entries");

        if (ImGui.Button($"{Icons.SyncAlt} Refresh"))
        {
            BuildMarketEntries();
            RecalculateRoutes();
        }

        ImGui.SameLine();
        if (ImGui.Button($"{Icons.Map} Reset Camera"))
        {
            tradeMap.ResetView();
        }

        ImGui.Separator();
    }

    private void DrawCommodityList()
    {
        var fullHeight = ImGui.GetContentRegionAvail().Y;
        var filterHeight = 190 * ImGuiHelper.Scale;
        var mapOptionsHeight = 220 * ImGuiHelper.Scale;
        var listHeight = Math.Max(140 * ImGuiHelper.Scale,
            fullHeight - filterHeight - mapOptionsHeight - (16 * ImGuiHelper.Scale));

        if (ImGui.BeginChild("##commoditylistpanel", new Vector2(0, listHeight), ImGuiChildFlags.Borders))
        {
            ImGui.TextDisabled("Commodities");
            ImGui.SetNextItemWidth(-1);
            ImGui.InputText("##commodityfilter", ref commodityFilter, 256);
            ImGui.Separator();

            foreach (var commodity in FilterCommodities())
            {
                var selected = selectedCommodity != null &&
                               selectedCommodity.Equals(commodity.Nickname, StringComparison.OrdinalIgnoreCase);
                var label = $"{commodity.Nickname}  ({commodity.Routes.Count})";
                if (ImGui.Selectable(label, selected))
                {
                    selectedCommodity = commodity.Nickname;
                    selectedRoute = commodity.Routes.FirstOrDefault();
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(
                        $"{CommodityDisplayName(commodity)}\nBase price: {commodity.BasePrice}\nMarkets: {commodity.Markets.Count}\nRoutes: {commodity.Routes.Count}");
                }
            }
        }
        ImGui.EndChild();

        DrawFilterPanel();
        DrawMapOptionsPanel();
    }

    private void DrawFilterPanel()
    {
        if (ImGui.BeginChild("##tradefilterpanel", new Vector2(0, 190 * ImGuiHelper.Scale), ImGuiChildFlags.Borders))
        {
            ImGui.TextDisabled("Route filters");
            ImGui.Separator();

            if (DrawOptionalFloatFilter("Min buyer multiplier", ref filterBuyerMultiplier,
                    ref buyerMultiplierMinimum, "minimum buyer multiplier"))
                RecalculateRoutes();

            if (DrawOptionalFloatFilter("Max seller multiplier", ref filterSellerMultiplier,
                    ref sellerMultiplierMaximum, "maximum seller multiplier"))
                RecalculateRoutes();

            var changed = false;
            changed |= ImGui.Checkbox("##rangemultenabled", ref filterRangeMultiplier);
            ImGui.SameLine();
            ImGui.Text("Range multiplier");
            ImGui.BeginDisabled(!filterRangeMultiplier);
            ImGui.SetNextItemWidth(FilterInputWidth());
            changed |= ImGui.DragFloatRange2("##rangemult", ref rangeMultiplierMinimum,
                ref rangeMultiplierMaximum, 0.05f, 0, 100, "%.2f", "%.2f");
            ImGui.EndDisabled();
            ImGui.TextDisabled("buyer and seller multipliers must both be inside this range");

            if (changed)
            {
                NormalizeMultiplierFilters();
                RecalculateRoutes();
            }
        }

        ImGui.EndChild();
    }

    private void DrawMapOptionsPanel()
    {
        if (ImGui.BeginChild("##mapoptionspanel", ImGui.GetContentRegionAvail(), ImGuiChildFlags.Borders))
        {
            ImGui.TextDisabled("Map options");
            ImGui.Separator();

            var newIncludeJumpHoles = includeJumpHoles;
            if (ImGui.Checkbox("Include jump holes", ref newIncludeJumpHoles))
            {
                includeJumpHoles = newIncludeJumpHoles;
                RecalculateRoutes();
            }

            ImGui.SetNextItemWidth(FilterInputWidth());
            maxRoutesShown = Math.Clamp(maxRoutesShown, 25, 5000);
            ImGui.InputInt("Shown routes", ref maxRoutesShown, 25, 100);
            maxRoutesShown = Math.Clamp(maxRoutesShown, 25, 5000);

            ImGui.SetNextItemWidth(FilterInputWidth());
            maxMapRoutes = Math.Clamp(maxMapRoutes, 0, 5000);
            ImGui.InputInt("Map routes", ref maxMapRoutes, 10, 50);
            maxMapRoutes = Math.Clamp(maxMapRoutes, 0, 5000);

            ImGui.SetNextItemWidth(FilterInputWidth());
            ImGui.InputFloat("System spacing", ref mapSystemSpacing, 2f, 10f, "%.0f");
            mapSystemSpacing = Math.Clamp(mapSystemSpacing, 0, 400);

            ImGui.SetNextItemWidth(FilterInputWidth());
            ImGui.InputFloat("Sector spacing", ref mapSectorSystemSpacing, 2f, 10f, "%.0f");
            mapSectorSystemSpacing = Math.Clamp(mapSectorSystemSpacing, 0, 400);

            ImGui.SetNextItemWidth(FilterInputWidth());
            ImGui.InputText("System exclusion", ref mapSystemExclusion, 128);

            if (ImGui.Button($"{Icons.Save} Save options"))
                SaveMapOptions();
        }

        ImGui.EndChild();
    }

    private static float FilterInputWidth() =>
        Math.Min(116 * ImGuiHelper.Scale, Math.Max(64 * ImGuiHelper.Scale, ImGui.GetContentRegionAvail().X * 0.46f));

    private void LoadMapOptions()
    {
        includeJumpHoles = window.Config.TradingPlannerIncludeJumpholes;
        maxRoutesShown = Math.Clamp(window.Config.TradingPlannerShownRoutes, 25, 5000);
        maxMapRoutes = Math.Clamp(window.Config.TradingPlannerMapRoutes, 0, 5000);
        mapSystemSpacing = Math.Clamp(window.Config.TradingPlannerSystemSpacing, 0, 400);
        mapSectorSystemSpacing = Math.Clamp(window.Config.TradingPlannerSectorSpacing, 0, 400);
        mapSystemExclusion = window.Config.TradingPlannerSystemExclusion ?? "";
    }

    private void SaveMapOptions()
    {
        window.Config.TradingPlannerIncludeJumpholes = includeJumpHoles;
        window.Config.TradingPlannerShownRoutes = maxRoutesShown;
        window.Config.TradingPlannerMapRoutes = maxMapRoutes;
        window.Config.TradingPlannerSystemSpacing = mapSystemSpacing;
        window.Config.TradingPlannerSectorSpacing = mapSectorSystemSpacing;
        window.Config.TradingPlannerSystemExclusion = mapSystemExclusion;
        window.Config.Save();
    }

    private static bool DrawOptionalFloatFilter(string label, ref bool enabled, ref float value, string hint)
    {
        var changed = ImGui.Checkbox($"##{label}enabled", ref enabled);
        ImGui.SameLine();
        ImGui.Text(label);
        ImGui.SameLine();
        ImGui.BeginDisabled(!enabled);
        ImGui.SetNextItemWidth(FilterInputWidth());
        changed |= ImGui.InputFloat($"##{label}value", ref value, 0.1f, 0.5f, "%.2f");
        ImGui.EndDisabled();
        ImGui.TextDisabled(hint);
        return changed;
    }

    private void DrawRoutesView()
    {
        var commodity = SelectedCommodity();
        if (commodity == null)
        {
            ImGui.TextDisabled("No commodity selected.");
            return;
        }

        ImGui.Text($"{CommodityDisplayName(commodity)}");
        ImGui.SameLine();
        ImGui.TextDisabled(
            $"Base {commodity.BasePrice} | markets {commodity.Markets.Count} | routes {commodity.Routes.Count}");

        if (selectedSystem != null)
        {
            if (ImGui.BeginTable("##routeplannerwithinspector", 2,
                    ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable | ImGuiTableFlags.NoHostExtendY,
                    ImGui.GetContentRegionAvail()))
            {
                ImGui.TableSetupColumn("##planner", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("##systeminspector", ImGuiTableColumnFlags.WidthFixed, 640 * ImGuiHelper.Scale);
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                DrawRoutePlannerArea(commodity);

                ImGui.TableNextColumn();
                DrawMarketsView();

                ImGui.EndTable();
            }

            return;
        }

        DrawRoutePlannerArea(commodity);
    }

    private void DrawRoutePlannerArea(TradeCommodity commodity)
    {
        var avail = ImGui.GetContentRegionAvail();
        var minMapHeight = 120 * ImGuiHelper.Scale;
        var maxMapHeight = Math.Max(minMapHeight, avail.Y - (170 * ImGuiHelper.Scale));
        if (mapHeight <= 0)
            mapHeight = Math.Clamp(avail.Y * 0.32f, minMapHeight, maxMapHeight);
        mapHeight = Math.Clamp(mapHeight, minMapHeight, maxMapHeight);
        DrawTradeMap(commodity, new Vector2(avail.X, mapHeight));
        DrawMapSplitter(avail.X, minMapHeight, maxMapHeight);
        ImGui.Separator();

        ImGui.SetNextItemWidth(260 * ImGuiHelper.Scale);
        ImGui.InputText("Route filter", ref routeFilter, 256);
        ImGui.SameLine();
        ImGui.TextDisabled("matches base, system, or path");

        DrawRouteTable(commodity);
    }

    private void DrawMapSplitter(float width, float minHeight, float maxHeight)
    {
        var splitterHeight = 8 * ImGuiHelper.Scale;
        ImGui.InvisibleButton("##maproutesplitter", new Vector2(width, splitterHeight));
        var hovered = ImGui.IsItemHovered();
        var active = ImGui.IsItemActive();
        var draw = ImGui.GetWindowDrawList();
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        var y = (min.Y + max.Y) * 0.5f;
        var color = ImGui.GetColorU32(active ? ImGuiCol.ButtonActive :
            hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Border);
        draw.AddLine(new Vector2(min.X, y), new Vector2(max.X, y), color, 2 * ImGuiHelper.Scale);

        if (active)
            mapHeight = Math.Clamp(mapHeight + ImGui.GetIO().MouseDelta.Y, minHeight, maxHeight);
        if (hovered || active)
            ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNS);
    }

    private void DrawRouteTable(TradeCommodity commodity)
    {
        var visibleRoutes = commodity.Routes
            .Where(RouteMatchesFilter)
            .Take(Math.Max(25, maxRoutesShown))
            .ToList();

        var tableSize = ImGui.GetContentRegionAvail();
        tableSize.Y -= 3 * ImGuiHelper.Scale;
        if (!ImGui.BeginTable("##routes", 8,
                ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable |
                ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoHostExtendY,
                tableSize))
            return;

        ImGui.TableSetupColumn("Seller", ImGuiTableColumnFlags.WidthStretch, 1.4f);
        ImGui.TableSetupColumn("Buyer", ImGuiTableColumnFlags.WidthStretch, 1.4f);
        ImGui.TableSetupColumn("Jumps", ImGuiTableColumnFlags.WidthFixed, 56 * ImGuiHelper.Scale);
        ImGui.TableSetupColumn("Distance", ImGuiTableColumnFlags.WidthFixed, 82 * ImGuiHelper.Scale);
        ImGui.TableSetupColumn("Sell x", ImGuiTableColumnFlags.WidthFixed, 70 * ImGuiHelper.Scale);
        ImGui.TableSetupColumn("Buy x", ImGuiTableColumnFlags.WidthFixed, 70 * ImGuiHelper.Scale);
        ImGui.TableSetupColumn("Profit/u", ImGuiTableColumnFlags.WidthFixed, 90 * ImGuiHelper.Scale);
        ImGui.TableSetupColumn("Path", ImGuiTableColumnFlags.WidthStretch, 1.6f);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        for (var i = 0; i < visibleRoutes.Count; i++)
        {
            var route = visibleRoutes[i];
            ImGui.PushID(i);
            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            var selected = ReferenceEquals(selectedRoute, route);
            if (ImGui.Selectable($"{route.Seller.Base.Nickname}##select", selected,
                    ImGuiSelectableFlags.SpanAllColumns))
            {
                selectedRoute = route;
                selectedBase = route.Seller.Base;
            }

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(RouteTooltip(route));

            ImGui.TableNextColumn();
            ImGui.Text(route.Buyer.Base.Nickname);
            ImGui.TableNextColumn();
            ImGui.Text(route.Hops.ToString(CultureInfo.InvariantCulture));
            ImGui.TableNextColumn();
            ImGui.Text(FormatDistance(route.Distance));
            ImGui.TableNextColumn();
            ImGui.Text($"x{route.Seller.Multiplier:0.##}");
            ImGui.TableNextColumn();
            ImGui.Text($"x{route.Buyer.Multiplier:0.##}");
            ImGui.TableNextColumn();
            ImGui.Text(FormatNumber(route.ProfitPerUnit));
            ImGui.TableNextColumn();
            ImGui.Text(RoutePathText(route));
            ImGui.PopID();
        }

        ImGui.EndTable();
    }

    private void DrawMarketsView()
    {
        if (selectedSystem == null && selectedBase?.System != null &&
            systems.TryGetValue(selectedBase.System, out var baseSystem))
        {
            selectedSystem = baseSystem;
        }

        if (selectedSystem == null)
        {
            ImGui.TextDisabled("Click a system in the map to inspect its bases and commodities.");
            return;
        }

        var bases = BasesForSelectedSystem().ToList();
        if (ImGui.SmallButton("x##closesystemmarket"))
        {
            selectedSystem = null;
            return;
        }

        ImGui.SameLine();
        ImGui.Text(SystemDisplayName(selectedSystem.System));
        ImGui.SameLine();
        ImGui.TextDisabled($"{bases.Count} bases");

        var avail = ImGui.GetContentRegionAvail();
        var basesHeight = Math.Clamp(avail.Y * 0.30f, 120 * ImGuiHelper.Scale, 240 * ImGuiHelper.Scale);
        if (ImGui.BeginChild("##systembases", new Vector2(0, basesHeight), ImGuiChildFlags.Borders))
        {
            ImGui.TextDisabled("Bases");
            ImGui.Separator();
            foreach (var b in bases)
            {
                var isSelected = selectedBase == b;
                if (ImGui.Selectable(BaseDisplayName(b), isSelected))
                    selectedBase = b;
            }
        }
        ImGui.EndChild();

        DrawSelectedBaseMarket();
    }

    private IEnumerable<Base> BasesForSelectedSystem()
    {
        if (selectedSystem == null)
            return [];

        return allBases
            .Where(x => x.System != null &&
                        x.System.Equals(selectedSystem.System.Nickname, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Nickname);
    }

    private void DrawTradeMap(TradeCommodity commodity, Vector2 size)
    {
        var selectedPath = selectedRoute?.Path
            .Select(x => x.System.Nickname)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<UniverseMap.Route> routeOverlays = [];
        foreach (var route in commodity.Routes.Take(maxMapRoutes))
        {
            if (!RouteVisibleOnMap(route))
                continue;

            routeOverlays.Add(new UniverseMap.Route(
                route.Path,
                ImGui.GetColorU32(new Vector4(1f, 0.55f, 0.10f, 0.16f)),
                2.0f * ImGuiHelper.Scale));
        }

        if (selectedRoute is { } selected && selected.Commodity == commodity && RouteVisibleOnMap(selected))
            routeOverlays.Add(new UniverseMap.Route(
                selected.Path,
                ImGui.GetColorU32(new Vector4(1f, 0.18f, 0.10f, 0.95f)),
                4.0f * ImGuiHelper.Scale));

        var mapConnections = connections
            .Select(x => new UniverseMap.Connection(x.Source, x.Target, x.Legal))
            .ToList();
        var options = new UniverseMap.ViewOptions
        {
            Id = "##trademap",
            IsVisible = x => x is PlannerSystem plannerSystem && SystemVisibleOnMap(plannerSystem),
            Label = x => SystemDisplayName(x.System),
            Tooltip = x =>
            {
                var baseCount = allBases.Count(b => b.System != null &&
                                                    b.System.Equals(x.System.Nickname,
                                                        StringComparison.OrdinalIgnoreCase));
                return $"{SystemDisplayName(x.System)}\nBases: {baseCount}";
            },
            OnClick = x =>
            {
                if (x is not PlannerSystem plannerSystem)
                    return;
                selectedSystem = plannerSystem;
                selectedBase = BasesForSelectedSystem().FirstOrDefault();
            },
            HighlightedSystems = selectedPath,
            NodeSpacing = mapSystemSpacing,
            RelatedNodeSpacing = mapSectorSystemSpacing,
            EmptyText = "No systems visible with current map exclusion."
        };

        tradeMap.Draw(systems.Values.Cast<EditorSystem>().ToList(),
            data.GameData,
            mapConnections,
            routeOverlays,
            size,
            options);
    }

    private bool SystemVisibleOnMap(PlannerSystem system)
    {
        var text = mapSystemExclusion.Trim();
        return string.IsNullOrWhiteSpace(text) ||
               !system.System.Nickname.Contains(text, StringComparison.OrdinalIgnoreCase);
    }

    private bool RouteVisibleOnMap(TradeRoute route) =>
        route.Path.All(SystemVisibleOnMap);

    private IEnumerable<TradeCommodity> FilterCommodities()
    {
        var text = commodityFilter.Trim();
        var filtered = commodities.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(text))
        {
            filtered = filtered.Where(x =>
                x.Nickname.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                CommodityDisplayName(x).Contains(text, StringComparison.OrdinalIgnoreCase));
        }

        return filtered.OrderByDescending(x => x.Routes.Count).ThenBy(x => x.Nickname);
    }

    private bool RouteMatchesFilter(TradeRoute route)
    {
        var text = routeFilter.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return true;

        return route.Seller.Base.Nickname.Contains(text, StringComparison.OrdinalIgnoreCase) ||
               route.Buyer.Base.Nickname.Contains(text, StringComparison.OrdinalIgnoreCase) ||
               route.Seller.System.System.Nickname.Contains(text, StringComparison.OrdinalIgnoreCase) ||
               route.Buyer.System.System.Nickname.Contains(text, StringComparison.OrdinalIgnoreCase) ||
               RoutePathText(route).Contains(text, StringComparison.OrdinalIgnoreCase);
    }

    private TradeCommodity? SelectedCommodity() =>
        selectedCommodity == null
            ? null
            : commodities.FirstOrDefault(x => x.Nickname.Equals(selectedCommodity, StringComparison.OrdinalIgnoreCase));

    private void BuildSystems()
    {
        systems.Clear();
        foreach (var system in data.GameData.Items.Systems)
            systems[system.Nickname] = new PlannerSystem(system, system.UniversePosition);
    }

    private void BuildConnections()
    {
        connections.Clear();
        anyJumpGraph.Clear();
        legalJumpGraph.Clear();
        Dictionary<string, PlannerConnection> byPair = new(StringComparer.OrdinalIgnoreCase);

        foreach (var source in systems.Values)
        {
            foreach (var obj in source.System.Objects)
            {
                if (obj.Dock?.Kind != DockKinds.Jump ||
                    string.IsNullOrWhiteSpace(obj.Dock.Target) ||
                    obj.Dock.Target.Equals(source.System.Nickname, StringComparison.OrdinalIgnoreCase) ||
                    !systems.TryGetValue(obj.Dock.Target, out var target))
                    continue;

                var legal = obj.Archetype?.Type == ArchetypeType.jump_gate;
                AddGraphEdge(anyJumpGraph, source.System.Nickname, target.System.Nickname);
                if (legal)
                    AddGraphEdge(legalJumpGraph, source.System.Nickname, target.System.Nickname);

                var key = PairKey(source.System.Nickname, target.System.Nickname);
                if (byPair.TryGetValue(key, out var existing))
                {
                    existing.Legal |= legal;
                }
                else
                {
                    byPair[key] = new PlannerConnection(source, target, legal);
                }
            }
        }

        connections.AddRange(byPair.Values.OrderBy(x => x.Source.System.Nickname).ThenBy(x => x.Target.System.Nickname));
    }

    private void BuildMarketEntries()
    {
        marketEntries.Clear();
        foreach (var b in data.GameData.Items.Bases)
        {
            if (b.System == null || !systems.TryGetValue(b.System, out var system))
                continue;

            var localPosition = system.System.Objects.FirstOrDefault(x =>
                x.Base?.Nickname?.Equals(b.Nickname, StringComparison.OrdinalIgnoreCase) == true)?.Position;

            foreach (var sold in b.SoldGoods)
            {
                if (sold.Good.Ini.Category != GoodCategory.Commodity || sold.Good.Ini.Price <= 0)
                    continue;

                marketEntries.Add(new TradeMarketEntry
                {
                    Base = b,
                    System = system,
                    Good = sold.Good,
                    ForSale = sold.ForSale,
                    Price = sold.Price,
                    Multiplier = sold.Price / (double)sold.Good.Ini.Price,
                    Rank = sold.Rank,
                    Rep = sold.Rep,
                    LocalPosition = localPosition
                });
            }
        }
    }

    private void RecalculateRoutes()
    {
        var previousSelectedRoute = selectedRoute;
        commodities = marketEntries
            .GroupBy(x => x.Good.Nickname, StringComparer.OrdinalIgnoreCase)
            .Select(group => new TradeCommodity
            {
                Good = group.First().Good,
                Markets = group.OrderBy(x => x.Base.Nickname).ToList()
            })
            .OrderBy(x => x.Nickname)
            .ToList();

        foreach (var commodity in commodities)
        {
            var sellers = commodity.Markets
                .Where(x => x.ForSale)
                .OrderBy(x => x.Multiplier)
                .ThenBy(x => x.Base.Nickname)
                .ToList();
            var buyers = commodity.Markets
                .OrderByDescending(x => x.Multiplier)
                .ThenBy(x => x.Base.Nickname)
                .ToList();

            foreach (var seller in sellers)
            {
                foreach (var buyer in buyers)
                {
                    if (buyer.Base == seller.Base || buyer.Multiplier <= seller.Multiplier)
                        continue;
                    if (!RoutePassesMultiplierFilters(seller.Multiplier, buyer.Multiplier))
                        continue;

                    if (!TryGetPath(seller.System, buyer.System, out var path))
                        continue;

                    var profit = (double)buyer.Price - seller.Price;
                    if (profit <= 0)
                        continue;

                    commodity.Routes.Add(new TradeRoute
                    {
                        Commodity = commodity,
                        Seller = seller,
                        Buyer = buyer,
                        Path = path,
                        Hops = Math.Max(0, path.Count - 1),
                        Distance = CalculateDistance(path, seller, buyer),
                        ProfitPerUnit = profit
                    });
                }
            }

            commodity.Routes.Sort((a, b) =>
            {
                var hops = a.Hops.CompareTo(b.Hops);
                if (hops != 0) return hops;
                var profit = -a.ProfitPerUnit.CompareTo(b.ProfitPerUnit);
                if (profit != 0) return profit;
                var seller = string.Compare(a.Seller.Base.Nickname, b.Seller.Base.Nickname, StringComparison.OrdinalIgnoreCase);
                return seller != 0
                    ? seller
                    : string.Compare(a.Buyer.Base.Nickname, b.Buyer.Base.Nickname, StringComparison.OrdinalIgnoreCase);
            });
        }

        if (previousSelectedRoute != null)
        {
            selectedRoute = commodities
                .SelectMany(x => x.Routes)
                .FirstOrDefault(x =>
                    x.Commodity.Nickname.Equals(previousSelectedRoute.Commodity.Nickname, StringComparison.OrdinalIgnoreCase) &&
                    x.Seller.Base.Nickname.Equals(previousSelectedRoute.Seller.Base.Nickname, StringComparison.OrdinalIgnoreCase) &&
                    x.Buyer.Base.Nickname.Equals(previousSelectedRoute.Buyer.Base.Nickname, StringComparison.OrdinalIgnoreCase));
        }

        if (selectedRoute == null)
            selectedRoute = SelectedCommodity()?.Routes.FirstOrDefault();
    }

    private void NormalizeMultiplierFilters()
    {
        buyerMultiplierMinimum = Math.Clamp(buyerMultiplierMinimum, 0, 100);
        sellerMultiplierMaximum = Math.Clamp(sellerMultiplierMaximum, 0, 100);
        rangeMultiplierMinimum = Math.Clamp(rangeMultiplierMinimum, 0, 100);
        rangeMultiplierMaximum = Math.Clamp(rangeMultiplierMaximum, 0, 100);
        if (rangeMultiplierMaximum < rangeMultiplierMinimum)
            (rangeMultiplierMinimum, rangeMultiplierMaximum) = (rangeMultiplierMaximum, rangeMultiplierMinimum);
    }

    private bool RoutePassesMultiplierFilters(double sellerMultiplier, double buyerMultiplier)
    {
        if (filterBuyerMultiplier && buyerMultiplier < buyerMultiplierMinimum)
            return false;
        if (filterSellerMultiplier && sellerMultiplier > sellerMultiplierMaximum)
            return false;
        if (filterRangeMultiplier &&
            (sellerMultiplier < rangeMultiplierMinimum ||
             sellerMultiplier > rangeMultiplierMaximum ||
             buyerMultiplier < rangeMultiplierMinimum ||
             buyerMultiplier > rangeMultiplierMaximum))
            return false;
        return true;
    }

    private bool TryGetPath(PlannerSystem source, PlannerSystem target, out List<PlannerSystem> path)
    {
        path = [];
        if (source == target)
        {
            path.Add(source);
            return true;
        }

        var lancerPaths = includeJumpHoles
            ? source.System.ShortestPathsAny
            : source.System.ShortestPathsLegal;
        if (lancerPaths.TryGetValue(target.System, out var existingPath))
        {
            path = existingPath
                .Select(x => systems.TryGetValue(x.Nickname, out var plannerSystem) ? plannerSystem : null)
                .Where(x => x != null)
                .ToList()!;
            if (path.Count > 0)
                return true;
        }

        return TryGetGraphPath(includeJumpHoles ? anyJumpGraph : legalJumpGraph,
            source.System.Nickname, target.System.Nickname, out path);
    }

    private bool TryGetGraphPath(
        Dictionary<string, List<string>> graph,
        string source,
        string target,
        out List<PlannerSystem> path)
    {
        path = [];
        if (!graph.ContainsKey(source) || !graph.ContainsKey(target))
            return false;

        Queue<string> queue = new();
        HashSet<string> visited = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string?> previous = new(StringComparer.OrdinalIgnoreCase);
        queue.Enqueue(source);
        visited.Add(source);
        previous[source] = null;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current.Equals(target, StringComparison.OrdinalIgnoreCase))
                break;

            foreach (var next in graph[current])
            {
                if (!visited.Add(next))
                    continue;
                previous[next] = current;
                queue.Enqueue(next);
            }
        }

        if (!previous.ContainsKey(target))
            return false;

        List<string> names = [];
        string? walker = target;
        while (walker != null)
        {
            names.Add(walker);
            walker = previous[walker];
        }

        names.Reverse();
        foreach (var name in names)
        {
            if (systems.TryGetValue(name, out var system))
                path.Add(system);
        }

        return path.Count > 0;
    }

    private double CalculateDistance(List<PlannerSystem> path, TradeMarketEntry seller, TradeMarketEntry buyer)
    {
        if (path.Count <= 1)
        {
            if (seller.LocalPosition.HasValue && buyer.LocalPosition.HasValue)
                return Vector3.Distance(seller.LocalPosition.Value, buyer.LocalPosition.Value);
            return 0;
        }

        double distance = 0;
        for (var i = 0; i < path.Count - 1; i++)
            distance += Vector2.Distance(path[i].Position, path[i + 1].Position);
        return distance;
    }

    private static void AddGraphEdge(Dictionary<string, List<string>> graph, string a, string b)
    {
        if (!graph.TryGetValue(a, out var aList))
            graph[a] = aList = [];
        if (!aList.Contains(b, StringComparer.OrdinalIgnoreCase))
            aList.Add(b);

        if (!graph.TryGetValue(b, out var bList))
            graph[b] = bList = [];
        if (!bList.Contains(a, StringComparer.OrdinalIgnoreCase))
            bList.Add(a);
    }

    private static string PairKey(string a, string b) =>
        string.Compare(a, b, StringComparison.OrdinalIgnoreCase) <= 0
            ? $"{a}\n{b}"
            : $"{b}\n{a}";

    private string BaseDisplayName(Base b)
    {
        var name = data.GameData.GetString(b.IdsName);
        return string.IsNullOrWhiteSpace(name) ? b.Nickname : $"{b.Nickname} ({name})";
    }

    private string CommodityDisplayName(TradeCommodity commodity) => CommodityDisplayName(commodity.Good);

    private string CommodityDisplayName(ResolvedGood good)
    {
        var name = data.GameData.GetString(good.Ini.IdsName);
        return string.IsNullOrWhiteSpace(name) ? good.Nickname : $"{good.Nickname} ({name})";
    }

    private string SystemDisplayName(StarSystem system)
    {
        var name = data.GameData.GetString(system.IdsName);
        return string.IsNullOrWhiteSpace(name) ? system.Nickname : $"{system.Nickname} ({name})";
    }

    private static string RoutePathText(TradeRoute route) =>
        string.Join(" -> ", route.Path.Select(x => x.System.Nickname));

    private string RouteTooltip(TradeRoute route) =>
        $"{route.Seller.Base.Nickname} ({route.Seller.System.System.Nickname}) -> {route.Buyer.Base.Nickname} ({route.Buyer.System.System.Nickname})\n" +
        $"{RoutePathText(route)}\n" +
        $"{route.Hops} jumps | x{route.Seller.Multiplier:0.##} -> x{route.Buyer.Multiplier:0.##} | profit/unit {FormatNumber(route.ProfitPerUnit)}";

    private static string FormatNumber(double value) =>
        value.ToString(value >= 100 ? "0" : "0.##", CultureInfo.InvariantCulture);

    private static string FormatDistance(double value) =>
        value == 0 ? "-" : value.ToString(value >= 100 ? "0" : "0.##", CultureInfo.InvariantCulture);

}
