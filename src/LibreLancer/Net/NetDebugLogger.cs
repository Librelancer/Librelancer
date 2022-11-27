using LiteNetLib;

namespace LibreLancer.Net;

public class NetDebugLogger : INetLogger
{
    public void WriteNet(NetLogLevel level, string str, params object[] args)
    {
        #if DEBUG
         if (level == NetLogLevel.Error ||
            level == NetLogLevel.Warning)
        {
            FLLog.Debug($"LiteNetLib {level}", string.Format(str, args));
        }
        #else
        // No-op
        // Compile nothing into release builds so we don't spam the log on
        // malformed packets
        #endif
    }
}