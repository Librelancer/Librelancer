using LibreLancer.Data.IO;

namespace LibreLancer.Data.GameData;

public class ResolvedThn
{
    public required ReadFileCallback ReadCallback;
    public required FileSystem VFS;
    public required string? DataPath;
    public required string? SourcePath;
}
