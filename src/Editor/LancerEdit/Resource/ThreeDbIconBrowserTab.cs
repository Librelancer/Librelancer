// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer.Graphics;
using LibreLancer.ImUI;
using LibreLancer.Resources;
using LibreLancer.Utf;

namespace LancerEdit;

public class ThreeDbIconBrowserTab : EditorTab
{
    const int LoadsPerFrame = 4;

    static readonly ThreeDbIconCollectionDefinition[] CollectionDefinitions =
    [
        new(
            "Map Game Object Icons",
            @"DATA\INTERFACE\NEURONET\NAVMAP\NEWNAVMAP\SPACEOBJECTS",
            false),
        new(
            "Base Map Icons",
            @"DATA\INTERFACE\NEURONET\NAVMAP\NEWNAVMAP",
            false),
    ];

    readonly MainWindow win;
    readonly GameDataContext context;
    readonly List<ThreeDbIconCollection> collections = [];
    readonly List<ThreeDbIconEntry> filtered = [];

    string search = "";
    int selectedCollection = 0;
    int page = 0;
    bool needsFilter = true;

    public ThreeDbIconBrowserTab(MainWindow win, GameDataContext context)
    {
        this.win = win;
        this.context = context;
        Title = "3DB Icon Browser";
        BuildCollections();
    }

    void BuildCollections()
    {
        foreach (var definition in CollectionDefinitions)
        {
            var files = GetThreeDbFiles(definition.Path, definition.Recursive)
                .Select(path => new ThreeDbIconEntry(definition.Name, path))
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            collections.Add(new ThreeDbIconCollection(definition.Name, definition.Path, files));
        }
    }

    IEnumerable<string> GetThreeDbFiles(string path, bool recursive)
    {
        foreach (var file in context.GameData.VFS.GetFiles(path))
        {
            if (file.EndsWith(".3db", StringComparison.OrdinalIgnoreCase))
                yield return CombineVfs(path, file);
        }

        if (!recursive)
            yield break;

        foreach (var dir in context.GameData.VFS.GetDirectories(path))
        {
            foreach (var file in GetThreeDbFiles(CombineVfs(path, dir), true))
                yield return file;
        }
    }

    static string CombineVfs(string path, string file) =>
        path.TrimEnd('\\', '/') + "\\" + file.TrimStart('\\', '/');

