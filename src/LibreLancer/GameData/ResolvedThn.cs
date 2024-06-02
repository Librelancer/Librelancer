using LibreLancer.Data.IO;
using LibreLancer.Thn;
using LibreLancer.Thorn;
using LibreLancer.Thorn.VM;

namespace LibreLancer.GameData;

public class ResolvedThn
{
    public byte[] Load() => VFS.ReadAllBytes(DataPath);
    public ThornReadFile ReadCallback;
    public FileSystem VFS;
    public string DataPath;
    public string SourcePath;
    public ThnScript LoadScript() => new ThnScript(Load(), ReadCallback, DataPath);
}
