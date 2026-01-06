// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LibreLancer.Data.Ini;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
public class Section : ICollection<Entry>
{
    private List<Entry> entries;

    public string Name { get; init; }

    public string? File { get; init; } = "[Null]";

    public int Line { get; init; } = -1;

    public Section(string name, int capacity = -1)
    {
        entries = capacity > 0 ? new List<Entry>(capacity) : [];
        Name = name;
    }

    public Entry this[int index]
    {
        get => entries[index];
        set => entries[index] = value;
    }

    public Entry? this[string name]
    {
        get
        {
            var candidates = entries.Where(e => e.Name == name).ToArray();
            var count = candidates.Count();
            return count == 0 ? null : candidates.First();
        }
    }

    public void Add(Entry item) => entries.Add(item);
    public void Clear() => entries.Clear();
    public bool Contains(Entry item) => entries.Contains(item);
    public void CopyTo(Entry[] array, int arrayIndex) => entries.CopyTo(array, arrayIndex);
    public int Count => entries.Count;
    public bool IsReadOnly => false;
    public bool Remove(Entry item) => entries.Remove(item);
    public List<Entry>.Enumerator GetEnumerator() => entries.GetEnumerator();
    IEnumerator<Entry> IEnumerable<Entry>.GetEnumerator() => entries.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => entries.GetEnumerator();

    public override string ToString()
    {
        //string result = "[" + Name + "]\r\n";
        //foreach (Entry e in entries) result += e + "\r\n";
        //return result;
        return Name;
    }
}
