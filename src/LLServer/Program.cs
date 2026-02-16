// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using LibreLancer;
using LibreLancer.Data;
using LibreLancer.Options;

namespace LLServer
{
    internal class MainClass
	{
        public static async Task<int> Main(string[] args)
        {
                   
            bool printHelp = false;
            bool printVersion = false;
            string? configPath = null;
            var os = new OptionSet
            {
                { "h|?|help", "shows this message and exits", x => printHelp = x != null },
                { "v|version", "shows the version and exits", x => printVersion = x != null },
                { "c|config=", "set path for the configuration file", x => configPath = x }
            };

            var extra = os.Parse(args);
            if (printHelp)
            {
                Console.WriteLine($"LLServer {Platform.GetInformationalVersion<MainClass>()}");
                os.WriteOptionDescriptions(Console.Out);
                Console.WriteLine("Run with makeconfig to generate a config file.");
                return 0;
            }

            if (printVersion)
            {
                Console.WriteLine(Platform.GetInformationalVersion<MainClass>());
                return 0;
            }

            AppHandler.ConsoleInit();
            configPath ??= Path.Combine(Platform.GetBasePath(), "llserver.json");
            if (extra.Count > 0 && extra[0] == "makeconfig")
            {
                MakeConfig(configPath);
				return 0;
			}

            if (!File.Exists(configPath))
            {
                await Console.Error.WriteLineAsync($"Can't find {configPath}. Use the --config option to specify a file or run LLServer makeconfig");
                return 2;
            }

			var config = JSON.Deserialize<ServerConfig>(await File.ReadAllTextAsync(configPath));
            config.DatabasePath = Path.GetFullPath(config.DatabasePath, Platform.GetBasePath());

            var app = new ServerApp(config);
            if (!app.StartServer())
            {
                await Console.Error.WriteLineAsync("Server failed to start");
                return 1;
            }
            
            using var sigterm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, _ =>
            {
                Console.WriteLine("Server shutting down");
                app.StopServer();
                Environment.Exit(0);
            });

            var running = true;
			while (running)
            {
                var input = Console.ReadLine();
                if (input == null)
                {
                    app.WaitExit();
                    break;
                }
                var (cmd, cmdargs) = GetCommand(input);
                switch (cmd.ToLowerInvariant())
				{
					case "stop":
					case "quit":
					case "exit":
						running = false;
						break;
                    case "ban":
                    {
                        Guid? g = null;
                        if (cmdargs.Length > 0)
                        {
                            if (Guid.TryParse(cmdargs, out var v))
                                g = v;
                            else
                                g = await app.Server?.Database.FindAccount(cmdargs)!;
                        }
                        if (g.HasValue)
                        {
                            FLLog.Info("Server", $"Banning account {g} for 30 days");
                            await app.Server?.Database.BanAccount(g.Value, DateTime.UtcNow.AddDays(30))!;
                        }
                        break;
                    }
                    case "unban":
                    {
                        Guid? g = null;
                        if (cmdargs.Length > 0)
                        {
                            if (Guid.TryParse(cmdargs, out var v))
                                g = v;
                            else
                                g = await app.Server?.Database.FindAccount(cmdargs)!;
                        }
                        if (g.HasValue)
                        {
                            FLLog.Info("Server", $"Unbanning account {g}");
                            await app.Server?.Database.UnbanAccount(g.Value)!;
                        }
                        break;
                    }
                    case "admin":
                    {
                        long? id = null;
                        if (cmdargs.Length > 0)
                            id = await app.Server?.Database.FindCharacter(cmdargs)!;
                        if (id.HasValue)
                        {
                            FLLog.Info("Server", $"Making '{cmdargs}' admin");
                            await app.Server?.Database.AdminCharacter(id.Value)!;
                            app.Server.AdminChanged(id.Value, true);
                        }
                        break;
                    }
                    case "deadmin":
                    {
                        long? id = null;
                        if (cmdargs.Length > 0)
                            id = await app.Server?.Database.FindCharacter(cmdargs)!;
                        if (id.HasValue)
                        {
                            FLLog.Info("Server", $"Removing '{cmdargs}' admin");
                            await app.Server?.Database.DeadminCharacter(id.Value)!;
                            app.Server.AdminChanged(id.Value, false);
                        }
                        break;
                    }
                }
            }

            Console.WriteLine("Server shutting down");
			app.StopServer();
			return 0;
		}

        private static (string cmd, string args) GetCommand(string commandString)
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
            return (cmd, args);
        }

        private static void MakeConfig(string configPath)
		{
			ServerConfig config = new ServerConfig();

			Console.Write("Freelancer Path: ");
			config.FreelancerPath = (Console.ReadLine() ?? "").Trim();

			Console.Write("Db Path: ");
			config.DatabasePath = (Console.ReadLine() ?? "").Trim();

			Console.Write("Server Name: ");
			config.ServerName = (Console.ReadLine() ?? "").Trim();

			Console.Write("Server Description: ");
			config.ServerDescription = (Console.ReadLine() ?? "").Trim();

			File.WriteAllText(configPath, JSON.Serialize(config));
		}
	}
}
