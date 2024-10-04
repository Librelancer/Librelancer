using System;
using System.IO;
using System.Numerics;
using System.Threading;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.Dialogs;
using LibreLancer.ImUI;

namespace LancerEdit;

public class AudioImportPopup : PopupWindow
{
    public static readonly FileDialogFilters AudioInputFilters = new FileDialogFilters(
        new FileFilter("Supported audio formats", "wav", "mp3", "ogg", "flac"),
        new FileFilter("WAV files", "wav"),
        new FileFilter("MP3 files", "mp3"),
        new FileFilter("Ogg files", "ogg"),
        new FileFilter("Flac files", "flac"));

    public static readonly FileDialogFilters AudioOutputFilters = new FileDialogFilters(
        new FileFilter("MP3-Encoded Wav", "wav"));

    public static void Run(MainWindow win, PopupManager m, Action<byte[]> onImport)
    {
        FileDialog.Open(path =>
        {
            AudioImportInfo info;
            using (var s = File.OpenRead(path))
            {
                info = AudioImporter.Analyze(s);
            }
            if (info == null)
            {
                m.MessageBox(onImport == null ? "Import" : "Convert Audio", $"'{Path.GetFileName(path)}' is corrupt or not a supported format");
            }
            else if (info.Kind == AudioImportKind.Copy)
            {
                if (onImport != null)
                {
                    m.MessageBox("Import", "Imported Freelancer-encoded wav as-is");
                    onImport(File.ReadAllBytes(path));
                }
                else
                {
                    m.MessageBox("Convert Audio", $"'{Path.GetFileName(path)}' is already Freelancer-encoded wav");
                }
            }
            else
            {
                m.OpenPopup(new AudioImportPopup(win, onImport, info, path));
            }
        }, AudioInputFilters);
    }

    public override string Title { get; set; }

    public override bool NoClose => true;
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;

    private string path;
    private string name;
    private AudioImportInfo info;
    private Action<byte[]> onImport;
    private MainWindow win;

    private AudioImportPopup(MainWindow win, Action<byte[]> onImport, AudioImportInfo info, string path)
    {
        Title = onImport == null ? "Convert Audio" : "Import Audio";
        this.onImport = onImport;
        this.path = path;
        this.name = $"Source: {Path.GetFileName(path)}";
        this.info = info;
        this.win = win;
    }

    private bool converting = false;

    void LogText(string text)
    {
        win.QueueUIThread(() =>
        {
            if(log != null)
                log.AppendText(text + "\n");
        });
    }

    void RunConversion(Stream outputStream, Action completed)
    {
        if (info.Kind != AudioImportKind.Mp3)
        {
            log = new AppLog();
            cancellation = new CancellationTokenSource();
            var stream = File.OpenRead(path);
            Mp3EncodePreset preset = Mp3EncodePreset.Bitrate;
            if (!useBitrate)
            {
                preset = quality switch
                {
                    10 => Mp3EncodePreset.Quality10,
                    20 => Mp3EncodePreset.Quality20,
                    30 => Mp3EncodePreset.Quality30,
                    40 => Mp3EncodePreset.Quality40,
                    50 => Mp3EncodePreset.Quality50,
                    60 => Mp3EncodePreset.Quality60,
                    70 => Mp3EncodePreset.Quality70,
                    80 => Mp3EncodePreset.Quality80,
                    90 => Mp3EncodePreset.Quality90,
                    _ => Mp3EncodePreset.Quality100
                };
            }
            converting = true;
            Mp3Encoder.EncodeStream(stream, outputStream, bitrate, preset, cancellation.Token, LogText)
                .ContinueWith(x =>
                {
                    stream.Dispose();
                    outputStream.Dispose();
                    win.QueueUIThread(() =>
                    {
                        if (!cancellation.IsCancellationRequested)
                        {
                            completed?.Invoke();
                            finished = true;
                        }
                        cancellation.Dispose();
                    });
                });
        }
        else
        {
            AudioImporter.ImportMp3(path, outputStream);
            outputStream.Dispose();
            completed?.Invoke();
            ImGui.CloseCurrentPopup();
        }
    }

