namespace LibreLancer.Data;

public record struct ValueRange<T>(T Min, T Max) where T : struct
{
    public override string ToString() => $"[{Min} -> {Max}]";
}