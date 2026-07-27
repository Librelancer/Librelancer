using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LancerEdit.GameContent.Stars;
using LibreLancer;
using GameArchetype = LibreLancer.Data.GameData.Archetype;
using LibreLancer.Data.GameData.Archetypes;
using LibreLancer.ImUI;
using LibreLancer.Data.Schema.Solar;

namespace LancerEdit.GameContent;

public class StarViewerTab : GameContentTab
{
    private readonly MainWindow win;
    private readonly GameDataContext context;
    private readonly StarPreviewRenderer renderer;
    private readonly Sun sun;
    private readonly StarPreviewState previewState;
    private readonly GameArchetype[] sunArchetypes;
    private int selectedArchetype;
    private Vector3 background = new(0.008f, 0.011f, 0.022f);
    private bool showBackdrop;
    private bool showDebugLensFlare;

    public StarViewerTab(MainWindow win, GameDataContext context, Sun sun)
    {
        this.win = win;
        this.context = context;
        this.sun = sun;
        previewState = StarPreviewState.Get(context, sun);
        renderer = new StarPreviewRenderer(context);
        sunArchetypes = context.GameData.Items.Archetypes
            .Where(x => x.Type == ArchetypeType.sun)
            .OrderBy(x => x.Nickname)
            .ToArray();
        selectedArchetype = Math.Max(0, Array.FindIndex(sunArchetypes,
            x => x.Nickname.Equals("sun_2000", StringComparison.OrdinalIgnoreCase)));
        Title = $"Star Preview - {sun.Nickname}";
    }

    public override unsafe void Draw(double elapsed)
    {
        DrawToolbar();
        ImGui.Separator();

        var avail = ImGui.GetContentRegionAvail();
        var sideWidth = Math.Clamp(avail.X * 0.28f, 260 * ImGuiHelper.Scale, 420 * ImGuiHelper.Scale);
        ImGui.BeginChild("##starViewport", new Vector2(avail.X - sideWidth - ImGui.GetStyle().ItemSpacing.X, 0), ImGuiChildFlags.Borders);
        DrawViewport();
        ImGui.EndChild();

        ImGui.SameLine();
        ImGui.BeginChild("##starInfo", new Vector2(0, 0), ImGuiChildFlags.Borders);
        DrawInfo();
        ImGui.EndChild();
    }

    void DrawToolbar()
    {
        if (ImGui.Button("Reset Preview"))
        {
            previewState.Reset();
        }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(220 * ImGuiHelper.Scale);
        ImGui.SliderFloat("Zoom", ref previewState.Zoom, 0.1f, 6f, "%.2f");
        ImGui.SameLine();
        DrawArchetypeCombo();
        ImGui.SameLine();
        ImGui.SetNextItemWidth(180 * ImGuiHelper.Scale);
        ImGui.ColorEdit3("Background", ref background, ImGuiColorEditFlags.NoInputs);
        ImGui.SameLine();
        ImGui.Checkbox("Backdrop", ref showBackdrop);
        ImGui.SameLine();
        ImGui.Checkbox("Debug Lens Flare", ref showDebugLensFlare);
    }

    unsafe void DrawViewport()
    {
        var min = ImGui.GetCursorScreenPos();
        var size = ImGui.GetContentRegionAvail();
        size.X = MathF.Max(size.X, 64 * ImGuiHelper.Scale);
        size.Y = MathF.Max(size.Y, 64 * ImGuiHelper.Scale);
        ImGui.InvisibleButton("##starObjectView", size);
        HandleViewportInput(size);

        var max = min + size;
        var rect = new Rectangle((int)min.X, (int)min.Y, (int)size.X, (int)size.Y);
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddCallback((_, cmd) =>
        {
            win.RenderContext.PushScissor(ImGuiHelper.GetClipRect(cmd), false);
            renderer.Render(sun, new Color4(background.X, background.Y, background.Z, 1f),
                win.RenderContext, rect, previewState.Zoom, 0, previewState.ViewOffset);
            win.RenderContext.PopScissor();
        }, IntPtr.Zero);
        if (showBackdrop)
            DrawSpaceOverlay(drawList, min, max);
        if (showDebugLensFlare)
            DrawLensOverlay(drawList, min, max);
        drawList.AddRect(min, max, ImGui.GetColorU32(ImGuiCol.Border));
    }

