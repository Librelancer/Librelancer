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
using System.Diagnostics;
using System.Threading;
using LibreLancer;
namespace Server
{
	class MainClass
	{
		public static int Main(string[] args)
		{
			var confpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "librelancer.serverpath.txt");
			string path = "";
			if (File.Exists("librelancer.serverpath.txt"))
			{
				path = File.ReadAllText("librelancer.serverpath.txt").Trim();
			}
			else if (File.Exists(confpath))
			{
				path = File.ReadAllText(confpath).Trim();
			}
			else
			{
				Console.Error.WriteLine("Failed to find librelancer.serverpath.txt");
				Console.Error.WriteLine("Please create it in the current directory or at {0}", confpath);
				return 2;
			}
			var srv = new GameServer(path);
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
	}
}
