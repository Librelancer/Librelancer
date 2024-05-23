using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using LibreLancer.Graphics;
using LibreLancer.ImageLib;
using SharpDX.MediaFoundation;

namespace LibreLancer.ContentEdit.Texture;

public static class TextureExporter
{
    static Bgra8[] Conv16To32(byte[] srcData, int width, int height)
    {
        Bgra8[] data = new Bgra8[width * height];
        var src = MemoryMarshal.Cast<byte, ushort>(srcData);
        for (int i = 0; i < width * height; i++)
        {
            var val = src[i];
            var r = (uint) ((val & 0x7C00) >> 10);
            r = (r << 3) | (r >> 2);
            var g = (uint) ((val & 0x03E0) >> 5);
            g = (g << 3) | (g >> 2);
            var b = (uint) (val & 0x001F);
            b = (b << 3) | (b >> 2);
            data[i] = new Bgra8(0xFF000000 | (r << 16) | (g << 8) | b);
        }
        return data;

    }

    static void SwapChannels(byte[] data)
    {
        for (int i = 0; i < data.Length; i += 4)
        {
            //Swap channels
            var x = data[i + 2];
            data[i + 2] = data[i];
            data[i] = x;
        }
    }

    /// <summary>
    /// Takes an ImageResource (from GameResourceManager) and creates a PNG file. This method also includes the source .dds as an ancillary chunk in the PNG for round-trip support at the expense of a larger file size
    /// </summary>
    /// <param name="resource">The resource to encode into png</param>
    /// <param name="embedDDS">Controls if DXT-compressed DDS resources are embedded as ancillary chunks</param>
    /// <returns>A byte[] array containing a png suitable for export, or null if the DDS is a cubemap</returns>
    /// <exception cref="InvalidOperationException">Internal error</exception>
    public static byte[] ExportTexture(ImageResource resource, bool embedDDS)
    {
        if (resource.Type == ImageType.LIF ||
            resource.Type == ImageType.TGA)
        {
            var toEncode = Generic.ImageFromStream(new MemoryStream(resource.Data));
            using var output = new MemoryStream();
            if (toEncode.Format == SurfaceFormat.Bgra8)
            {
                PNG.Save(output, toEncode.Width, toEncode.Height, Bgra8.BufferFromBytes(toEncode.Data), false);
            }
            else if (toEncode.Format == SurfaceFormat.Bgra5551)
            {
                //Convert to 32-bit RGB
                PNG.Save(output, toEncode.Width, toEncode.Height, Conv16To32(toEncode.Data, toEncode.Width, toEncode.Height), false);
            }
            else
            {
                throw new InvalidOperationException($"ImageFromStream returned unexpected format {toEncode.Format}");
            }

            return output.ToArray();
        }
        else if (resource.Type == ImageType.DDS)
        {
            var surface = DDS.ImageFromStream(new MemoryStream(resource.Data));
            if (surface == null)
                return null;
            using var output = new MemoryStream();
            Bgra8[] converted;
            if (surface[0].Format == SurfaceFormat.Bgra8) //Uncompressed
            {
                PNG.Save(output, surface[0].Width, surface[0].Height, Bgra8.BufferFromBytes(surface[0].Data), false);
                return output.ToArray();
            }
            else if (surface[0].Format == SurfaceFormat.Bgra5551)
            {
                converted = Conv16To32(surface[0].Data, surface[0].Width, surface[0].Height);
            }
            else if (surface[0].Format == SurfaceFormat.Dxt1 ||
                     surface[0].Format == SurfaceFormat.Dxt3 ||
                     surface[0].Format == SurfaceFormat.Dxt5)
            {
                converted = S3TC.Decompress(surface[0].Format, surface[0].Width, surface[0].Height, surface[0].Data);
            }
            else
            {
                return null;
            }
            if (embedDDS)
            {
                //Create ancillary chunk
                var ms = new MemoryStream();
                ms.Write(SHA256.HashData(MemoryMarshal.Cast<Bgra8, byte>(converted)));
                using var comp = new ZstdSharp.Compressor();
                ms.Write(comp.Wrap(resource.Data));
                var ancillary = new PNGAncillaryChunk("ddsz", ms.ToArray());
                PNG.Save(output, surface[0].Width, surface[0].Height, converted, false, ancillary);
            }
            else
            {
                PNG.Save(output, surface[0].Width, surface[0].Height, converted, false);
            }
            return output.ToArray();
        }
        throw new InvalidOperationException("Bad enum value");
    }
}
