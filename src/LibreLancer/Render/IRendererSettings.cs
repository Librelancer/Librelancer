using LibreLancer.Graphics;

namespace LibreLancer.Render;

public interface IRendererSettings
{
    int SelectedMSAA { get; }
    TextureFiltering SelectedFiltering { get; }
    int SelectedAnisotropy { get; }
    float LodMultiplier { get; }
}
