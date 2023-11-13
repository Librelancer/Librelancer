using System.Numerics;

namespace LibreLancer.Server.ConsoleCommands
{
    [ConsoleCommand]
    public class WarpCommand : IConsoleCommand
    {
        public string Name => "warp";
        public bool Admin => true;
        public void Run(Player player, string arguments)
        {
            if (!ConsoleCommands.ParseString(arguments, out float x, out float y, out float z))
            {
                player.RpcClient.OnConsoleMessage("Invalid argument. Expect float XYZ");
                return;
            }
            player.Space?.ForceMove(new Vector3(x,y,z));
        }
    }
}
