using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace LibreLancer.GeneratorCommon;

public class AttributeArguments
{
    private Dictionary<string, TypedConstant> args;

    public AttributeArguments(ImmutableArray<KeyValuePair<string, TypedConstant>> arguments)
    {
        args = new(arguments.Length);
        foreach (var kv in arguments)
        {
            args[kv.Key] = kv.Value;
        }
    }

    static int OptionalInt(object? optional) => (int)(optional ?? 0);
    static string OptionalString(object? optional) => optional?.ToString() ?? "";
    static bool OptionalBool(object? optional) => (bool)(optional ?? false);

    public int Integer(string property)
    {
        if (args.TryGetValue(property, out var v))
        {
            return OptionalInt(v.Value);
        }
        return 0;
    }
    public string String(string property)
    {
        if (args.TryGetValue(property, out var v))
        {
            return OptionalString(v.Value);
        }
        return "";
    }

    public EquatableArray<string> StringArray(string property)
    {
        if (args.TryGetValue(property, out var v) &&
            v.Kind == TypedConstantKind.Array)
        {
            var array = new string[v.Values.Length];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = OptionalString(v.Values[i].Value);
            }
            return new EquatableArray<string>(array);
        }
        return new EquatableArray<string>(Array.Empty<string>());
    }

    public bool Boolean(string property)
    {
        if (args.TryGetValue(property, out var v))
        {
            return OptionalBool(v.Value);
        }
        return false;
    }
}
