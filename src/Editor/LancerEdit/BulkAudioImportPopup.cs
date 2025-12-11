using ImGuiNET;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.ContentEdit.Model;
using LibreLancer.Dialogs;
using LibreLancer.ImUI;
using LibreLancer.Media;
using LibreLancer.Resources;
using LibreLancer.Utf.Ale;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Xml;
using static LancerEdit.UiState;

namespace LancerEdit;

public class BulkAudioImportPopup : PopupWindow
{
    public override string Title { get; set; } = "Bulk Audio Import / Convert Tool";
    public override bool NoClose => false;
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoCollapse;
    static readonly FileDialogFilters AudioInputFilters = new FileDialogFilters(
        new FileFilter("Supported audio formats", "wav", "mp3", "ogg", "flac"),
        new FileFilter("WAV files", "wav"),
        new FileFilter("MP3 files", "mp3"),
        new FileFilter("Ogg files", "ogg"),
        new FileFilter("Flac files", "flac"));

    Action<byte[]> onImport;
    MainWindow _win;
    PopupManager _pm;
    List<BulkAudioEntry> entries = new();
    AppLog log = null;

    string outputFolder = "";

    
    public enum AudioToolState
    {
        None = 0,
        Converting,
        ConvertingInProgress,
        ConvertingFinished,
        Importing,
        ImportingInProgress,
        ImportingFinished
    }
    AudioToolState currentState = AudioToolState.None;
    UiState uiState;

    private BulkAudioImportPopup(MainWindow win, PopupManager pm) {
        _win = win;
        _pm = pm;

        currentState = AudioToolState.Converting;
        uiState = new UiState();
    }
    public static void Run(MainWindow win, PopupManager pm)
    {
        var newPopup = new BulkAudioImportPopup(win, pm);
        pm.OpenPopup(newPopup);
    }
    private BulkAudioImportPopup(MainWindow win, PopupManager pm, Action<byte[]> onImport)
    {
        _win = win;
        _pm = pm;

        currentState = AudioToolState.Converting;
        uiState = new UiState();
    }
    public static void Run(MainWindow win, PopupManager pm, Action<byte[]> onImport) {
        var newPopup = new BulkAudioImportPopup(win, pm, onImport);
        pm.OpenPopup(newPopup);
    }

    public override void Draw(bool appearing) // Called once per frame
    {

        if (entries != null && entries.Count > 0 && uiState.DeleteIndex != -1)
        {
            entries.RemoveAt(uiState.DeleteIndex);
            uiState.DeleteIndex = -1;
        }

        if (appearing) // set initial window size
            ImGui.SetNextWindowSize(new Vector2(900, 400), ImGuiCond.Always);

        if (currentState is AudioToolState.Converting)
        {
            DrawInputFileSelect();
            DrawOutputFolderSelect();
            DrawConvertFilesTable();

        }

        if (currentState is AudioToolState.ConvertingFinished)
            DrawConvertFilesResultsTable();

        if (currentState is AudioToolState.Importing)
            DrawImportFilesTable();

        if (currentState is AudioToolState.ImportingFinished)
            DrawImportFilesTable();

        ImGui.Separator();

        // ACTION BUTTONS
        DrawActionBar();

        ImGui.Separator();
        DrawStatusBar();
    }

    private void DrawImportFilesTable()
    {
        throw new NotImplementedException();
    }

