using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ImGuiNET;
using LibreLancer.ContentEdit;
using LibreLancer.Dialogs;
using LibreLancer.ImUI;

namespace LancerEdit.Audio;

public readonly struct DisabledScope : IDisposable
{
    private readonly bool active;
    public DisabledScope(bool disabled)
    {
        active = disabled;
        if (disabled)
            ImGui.BeginDisabled();
    }
    public void Dispose()
    {
        if (active)
            ImGui.EndDisabled();
    }
}
public class BulkAudioImportPopup : PopupWindow
{
    public override string Title { get; set; } = "Bulk Audio Import / Convert Tool";
    public override bool NoClose => false;
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoCollapse;
    public override Vector2 InitSize => new Vector2(900, 400) * ImGuiHelper.Scale;

    static readonly FileDialogFilters _audioInputFilters = new FileDialogFilters(
        new FileFilter("Supported audio formats", "wav", "mp3", "ogg", "flac"),
        new FileFilter("WAV files", "wav"),
        new FileFilter("MP3 files", "mp3"),
        new FileFilter("Ogg files", "ogg"),
        new FileFilter("Flac files", "flac"));

    static readonly float _footerSpacing = 3.5f;
    static readonly int _tableMarginBottom = 75;

    private readonly MainWindow win;
    readonly Action<List<ImportEntry>> onImport;

    BulkAudioToolState state = new();

    CancellationTokenSource cancelToken;
    AppLog log;

    public BulkAudioImportPopup(MainWindow win)
    {
        this.win = win;
    }
    public BulkAudioImportPopup(MainWindow win, Action<List<ImportEntry>> onImport)
    {
        this.win = win;
        this.onImport = onImport;
    }

    public static void Run(MainWindow win, PopupManager pm)
    {
        pm.OpenPopup(new BulkAudioImportPopup(win));
    }

    public override void Draw(bool appearing)
    {
        HandleDeleteRequest();

        switch (state.CurrentState)
        {
            case ToolState.SelectFiles:
                DrawFileSelectionUi();
                break;

            case ToolState.Converting:
                DrawConvertingUi();
                break;

            case ToolState.ConversionResults:
                DrawConversionResultsUi();
                break;

            case ToolState.Importing:
                DrawImportUi();
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
        if (state.CurrentState == ToolState.SelectFiles)
        {
            if (state.DeleteIndex >= 0 && state.DeleteIndex < state.ConversionEntries.Count)
            {
                state.ConversionEntries.RemoveAt(state.DeleteIndex);
                state.DeleteIndex = -1;
            }
        }
        else if (state.CurrentState == ToolState.Importing)
        {
            if (state.DeleteIndex >= 0 && state.DeleteIndex < state.ImportEntries.Count)
            {
                state.ImportEntries.RemoveAt(state.DeleteIndex);
                state.DeleteIndex = -1;
            }
        }
    }
    void DrawTrimEditor()
    {
        var entry = state.TrimEditingEntry;
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
        float remaining = ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeight() * _footerSpacing;
        if (remaining > 0)
            ImGui.Dummy(new Vector2(1, remaining));
    }
    // SELECT FILES + PARAM UI
    void DrawFileSelectionUi()
    {
        DrawInputSelector();
        DrawOutputSelector();
        DrawEntryTable();
    }

    void DrawInputSelector()
    {
        ImGui.Text("Source Files:");
        ImGui.SameLine(Theme.LabelWidth);

        if (state.ErrorType == ErrorTypes.NoInputs)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, Theme.ErrorInputColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Theme.ErrorInputHoverColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, Theme.ErrorInputActiveColor);
        }
        if (ImGui.Button("Select Files", new Vector2(Theme.ButtonWidth, 0)))
        {
            win.QueueUIThread(() =>
            {
                FileDialog.OpenMultiple(files =>
                {
                    if (files == null || files.Length == 0)
                    {
                        state.ErrorType = ErrorTypes.None;
                        state.StatusMessage = "No files selected.";
                        state.IsError = true;
                        return;
                    }

                    foreach (var f in files)
                        TryAddFileOrScanFolder(f);

                    state.ErrorType = ErrorTypes.None;
                    state.StatusMessage = "Files added.";
                    state.IsError = false;

                }, _audioInputFilters);
            });
        }

