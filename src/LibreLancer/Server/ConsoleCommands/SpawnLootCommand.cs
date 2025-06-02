using System.Numerics;
using LibreLancer.GameData.Items;
using LibreLancer.Server.Components;

namespace LibreLancer.Server.ConsoleCommands;

[ConsoleCommand]
public class SpawnLootCommand : IConsoleCommand
{
    public string Name => "spawnloot";
    public bool Admin => true;
    public void Run(Player player, string arguments)
    {
        if (!ConsoleCommands.ParseString(arguments, out string l))
        {
            player.RpcClient.OnConsoleMessage("Invalid argument. Expect string");
            return;
        }
        player.Space?.World?.EnqueueAction(() =>
        {
            var p = player.Space.World.Players[player];
            var eq = player.Space.World.Server.GameData.Equipment.Get(l) as LootCrateEquipment;
            if (eq is null)
            {
                player.RpcClient.OnConsoleMessage($"{l} is not loot crate");
            }
            var pos = p.LocalTransform.Transform(new Vector3(0, 0, 20));
            player.Space.World.SpawnLoot(eq, null, 0, new Transform3D(pos, Quaternion.Identity));
        });
    }
}
