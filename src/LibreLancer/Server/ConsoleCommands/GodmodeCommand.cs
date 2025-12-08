using LibreLancer.Server.Components;
using LibreLancer.Net;

namespace LibreLancer.Server.ConsoleCommands
{
    [ConsoleCommand]
    public class GodmodeCommand : IConsoleCommand
    {
        public string Name => "godmode";
        public bool Admin => true;

        public void Run(Player player, string args)
        {
            // Check if player is in space gameplay
            if (player.Space != null && player.Space.World != null)
            {
                var playerObj = player.Space.World.GetObject(new ObjNetId(player.ID));
                if (playerObj != null && playerObj.TryGetComponent<SHealthComponent>(out var health))
                {
                    // Toggle godmode (invulnerability)
                    health.Invulnerable = !health.Invulnerable;
                    player.RpcClient.OnConsoleMessage($"Godmode {(health.Invulnerable ? "enabled" : "disabled")}");
                }
                else
                {
                    player.RpcClient.OnConsoleMessage("Could not find player health component");
                }
            }
            else
            {
                player.RpcClient.OnConsoleMessage("Godmode command only works in space");
            }
        }
    }
}