using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibreLancer.Thorn.VM;

namespace LibreLancer.Thorn.Libraries;

static class ThornIO
{
    public static object print(object[] args, ThornRuntime runtime)
    {
        runtime.Write(string.Join(' ', args.Select(x => x.ToString())) + "\n");
        return null;
    }

    public static object write(object[] args, ThornRuntime runtime)
    {
        runtime.Write(string.Concat(args.Select(x => x.ToString())));
        return null;
    }

    public static object dofile(object[] args, ThornRuntime runtime)
    {
        var filename = args[0].ToString();
        if (runtime.OnReadFile == null)
            throw new Exception("dofile support disabled");
        var bytes = runtime.OnReadFile(filename);
        int instCount = 0;
        return runtime.DoStreamInternal(new MemoryStream(bytes), filename, 0, ref instCount);
    }

    public static void SetBuiltins(Dictionary<string, object> Env, ThornRuntime runtime)
    {
        //IO (most removed)
        Env["print"] = (ThornRuntimeFunction)((args) => print(args, runtime));
        Env["write"] = (ThornRuntimeFunction)((args) => write(args, runtime));
        Env["dofile"] = (ThornRuntimeFunction)((args) => dofile(args, runtime));
    }
}