    private int quality = 100;
    private bool useBitrate = true;
    private int bitrate = 320;
    private CancellationTokenSource cancellation;
    private bool finished = false;
    private AppLog log;

    public override void Draw()
    {
        if (converting)
        {
            log.Draw(false, new Vector2(350, 350) * ImGuiHelper.Scale);
            if (ImGuiExt.Button("Ok", finished)) {
                ImGui.CloseCurrentPopup();
                log.Dispose();
                log = null;
            }
            ImGui.SameLine();
            if (ImGuiExt.Button("Cancel", !finished))
            {
                cancellation.Cancel();
                ImGui.CloseCurrentPopup();
                log.Dispose();
                log = null;
            }
            return;
        }
        ImGui.TextUnformatted(name);
        ImGui.TextUnformatted($"Channels: {info.Channels}");
        ImGui.TextUnformatted($"Sample Rate: {info.Frequency}");
        ImGui.Separator();
        switch (info.Kind)
        {
            case AudioImportKind.Mp3 when info.Trim != 0 && info.Samples != 0:
                ImGui.Text("Input is already mp3 (with LAME trimming info)");
                ImGui.Text("Wrapping .mp3 in .wav container, converting trim to FL");
                ImGui.TextUnformatted($"Trim: {info.Trim}, Length {info.Samples}");
                break;
            case AudioImportKind.Mp3:
                ImGui.Text("Input is already mp3 (no trimming info)");
                ImGui.Text("Wrapping .mp3 in .wav container");
                break;
            case AudioImportKind.WavUncompressed:
                ImGui.Text("Input is uncompressed .wav");
                ImGui.Text("It can be used as-is");
                break;
            case AudioImportKind.NeedsConversion:
                ImGui.Text("Input needs conversion to be used");
                break;
        }

        if (info.Kind != AudioImportKind.Mp3)
        {
            ImGuiExt.ButtonDivided("bvsq", "Bitrate", "Quality", ref useBitrate);
            if (useBitrate)
            {
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Bitrate (8-320): ");
                ImGui.InputInt("##bitrate", ref bitrate);
                bitrate = MathHelper.Clamp(bitrate, 8, 320);
            }
            else
            {
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Quality: ");
                if (ImGui.BeginCombo("##quality", quality.ToString()))
                {
                    if (ImGui.Selectable("100", quality == 100)) quality = 100;
                    if (ImGui.Selectable("90", quality == 90)) quality = 90;
                    if (ImGui.Selectable("80", quality == 80)) quality = 80;
                    if (ImGui.Selectable("70", quality == 70)) quality = 70;
                    if (ImGui.Selectable("60", quality == 60)) quality = 60;
                    if (ImGui.Selectable("50", quality == 50)) quality = 50;
                    if (ImGui.Selectable("40", quality == 40)) quality = 40;
                    if (ImGui.Selectable("30", quality == 30)) quality = 30;
                    if (ImGui.Selectable("20", quality == 20)) quality = 20;
                    if (ImGui.Selectable("10", quality == 10)) quality = 10;
                    ImGui.EndCombo();
                }
            }
        }
        ImGui.Separator();
        if (onImport != null &&
            info.Kind == AudioImportKind.WavUncompressed)
        {
            if (ImGui.Button("Import as-is"))
            {
                onImport(File.ReadAllBytes(path));
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
        }
        if (onImport == null && ImGui.Button("Convert"))
        {
            FileDialog.Save(x =>
            {
                RunConversion(File.Create(x), null);
            }, AudioOutputFilters);
        }
        if (onImport != null && ImGui.Button("Convert + Import"))
        {
            var stream = new MemoryStream();
            RunConversion(stream, () => { onImport(stream.ToArray()); });
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
        {
            ImGui.CloseCurrentPopup();
        }
    }

}
