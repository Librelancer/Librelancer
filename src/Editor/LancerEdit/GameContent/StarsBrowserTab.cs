using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LancerEdit.GameContent.Stars;
using LibreLancer;
using LibreLancer.Data.GameData.Archetypes;
using LibreLancer.Data.GameData.World;
using LibreLancer.ImUI;
using LibreLancer.Data.Schema.Solar;

namespace LancerEdit.GameContent;

public class StarsBrowserTab : GameContentTab
{
    private readonly MainWindow win;
    private readonly GameDataContext context;
    private readonly StarPreviewRenderer previewRenderer;
    private readonly Sun[] fullList;
    private readonly Dictionary<string, StarUsage[]> usagesByStar;
    private Sun[] displayList;
    private Sun selected;
    private string filterText = "";
    private int usageFilter;
    private bool usedFirst = true;

    private static readonly string[] UsageModes =
    [
        "All",
        "Used only",
        "Unused only"
    ];

    public StarsBrowserTab(MainWindow win, GameDataContext context)
    {
        this.win = win;
        this.context = context;
        previewRenderer = new StarPreviewRenderer(context);
        fullList = context.GameData.Items.Stars.OrderBy(x => x.Nickname).ToArray();
        usagesByStar = BuildUsageLookup(context);
        displayList = SortStars(fullList);
        selected = displayList.FirstOrDefault();
        Title = "Stars Browser";
    }

    public override unsafe void Draw(double elapsed)
    {
        DrawToolbar();
        ImGui.Separator();

        var avail = ImGui.GetContentRegionAvail();
        var leftWidth = Math.Clamp(avail.X * 0.58f, 360 * ImGuiHelper.Scale, Math.Max(360 * ImGuiHelper.Scale, avail.X - 300 * ImGuiHelper.Scale));
        ImGui.BeginChild("##starsList", new Vector2(leftWidth, 0), ImGuiChildFlags.Borders);
        DrawGrid();
        ImGui.EndChild();

        ImGui.SameLine();
        ImGui.BeginChild("##starDetails", new Vector2(0, 0), ImGuiChildFlags.Borders);
        DrawSelected();
        ImGui.EndChild();
    }

    void DrawToolbar()
    {
        ImGui.SetNextItemWidth(300 * ImGuiHelper.Scale);
        if (ImGui.InputTextWithHint("##starSearch", "Search stars", ref filterText, 250))
        {
            ApplyFilter();
        }
        ImGui.SameLine();
        ImGui.TextDisabled($"{displayList.Length} / {fullList.Length} stars");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(120 * ImGuiHelper.Scale);
        if (ImGui.Combo("Usage", ref usageFilter, UsageModes, UsageModes.Length))
            ApplyFilter();
        ImGui.SameLine();
        if (ImGui.Checkbox("Used first", ref usedFirst))
            ApplyFilter();
        ImGui.SameLine();
        if (ImGui.Button("Refresh previews"))
        {
            StarThumbnailStore.Clear(context);
            ImGuiHelper.AnimatingElement();
        }
    }

    void ApplyFilter()
    {
        IEnumerable<Sun> stars = fullList;

        if (!string.IsNullOrWhiteSpace(filterText))
            stars = stars.Where(x => x.Nickname.Contains(filterText, StringComparison.OrdinalIgnoreCase));

        stars = usageFilter switch
        {
            1 => stars.Where(IsUsed),
            2 => stars.Where(x => !IsUsed(x)),
            _ => stars
        };

        displayList = SortStars(stars);

        if (selected == null || !displayList.Contains(selected))
            selected = displayList.FirstOrDefault();
    }

    unsafe void DrawGrid()
    {
        if (displayList.Length == 0)
        {
            ImGui.TextDisabled("No stars found");
            return;
        }

        var availableWidth = ImGui.GetContentRegionAvail().X;
        var desiredCellWidth = 150 * ImGuiHelper.Scale;
        var columns = Math.Max(1, (int)(availableWidth / desiredCellWidth));
        var cellWidth = MathF.Max(96 * ImGuiHelper.Scale, availableWidth / columns);
        var thumb = MathF.Min(110 * ImGuiHelper.Scale, MathF.Max(72 * ImGuiHelper.Scale, cellWidth - 18 * ImGuiHelper.Scale));
        if (!ImGui.BeginTable("##starsGrid", columns, ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.PadOuterX))
            return;

        var clipper = new ImGuiListClipper();
        var rows = (displayList.Length + columns - 1) / columns;
        clipper.Begin(rows);
        while (clipper.Step())
        {
            for (int row = clipper.DisplayStart; row < clipper.DisplayEnd; row++)
            {
                ImGui.TableNextRow();
                for (int col = 0; col < columns; col++)
                {
                    var index = row * columns + col;
                    ImGui.TableNextColumn();
                    if (index >= displayList.Length)
                        continue;

                    var sun = displayList[index];
                    ImGui.PushID(sun.Nickname);
                    if (SunButton(sun, new Vector2(thumb), sun == selected))
                    {
                        selected = sun;
                        if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                            OpenViewer(sun);
                    }

                    var label = sun.Nickname;
                    if (label.Length > 22)
                        label = label.Substring(0, 19) + "...";
                    ImGui.TextWrapped(label);
                    ImGui.TextDisabled($"r {sun.Radius:0.##}");
                    var usageCount = UsageCount(sun);
                    ImGui.TextDisabled(usageCount > 0 ? $"used {usageCount}" : "unused");
                    ImGui.PopID();
                }
            }
        }
        ImGui.EndTable();
    }