    #region Draw UI Methods
    void DrawActionBar()
    {
        using (var tb = Toolbar.Begin("##actions", false))
        {
            if(currentState is AudioToolState.Importing or AudioToolState.Converting)
            {
                if (ImGui.Button("Clear All"))
                    entries.Clear();
                ImGui.SameLine();
                ImGui.SeparatorEx(ImGuiSeparatorFlags.Vertical, 1);
                ImGui.SameLine();
            }

            if (currentState is AudioToolState.Importing)
            {
                if (ImGui.Button("Import All"))
                {

                }
                ImGui.SameLine();
                ImGui.SeparatorEx(ImGuiSeparatorFlags.Vertical, 1);
                ImGui.SameLine();
            }
            if (currentState is AudioToolState.ConvertingFinished && onImport != null)
            {
                if (ImGui.Button("Continue to Import"))
                {
                    currentState = AudioToolState.Importing;
                }
                ImGui.SameLine();
                ImGui.SeparatorEx(ImGuiSeparatorFlags.Vertical, 1);
                ImGui.SameLine();
            }

            if (currentState is AudioToolState.Converting)
            {
                if (ImGui.Button("Convert All"))
                {
                    if (string.IsNullOrWhiteSpace(outputFolder) || !Directory.Exists(outputFolder))
                    {
                        uiState.StatusMessage = "Please select an output directory for converted files";
                        uiState.IsError = true;
                        uiState.ErrorType = ErrorTypes.NoOutput;
                    }
                    else if (entries == null || entries.Count <= 0)
                    {
                        uiState.StatusMessage = "Please select atleast 1 file to convert";
                        uiState.IsError = true;
                        uiState.ErrorType = ErrorTypes.NoInputs;
                    }
                    else if (!entries.Any(e => !e.IgnoreConvert && e.Info != null))
                    {
                        uiState.StatusMessage = "Please ensure atleast 1 file has an action of 'Convert'";
                        uiState.IsError = true;
                        uiState.ErrorType = ErrorTypes.NoInputs;
                    }
                    else
                    {
                        uiState.StatusMessage = String.Empty;
                        uiState.IsError = false;
                        uiState.ErrorType = ErrorTypes.None;

                        StartConversion();
                    }
                }
                ImGui.SameLine();
                ImGui.SeparatorEx(ImGuiSeparatorFlags.Vertical, 1);
                ImGui.SameLine();
            }

            if (ImGui.Button("Close"))
                ImGui.CloseCurrentPopup();
        }
    }
    void DrawConvertFilesTable()
    {
        float tableHeight = ImGui.GetContentRegionAvail().Y - 75;
        if (tableHeight < 100) tableHeight = 100; // minimum height safety

        ImGui.BeginChild("bulk_table_child", new Vector2(0, tableHeight), ImGuiChildFlags.None, ImGuiWindowFlags.NoMove);
        if (ImGui.BeginTable("bulk_audio_table", 8,
            ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY
            | ImGuiTableFlags.Resizable | ImGuiTableFlags.HighlightHoveredColumn | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoHostExtendX)) // ⭐ resizable/stretch columns
        {
            ImGui.TableSetupColumn("Filename", ImGuiTableColumnFlags.WidthStretch, 200);
            ImGui.TableSetupColumn("Comment", ImGuiTableColumnFlags.WidthStretch, 200);
            ImGui.TableSetupColumn("Channels", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 60);
            ImGui.TableSetupColumn("Sample Rate", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 90);
            ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.NoClip | ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 125);
            ImGui.TableSetupColumn("Mode", ImGuiTableColumnFlags.NoClip | ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 90);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.NoClip | ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 125);
            ImGui.TableSetupColumn(Icons.TrashAlt.ToString(), ImGuiTableColumnFlags.NoClip | ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.NoHeaderLabel, 30);
            ImGui.TableHeadersRow();
            // Set minimum widths

