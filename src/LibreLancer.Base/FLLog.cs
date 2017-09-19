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
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Runtime.InteropServices;

namespace LibreLancer
{
	public enum LogSeverity {
		Debug,
		Info,
		Warning,
		Error
	}
	public static class FLLog
	{
		#if DEBUG
		public static LogSeverity MinimumSeverity = LogSeverity.Debug;
#else
		public static LogSeverity MinimumSeverity = LogSeverity.Info;
#endif

		[DllImport("libc")]
		static extern bool isatty(int desc);

		public static void Write(string component, string message, LogSeverity severity)
		{
			if ((int)severity < (int)MinimumSeverity)
				return;
			
			var newC = ConsoleColor.White;
			switch (severity) {
			case LogSeverity.Debug:
				newC = ConsoleColor.DarkGray;
				break;
			case LogSeverity.Error:
				newC = ConsoleColor.Red;
				break;
			case LogSeverity.Warning:
				newC = ConsoleColor.Yellow;
				break;
			}
			if (Platform.RunningOS == OS.Windows)
			{
				var c = Console.ForegroundColor;
				Console.ForegroundColor = newC;
				Console.WriteLine("[{1}] {0}: {2}", component, severity, message);
				Console.ForegroundColor = c;
			}
			else if (newC != ConsoleColor.White && isatty(1))
			{
				string cc = "";
				if (newC == ConsoleColor.DarkGray) cc = "\x1b[90m";
				if (newC == ConsoleColor.Yellow) cc = "\x1b[33m";
				if (newC == ConsoleColor.Red) cc = "\x1b[91m";
				Console.WriteLine("{3}[{1}] {0}: {2}\x1b[0m", component, severity, message, cc);
			}
			else
				Console.WriteLine("[{1}] {0}: {2}", component, severity, message);
		}


		public static void Info(string component, string message)
		{
			Write (component, message, LogSeverity.Info);
		}
		public static void Debug(string component, string message)
		{
			Write (component, message, LogSeverity.Debug);
		}
		public static void Warning(string component, string message)
		{
			Write (component, message, LogSeverity.Warning);
		}
		public static void Error(string component, string message)
		{
			Write (component, message, LogSeverity.Error);
		}
	}
}

