using LibreLancer.Data.IO;
using LibreLancer.Thn;

namespace LibreLancer.GameData;

public class ResolvedThn
{
    public byte[] Load() => VFS.ReadAllBytes(DataPath);

    public FileSystem VFS;
    public string DataPath;
    public string SourcePath;
}
