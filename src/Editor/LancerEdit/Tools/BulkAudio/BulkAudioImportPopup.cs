using ImGuiNET;
using LibreLancer;
using LibreLancer.Dialogs;
using LibreLancer.ImUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using LancerEdit.Tools.BulkAudio.Services;
using System.Runtime.CompilerServices;

namespace LancerEdit.Tools.BulkAudio;


public readonly struct DisabledScope : IDisposable
{
    private readonly bool _active;
    public DisabledScope(bool disabled)
    {
        _active = disabled;
        if (disabled)
            ImGui.BeginDisabled();
    }
    public void Dispose()
    {
        if (_active)
            ImGui.EndDisabled();
    }
}
public class BulkAudioImportPopup : PopupWindow
{
    public override string Title { get; set; } = "Bulk Audio Import / Convert Tool";
    public override bool NoClose => false;
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoCollapse;

    const float LABEL_WIDTH = 100f;
    const float BUTTON_WIDTH = 110f;

    MainWindow _win;
    PopupManager _pm;

    readonly IAudioAnalysisService _analysisService;
    readonly IAudioConversionService _conversionService;

    List<BulkAudioEntry> entries = new();
    UiState uiState = new UiState();

    CancellationTokenSource cancelToken;
    AppLog log;

    string outputFolder = "";

    // UI state machine
    enum ToolState
    {
        SelectFiles,
        TrimTool,
        Converting,
        ConversionResults,
        Importing,
        ImportResults
    }

    ToolState currentState = ToolState.SelectFiles;

    public BulkAudioImportPopup(MainWindow win, PopupManager pm)
    {
        _win = win;
        _pm = pm;

        _analysisService = new DefaultAudioAnalysisService();
        _conversionService = new DefaultAudioConversionService();
    }

    public static void Run(MainWindow win, PopupManager pm)
    {
        pm.OpenPopup(new BulkAudioImportPopup(win, pm));
    }

    public override void Draw(bool appearing)
    {
        if (appearing) // set initial window size
            ImGui.SetNextWindowSize(new Vector2(900, 400), ImGuiCond.Always);

        HandleDeleteRequest();

        switch (currentState)
        {
            case ToolState.SelectFiles:
                DrawFileSelectionUI();
                break;

            case ToolState.Converting:
                DrawConvertingUI();
                break;

            case ToolState.ConversionResults:
                DrawConversionResultsUI();
                break;

            case ToolState.Importing:
                DrawImportUI();
                break;

            case ToolState.ImportResults:
                DrawImportResultsUI();
                break;
            case ToolState.TrimTool:
                DrawTrimEditor();
                break;
        }

        ImGui.Separator();
        DrawActionBar();
        ImGui.Separator();
        DrawStatusBar();
    }

    // UI SUBCOMPONENTS

    void HandleDeleteRequest()
    {
        if (uiState.DeleteIndex >= 0 && uiState.DeleteIndex < entries.Count)
        {
            entries.RemoveAt(uiState.DeleteIndex);
            uiState.DeleteIndex = -1;
        }
    }

    void DrawTrimEditor()
    {
        var entry = uiState.TrimEditingEntry;
        if (entry == null) return;

        var fileName = Path.GetFileName(entry.OriginalPath);

        // --- Top content ---
        ImGui.Text($"Trim MP3: {fileName}");
        ImGui.Separator();
        ImGui.Text("This MP3 has no trimming metadata.");
        ImGui.Text("Enter start/end samples manually (Audacity recommended).");
        ImGui.Spacing();

        int start = entry.TrimStart;
        if (ImGui.InputInt("Trim Start (samples)", ref start))
            entry.TrimStart = Math.Max(0, start);

        int end = entry.TrimEnd;
        if (ImGui.InputInt("Trim End (samples)", ref end))
            entry.TrimEnd = Math.Max(0, end);

        ImGui.Separator();

        // --- FLEX SPACER (push buttons to bottom) ---
        float remaining = ImGui.GetContentRegionAvail().Y - 75;
        if (remaining > 0)
            ImGui.Dummy(new Vector2(1, remaining));
    }
    // SELECT FILES + PARAM UI
    void DrawFileSelectionUI()
    {
        DrawInputSelector();
        DrawOutputSelector();
        DrawEntryTable();
    }