            foreach (var e in entries)
            {
                ImGui.TableNextRow();

                ImGui.TableNextColumn(); ImGui.Text(Path.GetFileName(e.Path));
                ImGui.TableNextColumn(); ImGui.Text(Path.GetFileName(e.Comment));
                ImGui.TableNextColumn(); CenterText(e.Info?.Channels.ToString() ?? "-");
                ImGui.TableNextColumn(); CenterText(e.Info?.Frequency.ToString() ?? "-");

                // Action dropdown
                ImGui.TableNextColumn(); ImGui.PushItemWidth(-1);
                if(e.IsLocked)
                    ImGui.BeginDisabled();
                if (ImGui.BeginCombo($"##act{e.Path}", e.IgnoreConvert ? "Ignore" : "Convert"))
                {
                    if (ImGui.Selectable("Convert", !e.IgnoreConvert)) e.IgnoreConvert = false;
                    if (ImGui.Selectable("Ignore", e.IgnoreConvert)) e.IgnoreConvert = true;
                    ImGui.EndCombo();
                }
                ImGui.PopItemWidth();
                
                // Mode toggle
                ImGui.TableNextColumn();
                ImGui.PushItemWidth(-1);
                ImGuiExt.ButtonDivided($"##mode{e.Path}", "Bitrate", "Quality", ref e.UseBitrate);
                ImGui.PopItemWidth();

                // Value input
                ImGui.TableNextColumn();
                ImGui.PushItemWidth(-1);
                if (e.UseBitrate)
                {
                    ImGui.InputInt($"##br{e.Path}", ref e.Bitrate);
                    e.Bitrate = MathHelper.Clamp(e.Bitrate, 8, 320);
                }
                else if (ImGui.BeginCombo($"##ql{e.Path}", e.Quality.ToString()))
                {
                    foreach (var q in new[] { 100, 90, 80, 70, 60, 50, 40, 30, 20, 10 })
                        if (ImGui.Selectable(q.ToString(), e.Quality == q)) e.Quality = q;
                    ImGui.EndCombo();
                }
                ImGui.PopItemWidth();
                if (e.IsLocked)
                    ImGui.EndDisabled();
                //Delete Button
                ImGui.TableNextColumn();
                ImGui.PushItemWidth(-1);
                if (ImGui.SmallButton($"{Icons.TrashAlt.ToString()}##del{e.Path}"))
                {
                    uiState.DeleteIndex = entries.IndexOf(e);
                    
                }
                ImGui.PopItemWidth();
            }

