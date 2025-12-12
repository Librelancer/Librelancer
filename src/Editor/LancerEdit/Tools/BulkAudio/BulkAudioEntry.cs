using System;
using System.IO;
using LibreLancer.ContentEdit;

namespace LancerEdit.Tools.BulkAudio;

public class BulkAudioEntry
{
    public string OriginalPath { get; set; }
    public string OutputPath { get; set; }

    public AudioImportInfo Info { get; set; }
    public string Comment { get; set; }

    // UI Settings
    public bool IgnoreConvert { get; set; }
    public bool IsLocked { get; set; }
    public bool UseBitrate { get; set; } = true;
    public int Bitrate { get; set; } = 320;
    public int Quality { get; set; } = 100;

    // MP3 Trim Fields (only used when Info.Kind == Mp3 && Info.Samples == 0)
    public bool RequiresTrim => Info != null && Info.Kind == AudioImportKind.Mp3 && Info.Samples == 0;
    public int TrimStart { get; set; }
    public int TrimEnd { get; set; }


    // Conversion Results
    public bool Success { get; set; }
    public string Error { get; set; }

    public BulkAudioEntry(string path, AudioImportInfo info)
    {
        OriginalPath = path;
        Info = info;

        if (info == null)
        {
            Comment = "Invalid or unsupported file";
            IgnoreConvert = true;
            IsLocked = true;
            return;
        }

        switch (info.Kind)
        {
            case AudioImportKind.Mp3 when info.Trim != 0 && info.Samples != 0:
                Comment = $"Already MP3 with LAME trim info\nWrap as Freelancer WAV\nTrim: {info.Trim}, Samples: {info.Samples}";
                break;

            case AudioImportKind.Mp3 when info.Samples == 0:
                Comment = "MP3 (no trimming info)\nRequires manual trim entry";
                break;

            case AudioImportKind.WavUncompressed:
                Comment = "Uncompressed WAV\nCan be used as-is";
                break;

            case AudioImportKind.Copy:
                Comment = "Already Freelancer-compliant WAV (MP3-encoded)";
                break;

            case AudioImportKind.NeedsConversion:
                Comment = "Needs conversion to Freelancer WAV";
                break;

            default:
                Comment = "Unsupported or unknown format";
                IgnoreConvert = true;
                IsLocked = true;
                break;
        }
    }
}
