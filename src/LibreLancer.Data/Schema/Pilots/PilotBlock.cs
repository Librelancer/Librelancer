using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Pilots;

public abstract class PilotBlock
{
    [Entry("nickname", Required = true)] public string Nickname = null!;
}
