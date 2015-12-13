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

