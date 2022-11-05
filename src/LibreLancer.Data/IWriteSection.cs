using System.Text;

namespace LibreLancer.Data;

public interface IWriteSection
{
    void WriteTo(StringBuilder builder);
}