    unsafe bool SunButton(Sun sun, Vector2 size, bool isSelected)
    {
        var clicked = ImGui.InvisibleButton("##sun", size);
        var hovered = ImGui.IsItemHovered();
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        var drawList = ImGui.GetWindowDrawList();
        DrawCardPreview(sun, min, max, drawList);
        var border = isSelected
            ? ImGui.GetColorU32(ImGuiCol.ButtonActive)
            : hovered
                ? ImGui.GetColorU32(ImGuiCol.ButtonHovered)
                : ImGui.GetColorU32(ImGuiCol.Border);
        drawList.AddRect(min, max, border);

        if (hovered)
        {
            ImGui.BeginTooltip();
            ImGui.Text(sun.Nickname);
            ImGui.Text($"Radius: {sun.Radius:0.##}");
            ImGui.Text("Preview: stararch.ini billboard");
            ImGui.EndTooltip();
        }

        return clicked;
    }

    unsafe void DrawCardPreview(Sun sun, Vector2 min, Vector2 max, ImDrawListPtr drawList)
    {
        var previewState = StarPreviewState.Get(context, sun);
        var rect = new Rectangle(
            (int)min.X,
            (int)min.Y,
            Math.Max(1, (int)(max.X - min.X)),
            Math.Max(1, (int)(max.Y - min.Y)));
        drawList.AddCallback((_, cmd) =>
        {
            win.RenderContext.PushScissor(ImGuiHelper.GetClipRect(cmd), false);
            previewRenderer.Render(sun, StarPreviewRenderer.PreviewBackground, win.RenderContext,
                rect, previewState.Zoom, 0, previewState.ViewOffset);
            win.RenderContext.PopScissor();
        }, IntPtr.Zero);
    }

    unsafe void DrawSelected()
    {
        if (selected == null)
        {
            ImGui.TextDisabled("No star selected");
            return;
        }

        ImGui.Text(selected.Nickname);
        ImGui.SameLine();
        if (ImGui.Button("Open Preview"))
            OpenViewer(selected);
        ImGui.Spacing();

        var previewSize = MathF.Min(260 * ImGuiHelper.Scale, MathF.Max(120 * ImGuiHelper.Scale, ImGui.GetContentRegionAvail().X * 0.42f));
        ImGui.BeginGroup();
        SunButton(selected, new Vector2(previewSize), true);
        ImGui.EndGroup();

        ImGui.SameLine();
        ImGui.BeginGroup();

        var source = context.GameData.Items.Ini.Stars?.Stars
            .FirstOrDefault(x => x.Nickname.Equals(selected.Nickname, StringComparison.OrdinalIgnoreCase));

        if (source != null)
            DrawIniDetails(source);
        else
            ImGui.TextDisabled("Source stararch.ini entry not available");

        DrawUsageDetails(selected);
        ImGui.EndGroup();

        DrawRuntimeDetails(selected);
    }

    void DrawIniDetails(Star source)
    {
        if (!ImGui.BeginTable("##starDetailsTable", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg))
            return;

        DetailRow("Nickname", source.Nickname);
        DetailRow("Radius", source.Radius.ToString("0.####"));
        DetailRow("Star Glow", source.StarGlow);
        DetailRow("Star Center", source.StarCenter);
        DetailRow("Spines", source.Spines);
        DetailRow("Lens Flare", source.LensFlare);
        DetailRow("Lens Glow", source.LensGlow);
        DetailRow("Intensity Fade In", source.IntensityFadeIn.ToString());
        DetailRow("Intensity Fade Out", source.IntensityFadeOut.ToString());
        DetailRow("Zone Occlusion Fade In", source.ZoneOcclusionFadeIn?.ToString("0.####"));
        DetailRow("Zone Occlusion Fade Out", source.ZoneOcclusionFadeOut?.ToString("0.####"));

        ImGui.EndTable();
    }

    void DrawUsageDetails(Sun sun)
    {
        ImGui.Spacing();
        ImGui.SeparatorText("Usage");

        if (!usagesByStar.TryGetValue(sun.Nickname, out var usages) || usages.Length == 0)
        {
            ImGui.TextDisabled("Not used by any system object");
            return;
        }

        ImGui.TextDisabled($"{usages.Length} object(s)");
        var tableFlags = ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg;
        var tableSize = Vector2.Zero;
        if (usages.Length > 6)
        {
            tableFlags |= ImGuiTableFlags.ScrollY;
            tableSize = new Vector2(0, 170 * ImGuiHelper.Scale);
        }

        if (!ImGui.BeginTable("##starUsageTable", 3, tableFlags, tableSize))
            return;

        ImGui.TableSetupColumn("System");
        ImGui.TableSetupColumn("Object");
        ImGui.TableSetupColumn("Archetype");
        ImGui.TableHeadersRow();

        foreach (var usage in usages)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextWrapped(DisplayName(usage.SystemNickname, usage.SystemName));
            ImGui.TableNextColumn();
            ImGui.TextWrapped(DisplayName(usage.ObjectNickname, usage.ObjectName));
            ImGui.TableNextColumn();
            ImGui.TextWrapped(usage.ArchetypeNickname);
        }

