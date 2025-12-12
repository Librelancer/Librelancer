using LibreLancer.ContentEdit;

namespace LancerEdit.Audio;

public class ConversionEntry
{
    public enum ConversionAction
    {
        Convert,
        Ignore
    }
    public enum ConversionMode
    {
        Bitrate,
        Quality
    }

    public string OriginalPath = string.Empty;
    public string OutputPath = string.Empty;
    public string Comment = string.Empty;
    public AudioImportInfo Info = new();
    public ConversionAction Action = ConversionAction.Ignore;
    public ConversionMode Mode = ConversionMode.Bitrate;
    public bool IsLocked = false;
    public int Bitrate = 320;
    public int Quality = 100;

    // MP3 Trim Fields (only used when Info.Kind == Mp3 && Info.Samples == 0)
    public bool RequiresTrim => Info != null && Info.Kind == AudioImportKind.Mp3 && Info.Samples == 0;
    public int TrimStart = 0;
    public int TrimEnd = 0;

    // Conversion Results
    public bool Success = false;
    public string Error = string.Empty;

    public ConversionEntry(string path, AudioImportInfo info)
    {
        OriginalPath = path;
        Info = info;

        if (info == null)
        {
            Comment = "Invalid or unsupported file";
            Action = ConversionAction.Ignore;
            IsLocked = true;
            return;
        }

        switch (info.Kind)
        {
            case AudioImportKind.Mp3 when info.Trim != 0 && info.Samples != 0:
                Comment = $"Already MP3 with LAME trim info\nWrap as Freelancer WAV\nTrim: {info.Trim}, Samples: {info.Samples}";
                Action = ConversionAction.Convert;
                break;

            case AudioImportKind.Mp3 when info.Samples == 0:
                Comment = "MP3 (no trimming info)\nRequires manual trim entry";
                Action = ConversionAction.Convert;
                break;

            case AudioImportKind.WavUncompressed:
                Comment = "Uncompressed WAV\nCan be used as-is";
                Action = ConversionAction.Convert;
                break;

            case AudioImportKind.Copy:
                Comment = "Already Freelancer-compliant WAV (MP3-encoded)";
                Action = ConversionAction.Ignore;
                break;

            case AudioImportKind.NeedsConversion:
                Comment = "Needs conversion to Freelancer WAV";
                Action = ConversionAction.Convert;
                break;

            default:
                Comment = "Unsupported or unknown format";
                Action = ConversionAction.Ignore;
                IsLocked = true;
                break;
        }
    }
}
