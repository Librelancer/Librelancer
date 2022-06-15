using System;
using System.Linq;

namespace LibreLancer.Net.ConsoleCommands
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
                    .Where(x => !x.Admin || player.IsAdmin)
                    .Select(x => x.Name)
                    .OrderBy(x => x));
            player.RemoteClient.OnConsoleMessage($"Available Commands: netstat, debug, {commandList}");
        }
    }
}