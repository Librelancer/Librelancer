using System;

namespace LibreLancer.Graphics.Backends.Null;

class NullTexture2D : NullTexture, ITexture2D
{
    public NullTexture2D(int width, int height, bool mipMaps, SurfaceFormat format, int levelCount) : base(format, levelCount, width * height * 4)
    {
    }

    public int Width { get; set; }
    public int Height { get; set; }
    public void SetFiltering(TextureFiltering filtering)
    {
    }

    public void GetData<T>(int level, Rectangle? rect, T[] data, int start, int count) where T : struct
    {
    }

    public void GetData<T>(T[] data) where T : struct
    {
    }

    public void SetData<T>(int level, Rectangle? rect, T[] data, int start, int count) where T : unmanaged
    {
    }

    public void SetWrapModeS(WrapMode mode)
    {
    }

    public void SetWrapModeT(WrapMode mode)
    {
    }

    public void SetData(int level, Rectangle rect, IntPtr data)
    {
    }

    public void SetData<T>(T[] data) where T : unmanaged
    {
    }

    public bool Dxt1 => Format == SurfaceFormat.Dxt1;
}
