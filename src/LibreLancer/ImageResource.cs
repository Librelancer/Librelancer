namespace LibreLancer;

public enum ImageType
{
    TGA,
    LIF,
    DDS,
}

public record ImageResource(ImageType Type, byte[] Data);
