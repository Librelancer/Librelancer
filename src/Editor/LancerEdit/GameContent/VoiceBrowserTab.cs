using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Data;
using LibreLancer.Data.Ini;
using LibreLancer.Dialogs;
using LibreLancer.ImUI;
using LibreLancer.Media;
using LibreLancer.Utf;

namespace LancerEdit.GameContent;

public class VoiceBrowserTab : EditorTab
{
    private readonly MainWindow win;
    private readonly GameDataContext context;
    private readonly VoiceEntry[] voices;
    private readonly Dictionary<string, VoiceUtfIndex> utfIndexes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<uint, List<VoiceUtfNode>> nodesByHash = new();
    private readonly Dictionary<uint, List<string>> messagesByHash = new();
    private readonly Dictionary<string, VoiceSourceInfo> voiceSources;
    private readonly Dictionary<string, List<PermutationSourceInfo>> permutationSources;
    private readonly FileDialogFilters wavFilters = new(new FileFilter("WAV Files", "wav"));

    private VoiceEntry selectedVoice;
    private VoiceLineRow[] rows = [];
    private SoundInstance currentInstance;
    private string currentPlaybackId;
    private string search = "";
    private int statusFilter;
    private int sourceFilter;

    private static readonly string[] StatusFilters =
    [
        "All",
        "Problems",
        "Linked",
        "INI missing",
        "UTF missing"
    ];

    private static readonly string[] SourceFilters =
    [
        "All",
        "Base",
        "Space",
        "Mission",
        "Recognizable",
        "Mixed",
        "Other"
    ];

