namespace LibreLancer.Graphics;

public record struct SamplerState(TextureFiltering Filtering, WrapMode WrapS, WrapMode WrapT)
{
    public static readonly SamplerState LinearRepeat = new(TextureFiltering.Linear, WrapMode.Repeat,
        WrapMode.Repeat);

    internal static readonly SamplerState Unset = new((TextureFiltering)(-1), (WrapMode) (-1), (WrapMode)(-1));
}
