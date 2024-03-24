namespace LibreLancer.Thorn;

public struct ThornTuple
{
    public object[] Values;
    public ThornTuple(params object[] values)
    {
        Values = values;
    }
}
