namespace LibreLancer.Data.GameData;

public enum PreloadType
{
    Ship,
    Simple,
    Solar,
    Equipment,
    Sound,
    Voice
}

public record PreloadObject(PreloadType Type, params HashValue[] Values);
