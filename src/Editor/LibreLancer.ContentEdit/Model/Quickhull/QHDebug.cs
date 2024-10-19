using System.Diagnostics;

namespace LibreLancer.ContentEdit.Model.Quickhull;

static class QHDebug
{
    [Conditional("QHDEBUG")]
    public static void debug(string str) => FLLog.Debug("Quickhull", str);

    #if QHDEBUG
    public const bool IsDebug = true;
    #else
    public const bool IsDebug = false;
    #endif
}
