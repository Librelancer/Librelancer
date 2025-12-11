using System;
using System.Threading;
using System.Threading.Tasks;

namespace LancerEdit.Tools.BulkAudio.Services;

public interface IAudioConversionService
{
    Task<ConversionResult> ConvertAsync(
        ConversionJob job,
        IProgress<float> progress,
        CancellationToken token,
        Action<string> logCallback = null
    );
}