    void HandleViewportInput(Vector2 size)
    {
        if (!ImGui.IsItemHovered() && !ImGui.IsItemActive())
            return;

        var wheel = ImGui.GetIO().MouseWheel;
        if (ImGui.IsItemHovered() && wheel != 0)
        {
            previewState.Zoom = Math.Clamp(previewState.Zoom * MathF.Pow(1.12f, wheel), 0.1f, 6f);
            ImGuiHelper.AnimatingElement();
        }

        if (ImGui.IsMouseDragging(ImGuiMouseButton.Left, 1f))
        {
            var delta = ImGui.GetMouseDragDelta(ImGuiMouseButton.Left, 1f);
            ImGui.ResetMouseDragDelta(ImGuiMouseButton.Left);
            previewState.ViewOffset += new Vector2(delta.X / MathF.Max(1f, size.X), delta.Y / MathF.Max(1f, size.Y));
            previewState.ViewOffset.X = Math.Clamp(previewState.ViewOffset.X, -0.45f, 0.45f);
            previewState.ViewOffset.Y = Math.Clamp(previewState.ViewOffset.Y, -0.35f, 0.35f);
            ImGuiHelper.AnimatingElement();
        }
    }

    void DrawArchetypeCombo()
    {
        if (sunArchetypes.Length == 0)
        {
            ImGui.TextDisabled("Archetype: (none)");
            return;
        }

        ImGui.SetNextItemWidth(180 * ImGuiHelper.Scale);
        if (ImGui.BeginCombo("Archetype", sunArchetypes[selectedArchetype].Nickname))
        {
            for (var i = 0; i < sunArchetypes.Length; i++)
            {
                if (ImGui.Selectable(sunArchetypes[i].Nickname, i == selectedArchetype))
                    selectedArchetype = i;
            }
            ImGui.EndCombo();
        }
    }

    void DrawSpaceOverlay(ImDrawListPtr drawList, Vector2 min, Vector2 max)
    {
        var size = max - min;
        var seed = sun.Nickname.GetHashCode();
        for (var i = 0; i < 80; i++)
        {
            seed = unchecked(seed * 1664525 + 1013904223);
            var x = ((seed >>> 8) & 0xFFFF) / 65535f;
            seed = unchecked(seed * 1664525 + 1013904223);
            var y = ((seed >>> 8) & 0xFFFF) / 65535f;
            seed = unchecked(seed * 1664525 + 1013904223);
            var alpha = 0.18f + (((seed >>> 8) & 0xFF) / 255f) * 0.42f;
            var p = min + new Vector2(x * size.X, y * size.Y);
            drawList.AddCircleFilled(p, 1.1f * ImGuiHelper.Scale, ImGui.GetColorU32(new Vector4(0.82f, 0.9f, 1f, alpha)));
        }
    }

    void DrawLensOverlay(ImDrawListPtr drawList, Vector2 min, Vector2 max)
    {
        var source = context.GameData.Items.Ini.Stars?.Stars
            .FirstOrDefault(x => x.Nickname.Equals(sun.Nickname, StringComparison.OrdinalIgnoreCase));
        if (source == null)
            return;

        var size = max - min;
        var center = (min + max) * 0.5f;
        var starPos = center + new Vector2(previewState.ViewOffset.X * size.X, previewState.ViewOffset.Y * size.Y);
        var unit = MathF.Min(size.X, size.Y);

        if (!string.IsNullOrWhiteSpace(source.LensGlow))
        {
            var glow = context.GameData.Items.Ini.Stars.LensGlows
                .FirstOrDefault(x => x.Nickname.Equals(source.LensGlow, StringComparison.OrdinalIgnoreCase));
            if (glow != null)
            {
                var radius = unit * 0.06f * MathF.Max(1, glow.RadiusScale);
                DrawSoftCircle(drawList, starPos, radius, glow.InnerColor, 0.16f, 6);
            }
        }

        if (string.IsNullOrWhiteSpace(source.LensFlare))
            return;

        var flare = context.GameData.Items.Ini.Stars.LensFlares
            .FirstOrDefault(x => x.Nickname.Equals(source.LensFlare, StringComparison.OrdinalIgnoreCase));
        if (flare == null)
            return;

        var axis = center - starPos;
        if (axis.LengthSquared() < 1f)
            axis = new Vector2(unit * 0.18f, 0);

        foreach (var bead in flare.Beads)
        {
            var p = starPos + axis * (1f + bead.A * 2.8f);
            var radius = Math.Clamp(flare.MinRadius + (flare.MaxRadius - flare.MinRadius) * bead.B,
                flare.MinRadius, flare.MaxRadius) * ImGuiHelper.Scale * 0.42f;
            var color = new Color3f(bead.C, bead.D, bead.E);
            DrawSoftCircle(drawList, p, radius, color, Math.Clamp(bead.F * 1.8f, 0.02f, 0.28f), 4);
        }
    }

