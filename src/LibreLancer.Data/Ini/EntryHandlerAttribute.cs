using System;
using JetBrains.Annotations;

namespace LibreLancer.Data.Ini;



[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class EntryHandlerAttribute : Attribute
{
    public int MinComponents = 0;
    public bool Multiline = false;
    public string Name;

    public EntryHandlerAttribute(string name)
    {
        Name = name;
    }
}
