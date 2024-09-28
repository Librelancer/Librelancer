namespace LibreLancer.GameData;

public interface IDataEquatable<in T>
{
    bool DataEquals(T other);
}
