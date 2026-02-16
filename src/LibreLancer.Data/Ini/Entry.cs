// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LibreLancer.Data.Ini;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
public class Entry : ICollection<ValueBase>
{
    private readonly IList<ValueBase> values;

    public string Name { get; init; }

    public Section Section { get; init; }

    public int Line { get; init; } = -1;

    public Entry(Section section, string name, int capacity = -1)
    {
        Section = section;
        Name = name;
        values = capacity > 0 ? new List<ValueBase>(capacity) : new List<ValueBase>();
    }

    public ValueBase this[int index]
    {
        get => values[index];
        set => values[index] = value;
    }

    public void Add(ValueBase item) => values.Add(item);
    public void Clear() => values.Clear();
    public bool Contains(ValueBase item) => values.Contains(item);
    public void CopyTo(ValueBase[] array, int arrayIndex) => values.CopyTo(array, arrayIndex);
    public int Count => values.Count;
    public bool IsReadOnly => false;
    public bool Remove(ValueBase item) => values.Remove(item);
    public IEnumerator<ValueBase> GetEnumerator() => values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => values.GetEnumerator();

    /*public static bool operator ==(Entry operand1, Entry operand2)
    {
        return operand1.Equals(operand2);
    }

    public static bool operator !=(Entry operand1, Entry operand2)
    {
        return !(operand1 == operand2);
    }

    public override bool Equals(object obj)
    {
        if (obj is Entry)
        {
            Entry e = (Entry)obj;
            return namePointer == e.namePointer && values == e.values;
        }
        else return false;
    }

    public override int GetHashCode()
    {
        return namePointer.GetHashCode() ^ values.GetHashCode();
    }*/

    public override string ToString()
    {
        StringBuilder sb = new(Name);
        if (values.Count > 0)
        {
            sb.Append(" = ");
        }

        for (var i = 0; i < values.Count; i++)
        {
            sb.Append(values[i]);
            if (i < values.Count - 1)
            {
                sb.Append(", ");
            }
        }

        return sb.ToString();
    }
}
