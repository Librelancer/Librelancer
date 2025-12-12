using ImGuiNET;
using LibreLancer.ContentEdit;
using LibreLancer.Dialogs;
using LibreLancer.ImUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static LancerEdit.Audio.BulkAudioToolState;
using static LancerEdit.Audio.ConversionEntry;

namespace LancerEdit.Audio;

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
    public override Vector2 InitSize => new Vector2(900, 400) * ImGuiHelper.Scale;

    static readonly FileDialogFilters AudioInputFilters = new FileDialogFilters(
        new FileFilter("Supported audio formats", "wav", "mp3", "ogg", "flac"),
        new FileFilter("WAV files", "wav"),
        new FileFilter("MP3 files", "mp3"),
        new FileFilter("Ogg files", "ogg"),
        new FileFilter("Flac files", "flac"));

    const float LABEL_WIDTH = 100f;
    const float BUTTON_WIDTH = 110f;
    const float FOOTER_SPACING = 3.5f;
    const int TABLE_MARGIN_BOTTOM = 75;
    Vector4 ERROR_TEXT_COLOUR = new Vector4(1f, 0.3f, 0.3f, 1f);
    Vector4 SUCCESS_TEXT_COLOUR = new Vector4(0f, 0.8f, 0.2f, 1f);

    MainWindow _win;
    PopupManager _pm;
    Action<List<ImportEntry>> _onImport;

    BulkAudioToolState _state = new BulkAudioToolState();

    CancellationTokenSource cancelToken;
    AppLog log;

    public BulkAudioImportPopup(MainWindow win, PopupManager pm)
    {
        _win = win;
        _pm = pm;
    }
    public BulkAudioImportPopup(MainWindow win, PopupManager pm, Action<List<ImportEntry>> onImport)
    {
        _win = win;
        _pm = pm;
        _onImport = onImport;
    }

    public static void Run(MainWindow win, PopupManager pm)
    {
        pm.OpenPopup(new BulkAudioImportPopup(win, pm));
    }

    public override void Draw(bool appearing)
    {
        HandleDeleteRequest();

        switch (_state.CurrentState)
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
        if (_state.CurrentState == ToolState.SelectFiles)
        {
            if (_state.DeleteIndex >= 0 && _state.DeleteIndex < _state.ConversionEntries.Count)
            {
                _state.ConversionEntries.RemoveAt(_state.DeleteIndex);
                _state.DeleteIndex = -1;
            }
        }
        else if (_state.CurrentState == ToolState.Importing)
        {
            if (_state.DeleteIndex >= 0 && _state.DeleteIndex < _state.ImportEntries.Count)
            {
                _state.ImportEntries.RemoveAt(_state.DeleteIndex);
                _state.DeleteIndex = -1;
            }
        }
    }
    void DrawTrimEditor()
    {
        var entry = _state.TrimEditingEntry;
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
        float remaining = ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeight()*FOOTER_SPACING ;
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
        ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);

        if (_state.ErrorType == ErrorTypes.NoInputs)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.4f, 0.05f, 0.05f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.6f, 0.1f, 0.1f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.7f, 0.1f, 0.1f, 1f));
        }
        if (ImGui.Button("Select Files", new Vector2(BUTTON_WIDTH * ImGuiHelper.Scale, 0)))
        {
            _win.QueueUIThread(() =>
            {
                FileDialog.OpenMultiple(files =>
                {
                    if (files == null || files.Length == 0)
                    {
                        _state.ErrorType = ErrorTypes.None;
                        _state.StatusMessage = "No files selected.";
                        _state.IsError = true;
                        return;
                    }

                    foreach (var f in files)
                        TryAddFileOrScanFolder(f);

                    _state.ErrorType = ErrorTypes.None;
                    _state.StatusMessage = "Files added.";
                    _state.IsError = false;

                }, AudioInputFilters);
            });
        }

        if (_state.ErrorType == ErrorTypes.NoInputs)
            ImGui.PopStyleColor(3);
    }

    void DrawOutputSelector()
    {
        ImGui.Text("Output Folder:");
        ImGui.SameLine(LABEL_WIDTH * ImGuiHelper.Scale);

        if (_state.ErrorType == ErrorTypes.NoOutput)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.4f, 0.05f, 0.05f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.6f, 0.1f, 0.1f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.7f, 0.1f, 0.1f, 1f));
        }

        if (ImGui.Button("Choose", new Vector2(BUTTON_WIDTH * ImGuiHelper.Scale, 0)))
        {
            _win.QueueUIThread(() =>
            {
                FileDialog.ChooseFolder(path => _state.OutputFolder = path);
            });
        }

        if (_state.ErrorType == ErrorTypes.NoOutput)
            ImGui.PopStyleColor(3);

        ImGui.SameLine();
        ImGui.PushItemWidth(-1);

        if (_state.ErrorType == ErrorTypes.NoOutput)
        {
            ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.4f, 0.05f, 0.05f, 1f));  // dark red
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(0.6f, 0.1f, 0.1f, 1f));
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(0.7f, 0.1f, 0.1f, 1f));

        }


        ImGui.InputText("##outputfolder", ref _state.OutputFolder, 256);

        ImGui.PopItemWidth();
        if (_state.ErrorType is ErrorTypes.NoOutput)
            ImGui.PopStyleColor(3);
    }

    void DrawEntryTable()
    {
        float tableHeight = ImGui.GetContentRegionAvail().Y - TABLE_MARGIN_BOTTOM * ImGuiHelper.Scale;
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

            foreach (var e in _state.ConversionEntries)
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
            _state.DeleteIndex = _state.ConversionEntries.IndexOf(e);

        ImGui.TableNextColumn();


            if (ImGuiExt.Button($"{Icons.Cut}##trim{e.OriginalPath}", e.RequiresTrim, new Vector2(ImGui.GetColumnWidth(), ImGui.GetColumnWidth())))
            {
                _state.TrimEditingEntry = e;
                // Backup original values
                _state.BackupTrimStart = e.TrimStart;
                _state.BackupTrimEnd = e.TrimEnd;
                _state.CurrentState = ToolState.TrimTool;
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

        ImGui.ProgressBar(_state.Progress, new Vector2(-1, 20));
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
        float tableHeight = ImGui.GetContentRegionAvail().Y - TABLE_MARGIN_BOTTOM * ImGuiHelper.Scale;
        ImGui.BeginChild("results_child", new Vector2(0, tableHeight), ImGuiChildFlags.Borders);

        if (ImGui.BeginTable("results_table", 4,
            ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY))
        {
            ImGui.TableSetupColumn("OK?", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("File Name", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("File Path", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Message", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            foreach (var e in _state.ConversionEntries)
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
    void DrawImportUI()
    {
        ImGui.Text("Select Files for import");
        ImGui.Separator();
        DrawImportTable();
        ImGui.Spacing();

    }

    void DrawImportTable()
    {
        float tableHeight = ImGui.GetContentRegionAvail().Y - TABLE_MARGIN_BOTTOM * ImGuiHelper.Scale;
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

            foreach (var importEntry in _state.ImportEntries)
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
        if (_state.ErrorType == ErrorTypes.NodeNameInvalid && string.IsNullOrWhiteSpace(e.NodeName))
        {
            ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.4f, 0.05f, 0.05f, 1f));  // dark red
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(0.6f, 0.1f, 0.1f, 1f));
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(0.7f, 0.1f, 0.1f, 1f));

        }
        ImGui.TableNextColumn();
        ImGui.PushItemWidth(-1);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding,
                new Vector2(style.FramePadding.X, rowHeight));
        ImGui.InputText($"##nodeName{e.OriginalPath}", ref e.NodeName, 512);
        ImGui.PopItemWidth();
        ImGui.PopStyleVar();
        if (_state.ErrorType is ErrorTypes.NoOutput)
            ImGui.PopStyleColor(3);
    }
    // ACTION BAR
    void DrawActionBar()
    {
        using var tb = Toolbar.Begin("##actions", false);

        switch (_state.CurrentState)
        {
            case ToolState.SelectFiles:
                DrawSelectFilesActions();
                break;

            case ToolState.ConversionResults:
                DrawConversionResultsActions();
                break;
            case ToolState.Importing:
                DrawSelectImportFilesActions();
                break;

            case ToolState.TrimTool:
                DrawTrimToolActions();
                break;
        }
    }

    void DrawTrimToolActions()
    {
        if (ImGui.Button("OK", new Vector2(BUTTON_WIDTH * ImGuiHelper.Scale, 0)))
        {
            _state.TrimEditingEntry = null;
            _state.CurrentState = ToolState.SelectFiles;
        }

        ImGui.SameLine();

        if (ImGui.Button("Cancel", new Vector2(BUTTON_WIDTH * ImGuiHelper.Scale, 0)))
        {
            _state.TrimEditingEntry.TrimStart = _state.BackupTrimStart;
            _state.TrimEditingEntry.TrimEnd = _state.BackupTrimEnd;
            _state.TrimEditingEntry = null;
            _state.CurrentState = ToolState.SelectFiles;
        }
    }

    void DrawSelectFilesActions()
    {
        if (ImGui.Button("Clear All", new Vector2(BUTTON_WIDTH * ImGuiHelper.Scale, 0)))
            _state.ConversionEntries.Clear();

        ImGui.SameLine();

        if (ImGui.Button("Convert All", new Vector2(BUTTON_WIDTH * ImGuiHelper.Scale, 0)))
        {
            if (!ValidateBeforeConvert()) return;
            StartConversionAsync();
        }

        ImGui.SameLine();
        if (ImGui.Button("Close", new Vector2(BUTTON_WIDTH * ImGuiHelper.Scale, 0)))
            ImGui.CloseCurrentPopup();
    }

    void DrawSelectImportFilesActions()
    {
        ImGui.SameLine();


            if (ImGuiExt.Button("Import", _state.ImportEntries.Count > 0, new Vector2(BUTTON_WIDTH * ImGuiHelper.Scale, 0)))
            {
                if (!ValidateBeforeImport()) return;

                var importList = _state.ImportEntries
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

                _onImport(importList.Where(e => e.Action is ImportAction.Import && e.Data != null).ToList());
                ImGui.CloseCurrentPopup();
            }
       

        ImGui.SameLine();
        if (ImGui.Button("Close", new Vector2(BUTTON_WIDTH * ImGuiHelper.Scale, 0)))
            ImGui.CloseCurrentPopup();
    }

    void DrawConversionResultsActions()
    {
        if (_onImport != null)
        {
            if (ImGui.Button("Import Files", new Vector2(BUTTON_WIDTH * ImGuiHelper.Scale, 0)))
            {
                _state.ImportEntries.Clear();
                foreach (var e in _state.ConversionEntries)
                {
                    if (e.Action is ConversionAction.Convert && e.Success)
                    {
                        _state.ImportEntries.Add(new ImportEntry
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
                        _state.ImportEntries.Add(new ImportEntry
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

                    _state.ImportEntries.Add(new ImportEntry
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
                _state.CurrentState = ToolState.Importing;



            }
        }
        ImGui.SameLine();
        if (ImGui.Button("Close", new Vector2(BUTTON_WIDTH * ImGuiHelper.Scale, 0)))
            ImGui.CloseCurrentPopup();
    }

    bool ValidateBeforeConvert()
    {
        if ((string.IsNullOrWhiteSpace(_state.OutputFolder) || !Directory.Exists(_state.OutputFolder))
            && _state.ConversionEntries.Any(e => e.Action is ConversionAction.Convert))
        {
            _state.ErrorType = ErrorTypes.NoOutput;
            _state.StatusMessage = "Please select a valid output folder.";
            _state.IsError = true;
            return false;
        }

        if (!_state.ConversionEntries.Any(e => e.Action is ConversionAction.Convert ||
            e.Action is ConversionAction.Ignore && e.Info.Kind is AudioImportKind.Copy or AudioImportKind.Mp3))
            
        {
            _state.ErrorType = ErrorTypes.NoInputs;
            _state.StatusMessage = "No files selected for conversion.";
            _state.IsError = true;
            return false;
        }

        _state.IsError = false;
        return true;
    }
    bool ValidateBeforeImport()
    {
        if (!_state.ImportEntries.Any(e => e.Action is ImportAction.Import))
        {
            _state.ErrorType = ErrorTypes.NoImports;
            _state.StatusMessage = "No files selected for import.";
            _state.IsError = true;
            return false;
        }

        if (_state.ImportEntries.Any(e => e.Action is ImportAction.Import && string.IsNullOrEmpty(e.NodeName)))
        {
            _state.ErrorType = ErrorTypes.NodeNameInvalid;
            _state.StatusMessage = "There are invalid Node Names";
            _state.IsError = true;
            return false;
        }
        _state.ErrorType = ErrorTypes.None;
        _state.StatusMessage = "";
        _state.IsError = false;
        return true;
    }
    // STATUS BAR
    void DrawStatusBar()
    {
        ImGui.BeginChild("status", new Vector2(0, 30), ImGuiChildFlags.Borders, ImGuiWindowFlags.NoScrollbar);
        ImGui.TextColored(_state.IsError ? ERROR_TEXT_COLOUR : SUCCESS_TEXT_COLOUR, _state.StatusMessage ?? "");
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
        if (_state.ConversionEntries.Any(e => e.OriginalPath.Equals(path, StringComparison.OrdinalIgnoreCase)))
            return;

        var info = AudioConverter.Analyze(path);
        _state.ConversionEntries.Add(new ConversionEntry(path, info));
    }


    // CONVERSION
    void StartConversionAsync()
    {
        _state.CurrentState = ToolState.Converting;

        cancelToken = new CancellationTokenSource();
        log = new AppLog();
        _state.Progress = 0f;

        var jobs = _state.ConversionEntries.Where(e => e.Action is ConversionAction.Convert)
            .Select(e => new ConversionJob(e, _state.OutputFolder))
            .ToList();

        Task.Run(async () =>
        {
            int i = 0;

            foreach (var job in jobs)
            {
                var entry = _state.ConversionEntries.First(e => e.OriginalPath == job.InputPath);

                var progress = new Progress<float>(p =>
                {
                    _state.Progress = (i + p) / jobs.Count;
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

                _state.Progress = (float)i / jobs.Count;
            }

            _win.QueueUIThread(() =>
            {
                _state.CurrentState = ToolState.ConversionResults;
                _state.StatusMessage = "Conversion complete.";
            });

        });

    }

}
