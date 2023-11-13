using System.Linq;

namespace LibreLancer.Server.ConsoleCommands
{
    [ConsoleCommand]
    public class HelpCommand : IConsoleCommand
    {
        public string Name => "help";
        public bool Admin => false;
        public void Run(Player player, string arguments)
        {
            var commandList = string.Join(", ",
                ConsoleCommands.AllCommands
                    .Where(x => !x.Admin || (player.Character?.Admin ?? false))
                    .Select(x => x.Name)
                    .OrderBy(x => x));
            player.RpcClient.OnConsoleMessage($"Available Commands: pos, ping, debug, {commandList}");
        }
    }
}
