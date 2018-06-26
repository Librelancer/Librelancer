using System;
namespace LancerEdit
{
    public static class DefaultTexture
    {
        public static readonly byte[] Data;
        static DefaultTexture()
        {
            using(var stream = typeof(DefaultTexture).Assembly.GetManifestResourceStream("LancerEdit.defaulttexture.dds")) {
                Data = new byte[(int)stream.Length];
                stream.Read(Data, 0, (int)stream.Length);
            }
        }
    }
}