        ImGui.EndTable();
    }

    void DrawRuntimeDetails(Sun sun)
    {
        ImGui.Spacing();
        ImGui.SeparatorText("Rendered Layers");

        if (!ImGui.BeginTable("##starRuntimeTable", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg))
            return;

        var usage = PrimaryUsage(sun);
        DetailRow("Preview Source", "stararch.ini billboard");
        DetailRow("Preview Object", usage != null ? DisplayName(usage.ObjectNickname, usage.ObjectName) : "(none)");
        DetailRow("Visual Radius", StarPreviewRenderer.GetVisualRadius(sun).ToString("0.####"));
        DetailRow("Glow Texture", sun.GlowSprite);
        DetailRow("Glow Scale", sun.GlowScale.ToString("0.####"));
        DetailRow("Glow Inner", FormatColor(sun.GlowColorInner));
        DetailRow("Glow Outer", FormatColor(sun.GlowColorOuter));
        DetailRow("Center Texture", sun.CenterSprite);
        DetailRow("Center Scale", sun.CenterScale.ToString("0.####"));
        DetailRow("Center Inner", FormatColor(sun.CenterColorInner));
        DetailRow("Center Outer", FormatColor(sun.CenterColorOuter));
        DetailRow("Spines Texture", sun.SpinesSprite);
        DetailRow("Spines Scale", sun.SpinesScale.ToString("0.####"));
        DetailRow("Spines Count", (sun.Spines?.Count ?? 0).ToString());

        ImGui.EndTable();
    }

    static string FormatColor(Color3f color) =>
        $"{color.R:0.###}, {color.G:0.###}, {color.B:0.###}";

    Sun[] SortStars(IEnumerable<Sun> stars) =>
        (usedFirst
            ? stars.OrderByDescending(IsUsed).ThenBy(x => x.Nickname)
            : stars.OrderBy(IsUsed).ThenBy(x => x.Nickname)).ToArray();

    bool IsUsed(Sun sun) => UsageCount(sun) > 0;

    int UsageCount(Sun sun) =>
        usagesByStar.TryGetValue(sun.Nickname, out var usages) ? usages.Length : 0;

    static Dictionary<string, StarUsage[]> BuildUsageLookup(GameDataContext context)
    {
        var usages = new Dictionary<string, List<StarUsage>>(StringComparer.OrdinalIgnoreCase);
        foreach (var system in context.GameData.Items.Systems)
        {
            foreach (var obj in system.Objects)
            {
                if (obj.Star == null)
                    continue;

                if (!usages.TryGetValue(obj.Star.Nickname, out var starUsages))
                {
                    starUsages = [];
                    usages[obj.Star.Nickname] = starUsages;
                }

                starUsages.Add(new StarUsage(
                    system.Nickname,
                    ResolveName(context, system.IdsName),
                    obj.Nickname,
                    ResolveName(context, obj.IdsName),
                    obj.Archetype?.Nickname ?? "(none)"));
            }
        }

        return usages.ToDictionary(
            x => x.Key,
            x => x.Value
                .OrderBy(u => u.SystemNickname)
                .ThenBy(u => u.ObjectNickname)
                .ToArray(),
            StringComparer.OrdinalIgnoreCase);
    }

    static string ResolveName(GameDataContext context, int idsName)
    {
        if (idsName == 0)
            return "";
        return context.GameData.GetString(idsName) ?? "";
    }

    static string DisplayName(string nickname, string name) =>
        string.IsNullOrWhiteSpace(name) ? nickname : $"{name} ({nickname})";

    void OpenViewer(Sun sun)
    {
        var title = $"Star Preview - {sun.Nickname}";
        var existing = win.TabControl.Tabs.FirstOrDefault(x => x.Title == title);
        if (existing != null)
            win.TabControl.SetSelected(existing);
        else
            win.AddTab(new StarViewerTab(win, context, sun));
    }

    StarUsage PrimaryUsage(Sun sun) =>
        usagesByStar.TryGetValue(sun.Nickname, out var usages)
            ? usages.FirstOrDefault()
            : null;

    static void DetailRow(string label, string value)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TextDisabled(label);
        ImGui.TableNextColumn();
        ImGui.TextWrapped(string.IsNullOrWhiteSpace(value) ? "(none)" : value);
    }

    public override void Dispose()
    {
        previewRenderer.Dispose();
        base.Dispose();
    }

    private sealed record StarUsage(
        string SystemNickname,
        string SystemName,
        string ObjectNickname,
        string ObjectName,
        string ArchetypeNickname);
}
