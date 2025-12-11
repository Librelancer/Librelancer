using System.IO;
using LibreLancer.ContentEdit;

namespace LancerEdit.Tools.BulkAudio.Services;

public class DefaultAudioAnalysisService : IAudioAnalysisService
{
    public AudioImportInfo Analyze(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            return AudioImporter.Analyze(stream);
        }
        catch
        {
            return null; // treat as unsupported or corrupt
        }
    }
}
