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

namespace LancerEdit;

public class BulkAudioImportPopup : PopupWindow
{
    public override string Title { get; set; } = "Bulk Audio Import / Convert";
    public override bool NoClose => converting;

    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoCollapse;

    public static readonly FileDialogFilters AudioInputFilters = new FileDialogFilters(
        new FileFilter("Supported audio formats", "wav", "mp3", "ogg", "flac"),
        new FileFilter("WAV files", "wav"),
        new FileFilter("MP3 files", "mp3"),
        new FileFilter("Ogg files", "ogg"),
        new FileFilter("Flac files", "flac"));

    MainWindow win;
    PopupManager pm;
    List<BulkAudioEntry> entries = new();
    string outputFolder = "";
    bool converting = false;
    float progress = 0;
    AppLog log = null;


    string statusMessage = "";
    bool isOutputFolderInvalid = false;
    bool isInputFilesInvalid = false;
    private int deleteIndex = -1;

    public BulkAudioImportPopup(MainWindow win, PopupManager pm)
    {
        this.win = win;
        this.pm = pm;
    }

    public override void Draw(bool appearing) // Called once per frame
    {
        if (appearing) // set initial window size
        {
            ImGui.SetNextWindowSize(new Vector2(900, 400), ImGuiCond.Always);
        }

        // SOURCE FILE SELECTOR
        ImGui.Text("Source Files:  ");
        ImGui.SameLine();
        if (ImGui.Button("Select   Files"))
        {

            win.QueueUIThread(() =>
            {
                FileDialog.OpenMultiple((files =>
                    {
                        if (files == null || files.Length <= 0)
                        {
                            statusMessage = "No files were selected";
                            isInputFilesInvalid = true;
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
                                        if (IsSupportedAudioFile(file))
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
                                pm.MessageBox("Error", $"Failed to scan:\n{f}\n\n{ex.Message}");
                            }
                        }

                        statusMessage = "Files selected";
                        isInputFilesInvalid = false;
                    }), AudioInputFilters);
            });
        }

