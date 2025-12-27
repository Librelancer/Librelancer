using System;
using System.Collections.Generic;
using System.Text;

namespace LancerEdit.GameContent.Groups;

public abstract class SystemObjectGroup<T>
{
    public readonly List<T> Members = new();

    public bool Contains(T obj) => Members.Contains(obj);

    public virtual void Add(T obj)
    {
        if (!Members.Contains(obj))
            Members.Add(obj);
    }
}
