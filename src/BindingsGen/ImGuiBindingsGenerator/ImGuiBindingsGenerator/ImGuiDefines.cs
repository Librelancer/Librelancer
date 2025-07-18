namespace ImGuiBindingsGenerator;

public class ImGuiDefines
{
    private Dictionary<string, string> knownDefines = new();
    private Dictionary<string, string> knownConstants = new();
    
    public bool EvalConditionals(List<ConditionalItem>? conditionals)
    {
        if (conditionals is { Count: > 0 })
        {
            if (conditionals.Count == 1)
            {
                var condition = conditionals[0];
                return ((condition.Condition == "ifdef" && knownDefines.ContainsKey(condition.Expression)) ||
                        (condition.Condition == "ifndef" && !knownDefines.ContainsKey(condition.Expression)) ||
                        (condition.Condition == "if" && condition.Expression.StartsWith("defined") &&
                         !condition.Expression.StartsWith("&&") &&
                         knownDefines.ContainsKey(
                             condition.Expression.Substring(8, condition.Expression.Length - 8 - 1))));
            }
            else
            {
                var condition = conditionals[1];
                return ((condition.Condition == "ifdef" && knownDefines.ContainsKey(condition.Expression)) ||
                        (condition.Condition == "ifndef" && !knownDefines.ContainsKey(condition.Expression)));
            }
        }
        else
        {
            return true;
        }
    }

    public void Define(string key, string value)
    {
        knownDefines[key] = value;
    }

    public void AddConstant(string key, string value)
    {
        knownConstants[key] = value;
    }

    public string ProcessBounds(string bounds)
    {
        var all = knownDefines.Concat(knownConstants)
            .OrderByDescending(x => x.Key.Length);
        foreach (var kv in all)
        {
            bounds = bounds.Replace(kv.Key, kv.Value);
        }
        return bounds;
    }
    
    public void AddDefines(List<DefineItem> defines)
    {
        // dear_bindings writes defines in a strange manner, producing redefines, so when we group them by count, we can produce more accurate result
        var defineGroups = defines.GroupBy(x => x.Conditionals?.Count ?? 0);

        foreach (var group in defineGroups)
        {
            if (group.Key == 0)
            {
                foreach (var define in group)
                {
                    knownDefines[define.Name] = define.Content ?? "";
                }
            }
            else if (group.Key == 1)
            {
                foreach (var define in group)
                {
                    var condition = EvalConditionals(define.Conditionals);
                    if (condition)
                    {
                        knownDefines[define.Name] = define.Content ?? "";
                    }
                    else
                    {
                        //skip
                    }
                }
            }
            else
            {
                Dictionary<string, string> newDefines = new();
                foreach (var define in group)
                {
                    var condition = EvalConditionals(
                        define.Conditionals!.Skip(group.Key - 1)
                            .ToList()
                    );
                    if (condition)
                    {
                        newDefines[define.Name] = define.Content ?? "";
                    }
                    else
                    {
                        //skip
                    }
                }

                foreach (var (key, value) in newDefines)
                {
                    knownDefines[key] = value;
                }
            }
        }

        Console.WriteLine("Defines");
        foreach (var define in knownDefines)
        {
            Console.WriteLine($"{define.Key}: {define.Value}");
        }
    }
    
}