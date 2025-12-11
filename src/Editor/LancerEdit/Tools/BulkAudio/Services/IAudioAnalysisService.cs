using LibreLancer.ContentEdit;

namespace LancerEdit.Tools.BulkAudio.Services;

public interface IAudioAnalysisService
{
    AudioImportInfo Analyze(string path);
}