    static void DrawSoftCircle(ImDrawListPtr drawList, Vector2 center, float radius, Color3f color, float alpha, int rings)
    {
        for (var i = rings; i >= 1; i--)
        {
            var t = i / (float)rings;
            var a = alpha * (1f - t * 0.72f);
            drawList.AddCircleFilled(center, radius * t,
                ImGui.GetColorU32(new Vector4(color.R, color.G, color.B, a)));
        }
    }

    void DrawInfo()
    {
        ImGui.Text(sun.Nickname);
        ImGui.TextDisabled("Freelancer sun billboard preview");
        ImGui.Spacing();
        DrawThumbnailReference();
        ImGui.Spacing();

        var source = context.GameData.Items.Ini.Stars?.Stars
            .FirstOrDefault(x => x.Nickname.Equals(sun.Nickname, StringComparison.OrdinalIgnoreCase));
        var archetype = sunArchetypes.Length > 0 ? sunArchetypes[selectedArchetype] : null;

        if (!ImGui.BeginTable("##starInfoTable", 2, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg))
            return;

        DetailRow("Object Archetype", archetype?.Nickname);
        DetailRow("Preview Source", "stararch.ini billboard");
        DetailRow("Archetype Type", archetype?.Type.ToString());
        DetailRow("Solar Radius", archetype?.SolarRadius.ToString("0.####"));
        DetailRow("Preview Zoom", $"{previewState.Zoom:0.##}x");
        DetailRow("Radius", sun.Radius.ToString("0.####"));
        DetailRow("Visual Radius", StarPreviewRenderer.GetVisualRadius(sun).ToString("0.####"));
        DetailRow("Star Glow", source?.StarGlow);
        DetailRow("Glow Texture", sun.GlowSprite);
        DetailRow("Glow Scale", sun.GlowScale.ToString("0.####"));
        DetailRow("Star Center", source?.StarCenter);
        DetailRow("Center Texture", sun.CenterSprite);
        DetailRow("Center Scale", sun.CenterScale.ToString("0.####"));
        DetailRow("Spines", source?.Spines);
        DetailRow("Spines Texture", sun.SpinesSprite);
        DetailRow("Spines Scale", sun.SpinesScale.ToString("0.####"));
        DetailRow("Spines Count", (sun.Spines?.Count ?? 0).ToString());
        DetailRow("Lens Flare", source?.LensFlare);
        DetailRow("Lens Glow", source?.LensGlow);

        ImGui.EndTable();
    }

    unsafe void DrawThumbnailReference()
    {
        var size = new Vector2(128 * ImGuiHelper.Scale);
        ImGui.TextDisabled($"Preview thumbnail ({StarPreviewRenderer.ThumbnailTextureSize} x {StarPreviewRenderer.ThumbnailTextureSize})");
        var min = ImGui.GetCursorScreenPos();
        ImGui.Dummy(size);
        var drawList = ImGui.GetWindowDrawList();
        var rect = new Rectangle(
            (int)min.X,
            (int)min.Y,
            Math.Max(1, (int)size.X),
            Math.Max(1, (int)size.Y));
        drawList.AddCallback((_, cmd) =>
        {
            win.RenderContext.PushScissor(ImGuiHelper.GetClipRect(cmd), false);
            renderer.Render(sun, new Color4(background.X, background.Y, background.Z, 1f),
                win.RenderContext, rect, previewState.Zoom, 0, previewState.ViewOffset);
            win.RenderContext.PopScissor();
        }, IntPtr.Zero);
        drawList.AddRect(min, min + size, ImGui.GetColorU32(ImGuiCol.Border));
    }

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
        renderer.Dispose();
        base.Dispose();
    }
}
