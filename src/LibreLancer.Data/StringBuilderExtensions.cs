using System.Collections.Immutable;
using System.Numerics;
using System.Text;

namespace LibreLancer.Data;

static class StringBuilderExtensions
{
    static string Float(float f) => f.ToString("0.#########");

    public static StringBuilder AppendFloat(this StringBuilder builder, float f)
    {
        return builder.Append(Float(f));
    }
    
    public static StringBuilder AppendEntry(this StringBuilder builder, string name, float value)
    {
        return builder.Append(name).Append(" = ").AppendLine(Float(value));
    }
    
    public static StringBuilder AppendEntry(this StringBuilder builder, string name, float value1, string value2)
    {
        return builder.Append(name).Append(" = ").Append(Float(value1)).Append(", ").AppendLine(value2);
    }
    
    public static StringBuilder AppendEntry(this StringBuilder builder, string name, HashValue value, bool writeIfZero = true)
    {
        if (!writeIfZero && value == 0) return builder;
        return builder.Append(name).Append(" = ").AppendLine(((uint) value).ToString());
    }
    public static StringBuilder AppendEntry(this StringBuilder builder, string name, int value, bool writeIfZero = true)
    {
        if (!writeIfZero && value == 0) return builder;
        return builder.Append(name).Append(" = ").AppendLine(value.ToString());
    }

    public static StringBuilder AppendEntry(this StringBuilder builder, string name, bool value)
    {
        return builder.Append(name).Append(" = ").AppendLine(value ? "true" : "false");
    }
    public static StringBuilder AppendEntry(this StringBuilder builder, string name, uint value, bool writeIfZero = true)
    {
        if (!writeIfZero && value == 0) return builder;
        return builder.Append(name).Append(" = ").AppendLine(value.ToString());
    }
    
    public static StringBuilder AppendEntry(this StringBuilder builder, string name, long value)
    {
        return builder.Append(name).Append(" = ").AppendLine(value.ToString());
    }

    public static StringBuilder AppendEntry(this StringBuilder builder, string name, Vector3 value)
    {
        return builder.AppendLine($"{name} = {Float(value.X)}, {Float(value.Y)}, {Float(value.Z)}");
    }

    public static StringBuilder AppendEntry(this StringBuilder builder, string name, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return builder;
        return builder.Append(name).Append(" = ").AppendLine(value);
    }
}