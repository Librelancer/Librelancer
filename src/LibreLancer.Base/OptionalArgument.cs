namespace LibreLancer;

public struct OptionalArgument<T>
{
    public bool Present;
    public T Value;
    public static implicit operator OptionalArgument<T>(T val) => new () { Value = val, Present = true };
    public T Get(T defaultVal) => Present ? Value : defaultVal;

    public override string ToString() =>
        Present ? Value?.ToString() ?? "null" : "(none)";
}
