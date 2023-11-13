using LibreLancer.Server.Components;

namespace LibreLancer.Server.ConsoleCommands;

[ConsoleCommand]
public class DamageSelfCommand : IConsoleCommand
{
    public string Name => "damageself";
    public bool Admin => true;
    public void Run(Player player, string arguments)
    {
        if (!ConsoleCommands.ParseString(arguments, out float x))
        {
            player.RpcClient.OnConsoleMessage("Invalid argument. Expect float");
            return;
        }
        player.Space?.World?.EnqueueAction(() =>
        {
            var component = player.Space.World.Players[player].GetComponent<SHealthComponent>();
            component.Damage(x, x);
        });
    }
}
