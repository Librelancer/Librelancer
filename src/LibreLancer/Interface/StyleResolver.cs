using System;
using System.Collections.Generic;

namespace LibreLancer.Interface;

public class StyleResolver
{
    private Dictionary<string, StylePropertyBase> values = new(StringComparer.OrdinalIgnoreCase);

    public T Create<T>() where T : IStyle, new()
    {
        T style = new T();
        style.Create(this);
        return style;
    }

    public StyleResolver Query<T>(StyledProperty<T> property)
    {
        if (values.TryGetValue(property.Name, out var p) &&
            p is StyledProperty<T> v)
        {
            property.Set(v.Value);
        }
        return this;
    }

    public StyleResolver Add(IStyle? style)
    {
        style?.Set(this);
        return this;
    }

    public StyleResolver Add(StylePropertyBase property)
    {
        if (!property.IsSet)
            return this;
        values[property.Name] = property;
        return this;
    }


}

public interface IStyle
{
    public void Set(StyleResolver resolver);
    public void Create(StyleResolver resolver);
}

public class StylePropertyBase(string name, bool isSet)
{
    public string Name = name;
    public bool IsSet = isSet;
}

public class StyledProperty<T> : StylePropertyBase
{
    public T? Value;
    public StyledProperty(string name) : base(name, false)
    {
    }

    public StyledProperty(string name, T? def) : base(name, false)
    {
        Value = def;
    }

    public void Set(T? value)
    {
        Value = value;
        IsSet = true;
    }
}
