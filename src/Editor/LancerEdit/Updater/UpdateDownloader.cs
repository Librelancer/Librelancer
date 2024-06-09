using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using LibreLancer.ContentEdit;

namespace LancerEdit.Updater;

public static class UpdateDownloader
{
    private const int BufferSize = 8192;

    public delegate void DownloadProgress(long downloaded, long total);

    public static async Task<EditResult<string>> DownloadUpdate(string url, DownloadProgress progress = null, CancellationToken cancellationToken = default)
    {
        string result = null;
        try
        {
            using var http = new HttpClient();
            using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return EditResult<string>.Error($"Update server returned HTTP status code {response.StatusCode}");
            result = Path.GetTempFileName();
            await using (var stream = File.Create(result)) {
                //Copy sfx header (linux only)
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    await using (var src =
                                 typeof(UpdateDownloader).Assembly.GetManifestResourceStream(
                                     "LancerEdit.Updater.Sfx-linux"))
                        await src!.CopyToAsync(stream);
                }
                //Download archive
                var totalLength = response.Content.Headers.ContentLength ?? -1;
                await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
                var buffer = new byte[BufferSize];
                long totalBytesRead = 0;
                int bytesRead;
                while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0) {
                    await stream.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                    totalBytesRead += bytesRead;
                    progress?.Invoke(totalBytesRead, totalLength);
                }
            }
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                File.SetUnixFileMode(result,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
            return result.AsResult();
        }
        catch (Exception e)
        {
            if (result != null) {
                try
                {
                    File.Delete(result);
                }
                catch
                {
                    // ignored
                }
            }
           if(e is TaskCanceledException)
                return EditResult<string>.Error("Download was cancelled");
           else
               return EditResult<string>.Error(e.ToString());
        }
    }
}
