using System;
using System.IO;
using TeximpNet.Compression;
using LibreLancer;
namespace LancerEdit
{
    public enum DDSFormat
    {
        Uncompressed = CompressionFormat.BGRA,
        DXT1 = CompressionFormat.DXT1,
        DXT1a = CompressionFormat.DXT1a,
        DXT3 = CompressionFormat.DXT3,
        DXT5 = CompressionFormat.DXT5
    }
    public class TextureImport
    {
        static bool first = true;
        public static byte[] CreateDDS(string input, DDSFormat format, bool slow)
        {
            if (first)
            {
                if (Platform.RunningOS == OS.Linux)
                {
                    TeximpNet.Unmanaged.FreeImageLibrary.Instance.LoadLibrary("libfreeimage.so.3");
                    var mypath = Path.GetDirectoryName(typeof(TextureImport).Assembly.Location);
                    TeximpNet.Unmanaged.NvTextureToolsLibrary.Instance.LoadLibrary(Path.Combine(mypath,"libnvtt.so"));
                }
                else if(Platform.RunningOS == OS.Windows)
                {
                    TeximpNet.Unmanaged.FreeImageLibrary.Instance.LoadLibrary("FreeImage32.dll", "FreeImage64.dll");
                    TeximpNet.Unmanaged.NvTextureToolsLibrary.Instance.LoadLibrary("nvtt32.dll", "nvtt64.dll");
                }
                first = false;
            }
            using (var stream = new MemoryStream())
            {
                using (var surface = TeximpNet.Surface.LoadFromFile(input))
                {
                    using (var compress = new Compressor())
                    {
                        compress.Input.GenerateMipmaps = true;
                        compress.Input.SetData(surface);
                        compress.Compression.Format = (CompressionFormat)format;
                        compress.Compression.Quality = slow ? CompressionQuality.Production : CompressionQuality.Normal;
                        compress.Compression.SetBGRAPixelFormat();
                        compress.Process(stream);
                    }
                }
                return stream.ToArray();
            }
        }
    }
}
