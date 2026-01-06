using System.IO;

namespace LibreLancer.Data.IO;

public abstract class VfsFile : VfsItem
{
    public abstract Stream OpenRead();

    public virtual string? GetBackingFilename() => null;
}
