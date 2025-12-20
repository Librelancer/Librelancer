namespace LibreLancer.Data;

public interface IDataEquatable<in T>
{
    bool DataEquals(T other);
}
