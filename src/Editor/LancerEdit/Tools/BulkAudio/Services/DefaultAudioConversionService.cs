using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LibreLancer.ContentEdit;
using LibreLancer.Media;

namespace LancerEdit.Tools.BulkAudio.Services;

public class DefaultAudioConversionService : IAudioConversionService
{
    public async Task<ConversionResult> ConvertAsync(
        ConversionJob job,
        IProgress<float> progress,
        CancellationToken token,
        Action<string> logCallback = null)
    {
        try
        {
            token.ThrowIfCancellationRequested();

            // Determine conversion type
            switch (job.Info.Kind)
            {
                case AudioImportKind.WavUncompressed:
                    return await PassthroughWav(job, token);

                case AudioImportKind.Copy:
                    return await PassthroughCopy(job, token);

                case AudioImportKind.Mp3 when job.Info.Samples != 0:
                    // MP3 with LAME trimming info
                    return await WrapMp3WithTrim(job, token);

                case AudioImportKind.Mp3:
                    // MP3 without trim: user must provide trim values
                    return await WrapMp3ManualTrim(job, token);

                case AudioImportKind.NeedsConversion:
                default:
                    // Re-encode using MP3Encoder
                    return await ReencodeToFreelancerWav(job, progress, token, logCallback);
            }
        }
        catch (OperationCanceledException)
        {
            return ConversionResult.Fail("Cancelled");
        }
        catch (Exception ex)
        {
            return ConversionResult.Fail(ex.Message);
        }
    }

    // ----------------------------------------------------------------------
    // WAV Passthrough (Uncompressed WAV)
    // ----------------------------------------------------------------------
    private async Task<ConversionResult> PassthroughWav(ConversionJob job, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        File.Copy(job.InputPath, job.OutputPath, overwrite: true);

        return ConversionResult.Ok(job.OutputPath);
    }

    // ----------------------------------------------------------------------
    // WAV Passthrough (Freelancer MP3-in-WAV container)
    // AudioImportKind.Copy case in original code
    // ----------------------------------------------------------------------
    private async Task<ConversionResult> PassthroughCopy(ConversionJob job, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        File.Copy(job.InputPath, job.OutputPath, overwrite: true);
        return ConversionResult.Ok(job.OutputPath);
    }

    // ----------------------------------------------------------------------
    // Wrap MP3 with LAME trim (AudioImporter.ImportMp3)
    // ----------------------------------------------------------------------
    private async Task<ConversionResult> WrapMp3WithTrim(ConversionJob job, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        using var output = File.Create(job.OutputPath);
        AudioImporter.ImportMp3(job.InputPath, output, job.Info.Trim, job.Info.Samples);

        return ConversionResult.Ok(job.OutputPath);
    }

    // ----------------------------------------------------------------------
    // Wrap MP3 without trimming info, use manual trim (UI required)
    // ----------------------------------------------------------------------
    private async Task<ConversionResult> WrapMp3ManualTrim(ConversionJob job, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        using var output = File.Create(job.OutputPath);
        AudioImporter.ImportMp3(job.InputPath, output, job.ManualTrimStart, job.ManualTrimEnd);

        return ConversionResult.Ok(job.OutputPath);
    }

    // ----------------------------------------------------------------------
    // Re-encode audio to MP3 → wrap into Freelancer WAV container
    // (OGG, FLAC, other formats)
    // ----------------------------------------------------------------------
    private async Task<ConversionResult> ReencodeToFreelancerWav(
        ConversionJob job,
        IProgress<float> progress,
        CancellationToken token,
        Action<string> logCallback)
    {
        token.ThrowIfCancellationRequested();

        using var input = File.OpenRead(job.InputPath);
        using var output = File.Create(job.OutputPath);

        Mp3EncodePreset preset = job.UseBitrate
            ? Mp3EncodePreset.Bitrate
            : QualityToPreset(job.Quality);

        var tcs = new TaskCompletionSource<ConversionResult>();

        Mp3Encoder.EncodeStream(
                input,
                output,
                job.Bitrate,
                preset,
                token,
                msg => logCallback?.Invoke(msg)
            )
            .ContinueWith(task =>
            {
                if (task.IsCanceled)
                {
                    tcs.TrySetResult(ConversionResult.Fail("Cancelled"));
                    return;
                }

                if (task.IsFaulted)
                {
                    tcs.TrySetResult(ConversionResult.Fail(task.Exception?.GetBaseException().Message));
                    return;
                }

                tcs.TrySetResult(ConversionResult.Ok(job.OutputPath));
            });

        return await tcs.Task;
    }

    // Quality-to-preset conversion (preserves original mapping)
    private Mp3EncodePreset QualityToPreset(int q) =>
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
            10 => Mp3EncodePreset.Quality10,
            _ => Mp3EncodePreset.Quality100
        };
}
