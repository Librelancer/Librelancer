// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Linq;
using LibreLancer;
using LibreLancer.Data;
using Microsoft.EntityFrameworkCore;

namespace Server
{
	public class Config
	{
		public string ServerName;
		public string ServerDescription;
		public string FreelancerPath;

		public string DatabasePath;
        public bool UseLazyLoading;
    }

	class MainClass
	{
        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--makeconfig")
			{
				MakeConfig();
				return 0;
			}

            if (!File.Exists("librelancerserver.config.json"))
            {
                Console.Error.WriteLine("Can't find librelancerserver.config.json");
                return 2;
            }
			var config = JSON.Deserialize<Config>(File.ReadAllText("librelancerserver.config.json"));
            config.DatabasePath = Path.GetFullPath(config.DatabasePath);
			var srv = new GameServer(config.FreelancerPath);
			var ctxFactory = new SqlDesignTimeFactory(config);
            if (!File.Exists(config.DatabasePath))
            {
                FLLog.Info("Server", $"Creating database file {config.DatabasePath}");
                using (var ctx = ctxFactory.CreateDbContext(new string[0]))
                {
                    ctx.Database.Migrate();
                }
            }

            using (var ctx = ctxFactory.CreateDbContext(new string[0]))
            {
                //Force create model early
                FLLog.Debug("model", ctx.Model.ToString());
            }
            srv.DbContextFactory = ctxFactory;
            
            srv.ServerName = config.ServerName;
			srv.ServerDescription = config.ServerDescription;
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
			Config config = new Config();
            config.UseLazyLoading = true;

			Console.Write("Freelancer Path: ");
			config.FreelancerPath = (Console.ReadLine() ?? "").Trim();

			Console.Write("Db Path: ");
			config.DatabasePath = (Console.ReadLine() ?? "").Trim();

			Console.Write("Server Name: ");
			config.ServerName = (Console.ReadLine() ?? "").Trim();

			Console.Write("Server Description: ");
			config.ServerDescription = (Console.ReadLine() ?? "").Trim();

			File.WriteAllText("librelancerserver.config.json", JSON.Serialize(config));
		}
	}
}
