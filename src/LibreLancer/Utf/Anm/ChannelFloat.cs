namespace LibreLancer.Utf.Anm;

public record struct ChannelFloat(float A, float B, float Weight)
{
    public static implicit operator ChannelFloat(float a) => new(a, a, 0);
    public float Eval() => MathHelper.Lerp(A, B, Weight);
}
