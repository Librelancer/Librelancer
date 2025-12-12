using System.Numerics;
using LibreLancer.GameData.Items;
using LibreLancer.Server.Components;

namespace LibreLancer.Server.ConsoleCommands;
[ConsoleCommand]

// debug command to spawn h-fuel loot near the player to increase networth fast "naturaly". For testing.
public class SpawnFuelCommand : IConsoleCommand
{
    public string Name => "spawnfuel";
    public bool Admin => true;
    public void Run(Player player, string arguments)
    {
        if (!int.TryParse(arguments.Trim(), out int count) || count <= 0)
        {
            count = 1;
        }

        player.Space?.World?.EnqueueAction(() =>
        {
            var p = player.Space.World.Players[player];
            var eq = player.Space.World.Server.GameData.Equipment.Get("commodity_H_fuel");
            if (eq is null)
            {
                player.RpcClient.OnConsoleMessage("commodity_H_fuel not found");
                return;
            }
            if (eq.LootAppearance == null)
            {
                player.RpcClient.OnConsoleMessage("commodity_H_fuel has no loot_appearance");
                return;
            }
            var pos = p.LocalTransform.Transform(new Vector3(0, 0, 20));
            player.Space.World.SpawnLoot(eq.LootAppearance, eq, count, new Transform3D(pos, Quaternion.Identity));
            player.RpcClient.OnConsoleMessage($"Spawned {count} commodity_H_fuel loot");
        });
    }
}