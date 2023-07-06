namespace LibreLancer.GameData;

public enum PreloadType
{
    Ship,
    Simple,
    Solar,
    Equipment,
    Sound,
    Voice
}
public record PreloadObject(PreloadType Type, params string[] Values);