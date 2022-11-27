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
                    player.RemoteClient.OnConsoleMessage($"Player {target} is not online");
                }
                else {
                    targetPlayer.Character.UpdateCredits(player.Character.Credits + credits);
                    targetPlayer.UpdateCurrentInventory();
                    player.RemoteClient.OnConsoleMessage($"Added {credits} credits to {target}");
                }
            } else if (ConsoleCommands.ParseString(args, out credits)) {
                player.Character.UpdateCredits(player.Character.Credits + credits);
                player.UpdateCurrentInventory();
                player.RemoteClient.OnConsoleMessage($"Added {credits} credits to {player.Name}");
            }
        }
    }
}