using System;
using System.IO;
using Mono.CSharp;
namespace MonoCSharpTest
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			var printer = new ConsoleReportPrinter();
			var settings = new CompilerSettings();
			var evaluator = new Evaluator(new CompilerContext(settings, printer));
			object r; bool s;
			using (var reader = new StreamReader(typeof(MainClass).Assembly.GetManifestResourceStream("MonoCSharpTest.slngen_cs.embed")))
			{
				var code = reader.ReadToEnd();
				var c = code.Replace("<INSERT_CONFIG_HERE>", File.ReadAllText("slngen.conf"));
				string expr = null;
				foreach (var line in c.Split('\n'))
				{
					if (line == null || line == "") continue;
					expr = expr == null ? line : expr + "\n" + line;
					expr = evaluator.Evaluate(expr, out r, out s);
				}
			}
		}

		public enum AgentStatus : byte
		{
			// Received partial input, complete
			PARTIAL_INPUT = 1,

			// The result was set, expect the string with the result
			RESULT_SET = 2,

			// No result was set, complete
			RESULT_NOT_SET = 3,

			// Errors and warnings string follows
			ERROR = 4,

			// Stdout
			STDOUT = 5,
		}
	}
}
