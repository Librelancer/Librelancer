/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2017
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.IO;
using LibreLancer;
using LibreLancer.Compatibility;
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
