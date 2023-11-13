namespace LibreLancer.Server.ConsoleCommands
{
    [ConsoleCommand]
    public class NPCCommand : IConsoleCommand
    {
        public string Name => "npc";
        public bool Admin => true;
        public void Run(Player player, string arguments)
        {
            player.Space.World?.NPCs.RunScript(arguments).ContinueWith((t) =>
            {
                if(!string.IsNullOrWhiteSpace(t.Result))
                    player.RpcClient.OnConsoleMessage(t.Result);
            });
        }
    }
}
