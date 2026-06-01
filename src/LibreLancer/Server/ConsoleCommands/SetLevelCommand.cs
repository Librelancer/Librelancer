namespace LibreLancer.Server.ConsoleCommands
{
    [ConsoleCommand]
    public class SetLevelCommand : IConsoleCommand
    {
        public string Name => "setlevel";
        public bool Admin => true;

        private static void ApplyLevel(Player targetPlayer, int level)
        {
            using var c = targetPlayer.Character!.BeginTransaction();
            c.UpdateRank((uint) level);
            targetPlayer.RpcClient.UpdateCharacterProgress((int) targetPlayer.Character.Rank,
                (long) (targetPlayer.Story?.NextLevelWorth ?? -1));
        }

        public void Run(Player player, string args)
        {
            Player? targetPlayer;
            string targetName;
            int level;

            if (ConsoleCommands.ParseString<string, int>(args, out var target, out level))
            {
                targetPlayer = player.Game.GetConnectedPlayer(target);

                if (targetPlayer == null)
                {
                    player.RpcClient.OnConsoleMessage($"Player {target} is not online");
                    return;
                }

                targetName = target;
            }
            else if (ConsoleCommands.ParseString(args, out level))
            {
                targetPlayer = player;
                targetName = player.Name;
            }
            else
            {
                player.RpcClient.OnConsoleMessage("Invalid argument. Expecting [player] [level] or [level]");
                return;
            }

            if (level < 0)
            {
                player.RpcClient.OnConsoleMessage("Level must be >= 0");
                return;
            }

            ApplyLevel(targetPlayer, level);
            player.RpcClient.OnConsoleMessage($"Set {targetName}'s level to {level}");
        }
    }
}