            ImGui.EndTable();
        }
        if (entries != null && entries.Count > 0 && uiState.DeleteIndex != -1)
        {
            entries.RemoveAt(uiState.DeleteIndex);
            uiState.DeleteIndex = -1;
        }
        ImGui.EndChild();
    }
    void DrawConvertFilesResultsTable()
    {
        float tableHeight = ImGui.GetContentRegionAvail().Y - 75;
        if (tableHeight < 100) tableHeight = 100; // minimum height safety

        ImGui.BeginChild("bulk_table_results_child", new Vector2(0, tableHeight), ImGuiChildFlags.None, ImGuiWindowFlags.NoMove);
        if (ImGui.BeginTable("bulk_audio_results_table", 3,
            ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY
            | ImGuiTableFlags.Resizable | ImGuiTableFlags.HighlightHoveredColumn | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoHostExtendX)) // ⭐ resizable/stretch columns
        {
            ImGui.TableSetupColumn("Success", ImGuiTableColumnFlags.NoClip | ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.NoHeaderLabel, 30);
            ImGui.TableSetupColumn("Filename", ImGuiTableColumnFlags.WidthStretch, 200);
            ImGui.TableSetupColumn("Comments", ImGuiTableColumnFlags.WidthStretch, 200);
            ImGui.TableHeadersRow();
            // Set minimum widths

            foreach (var e in entries)
            {
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.Text(e.Success
                    ? Icons.Cube_LightGreen.ToString()
                    : Icons.Cube_Coral.ToString());

                ImGui.TableNextColumn(); ImGui.Text(Path.GetFileName(e.Path));
                ImGui.TableNextColumn(); ImGui.Text(Path.GetFileName(e.Error));
            }

            ImGui.EndTable();
        }
        
        ImGui.EndChild();
    }
    void DrawOutputFolderSelect()
    {
        ImGui.Text("Output Folder:");
        ImGui.SameLine();
        if (ImGui.Button("Select Folder"))
        {
            _win.QueueUIThread(() =>
            {
                FileDialog.ChooseFolder(
                    path => outputFolder = path
                );
            });
        }
        ImGui.SameLine();
        ImGui.PushItemWidth(-1);  // stretch to fill remaining space
        if (uiState.ErrorType is ErrorTypes.NoOutput)
        {
            ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.4f, 0.05f, 0.05f, 1f));  // dark red
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(0.6f, 0.1f, 0.1f, 1f));
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(0.7f, 0.1f, 0.1f, 1f));

        }
        ImGui.InputText("##out", ref outputFolder, 256);
        if (uiState.ErrorType is ErrorTypes.NoOutput)
            ImGui.PopStyleColor(3);
        ImGui.PopItemWidth();
    }
    void DrawInputFileSelect()
    {
        ImGui.Text("Source Files:  ");
        ImGui.SameLine();
        if (ImGui.Button("Select   Files"))
        {

            _win.QueueUIThread(() =>
            {
                FileDialog.OpenMultiple((files =>
                {
                    if (files == null || files.Length <= 0)
                    {
                        uiState.StatusMessage = "No files were selected";
                        uiState.IsError = true;
                        uiState.ErrorType = ErrorTypes.NoInputs;
                        return;
                    }

                    foreach (var f in files)
                    {
                        try
                        {
                            if (File.Exists(f))
                            {
                                // Single file
                                TryAddFile(f);
                            }
                            else if (Directory.Exists(f))
                            {
                                // Folder -> recursively scan
                                foreach (var file in Directory.EnumerateFiles(
                                    f, "*.*", SearchOption.AllDirectories))
                                {
                                    TryAddFile(file);
                                }
                            }
                            else
                            {
                                log?.AppendText($"Skipping unknown path: {f}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _pm.MessageBox("Error", $"Failed to scan:\n{f}\n\n{ex.Message}");
                        }
                    }
                    uiState.StatusMessage = "Files selected";
                    uiState.IsError = false;
                    uiState.ErrorType = ErrorTypes.None;
                }), AudioInputFilters);
            });
        }
    }
    void DrawStatusBar()
    {
        var region = ImGui.GetContentRegionAvail();
        ImGui.BeginChild("statusbar", new Vector2(region.X, 30), ImGuiChildFlags.Borders, ImGuiWindowFlags.NoScrollbar);

        if (string.IsNullOrEmpty(uiState.StatusMessage))
        {
            ImGui.Text("");
        }
        else if (uiState.IsError)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.3f, 0.3f, 1f)); // red text
            ImGui.Text(uiState.StatusMessage);
            ImGui.PopStyleColor();
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0f, 0.65f, 0.3f, 1f)); // Green text
            ImGui.Text(uiState.StatusMessage);
            ImGui.PopStyleColor();
        }

        if (currentState is AudioToolState.ImportingInProgress or AudioToolState.ConvertingInProgress)
        {
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.37f, 0.12f, 1f)); // Orange text
            ImGui.Text(currentState is AudioToolState.ConvertingInProgress ? "Converting Files " : "Importing Files ");
            ImGui.PopStyleColor();
            ImGui.SameLine();
            ImGui.ProgressBar(uiState.Progress, new Vector2(300, 60));
        }

        ImGui.EndChild();
    }
    #endregion

    #region Conversion Methods
    void StartConversion()
    {
        currentState = AudioToolState.ConvertingInProgress;
        uiState.Progress = 0;
        log = new AppLog();

        ThreadPool.QueueUserWorkItem(_ =>
        {
            int total = entries.Count;
            int index = 0;

            foreach (var e in entries)
            {
                if (!e.IgnoreConvert)
                {
                    try
                    {
                        ConvertOne(e);
                        e.Success = true;
                    }
                    catch (Exception ex)
                    {
                        e.Success = false;
                        e.Error = ex.Message;
                        log.AppendText($"Failed: {e.Path} -> {ex.Message}");
                    }
                }

                index++;
                uiState.Progress = index / total;
            }
        });

        currentState = AudioToolState.ConvertingFinished;
    }
    void ConvertOne(BulkAudioEntry e)
    {
        string outPath = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(e.Path) + ".wav");
        
        try
        {
            using var stream = File.Create(outPath);
            if (e.Info.Kind == AudioImportKind.WavUncompressed)
            {
                File.WriteAllBytes(outPath, File.ReadAllBytes(e.Path));
                log.AppendText($"Copied: {e.Path}");
                e.Path = outPath;
                e.Success = true;
                e.Error = String.Empty;
                return;
            }

            Mp3EncodePreset preset = e.UseBitrate
                ? Mp3EncodePreset.Bitrate
                : QualityToPreset(e.Quality);

            using var input = File.OpenRead(e.Path);

            Mp3Encoder.EncodeStream(input, stream, e.Bitrate, preset, CancellationToken.None,
                msg => log.AppendText($"{Path.GetFileName(e.Path)}: {msg}"));

            log.AppendText($"Converted: {e.Path}");
            e.Path = outPath;
        }
        catch (Exception ex)
        {
            e.Success = false;
            e.Error = ex.Message.Replace("\\","\\\\");
            return;
        }
        // WAV passthrough
        e.Path = outPath;
        e.Success = true;
        e.Error = String.Empty;
    }
    Mp3EncodePreset QualityToPreset(int q) =>
        q switch
        {
            100 => Mp3EncodePreset.Quality100,
            90 => Mp3EncodePreset.Quality90,
            80 => Mp3EncodePreset.Quality80,
            70 => Mp3EncodePreset.Quality70,
            60 => Mp3EncodePreset.Quality60,
            50 => Mp3EncodePreset.Quality50,
            40 => Mp3EncodePreset.Quality40,
            30 => Mp3EncodePreset.Quality30,
            20 => Mp3EncodePreset.Quality20,
            _ => Mp3EncodePreset.Quality10
        };
    #endregion

    void TryAddFile(string path)
    {
        if (entries.Exists(e => e.Path.Equals(path, StringComparison.OrdinalIgnoreCase)))
            return;

        AudioImportInfo info = null;

        try
        {
            using (var s = File.OpenRead(path))
            {
                info = AudioImporter.Analyze(s);
            }
        }
        catch
        {
            // Treat unreadable files as invalid
            info = null;
        }

        // If the format is unsupported or corrupt,
        // we still add an entry with Info = null so the table shows "Invalid".
        entries.Add(new BulkAudioEntry(path, info));
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
}

