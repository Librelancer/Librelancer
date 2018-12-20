// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using LibreLancer;
using LibreLancer.Data;
namespace Server
{
	class Config
	{
		public string server_name;
		public string server_description;
		public string freelancer_path;
		public string dbconnectionstring;
	}
	class MainClass
	{
		public static int Main(string[] args)
		{
			if (args.Length > 1 && args[0] == "--makeconfig")
			{
				MakeConfig();
				return 0;
			}
			var config = JSON.Deserialize<Config>(File.ReadAllText("librelancerserver.config.json"));
			var srv = new GameServer(config.freelancer_path);
			srv.DbConnectionString = config.dbconnectionstring;
			srv.ServerName = config.server_name;
			srv.ServerDescription = config.server_description;
			srv.Start();
			bool running = true;
			while (running)
			{
				var cmd = Console.ReadLine();
				switch (cmd.Trim().ToLowerInvariant())
				{
					case "stop":
					case "quit":
					case "exit":
						running = false;
						break;
				}
			}
			srv.Stop();
			return 0;
		}

		static void MakeConfig()
		{
			var config = new Config();
			Console.Write("Freelancer Path: ");
			config.freelancer_path = Console.ReadLine().Trim();
			Console.Write("Db Connection String: ");
			config.dbconnectionstring = Console.ReadLine().Trim();
			Console.Write("Server Name: ");
			config.server_name = Console.ReadLine().Trim();
			Console.Write("Server Description: ");
			config.server_description = Console.ReadLine().Trim();
			File.WriteAllText("librelancerserver.config.json", JSON.Serialize(config));
		}
	}
}
