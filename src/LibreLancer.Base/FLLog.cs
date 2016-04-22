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

		public static void Write(string component, string message, LogSeverity severity)
		{
			if ((int)severity < (int)MinimumSeverity)
				return;
			var c = Console.ForegroundColor;
			switch (severity) {
			case LogSeverity.Debug:
				Console.ForegroundColor = ConsoleColor.DarkGray;
				break;
			case LogSeverity.Error:
				Console.ForegroundColor = ConsoleColor.Red;
				break;
			case LogSeverity.Warning:
				Console.ForegroundColor = ConsoleColor.Yellow;
				break;
			}
			Console.WriteLine ("[{1}] {0}: {2}", component, severity, message);
			Console.ForegroundColor = c;
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