        // OUTPUT FOLDER SELECTOR
        ImGui.Text("Output Folder:");
        ImGui.SameLine();
        if (ImGui.Button("Select Folder"))
        {
            win.QueueUIThread(() =>
            {
                FileDialog.ChooseFolder(
                    path => outputFolder = path
                );
            });
        }
        ImGui.SameLine();
        ImGui.PushItemWidth(-1);  // stretch to fill remaining space
        if (isOutputFolderInvalid)
        {
            ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.4f, 0.05f, 0.05f, 1f));  // dark red
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(0.6f, 0.1f, 0.1f, 1f));
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(0.7f, 0.1f, 0.1f, 1f));

        }
        ImGui.InputText("##out", ref outputFolder, 256);
        if (isOutputFolderInvalid)
            ImGui.PopStyleColor(3);
        ImGui.PopItemWidth();
        //ImGui.Separator();


        // TABLE
        float tableHeight = ImGui.GetContentRegionAvail().Y - 75;
        if (tableHeight < 100) tableHeight = 100; // minimum height safety

        ImGui.BeginChild("bulk_table_child", new Vector2(0, tableHeight), ImGuiChildFlags.None, ImGuiWindowFlags.NoMove);
        if (ImGui.BeginTable("bulk_audio_table", 8,
            ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY
            | ImGuiTableFlags.Resizable | ImGuiTableFlags.HighlightHoveredColumn | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoHostExtendX)) // ⭐ resizable/stretch columns
        {
            ImGui.TableSetupColumn("Filename", ImGuiTableColumnFlags.WidthStretch , 200);
            ImGui.TableSetupColumn("Channels", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 60);
            ImGui.TableSetupColumn("Bitrate", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 90);
            ImGui.TableSetupColumn("Comment", ImGuiTableColumnFlags.WidthStretch, 200);
            ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.NoClip | ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 125);
            ImGui.TableSetupColumn("Mode", ImGuiTableColumnFlags.NoClip | ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 90);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.NoClip | ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 125);
            ImGui.TableSetupColumn(Icons.TrashAlt.ToString(), ImGuiTableColumnFlags.NoClip | ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize| ImGuiTableColumnFlags.NoHeaderLabel, 30);
            ImGui.TableHeadersRow();
            // Set minimum widths

            foreach (var e in entries)
            {
                ImGui.TableNextRow();

                ImGui.TableNextColumn(); ImGui.Text(Path.GetFileName(e.Path));
                ImGui.TableNextColumn(); CenterText(e.Info?.Channels.ToString() ?? "-");
                ImGui.TableNextColumn(); CenterText(e.Info?.Frequency.ToString() ?? "-");
                ImGui.TableNextColumn(); ImGui.Text(Path.GetFileName(e.Comment));

                // Action dropdown
                ImGui.TableNextColumn(); ImGui.PushItemWidth(-1);
                if (ImGui.BeginCombo($"##act{e.Path}", e.Ignore ? "Ignore" : "Convert"))
                {
                    if (ImGui.Selectable("Convert", !e.Ignore)) e.Ignore = false;
                    if (ImGui.Selectable("Ignore", e.Ignore)) e.Ignore = true;
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
                //Delete Button
                ImGui.TableNextColumn();
                ImGui.PushItemWidth(-1);
                if (ImGui.SmallButton($"{Icons.TrashAlt.ToString()}##del{e.Path}"))
                {
                    deleteIndex = entries.IndexOf(e);
                }
                ImGui.PopItemWidth();
            }

            ImGui.EndTable();
        }
        if (entries != null && entries.Count > 0 && deleteIndex != -1)
        {
            entries.RemoveAt(deleteIndex);
            deleteIndex = -1;
        }

        ImGui.EndChild();

        ImGui.Separator();

        // ACTION BUTTONS
        using (var tb = Toolbar.Begin("##actions", false))
        {

            if (ImGui.Button("Clear All Files"))
                entries.Clear();
            ImGui.SameLine();
            ImGui.SeparatorEx(ImGuiSeparatorFlags.Vertical, 1);
            ImGui.SameLine();
            if (ImGui.Button("Convert All"))
            {
                if (string.IsNullOrWhiteSpace(outputFolder) || !Directory.Exists(outputFolder))
                {
                    statusMessage = "Please select an output directory for converted files";
                    isOutputFolderInvalid = true;
                    isInputFilesInvalid = false;
                }
                else if (entries == null || entries.Count <= 0)
                {
                    statusMessage = "Please select atleast 1 file to convert";
                    isInputFilesInvalid = true;
                    isOutputFolderInvalid = false;
                }
                else if (!entries.Any(e => !e.Ignore && e.Info != null))
                {
                    statusMessage = "Please ensure atleast 1 file has an action of 'Convert'";
                    isInputFilesInvalid = false;
                    isOutputFolderInvalid = false;
                }
                else
                {
                    statusMessage = "";
                    isInputFilesInvalid = false;
                    isOutputFolderInvalid = false;

                    StartConversion();
                }
            }

            ImGui.SameLine();
            ImGui.SeparatorEx(ImGuiSeparatorFlags.Vertical, 1);
            ImGui.SameLine();

            if (ImGui.Button("Close"))
                ImGui.CloseCurrentPopup();
        }

        ImGui.Separator();
        DrawStatusBar();


    }


    // CONVERSION LOGIC
    void StartConversion()
    {
        converting = true;
        progress = 0;
        log = new AppLog();

        ThreadPool.QueueUserWorkItem(_ =>
        {
            int total = entries.Count;
            int index = 0;

            foreach (var e in entries)
            {
                if (!e.Ignore)
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
                progress = (float)index / total;
            }

            win.QueueUIThread(() => converting = false);
        });
    }

    void ConvertOne(BulkAudioEntry e)
    {
        string outPath = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(e.Path) + ".wav");

        using var stream = File.Create(outPath);

        // WAV passthrough
        if (e.Info.Kind == AudioImportKind.WavUncompressed)
        {
            File.WriteAllBytes(outPath, File.ReadAllBytes(e.Path));
            log.AppendText($"Copied: {e.Path}");
            return;
        }

        Mp3EncodePreset preset = e.UseBitrate
            ? Mp3EncodePreset.Bitrate
            : QualityToPreset(e.Quality);

        using var input = File.OpenRead(e.Path);

        Mp3Encoder.EncodeStream(input, stream, e.Bitrate, preset, CancellationToken.None,
            msg => log.AppendText($"{Path.GetFileName(e.Path)}: {msg}"));

        log.AppendText($"Converted: {e.Path}");
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

    void TryAddFile(string path)
    {
        Console.WriteLine($"trying to add {path} to the list");
        // Prevent duplicates
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

    bool IsSupportedAudioFile(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        return ext is ".wav" or ".mp3" or ".flac" or ".aiff" or ".ogg";
    }

    void DrawStatusBar()
    {
        var region = ImGui.GetContentRegionAvail();
        ImGui.BeginChild("statusbar", new Vector2(region.X, 30), ImGuiChildFlags.Borders, ImGuiWindowFlags.NoScrollbar);

        if (converting)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.37f, 0.12f, 1f)); // Orange text
            ImGui.Text("Converting Files ");
            ImGui.PopStyleColor();
            ImGui.SameLine();
            ImGui.ProgressBar(progress, new Vector2(300, 0));
        }
        else if (string.IsNullOrEmpty(statusMessage))
        {
            ImGui.Text("");
        }
        else if (isInputFilesInvalid || isOutputFolderInvalid)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.3f, 0.3f, 1f)); // red text
            ImGui.Text(statusMessage);
            ImGui.PopStyleColor();
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0f, 0.65f, 0.3f, 0.4f)); // Green text
            ImGui.Text(statusMessage);
            ImGui.PopStyleColor();
        }

        ImGui.EndChild();
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

// ENTRY MODEL
public class BulkAudioEntry
{
    public string Path;
    public AudioImportInfo Info;
    public string Comment;

    public bool Ignore = false;
    public bool UseBitrate = true;
    public int Bitrate = 320;
    public int Quality = 100;

    public bool Success;
    public string Error;

    public BulkAudioEntry(string path, AudioImportInfo info)
    {
        Path = path;
        Info = info;

        if (info == null)
            Comment = "Invalid/unsupported file";
        else if (info.Kind == AudioImportKind.WavUncompressed)
            Comment = "Uncompressed WAV (no conversion needed)";
        else if (info.Kind == AudioImportKind.Copy)
            Comment = "Already Freelancer-encoded";
        else if (info.Kind == AudioImportKind.Mp3)
            Comment = "Already MP3 (will be wrapped)";
        else
            Comment = "Needs conversion";
    }
}
