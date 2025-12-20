using System.Numerics;
using LibreLancer.Server.Components;

namespace LibreLancer.Server.ConsoleCommands;

[ConsoleCommand]
public class SpawnLootCommand : IConsoleCommand
{
    public string Name => "spawnloot";
    public bool Admin => true;
    public void Run(Player player, string arguments)
    {
        if (!ConsoleCommands.ParseString(arguments, out string l, out int count))
        {
            player.RpcClient.OnConsoleMessage("Invalid argument. Expecting [name] [count]");
            return;
        }

        if (count <= 0)
        {
            player.RpcClient.OnConsoleMessage("Count must be >= 1");
            return;
        }
        player.Space?.World?.EnqueueAction(() =>
        {
            var p = player.Space.World.Players[player];
            var eq = player.Space.World.Server.GameData.Items.Equipment.Get(l);
            if (eq is null)
            {
                player.RpcClient.OnConsoleMessage($"{l} is not equipment");
            }
            else if (eq.LootAppearance == null)
            {
                player.RpcClient.OnConsoleMessage($"{l} has no loot_appearance");
            }
            else
            {
                var pos = p.LocalTransform.Transform(new Vector3(0, 0, 20));
                player.Space.World.SpawnLoot(eq.LootAppearance, eq, count, new Transform3D(pos, Quaternion.Identity));
            }
        });
    }
}
