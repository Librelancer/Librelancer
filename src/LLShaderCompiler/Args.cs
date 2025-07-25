namespace LLShaderCompiler;

public class ArgumentContext
{
    public int MinArgs;
}

public class Args
{
    private List<(string Option, string Description)> argDescs = new();
    private Dictionary<string, Action<string>> stringArgs = new();
    private Dictionary<string, Action<ArgumentContext>> flagArgs = new();

    private string usage;

    public Args(string usage)
    {
        this.usage = usage;
        argDescs.Add(("--rsp", "Read arguments from response file."));
    }

    public void PrintUsage(TextWriter stream)
    {
        stream.WriteLine(usage);
        var paddingLen = argDescs.Max(x => x.Option.Length) + 4;
        foreach (var x in argDescs.OrderBy(x => x.Option))
        {
            stream.WriteLine($" {x.Option.PadRight(paddingLen)}{x.Description}");
        }
    }

    public void String(string nameLong, string description, Action<string> set)
    {
        stringArgs[$"--{nameLong}"] = set;
        argDescs.Add(($"--{nameLong}", description));
    }

    public void Flag(string nameLong, string description, Action<ArgumentContext> set)
    {
        flagArgs[$"--{nameLong}"] = set;
        argDescs.Add(($"--{nameLong}", description));
    }

    public void Flag(string nameLong, string description, Action set)
    {
        flagArgs[$"--{nameLong}"] = _ => set();
        argDescs.Add(($"--{nameLong}", description));
    }

    public string[] ParseArgs(string[] args, int minimum)
    {
        var ctx = new ArgumentContext() { MinArgs = minimum };
        List<string> positionalArgs = new List<string>();
        bool parseArgs = true;
        var allArgs = new List<string>(args);

        for (int i = 0; i < allArgs.Count; i++)
        {
            if (!parseArgs)
            {
                positionalArgs.Add(allArgs[i]);
                continue;
            }
            if (allArgs[i] == "--")
            {
                parseArgs = false;
                continue;
            }
            if (allArgs[i] == "--rsp")
            {
                if (i + 1 < args.Length)
                {
                    var newArgs = File.ReadAllLines(args[i + 1]).Select(x => x.Trim()).
                        Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
                    allArgs.InsertRange(i + 2, newArgs);
                    i++;
                    continue;
                }
                else
                {
                    Console.Error.WriteLine("--rsp missing argument");
                    PrintUsage(Console.Error);
                    Environment.Exit(1);
                }
            }
            if (allArgs[i].StartsWith("--"))
            {
                if (flagArgs.TryGetValue(allArgs[i], out var set))
                {
                    set(ctx);
                }
                else if (stringArgs.TryGetValue(allArgs[i], out var setString))
                {
                    if (i + 1 < args.Length)
                    {
                        setString(args[i + 1]);
                        i++;
                    }
                }
                else
                {
                    Console.Error.WriteLine($"Unknown option {allArgs[i]}");
                    PrintUsage(Console.Error);
                    Environment.Exit(1);
                }
            }
            else
            {
                positionalArgs.Add(allArgs[i]);
            }
        }
        if (positionalArgs.Count < ctx.MinArgs)
        {
            PrintUsage(Console.Out);
            Environment.Exit(args.Length == 0 ? 0 : 1);
        }
        return positionalArgs.ToArray();
    }
}
