using System;
using System.IO;
using Mono.CSharp;
public enum BPlatforms { Windows = 2, Linux = 4, MacOS = 8, All = Windows | Linux | MacOS }
namespace MonoCSharpTest
{
	class MainClass
	{
        public static void Main(string[] args)
		{
			var printer = new ConsoleReportPrinter();
			var settings = new CompilerSettings();
            settings.SetIgnoreWarning(0618);
			var evaluator = new Evaluator(new CompilerContext(settings, printer));
            evaluator.ReferenceAssembly(typeof(MainClass).Assembly);
			object r; bool s;
			using (var reader = new StreamReader(typeof(MainClass).Assembly.GetManifestResourceStream("MonoCSharpTest.slngen_cs.embed")))
			{
				var code = reader.ReadToEnd();
				var c = code.Replace("<INSERT_CONFIG_HERE>", File.ReadAllText("slngen.conf"));
				string expr = null;
                int linectr = 0;
				foreach (var line in c.Split('\n'))
				{
					if (line == null || line == "") continue;
					expr = expr == null ? line : expr + "\n" + line;
                    string toevalute = expr;
                    try
                    {
                        expr = evaluator.Evaluate(expr, out r, out s);
                    } catch (Exception ex)
                    {
                        Console.WriteLine("Exception near line {0} - failed to evaluate - {1}", linectr, toevalute);
                        Console.WriteLine(ex.Message);
                        return;
                    }
                    linectr++;
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
