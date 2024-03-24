using System;
using LibreLancer.Thorn.Bytecode;

namespace LibreLancer.Thorn.VM;

struct ThornClosure
{
    public LuaPrototype Method;
    public object[] Upvalues;

    internal static ThornClosure Default(LuaPrototype method) =>
        new ThornClosure() { Method = method, Upvalues = Array.Empty<object>()};
}
