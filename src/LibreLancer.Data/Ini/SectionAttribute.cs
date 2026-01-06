// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Data.Ini;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class SectionAttribute : Attribute
{
    public string Name;
    public string[]? Delimiters;
    public Type? Type;
    public bool Child;
    public SectionAttribute(string name, Type? type = null)
    {
        Name = name;
        Type = type;
    }
}
