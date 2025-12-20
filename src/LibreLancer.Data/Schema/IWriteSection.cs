using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema;

public interface IWriteSection
{
    void WriteTo(IniBuilder builder);
}
