using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using LibreLancer.Graphics;
using LibreLancer.ImageLib;

namespace LibreLancer.ContentEdit.Texture;

public static class TextureExporter
{
    static byte[] Conv16To32(byte[] srcData, int width, int height)
    {
        byte[] data = new byte[width * height * 4];
        var src = MemoryMarshal.Cast<byte, ushort>(srcData);
        var dst = MemoryMarshal.Cast<byte, uint>(data);
        for (int i = 0; i < width * height; i++)
        {
            var val = src[i];
            var r = (uint) ((val & 0x7C00) >> 10);
            r = (r << 3) | (r >> 2);
            var g = (uint) ((val & 0x03E0) >> 5);
            g = (g << 3) | (g >> 2);
            var b = (uint) (val & 0x001F);
            b = (b << 3) | (b >> 2);
            dst[i] = 0xFF000000 | (b << 16) | (g << 8) | r;
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
            if (toEncode.Format == SurfaceFormat.Color)
            {
                SwapChannels(toEncode.Data);
                PNG.Save(output, toEncode.Width, toEncode.Height, toEncode.Data, false);
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
            byte[] converted;
            if (surface[0].Format == SurfaceFormat.Color) //Uncompressed
            {
                SwapChannels(surface[0].Data);
                PNG.Save(output, surface[0].Width, surface[0].Height, surface[0].Data, false);
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
                SwapChannels(converted);
            }
            else
            {
                return null;
            }
            if (embedDDS)
            {
                //Create ancillary chunk
                var ms = new MemoryStream();
                using (SHA256 sha256Hash = SHA256.Create())
                {
                    ms.Write(sha256Hash.ComputeHash(converted));
                }
                ms.Write(resource.Data);
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
