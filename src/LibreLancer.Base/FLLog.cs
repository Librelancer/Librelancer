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
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;

//High performance console output - nonblocking + colour coded
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
			NonblockWrite(newC, string.Format("[{0}] {1}: {2}", severity, component, message));
		}

		struct NonblockingWrite
		{
			public ConsoleColor Color;
			public string Value;
		}

		static BlockingCollection<NonblockingWrite> m_Queue = new BlockingCollection<NonblockingWrite>();
		static FLLog()
		{
			Thread thread = new Thread(
	 		() =>
	 		{
				 while (true)
				 {
					 var q = m_Queue.Take();
					 if (Platform.RunningOS == OS.Windows)
					 {
						 var c = Console.ForegroundColor;
						 Console.ForegroundColor = q.Color;
						 Console.WriteLine(q.Value);
						 Console.ForegroundColor = c;
					 }
					 else if (q.Color != ConsoleColor.White && isatty(1))
					 {
						 string cc = "";
						 if (q.Color == ConsoleColor.DarkGray) cc = "\x1b[90m";
						 if (q.Color == ConsoleColor.Yellow) cc = "\x1b[33m";
						 if (q.Color == ConsoleColor.Red) cc = "\x1b[91m";
						 Console.WriteLine("{0}{1}\x1b[0m", cc, q.Value);
					 }
					 else
						 Console.WriteLine(q.Value);
				 }
	 		});
			thread.IsBackground = true;
            thread.Name = "Log";

            thread.Start();
		}
		static void NonblockWrite(ConsoleColor color, string message)
		{
			m_Queue.Add(new NonblockingWrite() { Color = color, Value = message });
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