    public VoiceBrowserTab(MainWindow win, GameDataContext context)
    {
        this.win = win;
        this.context = context;
        Title = "Voice Browser";

        BuildUtfIndexes();
        voiceSources = BuildVoiceSources();
        permutationSources = BuildPermutationSources();
        var voiceNames = context.GameData.Items.Ini.Voices.Voices.Keys
            .Concat(context.GameData.Items.Ini.MsnVoiceProps.VoiceProps.Select(x => x.Voice))
            .Distinct(StringComparer.OrdinalIgnoreCase);
        voices = voiceNames
            .Select(nickname =>
            {
                var source = voiceSources.GetValueOrDefault(nickname) ?? VoiceSourceInfo.Empty;
                return new VoiceEntry(nickname, context.GameData.GetVoicePath(nickname), source);
            })
            .OrderBy(x => x.Nickname, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        foreach (var voice in voices)
        {
            if (!context.GameData.Items.Ini.Voices.Voices.TryGetValue(voice.Nickname, out var data))
                continue;
            foreach (var line in data.Messages)
            {
                var hash = FLHash.CreateID(line.Message);
                if (!messagesByHash.TryGetValue(hash, out var messages))
                    messagesByHash[hash] = messages = [];
                if (!messages.Contains(line.Message, StringComparer.OrdinalIgnoreCase))
                    messages.Add(line.Message);
            }
        }
        selectedVoice = voices.FirstOrDefault();
        RebuildRows();
    }

    public override void Draw(double elapsed)
    {
        DrawToolbar();
        ImGui.Separator();

        var avail = ImGui.GetContentRegionAvail();
        var leftWidth = Math.Clamp(avail.X * 0.24f, 210 * ImGuiHelper.Scale, 340 * ImGuiHelper.Scale);
        ImGui.BeginChild("##voiceList", new Vector2(leftWidth, 0), ImGuiChildFlags.Borders);
        DrawVoiceList();
        ImGui.EndChild();

        ImGui.SameLine();
        ImGui.BeginChild("##voiceLines", new Vector2(0, 0), ImGuiChildFlags.Borders);
        DrawSelectedVoice();
        ImGui.EndChild();
    }

    private void DrawToolbar()
    {
        ImGui.SetNextItemWidth(300 * ImGuiHelper.Scale);
        if (ImGui.InputTextWithHint("##voiceSearch", "Search voice or line", ref search, 256))
            RebuildRows();
        ImGui.SameLine();
        ImGui.SetNextItemWidth(140 * ImGuiHelper.Scale);
        if (ImGui.Combo("Status", ref statusFilter, StatusFilters, StatusFilters.Length))
            RebuildRows();
        ImGui.SameLine();
        ImGui.SetNextItemWidth(150 * ImGuiHelper.Scale);
        if (ImGui.Combo("Source", ref sourceFilter, SourceFilters, SourceFilters.Length) &&
            selectedVoice != null &&
            !MatchesSourceFilter(selectedVoice))
        {
            selectedVoice = voices.FirstOrDefault(MatchesSourceFilter);
            RebuildRows();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Filter voices by the voice_*.ini files that define or extend them.");
        ImGui.SameLine();
        ImGui.TextDisabled($"{rows.Length} lines");
    }

    private bool MatchesSourceFilter(VoiceEntry voice)
    {
        if (sourceFilter == 0)
            return true;
        return voice.Source.KindLabel.Equals(SourceFilters[sourceFilter], StringComparison.OrdinalIgnoreCase);
    }

    private void DrawVoiceList()
    {
        foreach (var voice in voices.Where(MatchesSourceFilter))
        {
            var isSelected = selectedVoice == voice;
            var label = $"{voice.Nickname}##voice_{voice.Nickname}";
            if (ImGui.Selectable(label, isSelected))
            {
                StopPlayback();
                selectedVoice = voice;
                RebuildRows();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip($"{voice.UtfPath ?? "(no UTF path)"}\n{voice.Source.Tooltip}");
        }
    }

    private void DrawSelectedVoice()
    {
        if (selectedVoice == null)
        {
            ImGui.TextDisabled("No voices loaded");
            return;
        }

        ImGui.Text($"Voice: {selectedVoice.Nickname}");
        ImGui.Text($"UTF: {selectedVoice.UtfPath ?? "(none)"}");
        ImGui.TextDisabled($"Source: {selectedVoice.Source.KindLabel} ({selectedVoice.Source.Files.Count} file(s))");
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(selectedVoice.Source.Tooltip);
        DrawPermutationIssues();
        ImGui.Separator();
        DrawRows();
    }

    private void DrawRows()
    {
        var flags = ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable |
                    ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingStretchProp;
        if (!ImGui.BeginTable("##voiceRows", 7, flags, ImGui.GetContentRegionAvail()))
            return;

        ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed, 70 * ImGuiHelper.Scale);
        ImGui.TableSetupColumn("Message", ImGuiTableColumnFlags.WidthStretch, 2.3f);
        ImGui.TableSetupColumn("CRC", ImGuiTableColumnFlags.WidthFixed, 96 * ImGuiHelper.Scale);
        ImGui.TableSetupColumn("INI", ImGuiTableColumnFlags.WidthFixed, 42 * ImGuiHelper.Scale);
        ImGui.TableSetupColumn("UTF", ImGuiTableColumnFlags.WidthStretch, 1.2f);
        ImGui.TableSetupColumn("Issue", ImGuiTableColumnFlags.WidthStretch, 1.3f);
        ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 120 * ImGuiHelper.Scale);
        ImGui.TableHeadersRow();

        var clipper = new ImGuiListClipper();
        clipper.Begin(rows.Length);
        while (clipper.Step())
        {
            for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                DrawRow(rows[i], i);
        }
        ImGui.EndTable();
    }

    private void DrawRow(VoiceLineRow row, int index)
    {
        ImGui.TableNextRow();
        ImGui.PushID(index);

        ImGui.TableNextColumn();
        ImGui.TextColored(row.Ok ? new Vector4(0.35f, 0.85f, 0.45f, 1) : new Vector4(0.95f, 0.45f, 0.35f, 1),
            row.Ok ? $"{Icons.Check} OK" : $"{Icons.X} Bad");
        ImGui.TableNextColumn();
        ImGui.Text(row.Message);
        ImGui.TableNextColumn();
        ImGui.Text($"0x{row.Hash:X8}");
        ImGui.TableNextColumn();
        ImGui.Text(row.HasIni ? "yes" : "no");
        ImGui.TableNextColumn();
        ImGui.Text(row.UtfSummary);
        if (ImGui.IsItemHovered() && !string.IsNullOrEmpty(row.UtfTooltip))
            ImGui.SetTooltip(row.UtfTooltip);
        ImGui.TableNextColumn();
        ImGui.TextWrapped(row.Issue);
        ImGui.TableNextColumn();
        var isPlaying = IsPlaying(row);
        if (ImGui.Button($"{(isPlaying ? Icons.Stop : Icons.Play)}##play", new Vector2(34 * ImGuiHelper.Scale, 0)))
        {
            if (isPlaying)
                StopPlayback();
            else
                Play(row);
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(isPlaying ? "Stop this voice line." : "Play this voice line through Librelancer audio.");
        ImGui.SameLine();
        ImGui.BeginDisabled(row.FirstNode == null);
        if (ImGui.Button($"{Icons.Export}##export", new Vector2(34 * ImGuiHelper.Scale, 0)))
            Export(row);
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(row.FirstNode == null
                ? "No UTF audio node is linked, so there is nothing to export."
                : "Export the linked UTF audio node as a WAV file.");
        ImGui.EndDisabled();

        ImGui.PopID();
    }

    private void DrawPermutationIssues()
    {
        var props = context.GameData.Items.Ini.MsnVoiceProps.VoiceProps
            .FirstOrDefault(x => x.Voice.Equals(selectedVoice.Nickname, StringComparison.OrdinalIgnoreCase));
        if (props == null || props.PermutationCounts.Count == 0)
            return;
        context.GameData.Items.Ini.Voices.Voices.TryGetValue(selectedVoice.Nickname, out var voiceData);

        foreach (var (line, expected) in props.PermutationCounts)
        {
            if (!MatchesPermutationSearch(line, expected))
                continue;
            var probeLimit = ProbePermutationLimit(expected);
            var ini = GetPermutationNumbers(line, probeLimit, h =>
                voiceData?.Messages.Any(m => FLHash.CreateID(m.Message) == h) == true);
            var utf = GetPermutationNumbers(line, probeLimit, h => GetNodesForSelectedUtf(h).Count > 0);
            var utfMax = utf.Count == 0 ? 0 : utf.Max();
            var countExceedsUtf = utfMax == 0 || expected > utfMax;
            var iniMissingUtf = NumbersMissingFrom(ini, utf);
            if (!countExceedsUtf && iniMissingUtf.Count == 0)
                continue;
            if (statusFilter == 2 ||
                statusFilter == 3 ||
                (statusFilter == 4 && !countExceedsUtf && iniMissingUtf.Count == 0))
                continue;

            ImGui.TextColored(new Vector4(1f, 0.72f, 0.25f, 1f),
                $"{Icons.Warning} {line} permutation_count warning");
            ImGui.SameLine();
            ImGui.TextDisabled(
                $"Count: {expected}  INI: {FormatNumbers(ini)}  UTF: {FormatNumbers(utf)}  UTF max: {FormatMax(utfMax)}  INI missing UTF: {FormatNumbers(iniMissingUtf)}");
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(PermutationTooltip(line, expected, utfMax, iniMissingUtf));
        }
    }

    private void RebuildRows()
    {
        if (selectedVoice == null)
        {
            rows = [];
            return;
        }

        var voiceData = context.GameData.Items.Ini.Voices.Voices.GetValueOrDefault(selectedVoice.Nickname);
        var byHash = new Dictionary<uint, VoiceLineRow>();
        if (voiceData != null)
        {
            foreach (var msg in voiceData.Messages)
            {
                var hash = FLHash.CreateID(msg.Message);
                var nodes = GetNodesForSelectedUtf(hash);
                byHash[hash] = CreateRow(msg.Message, hash, true, nodes);
            }
        }

        if (selectedVoice.UtfPath != null && TryGetUtfIndex(selectedVoice.UtfPath, out var index))
        {
            foreach (var node in index.Nodes)
            {
                if (byHash.ContainsKey(node.Hash))
                    continue;
                byHash[node.Hash] = CreateRow(node.NodeName, node.Hash, false, [node]);
            }
        }

        IEnumerable<VoiceLineRow> filtered = byHash.Values;
        if (!string.IsNullOrWhiteSpace(search))
        {
            filtered = filtered.Where(x =>
                selectedVoice.Nickname.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.Message.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.Hash.ToString("X8").Contains(search, StringComparison.OrdinalIgnoreCase));
        }
        filtered = statusFilter switch
        {
            1 => filtered.Where(x => !x.Ok),
            2 => filtered.Where(x => x.Ok),
            3 => filtered.Where(x => !x.HasIni),
            4 => filtered.Where(x => x.HasIni && x.FirstNode == null),
            _ => filtered
        };
        rows = filtered.OrderBy(x => x.Message, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private VoiceLineRow CreateRow(string message, uint hash, bool hasIni, List<VoiceUtfNode> nodes)
    {
        var allNodes = nodesByHash.GetValueOrDefault(hash) ?? [];
        var issues = new List<string>();
        if (!hasIni)
            issues.Add("Missing [Sound]");
        if (nodes.Count == 0)
            issues.Add("Missing UTF node");
        if (messagesByHash.TryGetValue(hash, out var messages) && messages.Count > 1)
            issues.Add("CRC collision");
        if (nodes.Count > 1)
            issues.Add("Duplicate UTF node");
        var otherUtfs = allNodes
            .Select(x => x.UtfPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return new VoiceLineRow(
            message,
            hash,
            hasIni,
            nodes.FirstOrDefault(),
            nodes.Count == 0 ? "-" : string.Join(", ", nodes.Select(x => x.NodeName)),
            otherUtfs.Length > 1 ? "Same hash also exists in:\n" + string.Join("\n", otherUtfs) : "",
            issues.Count == 0 ? "Linked" : string.Join("; ", issues),
            issues.Count == 0);
    }

    private List<VoiceUtfNode> GetNodesForSelectedUtf(uint hash)
    {
        if (selectedVoice == null ||
            selectedVoice.UtfPath == null ||
            !TryGetUtfIndex(selectedVoice.UtfPath, out var index))
            return [];
        return index.ByHash.GetValueOrDefault(hash) ?? [];
    }

    private Dictionary<string, VoiceSourceInfo> BuildVoiceSources()
    {
        var sources = new Dictionary<string, VoiceSourceBuilder>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in context.GameData.Items.Ini.Freelancer.VoicePaths)
        {
            foreach (var section in IniFile.ParseFile(path, context.GameData.VFS))
            {
                if (!section.Name.Equals("voice", StringComparison.OrdinalIgnoreCase))
                    continue;
                var voice = EntryString(section, "extend") ?? EntryString(section, "nickname");
                if (string.IsNullOrWhiteSpace(voice))
                    continue;
                if (!sources.TryGetValue(voice, out var builder))
                    sources[voice] = builder = new VoiceSourceBuilder();
                builder.Add(path, section.Line, SourceKind(path));
            }
        }
        return sources.ToDictionary(x => x.Key, x => x.Value.Build(), StringComparer.OrdinalIgnoreCase);
    }

    private Dictionary<string, List<PermutationSourceInfo>> BuildPermutationSources()
    {
        var result = new Dictionary<string, List<PermutationSourceInfo>>(StringComparer.OrdinalIgnoreCase);
        var path = context.GameData.Items.Ini.Freelancer.DataPath + "MISSIONS\\voice_properties.ini";
        if (!context.GameData.VFS.FileExists(path))
            return result;

        foreach (var section in IniFile.ParseFile(path, context.GameData.VFS))
        {
            if (!section.Name.Equals("mVoiceProp", StringComparison.OrdinalIgnoreCase))
                continue;
            var voice = EntryString(section, "voice");
            if (string.IsNullOrWhiteSpace(voice))
                continue;
            foreach (var entry in section)
            {
                if (!entry.Name.Equals("permutation_count", StringComparison.OrdinalIgnoreCase) || entry.Count < 2)
                    continue;
                var line = entry[0].ToString();
                var expected = entry[1].ToInt32();
                var key = PermutationKey(voice, line);
                if (!result.TryGetValue(key, out var sources))
                    result[key] = sources = [];
                sources.Add(new PermutationSourceInfo(path, section.Line, entry.Line, expected));
            }
        }
        return result;
    }

    private string PermutationTooltip(string line, int expected, int utfMax, List<int> iniMissingUtf)
    {
        var tooltip =
            "Declared in DATA\\MISSIONS\\voice_properties.ini.\n" +
            "Count is permutation_count: the highest random variant the engine may try for this group.\n" +
            "INI is matching [Sound] msg variants in the merged voice. UTF is matching audio nodes in the selected voice UTF.\n" +
            "The numbered set may be sparse. This warns when permutation_count is higher than the highest UTF variant, or when a [Sound] variant has no UTF audio.";
        if (utfMax == 0)
            tooltip += "\nNo matching UTF variants were found for this group.";
        else if (expected > utfMax)
            tooltip += $"\npermutation_count {expected} is higher than UTF max {utfMax:D2}.";
        if (iniMissingUtf.Count > 0)
            tooltip += $"\n[Sound] variants without UTF audio: {FormatNumbers(iniMissingUtf)}";
        var key = PermutationKey(selectedVoice.Nickname, line);
        if (permutationSources.TryGetValue(key, out var sources))
        {
            tooltip += "\n";
            tooltip += string.Join("\n", sources.Select(x =>
                $"{x.Path}:{x.EntryLine}  expected {x.Expected}  [mVoiceProp] line {x.SectionLine}"));
        }
        else
        {
            tooltip += $"\nExpected {expected}. Source line was not found in the raw ini scan.";
        }
        return tooltip;
    }

    private bool MatchesPermutationSearch(string line, int expected)
    {
        if (string.IsNullOrWhiteSpace(search))
            return true;
        if (selectedVoice.Nickname.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            line.Contains(search, StringComparison.OrdinalIgnoreCase))
            return true;
        for (int i = 1; i <= ProbePermutationLimit(expected); i++)
        {
            var hash = FLHash.CreateID($"{line}_{i:D2}-");
            if (hash.ToString("X8").Contains(search, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static string EntryString(Section section, string name)
    {
        var entry = section.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return entry is { Count: > 0 } ? entry[0].ToString() : null;
    }

    private static string SourceKind(string path)
    {
        var file = Path.GetFileName(path);
        if (file.Contains("voices_base", StringComparison.OrdinalIgnoreCase))
            return "Base";
        if (file.Contains("voices_space", StringComparison.OrdinalIgnoreCase))
            return "Space";
        if (file.Contains("voices_mission", StringComparison.OrdinalIgnoreCase))
            return "Mission";
        if (file.Contains("voices_recognizable", StringComparison.OrdinalIgnoreCase))
            return "Recognizable";
        return "Other";
    }

    private static string PermutationKey(string voice, string line) =>
        voice + "\0" + line;

    private void BuildUtfIndexes()
    {
        foreach (var utf in GetUtfFiles(@"DATA\AUDIO"))
        {
            try
            {
                using var stream = context.GameData.VFS.Open(utf);
                var index = AddUtfIndex(utf, stream);
                foreach (var node in index.Nodes)
                {
                    if (!nodesByHash.TryGetValue(node.Hash, out var nodes))
                        nodesByHash[node.Hash] = nodes = [];
                    nodes.Add(node);
                }
            }
            catch (Exception ex)
            {
                FLLog.Error("Voice Browser", $"{utf}: {ex.Message}");
            }
        }
    }

    private VoiceUtfIndex AddUtfIndex(string utf, Stream stream)
    {
        var index = new VoiceUtfIndex(utf, stream);
        utfIndexes[utf] = index;
        utfIndexes[CanonicalUtfPath(utf)] = index;
        return index;
    }

    private bool TryGetUtfIndex(string utf, out VoiceUtfIndex index)
    {
        if (utfIndexes.TryGetValue(utf, out index) ||
            utfIndexes.TryGetValue(CanonicalUtfPath(utf), out index))
            return true;

        try
        {
            if (!context.GameData.VFS.FileExists(utf))
                return false;
            using var stream = context.GameData.VFS.Open(utf);
            index = AddUtfIndex(utf, stream);
            foreach (var node in index.Nodes)
            {
                if (!nodesByHash.TryGetValue(node.Hash, out var nodes))
                    nodesByHash[node.Hash] = nodes = [];
                nodes.Add(node);
            }
            return true;
        }
        catch (Exception ex)
        {
            FLLog.Error("Voice Browser", $"{utf}: {ex.Message}");
            index = null;
            return false;
        }
    }

    private IEnumerable<string> GetUtfFiles(string path)
    {
        foreach (var file in context.GameData.VFS.GetFiles(path))
        {
            if (file.EndsWith(".utf", StringComparison.OrdinalIgnoreCase))
                yield return CombineVfs(path, file);
        }
        foreach (var dir in context.GameData.VFS.GetDirectories(path))
        {
            foreach (var file in GetUtfFiles(CombineVfs(path, dir)))
                yield return file;
        }
    }

    private void Play(VoiceLineRow row)
    {
        StopPlayback();
        currentPlaybackId = PlaybackId(row);
        if (row.HasIni)
        {
            currentInstance = context.Sounds.GetInstance(selectedVoice.Nickname, row.Hash);
            currentInstance?.Play();
        }
        else if (row.FirstNode != null)
        {
            win.PlayBuffer(row.FirstNode.Data);
        }
    }

    private void StopPlayback()
    {
        currentInstance?.Stop();
        currentInstance = null;
        currentPlaybackId = null;
        win.StopBuffer();
    }

    private bool IsPlaying(VoiceLineRow row) =>
        currentPlaybackId == PlaybackId(row) &&
        (currentInstance is { Playing: true } || win.PlayingBuffer);

    private string PlaybackId(VoiceLineRow row) =>
        $"{selectedVoice?.Nickname}:{row.Hash:X8}:{row.HasIni}";

    private void Export(VoiceLineRow row)
    {
        if (row.FirstNode == null)
            return;
        FileDialog.Save(path => File.WriteAllBytes(path, row.FirstNode.Data), wavFilters);
    }

    private static List<int> GetPermutationNumbers(string line, int max, Func<uint, bool> exists)
    {
        var result = new List<int>();
        for (int i = 1; i <= max; i++)
        {
            var hash = FLHash.CreateID($"{line}_{i:D2}-");
            if (exists(hash))
                result.Add(i);
        }
        return result;
    }

    private static int ProbePermutationLimit(int expected) =>
        Math.Clamp(expected + 16, 64, 999);

    private static List<int> NumbersMissingFrom(List<int> required, List<int> available) =>
        required.Where(x => !available.Contains(x)).ToList();

    private static string FormatNumbers(List<int> numbers) =>
        numbers.Count == 0 ? "-" : string.Join(", ", FormatNumberRanges(numbers));

    private static string FormatMax(int value) =>
        value <= 0 ? "-" : value.ToString("D2");

    private static IEnumerable<string> FormatNumberRanges(List<int> numbers)
    {
        var ordered = numbers.OrderBy(x => x).Distinct().ToArray();
        for (int i = 0; i < ordered.Length; i++)
        {
            var start = ordered[i];
            var end = start;
            while (i + 1 < ordered.Length && ordered[i + 1] == end + 1)
            {
                end = ordered[++i];
            }
            yield return start == end ? start.ToString("D2") : $"{start:D2}-{end:D2}";
        }
    }


    private static string CombineVfs(string path, string file) =>
        path.TrimEnd('\\', '/') + "\\" + file.TrimStart('\\', '/');

    private static string CanonicalUtfPath(string path)
    {
        path = path.Replace('/', '\\');
        var marker = "\\AUDIO\\";
        var index = path.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index >= 0)
            return path[(index + 1)..].ToLowerInvariant();
        if (path.StartsWith("AUDIO\\", StringComparison.OrdinalIgnoreCase))
            return path.ToLowerInvariant();
        return path.ToLowerInvariant();
    }

    private record VoiceEntry(string Nickname, string UtfPath, VoiceSourceInfo Source);

    private record VoiceLineRow(
        string Message,
        uint Hash,
        bool HasIni,
        VoiceUtfNode FirstNode,
        string UtfSummary,
        string UtfTooltip,
        string Issue,
        bool Ok);

    private record VoiceUtfNode(string UtfPath, string NodeName, uint Hash, byte[] Data);

    private record VoiceSourceInfo(string KindLabel, List<string> Files, string Tooltip)
    {
        public static readonly VoiceSourceInfo Empty = new("Other", [], "No voice source file was found.");
    }

    private record PermutationSourceInfo(string Path, int SectionLine, int EntryLine, int Expected);

    private class VoiceSourceBuilder
    {
        private readonly List<string> declarations = [];
        private readonly HashSet<string> files = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> kinds = new(StringComparer.OrdinalIgnoreCase);

        public void Add(string path, int line, string kind)
        {
            files.Add(path);
            kinds.Add(kind);
            declarations.Add($"{path}:{line}");
        }

        public VoiceSourceInfo Build()
        {
            var kind = kinds.Count == 1 ? kinds.First() : "Mixed";
            var fileList = files.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
            return new VoiceSourceInfo(kind, fileList, string.Join("\n", declarations));
        }
    }

    private class VoiceUtfIndex : UtfFile
    {
        public List<VoiceUtfNode> Nodes = [];
        public Dictionary<uint, List<VoiceUtfNode>> ByHash = new();

        public VoiceUtfIndex(string path, Stream stream)
        {
            foreach (var child in parseFile(path, stream).Children)
            {
                if (child is not LeafNode leaf)
                    continue;
                var hash = ParseHash(child.Name);
                var node = new VoiceUtfNode(path, child.Name, hash, leaf.ByteArrayData);
                Nodes.Add(node);
                if (!ByHash.TryGetValue(hash, out var nodes))
                    ByHash[hash] = nodes = [];
                nodes.Add(node);
            }
        }

        private static uint ParseHash(string name)
        {
            if (name.StartsWith("0x", StringComparison.OrdinalIgnoreCase) &&
                uint.TryParse(name[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hash))
                return hash;
            return FLHash.CreateID(name);
        }
    }
}
