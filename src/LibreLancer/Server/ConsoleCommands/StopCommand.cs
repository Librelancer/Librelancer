using System.Numerics;

namespace LibreLancer.Server.ConsoleCommands
{
    [ConsoleCommand]
    public class StopCommand : IConsoleCommand
    {
        public string Name => "stop";
        public bool Admin => true;
        public void Run(Player player, string arguments)
        {
            player.RpcClient.StopShip();
        }
    }
}
