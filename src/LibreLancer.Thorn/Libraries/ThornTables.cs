using System;
using System.Collections.Generic;
using LibreLancer.Thorn.VM;

namespace LibreLancer.Thorn.Libraries;

static class ThornTables
{
    public static object getn(object[] args)
    {
        var table = (ThornTable)args[0];
        var n = table.Get("n");
        if (n is float nval)
            return nval;
        return (float)table.Length;
    }

    public static object tremove(object[] args)
    {
        var vlist = (ThornTable)args[0];
        object ret = null;

        int len = vlist.Length;

        int pos = args.Length < 2 ? len : (int)(float)args[1];

        if (pos >= len + 1 || (pos < 1 && len > 0))
            throw new Exception("bad argument #1 to 'remove' (position out of bounds)");

        for (int i = pos; i <= len; i++)
        {
            if (i == pos)
                ret = vlist.Get(i);
            vlist.Set(i, vlist.Get(i + 1));
        }

        return ret;
    }

    public static object next(object[] args)
    {
        var table = (ThornTable)args[0];
        var cindex = args[1];
        var pair = table.NextKey(cindex);
        if (pair == null)
            return new ThornTuple(null, null);
        else
            return new ThornTuple(pair.Value.Key, pair.Value.Value);
    }

    public static object setglobal(object[] args, ThornRuntime runtime)
    {
        var index = args[0].ToString();
        runtime.Env[index] = args[1];
        return args[1];
    }

    public static object getglobal(object[] args, ThornRuntime runtime)
    {
        var index = args[0].ToString();
        return runtime.Env[index];
    }




    public static void SetBuiltins(Dictionary<string, object> Env, ThornRuntime runtime)
    {
        Env["getn"] = (ThornRuntimeFunction)getn;
        Env["next"] = (ThornRuntimeFunction)next;
        Env["tremove"] = (ThornRuntimeFunction)tremove;
        Env["getglobal"] = (ThornRuntimeFunction)((e) => getglobal(e, runtime));
        Env["setglobal"] = (ThornRuntimeFunction)((e) => setglobal(e, runtime));
    }
}