    void DrawInputSelector()
    {
        ImGui.Text("Source Files:");
        ImGui.SameLine(LABEL_WIDTH);

        if (ImGui.Button("Select Files", new Vector2(BUTTON_WIDTH, 0)))
        {
            _win.QueueUIThread(() =>
            {
                FileDialog.OpenMultiple(files =>
                {
                    if (files == null || files.Length == 0)
                    {
                        uiState.StatusMessage = "No files selected.";
                        uiState.IsError = true;
                        uiState.ErrorType = UiState.ErrorTypes.NoInputs;
                        return;
                    }

                    foreach (var f in files)
                        TryAddFileOrScanFolder(f);

                    uiState.StatusMessage = "Files added.";
                    uiState.IsError = false;
                });
            });
        }
    }

    void DrawOutputSelector()
    {
        ImGui.Text("Output Folder:");
        ImGui.SameLine(LABEL_WIDTH);

        if (ImGui.Button("Choose", new Vector2(BUTTON_WIDTH,0)))
        {
            _win.QueueUIThread(() =>
            {
                FileDialog.ChooseFolder(path => outputFolder = path);
            });
        }

        ImGui.SameLine();
        ImGui.PushItemWidth(-1);
        if (uiState.ErrorType == UiState.ErrorTypes.NoOutput)
        {
            ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.4f, 0.05f, 0.05f, 1f));  // dark red
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(0.6f, 0.1f, 0.1f, 1f));
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(0.7f, 0.1f, 0.1f, 1f));

        }
        ImGui.InputText("##outputfolder", ref outputFolder, 256);
        ImGui.PopItemWidth();
        if (uiState.ErrorType is UiState.ErrorTypes.NoOutput)
            ImGui.PopStyleColor(3);
    }

    void DrawEntryTable()
    {
        float tableHeight = ImGui.GetContentRegionAvail().Y - 80;
        ImGui.BeginChild("entries_child", new Vector2(0, tableHeight), ImGuiChildFlags.Borders);

        if (ImGui.BeginTable("entries_table", 9,
            ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY |
            ImGuiTableFlags.Resizable | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("File", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Comment", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Channels", ImGuiTableColumnFlags.NoClip | ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 60);
            ImGui.TableSetupColumn("Sample Rate", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 80);
            ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 120);
            ImGui.TableSetupColumn("Mode",
                                    ImGuiTableColumnFlags.NoClip |
                                    ImGuiTableColumnFlags.WidthFixed |
                                    ImGuiTableColumnFlags.NoResize,
                                    90);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.NoClip | ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 125);
            ImGui.TableSetupColumn("Delete", ImGuiTableColumnFlags.NoClip | ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.NoHeaderLabel, 30);
            ImGui.TableSetupColumn("Trim", ImGuiTableColumnFlags.NoClip | ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.NoHeaderLabel, 30);
            ImGui.TableHeadersRow();

            foreach (var e in entries)
                DrawEntryRow(e);

            ImGui.EndTable();
        }

        ImGui.EndChild();
    }

    void DrawEntryRow(BulkAudioEntry e)
    {
        bool isBitrate = e.UseBitrate;
        int bitrate = e.Bitrate;
        var style = ImGui.GetStyle();
        float rowHeight = ImGui.GetTextLineHeight() * 0.6f;  // minimum safe


        ImGui.TableNextRow();

        // file
        ImGui.TableNextColumn();
        ImGui.Text(Path.GetFileName(e.OriginalPath));

        // comment
        ImGui.TableNextColumn();
        ImGui.Text(e.Comment ?? "-");

        // channels
        ImGui.TableNextColumn();
        CenterText(e.Info?.Channels.ToString() ?? "-");

        // samplerate
        ImGui.TableNextColumn();
        CenterText(e.Info?.Frequency.ToString() ?? "-");

        // action dropdown
        ImGui.TableNextColumn();
        ImGui.PushItemWidth(-1);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding,
            new Vector2(style.FramePadding.X, rowHeight));
        if (ImGui.BeginCombo($"##act{e.OriginalPath}", e.IgnoreConvert ? "Ignore" : "Convert"))
        {
            if (ImGui.Selectable("Convert", !e.IgnoreConvert)) e.IgnoreConvert = false;
            if (ImGui.Selectable("Ignore", e.IgnoreConvert)) e.IgnoreConvert = true;
            ImGui.EndCombo();
        }
        ImGui.PopItemWidth();
        ImGui.PopStyleVar();

        // encoder mode (bitrate/quality)
        ImGui.TableNextColumn();
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding,
            new Vector2(style.FramePadding.X, rowHeight));
        
        ImGuiExt.ButtonDivided($"##md{e.OriginalPath}", "Bitrate", "Quality", ref isBitrate);
        e.UseBitrate = isBitrate;
        ImGui.PopStyleVar();

        // value (either bitrate or quality)
        ImGui.TableNextColumn();
        ImGui.PushItemWidth(-1);
        
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding,
            new Vector2(style.FramePadding.X, rowHeight));
        if (e.UseBitrate)
        {
            ImGui.InputInt($"##br{e.OriginalPath}", ref bitrate);
            e.Bitrate = Math.Clamp(bitrate, 8, 320);
        }
        else
        {
            if (ImGui.BeginCombo($"##ql{e.OriginalPath}", e.Quality.ToString()))
            {
                foreach (var q in new[] { 100, 90, 80, 70, 60, 50, 40, 30, 20, 10 })
                    if (ImGui.Selectable(q.ToString(), q == e.Quality))
                        e.Quality = q;

                ImGui.EndCombo();
            }
        }
        ImGui.PopItemWidth();
        ImGui.PopStyleVar();

        // delete
        ImGui.TableNextColumn();
        if (ImGui.Button($"{Icons.TrashAlt}##del{e.OriginalPath}", new Vector2(ImGui.GetColumnWidth(), ImGui.GetColumnWidth())))
            uiState.DeleteIndex = entries.IndexOf(e);

        ImGui.TableNextColumn();

        // trim editor button
        //using (new DisabledScope(!e.RequiresTrim))
        {
            if (ImGui.Button($"{Icons.Cut}##trim{e.OriginalPath}", new Vector2(ImGui.GetColumnWidth(), ImGui.GetColumnWidth()))) {
                uiState.TrimEditingEntry = e;
                // Backup original values
                uiState.BackupTrimStart = e.TrimStart;
                uiState.BackupTrimEnd = e.TrimEnd;
                currentState = ToolState.TrimTool;
            }

        }
    }

    void CenterText(string text)
    {
        float width = ImGui.GetColumnWidth();
        float textWidth = ImGui.CalcTextSize(text).X;
        float offset = (width - textWidth) * 0.5f;
        if (offset > 0)
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

        ImGui.Text(text);
    }

    // CONVERTING UI
    void DrawConvertingUI()
    {
        ImGui.Text("Converting files...");
        ImGui.Spacing();

        ImGui.ProgressBar(uiState.Progress, new Vector2(-1, 20));
        ImGui.Spacing();

        ImGui.BeginDisabled();
        DrawEntryTable();
        ImGui.EndDisabled();

        ImGui.Spacing();
        ImGui.TextDisabled("Please wait...");
    }

    // RESULTS UI
    void DrawConversionResultsUI()
    {
        ImGui.Text("Conversion Results:");
        ImGui.Separator();
        DrawResultsTable();
    }

    void DrawResultsTable()
    {
        float tableHeight = ImGui.GetContentRegionAvail().Y - 80;
        ImGui.BeginChild("results_child", new Vector2(0, tableHeight), ImGuiChildFlags.Borders);

        if (ImGui.BeginTable("results_table", 4,
            ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY))
        {
            ImGui.TableSetupColumn("OK?", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("File Name", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("File Path", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Message", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            foreach (var e in entries)
            {
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.Text(e.Success ? Icons.Cube_LightGreen.ToString() : Icons.Cube_Coral.ToString());

                ImGui.TableNextColumn();
                ImGui.Text(Path.GetFileName(e.OriginalPath));

                ImGui.TableNextColumn();
                ImGui.Text(e.OriginalPath);

                ImGui.TableNextColumn();
                ImGui.Text(e.Error ?? "");
            }

            ImGui.EndTable();
        }

        ImGui.EndChild();
    }

    // IMPORT UI
    // (if onImport callback specified)
    void DrawImportUI()
    {
        ImGui.Text("Importing converted files...");
        ImGui.Spacing();
        ImGui.ProgressBar(uiState.Progress, new Vector2(-1, 20));
        ImGui.Spacing();
    }
    void DrawImportResultsUI()
    {
        ImGui.Text("Import complete.");
        ImGui.Text(uiState.StatusMessage);
    }

    // ACTION BAR
    void DrawActionBar()
    {
        using var tb = Toolbar.Begin("##actions", false);

        switch (currentState)
        {
            case ToolState.SelectFiles:
                DrawSelectFilesActions();
                break;

            case ToolState.ConversionResults:
                DrawConversionResultsActions();
                break;

            case ToolState.ImportResults:
                if (ImGui.Button("Close"))
                    ImGui.CloseCurrentPopup();
                break;
            case ToolState.TrimTool:
                DrawTrimToolActions();
                break;
        }
    }

    void DrawTrimToolActions()
    {
        if (ImGui.Button("OK", new Vector2(BUTTON_WIDTH, 0)))
        {
            uiState.TrimEditingEntry = null;
            currentState = ToolState.SelectFiles;
        }

        ImGui.SameLine();

        if (ImGui.Button("Cancel", new Vector2(BUTTON_WIDTH, 0)))
        {
            uiState.TrimEditingEntry.TrimStart = uiState.BackupTrimStart;
            uiState.TrimEditingEntry.TrimEnd = uiState.BackupTrimEnd;
            uiState.TrimEditingEntry = null;
            currentState = ToolState.SelectFiles;
        }
    }

    void DrawSelectFilesActions()
    {
        if (ImGui.Button("Clear All", new Vector2(BUTTON_WIDTH, 0)))
            entries.Clear();

        ImGui.SameLine();

        if (ImGui.Button("Convert All", new Vector2(BUTTON_WIDTH, 0)))
        {
            if (!ValidateBeforeConvert()) return;
            StartConversionAsync();
        }

        ImGui.SameLine();
        if (ImGui.Button("Close", new Vector2(BUTTON_WIDTH, 0)))
            ImGui.CloseCurrentPopup();
    }

    void DrawConversionResultsActions()
    {
        if (ImGui.Button("Close", new Vector2(BUTTON_WIDTH, 0)))
            ImGui.CloseCurrentPopup();
    }

    bool ValidateBeforeConvert()
    {
        if (string.IsNullOrWhiteSpace(outputFolder) || !Directory.Exists(outputFolder))
        {
            uiState.ErrorType = UiState.ErrorTypes.NoOutput;
            uiState.StatusMessage = "Please select a valid output folder.";
            uiState.IsError = true;
            return false;
        }

        if (!entries.Any(e => !e.IgnoreConvert))
        {
            uiState.ErrorType = UiState.ErrorTypes.NoInputs;
            uiState.StatusMessage = "No files selected for conversion.";
            uiState.IsError = true;
            return false;
        }

        uiState.IsError = false;
        return true;
    }

    // STATUS BAR
    void DrawStatusBar()
    {
        ImGui.BeginChild("status", new Vector2(0, 30), ImGuiChildFlags.Borders, ImGuiWindowFlags.NoScrollbar);

        if (string.IsNullOrEmpty(uiState.StatusMessage))
            ImGui.Text("");
        else if (uiState.IsError)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.3f, 0.3f, 1f));
            ImGui.Text(uiState.StatusMessage);
            ImGui.PopStyleColor();
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0f, 0.8f, 0.2f, 1f));
            ImGui.Text(uiState.StatusMessage);
            ImGui.PopStyleColor();
        }

        ImGui.EndChild();
    }


    // FILE / FOLDER HANDLING
    void TryAddFileOrScanFolder(string path)
    {
        if (File.Exists(path))
        {
            AddFile(path);
            return;
        }

        if (Directory.Exists(path))
        {
            foreach (var f in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
                AddFile(f);
        }
    }

    void AddFile(string path)
    {
        if (entries.Any(e => e.OriginalPath.Equals(path, StringComparison.OrdinalIgnoreCase)))
            return;

        var info = _analysisService.Analyze(path);
        entries.Add(new BulkAudioEntry(path, info));
    }


    // CONVERSION
    void StartConversionAsync()
    {
        currentState = ToolState.Converting;

        cancelToken = new CancellationTokenSource();
        log = new AppLog();
        uiState.Progress = 0f;

        var jobs = entries.Where(e => !e.IgnoreConvert)
            .Select(e => new ConversionJob(e, outputFolder))
            .ToList();

        Task.Run(async () =>
        {
            int i = 0;

            foreach (var job in jobs)
            {
                var entry = entries.First(e => e.OriginalPath == job.InputPath);

                var progress = new Progress<float>(p =>
                {
                    uiState.Progress = ((float)i + p) / jobs.Count;
                });

                var result = await _conversionService.ConvertAsync(
                    job,
                    progress,
                    cancelToken.Token,
                    msg => log.AppendText(msg)
                );

                entry.Success = result.Success;
                entry.Error = result.ErrorMessage;
                entry.OutputPath = result.OutputPath;

                i++;

                uiState.Progress = (float)i / jobs.Count;
            }

            _win.QueueUIThread(() =>
            {
                currentState = ToolState.ConversionResults;
                uiState.StatusMessage = "Conversion complete.";
            });

        });

    }

}
