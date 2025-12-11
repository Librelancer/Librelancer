using LibreLancer.ContentEdit;
using System;

namespace LancerEdit.Tools.BulkAudio;

public class ConversionJob
{
    public string InputPath { get; set; }
    public string OutputPath { get; set; }
    public AudioImportInfo Info { get; set; }

    public bool UseBitrate { get; set; }
    public int Bitrate { get; set; }
    public int Quality { get; set; }

    // Trim fields (only used for MP3 needing manual trim)
    public int ManualTrimStart { get; set; }
    public int ManualTrimEnd { get; set; }

    public ConversionJob(BulkAudioEntry entry, string outputRoot)
    {
        InputPath = entry.OriginalPath;
        Info = entry.Info;

        OutputPath = System.IO.Path.Combine(
            outputRoot,
            System.IO.Path.GetFileNameWithoutExtension(entry.OriginalPath) + ".wav"
        );

        UseBitrate = entry.UseBitrate;
        Bitrate = entry.Bitrate;
        Quality = entry.Quality;

        ManualTrimStart = entry.TrimStart;
        ManualTrimEnd = entry.TrimEnd;
    }
}
