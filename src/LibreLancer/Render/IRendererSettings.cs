using LibreLancer.Graphics;

namespace LibreLancer.Render;

public interface IRendererSettings
{
    AntialiasMode SelectedAA { get; }
    TextureFiltering SelectedFiltering { get; }
    int SelectedAnisotropy { get; }
    float LodMultiplier { get; }
    bool PerPixelLighting { get; }
}
