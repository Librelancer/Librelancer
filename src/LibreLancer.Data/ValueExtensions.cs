namespace LibreLancer.Data;

public static class ValueExtensions
{
    public static string ToStringInvariant(this float f) =>
        f == 0 ? "0" :  f.ToString("0.####");
}