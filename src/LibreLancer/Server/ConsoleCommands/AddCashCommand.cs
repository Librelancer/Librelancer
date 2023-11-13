namespace LibreLancer.Server.ConsoleCommands
{
    [ConsoleCommand]
    public class AddCashCommand : IConsoleCommand
    {
        public string Name => "addcash";
        public bool Admin => true;
        public void Run(Player player, string args)
        {
            if (ConsoleCommands.ParseString(args, out string target, out int credits))
            {
                var targetPlayer = player.Game.GetConnectedPlayer(target);
                if (targetPlayer == null) {
                    player.RpcClient.OnConsoleMessage($"Player {target} is not online");
                }
                else {
                    targetPlayer.AddCash(credits);
                    targetPlayer.UpdateCurrentInventory();
                    player.RpcClient.OnConsoleMessage($"Added {credits} credits to {target}");
                }
            } else if (ConsoleCommands.ParseString(args, out credits)) {
                player.AddCash(credits);
                player.UpdateCurrentInventory();
                player.RpcClient.OnConsoleMessage($"Added {credits} credits to {player.Name}");
            }
        }
    }
}