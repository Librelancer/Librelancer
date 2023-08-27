namespace LibreLancer.Sur;

public struct Point3<T> where T : unmanaged
{
    public T A;
    public T B;
    public T C;

    public Point3(T a, T b, T c)
    {
        A = a;
        B = b;
        C = c;
    }

    public override string ToString() => $"({A}, {B}, {C})";
}