using System.Text;
using LibreLancer.Ini;

namespace LibreLancer.Data;

public interface IWriteSection
{
    void WriteTo(IniBuilder builder);
}
