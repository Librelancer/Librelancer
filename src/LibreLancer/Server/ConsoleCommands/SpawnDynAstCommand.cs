#if DEBUG
using System.Linq;
using System.Numerics;

namespace LibreLancer.Server.ConsoleCommands;

[ConsoleCommand]
public class SpawnDynAstCommand :  IConsoleCommand
{
    public string Name => "spawndynast";
    public bool Admin => true;
    public void Run(Player player, string arguments)
    {
        player.Space?.World?.EnqueueAction(() =>
        {
            var transform = player.Space.World.Players[player];
            var pos = transform.LocalTransform.Transform(new Vector3(0, 0, -16));
            var force = Vector3.Transform(new Vector3(0, 0, -1000), transform.LocalTransform.Orientation);
            player.Space.World.SpawnDynamicAsteroid(player.Space.World.Server.GameData.Items.DynamicAsteroids.First(),
                new Transform3D(pos, Quaternion.Identity), 100, 2, force);
        });
    }
}
#endif
