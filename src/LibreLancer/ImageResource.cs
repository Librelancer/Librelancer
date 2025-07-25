namespace LibreLancer;

public enum ImageType
{
    TGA,
    DDS,
}

public record ImageResource(ImageType Type, byte[] Data);
