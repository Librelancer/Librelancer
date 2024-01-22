namespace LibreLancer.Graphics.Backends.Null;

class NullTextureCube : NullTexture, ITextureCube
{
    public NullTextureCube(int size, SurfaceFormat format, int levelCount, int estimatedTextureMemory) : base(format, levelCount, estimatedTextureMemory)
    {
        Size = size;
    }

    public int Size { get; set;  }
    public void SetData<T>(CubeMapFace face, int level, Rectangle? rect, T[] data, int start, int count) where T : unmanaged
    {
    }

    public void SetData<T>(CubeMapFace face, T[] data) where T : unmanaged
    {
    }

    public void SetFiltering(TextureFiltering filtering)
    {
    }
}
