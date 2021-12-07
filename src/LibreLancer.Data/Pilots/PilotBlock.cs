using LibreLancer.Ini;

namespace LibreLancer.Data.Pilots
{
    public class PilotBlock
    {
        [Entry("nickname", Required = true)] public string Nickname;
    }
}