    public override void Draw(double elapsed)
    {
        DrawToolbar();
        ImGui.Separator();

        if (needsFilter)
            ApplyFilter();

        var cellWidth = 170 * ImGuiHelper.Scale;
        var thumb = 96 * ImGuiHelper.Scale;
        var columns = GetGridColumns(cellWidth);
        var pageSize = GetDynamicPageSize(columns, thumb);
        var totalPages = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)pageSize));
        page = Math.Clamp(page, 0, totalPages - 1);

        var start = page * pageSize;
        var pageItems = filtered.Skip(start).Take(pageSize).ToArray();
        LoadVisiblePreviews(pageItems);
        DrawLoadingProgress();
        DrawPager(totalPages, pageSize);
        DrawGrid(pageItems, columns, thumb, cellWidth);
    }

    void DrawToolbar()
    {
        ImGui.SetNextItemWidth(260 * ImGuiHelper.Scale);
        if (ImGui.InputTextWithHint("##search3db", "Search 3DB icons", ref search, 128))
        {
            page = 0;
            needsFilter = true;
        }

        ImGui.SameLine();
        var names = new[] { "All Collections" }.Concat(collections.Select(x => x.Name)).ToArray();
        ImGui.SetNextItemWidth(260 * ImGuiHelper.Scale);
        if (ImGui.Combo("Collection", ref selectedCollection, names, names.Length))
        {
            page = 0;
            needsFilter = true;
        }

        ImGui.SameLine();
        ImGui.TextDisabled($"{filtered.Count} icons");
    }

    void ApplyFilter()
    {
        filtered.Clear();
        IEnumerable<ThreeDbIconEntry> src = selectedCollection == 0
            ? collections.SelectMany(x => x.Entries)
            : collections[selectedCollection - 1].Entries;

        if (!string.IsNullOrWhiteSpace(search))
        {
            src = src.Where(x =>
                x.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.Path.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.Collection.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        filtered.AddRange(src);
        needsFilter = false;
    }

    void LoadVisiblePreviews(IReadOnlyList<ThreeDbIconEntry> entries)
    {
        var loaded = 0;
        foreach (var entry in entries)
        {
            if (entry.Loaded)
                continue;
            LoadPreview(entry);
            loaded++;
            if (loaded >= LoadsPerFrame)
                break;
        }
        if (entries.Any(x => !x.Loaded))
            ImGuiHelper.AnimatingElement();
    }

    void LoadPreview(ThreeDbIconEntry entry)
    {
        entry.Loaded = true;
        try
        {
            context.Resources.LoadResourceFile(entry.Path);
            var textureName = context.Resources.TexturesInFile(entry.Path).FirstOrDefault();
            if (textureName == null)
            {
                using var stream = context.GameData.VFS.Open(entry.Path);
                UtfLoader.LoadResourceFile(stream, entry.Path, context.Resources, out var mat, out var txm, out _);
                textureName = txm?.Textures.Keys.FirstOrDefault();
                if (textureName == null && mat != null)
                {
                    var materialTextureNames = mat.Materials.Values
                        .SelectMany(GetMaterialTextureNames)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                    textureName = materialTextureNames.FirstOrDefault(x => context.Resources.FindTexture(x) is Texture2D) ??
                                  materialTextureNames.FirstOrDefault();
                }
            }
            if (textureName == null)
            {
                entry.Error = "No texture library or material texture reference";
                return;
            }
            entry.TextureName = textureName;
            if (context.Resources.FindTexture(textureName) is Texture2D tex)
            {
                entry.Texture = tex;
                entry.TextureId = ImGuiHelper.RegisterTexture(tex);
            }
            else
            {
                entry.Error = $"Texture '{textureName}' not loaded";
            }
        }
        catch (Exception ex)
        {
            entry.Error = ex.Message;
        }
    }

    static IEnumerable<string> GetMaterialTextureNames(LibreLancer.Utf.Mat.Material material)
    {
        if (!string.IsNullOrWhiteSpace(material.DtName))
            yield return material.DtName;
        if (!string.IsNullOrWhiteSpace(material.EtName))
            yield return material.EtName;
        if (!string.IsNullOrWhiteSpace(material.BtName))
            yield return material.BtName;
        if (!string.IsNullOrWhiteSpace(material.NtName))
            yield return material.NtName;
        if (!string.IsNullOrWhiteSpace(material.DmName))
            yield return material.DmName;
        if (!string.IsNullOrWhiteSpace(material.Dm0Name))
            yield return material.Dm0Name;
        if (!string.IsNullOrWhiteSpace(material.Dm1Name))
            yield return material.Dm1Name;
        if (!string.IsNullOrWhiteSpace(material.MtName))
            yield return material.MtName;
        if (!string.IsNullOrWhiteSpace(material.RtName))
            yield return material.RtName;
        if (!string.IsNullOrWhiteSpace(material.NmName))
            yield return material.NmName;
    }

    void DrawLoadingProgress()
    {
        var all = filtered.Count;
        if (all == 0)
            return;
        var loaded = filtered.Count(x => x.Loaded);
        if (loaded >= all)
            return;

        ImGui.Spacing();
        ImGui.Text($"{Icons.SyncAlt} Loading previews {loaded}/{all}");
        ImGui.ProgressBar(loaded / (float)all, new Vector2(-1, 0));
        ImGui.Spacing();
    }

    int GetGridColumns(float cellWidth) =>
        Math.Max(1, (int)(ImGui.GetContentRegionAvail().X / cellWidth));

    int GetDynamicPageSize(int columns, float thumb)
    {
        var cardHeight = thumb + 76 * ImGuiHelper.Scale;
        var reservedHeight = 42 * ImGuiHelper.Scale;
        if (filtered.Any(x => !x.Loaded))
            reservedHeight += 48 * ImGuiHelper.Scale;
        var availableHeight = Math.Max(cardHeight, ImGui.GetContentRegionAvail().Y - reservedHeight);
        var rows = Math.Max(1, (int)(availableHeight / cardHeight));
        return Math.Max(columns, columns * rows);
    }

    void DrawPager(int totalPages, int pageSize)
    {
        if (ImGui.Button(Icons.ArrowLeft.ToString(), new Vector2(34 * ImGuiHelper.Scale, 0)) && page > 0)
            page--;
        ImGui.SameLine();
        ImGui.Text($"Page {page + 1} / {totalPages}   {pageSize} per page");
        ImGui.SameLine();
        if (ImGui.Button(Icons.ArrowRight.ToString(), new Vector2(34 * ImGuiHelper.Scale, 0)) && page + 1 < totalPages)
            page++;
    }

    void DrawGrid(IReadOnlyList<ThreeDbIconEntry> entries, int columns, float thumb, float cellWidth)
    {
        if (!ImGui.BeginTable("##3dbicons", columns, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.PadOuterX))
            return;

        foreach (var entry in entries)
        {
            ImGui.TableNextColumn();
            ImGui.PushID(entry.Path);
            DrawIconCard(entry, thumb, cellWidth);
            ImGui.PopID();
        }

        ImGui.EndTable();
    }

    void DrawIconCard(ThreeDbIconEntry entry, float thumb, float cellWidth)
    {
        var imageSize = new Vector2(thumb);
        if (entry.TextureId.HasValue)
        {
            if (ImGui.ImageButton("preview", entry.TextureId.Value, imageSize, new Vector2(0, 1), new Vector2(1, 0)))
                OpenFull(entry);
        }
        else
        {
            ImGui.Button(entry.Loaded ? Icons.Warning.ToString() : Icons.SyncAlt.ToString(), imageSize);
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text(entry.Name);
            ImGui.Text(entry.Path);
            if (!string.IsNullOrEmpty(entry.TextureName))
                ImGui.Text("Texture: " + entry.TextureName);
            if (!string.IsNullOrEmpty(entry.Error))
                ImGui.TextColored(Theme.ErrorTextColor, entry.Error);
            ImGui.EndTooltip();
        }

        var label = entry.Name;
        if (label.Length > 24)
            label = label.Substring(0, 21) + "...";
        ImGui.TextWrapped(label);
        ImGui.TextDisabled(entry.Collection);
        if (ImGui.Button("Open Full", new Vector2(cellWidth - 18 * ImGuiHelper.Scale, 0)))
            OpenFull(entry);
    }

    void OpenFull(ThreeDbIconEntry entry)
    {
        if (entry.Texture != null)
            win.AddTab(new TextureViewer(entry.Name, entry.Texture, null, false));
    }

    record ThreeDbIconCollectionDefinition(string Name, string Path, bool Recursive);
    record ThreeDbIconCollection(string Name, string Path, List<ThreeDbIconEntry> Entries);

    class ThreeDbIconEntry(string collection, string path)
    {
        public string Collection = collection;
        public string Path = path;
        public string Name = System.IO.Path.GetFileName(path);
        public bool Loaded;
        public string TextureName;
        public Texture2D Texture;
        public ImTextureRef? TextureId;
        public string Error;
    }
}
