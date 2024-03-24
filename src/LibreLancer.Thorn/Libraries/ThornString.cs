using System;
using System.Collections.Generic;
using System.Text;
using LibreLancer.Thorn.VM;

namespace LibreLancer.Thorn.Libraries;

static class ThornString
{
    public static object strlen(object[] args) => args[0].ToString().Length;

    public static object strlower(object[] args) => args[0].ToString().ToLowerInvariant();

    public static object strupper(object[] args) => args[0].ToString().ToUpperInvariant();

    public static object strrep(object[] args)
    {
        var str = args[0].ToString();
        var count = (int)Convert.ToSingle(args[1]);
        var sb = new StringBuilder();
        while (count-- > 0)
            sb.Append(str);
        return sb.ToString();
    }

    public static object format(object[] args)
    {
        var str = args[0].ToString();
        return KopiLua.sprintf(str, args.AsSpan().Slice(1));
    }

    public static void SetBuiltins(Dictionary<string, object> Env, ThornRuntime runtime)
    {
        Env["strlen"] = (ThornRuntimeFunction)strlen;
        Env["strlower"] = (ThornRuntimeFunction)strlower;
        Env["strupper"] = (ThornRuntimeFunction)strupper;
        Env["strrep"] = (ThornRuntimeFunction)strrep;
        Env["format"] = (ThornRuntimeFunction)format;
    }
}
