using System.Collections.Generic;
using System.Text;
namespace InterfaceEdit;

public static class ProjectVariable
{
    public static string Substitute(string input, IDictionary<string,string> replacements, string defaultValue = null)
    {
        if (string.IsNullOrWhiteSpace(input) && defaultValue != null)
            return defaultValue;
        var builder = new StringBuilder();
        for (int i = 0; i < input.Length; i++) {
            if (input[i] == '{')
            {
                var endBrace = input.IndexOf('}', i);
                if (endBrace == -1) {
                    builder.Append('{');
                    continue;
                }
                var varname = input.Substring(i + 1, endBrace - i - 1);
                if (varname.Contains('{')) {
                    builder.Append('{');
                    continue;
                }
                if (replacements.TryGetValue(varname, out var replacement))
                {
                    builder.Append(replacement);
                }
                else
                {
                    builder.Append('{').Append(varname).Append('}');
                }
                i = endBrace;
            }
            else
            {
                builder.Append(input[i]);
            }
        }
        return builder.ToString();
    }
}