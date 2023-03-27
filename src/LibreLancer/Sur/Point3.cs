namespace LibreLancer.Sur;

public struct Point3
{
    public int A;
    public int B;
    public int C;

    public Point3(int a, int b, int c)
    {
        A = a;
        B = b;
        C = c;
    }

    public override string ToString() => $"({A}, {B}, {C})";
}