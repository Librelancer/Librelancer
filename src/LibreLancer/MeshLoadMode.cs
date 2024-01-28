using System;

namespace LibreLancer;

[Flags]
public enum MeshLoadMode
{
    GPU = 1,
    CPU = 2,
    All = 1 | 2
}