        if (state.ErrorType == ErrorTypes.NoInputs)
            ImGui.PopStyleColor(3);
    }

    void DrawOutputSelector()
    {
        ImGui.Text("Output Folder:");
        ImGui.SameLine(Theme.LabelWidth);

        if (state.ErrorType == ErrorTypes.NoOutput)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, Theme.ErrorInputColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Theme.ErrorInputHoverColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, Theme.ErrorInputActiveColor);
        }

        if (ImGui.Button("Choose", new Vector2(Theme.ButtonWidth, 0)))
        {
            win.QueueUIThread(() =>
            {
                FileDialog.ChooseFolder(path => state.OutputFolder = path);
            });
        }

        if (state.ErrorType == ErrorTypes.NoOutput)
            ImGui.PopStyleColor(3);

        ImGui.SameLine();
        ImGui.PushItemWidth(-1);

        if (state.ErrorType == ErrorTypes.NoOutput)
        {
            ImGui.PushStyleColor(ImGuiCol.FrameBg, Theme.ErrorInputColor);  // dark red
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, Theme.ErrorInputHoverColor);
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, Theme.ErrorInputActiveColor);

        }


        ImGui.InputText("##outputfolder", ref state.OutputFolder, 256);

        ImGui.PopItemWidth();
        if (state.ErrorType is ErrorTypes.NoOutput)
            ImGui.PopStyleColor(3);
    }

    void DrawEntryTable()
    {
        float tableHeight = ImGui.GetContentRegionAvail().Y - _tableMarginBottom * ImGuiHelper.Scale;
        ImGui.BeginChild("entries_child", new Vector2(0, tableHeight), ImGuiChildFlags.Borders);

        if (ImGui.BeginTable("entries_table", 9,
            ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY |
            ImGuiTableFlags.Resizable | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("File", ImGuiTableColumnFlags.WidthStretch, 1);
            ImGui.TableSetupColumn("Comment", ImGuiTableColumnFlags.WidthStretch, 2);
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

            foreach (var e in state.ConversionEntries)
                DrawEntryRow(e);

            ImGui.EndTable();
        }

        ImGui.EndChild();
    }

    void DrawEntryRow(ConversionEntry e)
    {
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

        //Editble
        using (new DisabledScope(e.IsLocked))
        {
            // action dropdown
            ImGui.TableNextColumn();
            ImGui.PushItemWidth(-1);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding,
            new Vector2(style.FramePadding.X, rowHeight));
            if (ImGui.BeginCombo($"##act{e.OriginalPath}", e.Action.ToString()))
            {
                foreach (ConversionAction action in Enum.GetValues(typeof(ConversionAction)))
                {
                    bool selected = e.Action == action;
                    if (ImGui.Selectable(action.ToString(), selected))
                        e.Action = action;
                }
                ImGui.EndCombo();
            }
            ImGui.PopItemWidth();
            ImGui.PopStyleVar();
            ImGui.TableNextColumn();


            // encoder mode (bitrate/quality)
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding,
                new Vector2(style.FramePadding.X, rowHeight));
            bool isBitrate = e.Mode == ConversionMode.Bitrate;

            ImGuiExt.ButtonDivided($"##md{e.OriginalPath}", "Bitrate", "Quality", ref isBitrate);
            e.Mode = isBitrate
                ? ConversionMode.Bitrate
                : ConversionMode.Quality;
            ImGui.PopStyleVar();

            // value (either bitrate or quality)
            ImGui.TableNextColumn();
            ImGui.PushItemWidth(-1);

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding,
                new Vector2(style.FramePadding.X, rowHeight));
            if (e.Mode is ConversionMode.Bitrate)
            {
                ImGui.InputInt($"##br{e.OriginalPath}", ref e.Bitrate);
                e.Bitrate = Math.Clamp(e.Bitrate, 8, 320);
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
        }

        // delete
        ImGui.TableNextColumn();
        if (ImGui.Button($"{Icons.TrashAlt}##del{e.OriginalPath}", new Vector2(ImGui.GetColumnWidth(), ImGui.GetColumnWidth())))
            state.DeleteIndex = state.ConversionEntries.IndexOf(e);

        ImGui.TableNextColumn();


        if (ImGuiExt.Button($"{Icons.Cut}##trim{e.OriginalPath}", e.RequiresTrim, new Vector2(ImGui.GetColumnWidth(), ImGui.GetColumnWidth())))
        {
            state.TrimEditingEntry = e;
            // Backup original values
            state.BackupTrimStart = e.TrimStart;
            state.BackupTrimEnd = e.TrimEnd;
            state.CurrentState = ToolState.TrimTool;
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
    void DrawConvertingUi()
    {
        ImGui.Text("Converting files...");
        ImGui.Spacing();

        ImGui.ProgressBar(state.Progress, new Vector2(-1, 20));
        ImGui.Spacing();

        ImGui.BeginDisabled();
        DrawEntryTable();
        ImGui.EndDisabled();

        ImGui.Spacing();
        ImGui.TextDisabled("Please wait...");
    }

    // RESULTS UI
    void DrawConversionResultsUi()
    {
        ImGui.Text("Conversion Results:");
        ImGui.Separator();
        DrawResultsTable();
    }

    void DrawResultsTable()
    {
        float tableHeight = ImGui.GetContentRegionAvail().Y - _tableMarginBottom * ImGuiHelper.Scale;
        ImGui.BeginChild("results_child", new Vector2(0, tableHeight), ImGuiChildFlags.Borders);

        if (ImGui.BeginTable("results_table", 4,
            ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY))
        {
            ImGui.TableSetupColumn("OK?", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("File Name", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("File Path", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Message", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            foreach (var e in state.ConversionEntries)
            {
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                if (e.Action is ConversionAction.Convert)
                {
                    ImGui.Text(e.Success
                        ? Icons.Cube_LightGreen.ToString()
                        : Icons.Cube_Coral.ToString());

                    ImGui.TableNextColumn();
                    ImGui.Text(e.Success
                        ? Path.GetFileName(e.OutputPath)
                        : Path.GetFileName(e.OriginalPath));

                    ImGui.TableNextColumn();
                    ImGui.Text(e.Success
                        ? Path.GetDirectoryName(e.OutputPath)
                        : "-");
                }
                else
                {
                    ImGui.Text(e.Info == null
                    ? Icons.Cube_LightPink.ToString()
                    : Icons.Cube_LightYellow.ToString());

                    ImGui.TableNextColumn();
                    ImGui.Text(Path.GetFileName(e.OriginalPath));

                    ImGui.TableNextColumn();
                    ImGui.Text(e.Info == null
                        ? "-"
                        : Path.GetDirectoryName(e.OriginalPath));
                }

                ImGui.TableNextColumn();
                ImGui.Text(e.Error ?? "");
            }
            ImGui.EndTable();
        }

        ImGui.EndChild();
    }


    // IMPORT UI
    // (if onImport callback specified)
    void DrawImportUi()
    {
        ImGui.Text("Select Files for import");
        ImGui.Separator();
        DrawImportTable();
        ImGui.Spacing();

    }

    void DrawImportTable()
    {
        float tableHeight = ImGui.GetContentRegionAvail().Y - _tableMarginBottom * ImGuiHelper.Scale;
        ImGui.BeginChild("entries_child", new Vector2(0, tableHeight), ImGuiChildFlags.Borders);

        if (ImGui.BeginTable("entries_table", 4,
            ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY |
            ImGuiTableFlags.Resizable | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("File", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 120);
            ImGui.TableSetupColumn("Version", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 120);
            ImGui.TableSetupColumn("Node Name", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            foreach (var importEntry in state.ImportEntries)
                DrawImportTableRow(importEntry);

            ImGui.EndTable();
        }

        ImGui.EndChild();
    }
    void DrawImportTableRow(ImportEntry e)
    {
        var style = ImGui.GetStyle();
        float rowHeight = ImGui.GetTextLineHeight() * 0.6f;  // minimum safe

        ImGui.TableNextRow();

        // file
        ImGui.TableNextColumn();
        ImGui.Text(Path.GetFileName(e.FileName));
        // action dropdown
        ImGui.TableNextColumn();
        using (new DisabledScope(e.IsActionLocked))
        {
            ImGui.PushItemWidth(-1);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding,
                new Vector2(style.FramePadding.X, rowHeight));
            if (ImGui.BeginCombo($"##act{e.OriginalPath}", e.Action.ToString()))
            {
                foreach (ImportAction action in Enum.GetValues(typeof(ImportAction)))
                {
                    bool selected = e.Action == action;
                    if (ImGui.Selectable(action.ToString(), selected))
                        e.Action = action;
                }
                ImGui.EndCombo();
            }
            ImGui.PopItemWidth();
            ImGui.PopStyleVar();
        }
        // version dropdown
        ImGui.TableNextColumn();

        using (new DisabledScope(e.IsVersionLocked))
        {
            ImGui.PushItemWidth(-1);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding,
                new Vector2(style.FramePadding.X, rowHeight));
            if (ImGui.BeginCombo($"##vers{e.OriginalPath}", e.Version.ToString()))
            {
                foreach (ImportVersion version in Enum.GetValues(typeof(ImportVersion)))
                {
                    bool selected = e.Version == version;
                    if (ImGui.Selectable(version.ToString(), selected))
                        e.Version = version;
                }
                ImGui.EndCombo();
            }
            ImGui.PopItemWidth();
            ImGui.PopStyleVar();
        }
        // Node name
        if (state.ErrorType == ErrorTypes.NodeNameInvalid && string.IsNullOrWhiteSpace(e.NodeName))
        {
            ImGui.PushStyleColor(ImGuiCol.FrameBg, Theme.ErrorInputColor);  // dark red
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, Theme.ErrorInputHoverColor);
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, Theme.ErrorInputActiveColor);

        }
        ImGui.TableNextColumn();
        ImGui.PushItemWidth(-1);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding,
                new Vector2(style.FramePadding.X, rowHeight));
        ImGui.InputText($"##nodeName{e.OriginalPath}", ref e.NodeName, 512);
        ImGui.PopItemWidth();
        ImGui.PopStyleVar();
        if (state.ErrorType is ErrorTypes.NoOutput)
            ImGui.PopStyleColor(3);
    }
    // ACTION BAR
    void DrawActionBar()
    {
        using var tb = Toolbar.Begin("##actions", false);
        switch (state.CurrentState)
        {
            case ToolState.SelectFiles:
                DrawSelectFilesActions(tb);
                break;

            case ToolState.ConversionResults:
                DrawConversionResultsActions(tb);
                break;
            case ToolState.Importing:
                DrawSelectImportFilesActions(tb);
                break;

            case ToolState.TrimTool:
                DrawTrimToolActions(tb);
                break;
        }
    }

    void DrawTrimToolActions(Toolbar tb)
    {
        if (tb.ButtonItem("OK", true, "Applies trim values"))
        {
            state.TrimEditingEntry = null;
            state.CurrentState = ToolState.SelectFiles;
        }

        ImGui.SameLine();

        if (tb.ButtonItem("Cancel", true, "Closes without applying trim values"))
        {
            state?.TrimEditingEntry?.TrimStart = state.BackupTrimStart;
            state?.TrimEditingEntry?.TrimEnd = state.BackupTrimEnd;
            state?.TrimEditingEntry = null;
            state?.CurrentState = ToolState.SelectFiles;
        }
    }

    void DrawSelectFilesActions(Toolbar tb)
    {
        if (tb.ButtonItem("Clear All", true, "Clears all files from the table"))
            state.ConversionEntries.Clear();

        ImGui.SameLine();
        ImGui.SeparatorEx(ImGuiSeparatorFlags.Vertical);
        ImGui.SameLine();
        if (tb.ButtonItem("Convert All", true, "Sets all entries Action value to 'Convert'"))
        {
            state.ConversionEntries
                .ForEach(e =>
            {
                if (!e.IsLocked)
                    e.Action = ConversionAction.Convert;
            });
        }
        ImGui.SameLine();
        if (tb.ButtonItem("Ignore All", true, "Sets all entries Action value to 'Ignore'"))
        {
            state.ConversionEntries
                .ForEach(e =>
                {
                    if (!e.IsLocked)
                        e.Action = ConversionAction.Ignore;
                });
        }
        ImGui.SameLine();
        if (tb.ButtonItem("All Bitrate Mode", true, "Sets all entries mode value to 'Bitrate'"))
        {
            state.ConversionEntries
                .ForEach(e =>
                {
                    if (!e.IsLocked)
                        e.Mode = ConversionMode.Bitrate;
                });
        }
        ImGui.SameLine();
        if (tb.ButtonItem("All Quality Mode", true, "Sets all entries mode value to 'Quality'"))
        {
            state.ConversionEntries
                .ForEach(e =>
                {
                    if (!e.IsLocked)
                        e.Mode = ConversionMode.Quality;
                });
        }
        ImGui.SameLine();
        ImGui.SeparatorEx(ImGuiSeparatorFlags.Vertical);
        ImGui.SameLine();
        if (tb.ButtonItem("Convert", true, "Starts the conversion process"))
        {
            if (!ValidateBeforeConvert()) return;
            StartConversionAsync();
        }

        ImGui.SameLine();
        if (tb.ButtonItem("Close", true, "Closes the tool"))
            ImGui.CloseCurrentPopup();
    }

    void DrawSelectImportFilesActions(Toolbar tb)
    {
        ImGui.SameLine();


        if (tb.ButtonItem("Import", state.ImportEntries.Count > 0, "Imports the selected files"))
        {
            if (!ValidateBeforeImport()) return;

            var importList = state.ImportEntries
                                        .Where(e => e.Action == ImportAction.Import)
                                        .ToList();

            foreach (var e in importList)
            {
                string path = e.Version == ImportVersion.Converted
                    ? e.ConvertedPath
                    : e.OriginalPath;

                if (!File.Exists(path))
                {
                    // Handle missing files gracefully
                    e.Data = null;
                    e.Action = ImportAction.Ignore;  // disable import
                    continue;
                }

                e.Data = File.ReadAllBytes(path);
            }

            onImport(importList.Where(e => e.Action is ImportAction.Import && e.Data != null).ToList());
            ImGui.CloseCurrentPopup();
        }


        ImGui.SameLine();
        if (tb.ButtonItem("Close", true, "Closes the tool"))
            ImGui.CloseCurrentPopup();
    }

    void DrawConversionResultsActions(Toolbar tb)
    {
        if (onImport != null)
        {
            if (tb.ButtonItem("Import Files", true, "Continue to Import Settings"))
            {
                state.ImportEntries.Clear();
                foreach (var e in state.ConversionEntries)
                {
                    if (e.Action is ConversionAction.Convert && e.Success)
                    {
                        state.ImportEntries.Add(new ImportEntry
                        {
                            OriginalPath = e.OriginalPath,
                            ConvertedPath = e.OutputPath,
                            Action = ImportAction.Import,
                            Version = ImportVersion.Converted,
                            FileName = Path.GetFileName(e.OutputPath),
                            NodeName = Path.GetFileNameWithoutExtension(e.OutputPath)
                        });
                        continue;
                    }
                    if (e.Action is ConversionAction.Convert && !e.Success
                        || e.Info == null)
                    {
                        continue;
                    }
                    if (e.Action is ConversionAction.Ignore &&
                        e.Info.Kind is AudioImportKind.Copy or AudioImportKind.WavUncompressed)
                    {
                        state.ImportEntries.Add(new ImportEntry
                        {
                            OriginalPath = e.OriginalPath,
                            ConvertedPath = e.OutputPath,
                            Action = ImportAction.Import,
                            Version = ImportVersion.Original,
                            FileName = Path.GetFileName(e.OriginalPath),
                            NodeName = Path.GetFileNameWithoutExtension(e.OriginalPath),

                            IsVersionLocked = true,
                        });
                        continue;
                    }

                    state.ImportEntries.Add(new ImportEntry
                    {
                        OriginalPath = e.OriginalPath,
                        ConvertedPath = e.OutputPath,
                        Action = ImportAction.Ignore,
                        Version = ImportVersion.Original,
                        FileName = Path.GetFileName(e.OriginalPath),
                        NodeName = "-",
                        IsVersionLocked = true,
                        IsActionLocked = true,
                        IsNodeNameLocked = true

                    });


                }
                state.CurrentState = ToolState.Importing;



            }
        }
        ImGui.SameLine();
        if (tb.ButtonItem("Close", true, "Closes the tool"))
            ImGui.CloseCurrentPopup();
    }

    bool ValidateBeforeConvert()
    {
        if ((string.IsNullOrWhiteSpace(state.OutputFolder) || !Directory.Exists(state.OutputFolder))
            && state.ConversionEntries.Any(e => e.Action is ConversionAction.Convert))
        {
            state.ErrorType = ErrorTypes.NoOutput;
            state.StatusMessage = "Please select a valid output folder.";
            state.IsError = true;
            return false;
        }

        if (!state.ConversionEntries.Any(e => e.Action is ConversionAction.Convert ||
            e.Action is ConversionAction.Ignore && e.Info.Kind is AudioImportKind.Copy or AudioImportKind.Mp3))

        {
            state.ErrorType = ErrorTypes.NoInputs;
            state.StatusMessage = "No files selected for conversion.";
            state.IsError = true;
            return false;
        }

        state.IsError = false;
        return true;
    }
    bool ValidateBeforeImport()
    {
        if (!state.ImportEntries.Any(e => e.Action is ImportAction.Import))
        {
            state.ErrorType = ErrorTypes.NoImports;
            state.StatusMessage = "No files selected for import.";
            state.IsError = true;
            return false;
        }

        if (state.ImportEntries.Any(e => e.Action is ImportAction.Import && string.IsNullOrEmpty(e.NodeName)))
        {
            state.ErrorType = ErrorTypes.NodeNameInvalid;
            state.StatusMessage = "There are invalid Node Names";
            state.IsError = true;
            return false;
        }
        state.ErrorType = ErrorTypes.None;
        state.StatusMessage = "";
        state.IsError = false;
        return true;
    }
    // STATUS BAR
    void DrawStatusBar()
    {
        ImGui.BeginChild("status", new Vector2(0, 30), ImGuiChildFlags.Borders, ImGuiWindowFlags.NoScrollbar);
        ImGui.TextColored(state.IsError ? Theme.ErrorTextColor : Theme.SuccessTextColor, state.StatusMessage ?? "");
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

        var regex = new Regex(@"\.(mp3|ogg|wav|flac)$", RegexOptions.IgnoreCase);
        if (Directory.Exists(path))
        {
            foreach (var f in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
                if (regex.IsMatch(Path.GetExtension(f)))
                    AddFile(f);
        }
    }

    void AddFile(string path)
    {
        if (state.ConversionEntries.Any(e => e.OriginalPath.Equals(path, StringComparison.OrdinalIgnoreCase)))
            return;

        var info = AudioConverter.Analyze(path);
        state.ConversionEntries.Add(new ConversionEntry(path, info));
    }


    // CONVERSION
    void StartConversionAsync()
    {
        state.CurrentState = ToolState.Converting;

        cancelToken = new CancellationTokenSource();
        log = new AppLog();
        state.Progress = 0f;

        var jobs = state.ConversionEntries.Where(e => e.Action is ConversionAction.Convert)
            .Select(e => new ConversionJob(e, state.OutputFolder))
            .ToList();

        Task.Run(async () =>
        {
            int i = 0;

            foreach (var job in jobs)
            {
                var entry = state.ConversionEntries.First(e => e.OriginalPath == job.InputPath);

                var progress = new Progress<float>(p =>
                {
                    state.Progress = (i + p) / jobs.Count;
                });

                var result = await AudioConverter.ConvertAsync(
                    job,
                    progress,
                    cancelToken.Token,
                    msg => log.AppendText(msg)
                );

                entry.Success = result.IsSuccess;
                entry.Error = result.IsError ? result.AllMessages() : null;
                entry.OutputPath = result.Data;

                i++;

                state.Progress = (float)i / jobs.Count;
            }

            win.QueueUIThread(() =>
            {
                state.CurrentState = ToolState.ConversionResults;
                state.StatusMessage = "Conversion complete.";
            });

        });

    }

}
