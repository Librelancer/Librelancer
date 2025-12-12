using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LibreLancer.ContentEdit;

namespace LancerEdit.Audio;

public static class AudioConverter
{
    public static AudioImportInfo Analyze(string path)
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
    public static async Task<EditResult<string>> ConvertAsync(
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
            return EditResult<string>.Error("Cancelled");
        }
        catch (Exception ex)
        {
            return EditResult<string>.Error(ex.ToString());
        }
    }

    private static async Task<EditResult<string>> PassthroughWav(
        ConversionJob job, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        File.Copy(job.InputPath, job.OutputPath, overwrite: true);
        return job.OutputPath.AsResult();
    }
    private static async Task<EditResult<string>> PassthroughCopy(
        ConversionJob job, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        File.Copy(job.InputPath, job.OutputPath, overwrite: true);
        return job.OutputPath.AsResult();
    }
    private static async Task<EditResult<string>> WrapMp3WithTrim(
        ConversionJob job, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        using var output = File.Create(job.OutputPath);
        AudioImporter.ImportMp3(job.InputPath, output, job.Info.Trim, job.Info.Samples);

        return job.OutputPath.AsResult();
    }
    private static async Task<EditResult<string>> WrapMp3ManualTrim(ConversionJob job, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        using var output = File.Create(job.OutputPath);
        AudioImporter.ImportMp3(job.InputPath, output, job.ManualTrimStart, job.ManualTrimEnd);

        return job.OutputPath.AsResult();
    }
    private static async Task<EditResult<string>> ReencodeToFreelancerWav(
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

        var tcs = new TaskCompletionSource<EditResult<string>>();

        await Mp3Encoder.EncodeStream(
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
                    tcs.TrySetResult(EditResult<string>.Error("Cancelled"));
                    return;
                }

                if (task.IsFaulted)
                {
                    tcs.TrySetResult(EditResult<string>.Error(
                        task.Exception?.GetBaseException().Message
                    ));
                    return;
                }

                tcs.TrySetResult(job.OutputPath.AsResult());
            });

        return await tcs.Task;
    }
    private static Mp3EncodePreset QualityToPreset(int q) =>
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
