using System.Numerics;

namespace LibreLancer.Net.ConsoleCommands
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
                player.RemoteClient.OnConsoleMessage("Invalid argument. Expect float XYZ");
                return;
            }
            player.World?.EnqueueAction(() =>
            {
                var obj = player.World.Players[player];
                obj.SetLocalTransform(Matrix4x4.CreateTranslation(x,y,z));
            });
        }
    }
}