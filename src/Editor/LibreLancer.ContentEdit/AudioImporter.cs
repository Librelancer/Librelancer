using System;
using System.IO;
using LibreLancer.Media;

namespace LibreLancer.ContentEdit;

public enum AudioImportKind
{
    NeedsConversion,
    Copy,
    WavUncompressed,
    Mp3,
}

public class AudioImportInfo
{
    public AudioImportKind Kind;
    public int Channels;
    public int Frequency;
    public int Trim;
    public int Samples;
    public int TotalMp3Bytes;
}

public static class AudioImporter
{

    static int GetByteLength(AudioDecoder audio)
    {
        int totalBytes = 0;
        Span<byte> buffer = stackalloc byte[2048];
        int r;
        do
        {
            r = audio.Read(buffer);
            totalBytes += r;
        } while (r > 0);
        return totalBytes;
    }

    public static AudioImportInfo Analyze(Stream stream)
    {
        try
        {
            var decoder = new AudioDecoder(stream);
            var info = new AudioImportInfo();
            info.Channels = decoder.Format == LdFormat.Mono8 || decoder.Format == LdFormat.Mono16 ? 1 : 2;
            info.Frequency = decoder.Frequency;
            info.Kind = AudioImportKind.NeedsConversion;
            if (decoder.GetString(AudioProperty.Codec, out var codec) &&
                decoder.GetString(AudioProperty.Container, out var container))
            {
                if (container == "wav" &&
                    codec == "pcm")
                {
                    info.Kind = AudioImportKind.WavUncompressed;
                }
                else if (container == "wav" &&
                    codec == "mp3")
                {
                    if (decoder.GetInt(AudioProperty.FlTrim, out info.Trim) &&
                        decoder.GetInt(AudioProperty.FlSamples, out info.Samples))
                    {
                        info.Kind = AudioImportKind.Copy;
                    }
                    else
                    {
                        info.Kind = AudioImportKind.Mp3;
                        if (!decoder.GetInt(AudioProperty.Mp3Trim, out info.Trim) ||
                            !decoder.GetInt(AudioProperty.Mp3Samples, out info.Samples))
                        {
                            info.Trim = info.Samples = 0;
                            info.TotalMp3Bytes = GetByteLength(decoder);
                        }
                    }
                }
                else if (container == "mp3" && codec == "mp3")
                {
                    info.Kind = AudioImportKind.Mp3;
                    if (!decoder.GetInt(AudioProperty.Mp3Trim, out info.Trim) ||
                        !decoder.GetInt(AudioProperty.Mp3Samples, out info.Samples))
                    {
                        info.Trim = info.Samples = 0;
                        info.TotalMp3Bytes = GetByteLength(decoder);
                    }
                }
            }
            return info;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static EditResult<bool> ImportMp3(byte[] mp3Bytes, Stream outputStream, int manualTrim = 0, int manualSamples = 0)
    {
        var info = Analyze(new MemoryStream(mp3Bytes));
        if (info == null || info.Kind != AudioImportKind.Mp3) {
            return EditResult<bool>.Error("File is not valid mp3");
        }

        // Decode file to get accurate duration
        int totalBytes = 0;
        using (var audio = new AudioDecoder(new MemoryStream(mp3Bytes)))
        {
            totalBytes = GetByteLength(audio);
        }

        int totalSamples = totalBytes / 2 / info.Channels;
        bool doManual = manualTrim != 0 && manualSamples != 0;
        bool writeTrim = doManual || (info.Samples != 0 && info.Trim != 0);
        var writer = new BinaryWriter(outputStream);
        int totalLength = mp3Bytes.Length +
                          12 + //riff header
                          24 + //wave fmt chunk
                          (writeTrim ? 24 : 0) + //fact and trim chunk
                          8; //data header
        writer.Write("RIFF"u8);
        writer.Write(totalLength);
        writer.Write("WAVE"u8);
        writer.Write("fmt "u8);
        writer.Write(16); //size
        writer.Write((ushort)0x55); //mp3
        writer.Write((ushort)info.Channels);
        writer.Write(info.Frequency);
        writer.Write((int)(mp3Bytes.Length / (totalSamples / (double)info.Frequency))); // approx byte rate
        writer.Write((ushort)(info.Channels * 2)); // block align
        writer.Write((ushort)0); //bits per sample (invalid for mp3)
        if (writeTrim) {
            writer.Write("fact"u8);
            writer.Write(4);
            writer.Write(doManual ? manualSamples : info.Samples);
            writer.Write("trim"u8);
            writer.Write(4);
            writer.Write(doManual ? manualTrim : info.Trim);
        }
        writer.Write("data"u8);
        writer.Write(mp3Bytes.Length);
        outputStream.Write(mp3Bytes);
        return true.AsResult();
    }
    public static EditResult<bool> ImportMp3(string mp3Path, Stream outputStream, int manualTrim = 0, int manualSamples = 0)
    {
        var mp3Bytes = EditResult<byte[]>.TryCatch(() => File.ReadAllBytes(mp3Path));
        if (mp3Bytes.IsError)
            return new EditResult<bool>(false, mp3Bytes.Messages);
        return ImportMp3(mp3Bytes.Data, outputStream, manualTrim, manualSamples);
    }
}
