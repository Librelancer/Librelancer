using LibreLancer.GeneratorCommon;

namespace LibreLancer.Net.Generator;

public record RPCInterface(string Name, string? ContainingNamespace, EquatableArray<RPCMethod> Methods)
{
    public string FullName() => string.IsNullOrEmpty(ContainingNamespace) ? Name : $"{ContainingNamespace}.{Name}";
}
public record RPCMethod(string Name, int Channel, RPCType ReturnType, EquatableArray<RPCParameter> Parameters);
public record struct RPCParameter(string Name, RPCType Type);

public record struct RPCType(string Name, bool Array, bool Enum, bool Task)
{
    public string FullName(bool overrideTask)
    {
        var basicName = Array ? $"{Name}[]" : Name;
        return Task && !overrideTask ? $"System.Threading.Tasks.Task<{Name}>" : basicName;
    }
}


