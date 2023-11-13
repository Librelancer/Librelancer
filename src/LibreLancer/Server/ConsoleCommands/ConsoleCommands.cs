using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace LibreLancer.Server.ConsoleCommands
{
    public static class ConsoleCommands
    {
        private static Dictionary<string, IConsoleCommand> commands =
            new Dictionary<string, IConsoleCommand>(StringComparer.OrdinalIgnoreCase);
        static ConsoleCommands()
        {
            foreach (var type in typeof(ConsoleCommands).Assembly.GetTypes()
                         .Where(x => x.GetCustomAttribute<ConsoleCommandAttribute>() != null))
            {
                var instance = (IConsoleCommand)Activator.CreateInstance(type);
                commands[instance.Name] = instance;
            }
        }

        public static IEnumerable<IConsoleCommand> AllCommands => commands.Values;

        public static void Run(Player player, string commandString)
        {
            var firstSpace = commandString.IndexOf(' ');
            string cmd;
            string args;
            if (firstSpace == -1) {
                cmd = commandString;
                args = "";
            }
            else
            {
                cmd = commandString.Substring(0, firstSpace).Trim();
                args = commandString.Substring(firstSpace).Trim();
            }
            if (!commands.TryGetValue(cmd, out var command))
            {
                player.RpcClient.OnConsoleMessage($"invalid command '{cmd}'");
                return;
            }
            if (command.Admin && !(player.Character?.Admin ?? false))
            {
                player.RpcClient.OnConsoleMessage($"Permission denied.");
                return;
            }
            command.Run(player, args);
        }

        public static bool ParseString<T>(string args, out T value)
        {
            if (ParseString(args, out var values, typeof(T)))
            {
                value = (T)values[0];
                return true;
            } else
            {
                value = default;
                return false;
            }
        }
        public static bool ParseString<T1, T2>(string args, out T1 a, out T2 b)
        {
            if (ParseString(args, out var values, typeof(T1), typeof(T2)))
            {
                a = (T1) values[0];
                b = (T2) values[1];
                return true;
            } else {
                a = default;
                b = default;
                return false;
            }
        }
        
        public static bool ParseString<T1, T2, T3>(string args, out T1 a, out T2 b, out T3 c)
        {
            if (ParseString(args, out var values, typeof(T1), typeof(T2), typeof(T3)))
            {
                a = (T1) values[0];
                b = (T2) values[1];
                c = (T3) values[2];
                return true;
            } else {
                a = default;
                b = default;
                c = default;
                return false;
            }
        }

        static bool ParseString(string s, out object[] values, params Type[] types)
        {
            string[] split;
            if (types.Length == 1) {
                split = new[] {s.Trim()};
            }
            else {
                split = s.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            }
            if (split.Length < types.Length) {
                values = null;
                return false;
            }
            values = new object[types.Length];
            for (int i = 0; i < types.Length; i++)
            {
                var T = types[i];
                switch (T)
                {
                    case Type _ when T == typeof(string):
                        values[i] = split[i];
                        break;
                    case Type _ when T == typeof(int):
                        if (!int.TryParse(split[i], out var integer))
                            return false;
                        values[i] = integer;
                        break;
                    case Type _ when T == typeof(float):
                        if (!float.TryParse(split[i], NumberStyles.Any, CultureInfo.InvariantCulture, out var floatValue))
                            return false;
                        values[i] = floatValue;
                        break;
                    default:
                        throw new InvalidOperationException($"Command arg parsing for {T}");
                }
            }
            return true;
        }
    }
}