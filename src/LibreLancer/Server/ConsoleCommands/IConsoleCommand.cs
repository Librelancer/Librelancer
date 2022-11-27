namespace LibreLancer.Server.ConsoleCommands
{
    public interface IConsoleCommand
    {
        string Name { get; }
        bool Admin { get; }
        void Run(Player player, string arguments);
    }
}