public class BulkAudioEntry
{
    public AudioImportInfo Info;
    public string Comment;
    public string Path;
    public bool IgnoreConvert = false;
    public bool IgnoreImport = false;
    public bool UseBitrate = true;
    public int Bitrate = 320;
    public int Quality = 100;
    public bool IsLocked = false;
    public bool Success;
    public string Error;

    public BulkAudioEntry(string path, AudioImportInfo info)
    {
        Info = info;
        Path = path;
        if (info == null) {
            Comment = "Invalid or unsupported file";
            IgnoreConvert = true;
            IsLocked = true;
            return;
        }

        switch (info.Kind)
        {
            case AudioImportKind.Mp3 when info.Trim != 0 && info.Samples != 0:
                Comment = $"Input is already mp3 (with LAME trimming info)\nWrapping .mp3 in .wav container, converting trim to FL\nTrim: {info.Trim}, Length {info.Samples}";
                break;
            case AudioImportKind.Mp3:
                Comment = $"Input is already mp3 (no trimming info)\nWrapping .mp3 in .wav container";
                break;
            case AudioImportKind.WavUncompressed:
                Comment = $"Input is uncompressed .wav\nIt can be used as-is";
                IgnoreConvert = true;
                break;
            case AudioImportKind.NeedsConversion:
                Comment = $"Input needs conversion to be used";
                break;
            case AudioImportKind.Copy:
                Comment = "Already MP3 (will be wrapped)";
                break;
            default:
                Comment = "Needs conversion";
                break;
        }
    }
}
public class UiState
{
    public enum ErrorTypes
    {
        None = 0,
        NoInputs,
        NoOutput
    }
    public string StatusMessage { get; set; } = string.Empty;
    public ErrorTypes ErrorType { get; set; } = ErrorTypes.None;
    public bool IsError { get; set; }
    public int Progress { get; set; }
    public int DeleteIndex { get; set; } = -1;
}
