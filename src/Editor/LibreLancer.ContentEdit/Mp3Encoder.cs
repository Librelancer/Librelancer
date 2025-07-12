using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using LibreLancer.Media;

namespace LibreLancer.ContentEdit;

public enum Mp3EncodePreset
{
    Bitrate = 0,
    Quality10 = 410,
    Quality20 = 420,
    Quality30 = 430,
    Quality40 = 440,
    Quality50 = 450,
    Quality60 = 460,
    Quality70 = 470,
    Quality80 = 480,
    Quality90 = 490,
    Quality100 = 500
}

public static class Mp3Encoder
{
    static Mp3Encoder()
    {
        Platform.RegisterDllMap(typeof(Mp3Encoder).Assembly);
    }

    [DllImport("libmp3lame")]
    static extern IntPtr get_lame_version();

    [DllImport("libmp3lame")]
    static extern IntPtr lame_init();

    [DllImport("libmp3lame")]
    static extern int lame_set_errorf(IntPtr lame, IntPtr function);

    [DllImport("libmp3lame")]
    static extern int lame_set_debugf(IntPtr lame, IntPtr function);

    [DllImport("libmp3lame")]
    static extern int lame_set_msgf(IntPtr lame, IntPtr function);

    [DllImport("libmp3lame")]
    static extern void lame_print_config(IntPtr lame);

    [DllImport("libmp3lame")]
    static extern int lame_set_num_channels(IntPtr lame, int channels);

    [DllImport("libmp3lame")]
    static extern int lame_set_in_samplerate(IntPtr lame, int samplerate);

    //0 = best, 9 = worst
    [DllImport("libmp3lame")]
    static extern int lame_set_quality(IntPtr lame, int quality);

    [DllImport("libmp3lame")]
    static extern int lame_set_preset(IntPtr lame, int preset);

    [DllImport("libmp3lame")]
    static extern int lame_init_params(IntPtr lame);

    [DllImport("libmp3lame")]
    static extern int lame_get_lametag_frame(IntPtr lame, byte[] buffer, int length);

    [DllImport("libmp3lame")]
    static extern int lame_encode_buffer_interleaved(IntPtr gfp, IntPtr pcm, int numsamples, IntPtr mp3buf,
        int mp3buf_size);

    [DllImport("libmp3lame")]
    static extern int lame_encode_buffer(IntPtr gfp, IntPtr buffer_l, IntPtr buffer_r, int numsamples, IntPtr mp3buf,
        int mp3buf_size);

    [DllImport("libmp3lame")]
    static extern int lame_encode_flush(IntPtr gfp, IntPtr mp3buf, int size);

    [DllImport("libmp3lame")]
    static extern void lame_close(IntPtr gfp);

    static byte[] GetLAMETagFrame(IntPtr context)
    {
        byte[] buffer = new byte[1];
        int frameSize = lame_get_lametag_frame(context, buffer, 0);
        if (frameSize == 0)
            return null;
        buffer = new byte[(int)frameSize];
        IntPtr res = lame_get_lametag_frame(context, buffer, frameSize);
        if (res != frameSize)
            return null;
        return buffer;
    }

    static unsafe void RunEncode(Stream input, Stream output, int bitrateKbps, Mp3EncodePreset preset, CancellationToken cancellation,
        Action<string> log = null)
    {
        using var audio = new AudioDecoder(input);
        var mp3file = new MemoryStream();

        log ??= (msg) => FLLog.Info("MP3", msg);

        bool stereo = audio.Format == LdFormat.Stereo8 || audio.Format == LdFormat.Stereo16;
        bool bits8 = audio.Format == LdFormat.Mono8 || audio.Format == LdFormat.Stereo8;

        log(bits8 ? "8-bit input" : "16-bit input");
        log(stereo ? "Stereo" : "Mono");
        log($"Input sample rate: {audio.Frequency}");

        using var msgf = VaListCallback.Create((msg) => log($"info: {msg}"));
        using var errorf = VaListCallback.Create((msg) => log($"error: {msg}"));
        using var debugf = VaListCallback.Create((msg) => log($"debug: {msg}"));

        var lame = lame_init();
        lame_set_msgf(lame, msgf.GetFunctionPointer());
        lame_set_errorf(lame, errorf.GetFunctionPointer());
        lame_set_debugf(lame, debugf.GetFunctionPointer());
        lame_set_in_samplerate(lame, audio.Frequency);
        lame_set_num_channels(lame, stereo ? 2 : 1);
        lame_set_quality(lame, 2);
        if (preset == Mp3EncodePreset.Bitrate) {
            lame_set_preset(lame, MathHelper.Clamp(bitrateKbps, 8, 320));
        }
        else {
            lame_set_preset(lame, (int)preset);
        }
        lame_init_params(lame);
        lame_print_config(lame);

        log("Converting...");
        Span<byte> wavbuffer = stackalloc byte[16384];
        Span<byte> mp3buffer = stackalloc byte[8192];
        Span<byte> buffer8 = stackalloc byte[8192];

        int read;
        do
        {
            if (cancellation.IsCancellationRequested) {
                log?.Invoke("Cancelled");
                lame_close(lame);
                return;
            }
            if (bits8)
            {
                Span<short> buffer16 = MemoryMarshal.Cast<byte, short>(wavbuffer);
                read = audio.Read(buffer8);
                for (int i = 0; i < read; i++) {
                    buffer16[i] = (short)((buffer8[i] - 0x80) << 8);
                }
                read *= 2;
            }
            else {
                read = audio.Read(wavbuffer);
            }
            if (cancellation.IsCancellationRequested) {
                log("Cancelled");
                lame_close(lame);
                return;
            }
            fixed (byte* wav = wavbuffer)
            fixed(byte* mp3 = mp3buffer)
            {
                int write = 0;
                if (read == 0)
                {
                    write = lame_encode_flush(lame, (IntPtr)mp3, 8192);
                }
                else
                {
                    if (stereo)
                    {
                        write = lame_encode_buffer_interleaved(
                            lame,
                            (IntPtr)wav, read / 4,
                            (IntPtr)mp3, 8192);
                    }
                    else
                    {
                        write = lame_encode_buffer(
                            lame,
                            (IntPtr)wav, IntPtr.Zero, read / 2,
                            (IntPtr)mp3, 8192);
                    }
                }

                if (write > 0)
                {
                    mp3file.Write(mp3buffer.Slice(0, write));
                }

            }
        } while (read != 0);

        if (cancellation.IsCancellationRequested) {
            log("Cancelled");
            lame_close(lame);
            return;
        }

        var lametag = GetLAMETagFrame(lame);
        if (lametag != null)
        {
            mp3file.Position = 0;
            mp3file.Write(lametag);
        }

        if (cancellation.IsCancellationRequested) {
            log("Cancelled");
            lame_close(lame);
            return;
        }

        var result = AudioImporter.ImportMp3(mp3file.ToArray(), output);
        foreach (var msg in result.Messages)
            log(msg.ToString());
        if (result.IsError)
        {
            log("Failed!");
        }
        else
        {
            log("Done");
        }
    }


    public static bool EncoderAvailable()
    {
        try
        {
            return get_lame_version() != IntPtr.Zero;
        }
        catch {
            return false;
        }
    }

    public static Task EncodeStream(Stream input, Stream output, int bitrateKbps, Mp3EncodePreset preset, CancellationToken cancellation = default, Action<string> log = null)
    {
        return Task.Run(() => RunEncode(input, output, bitrateKbps, preset, cancellation, log));
    }
}
