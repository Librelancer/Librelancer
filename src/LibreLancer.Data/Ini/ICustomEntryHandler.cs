// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;

namespace LibreLancer.Ini
{
    public class CustomEntry
    {
        public static readonly Action<ICustomEntryHandler, Entry> Ignore = (h, e) => { };
        public uint Hash;
        public Action<ICustomEntryHandler, Entry> Handler;

        public CustomEntry(string name, Action<ICustomEntryHandler, Entry> handler)
        {
            Hash = IniFile.Hash(name);
            Handler = handler;
        }
    }
    public interface ICustomEntryHandler
    {
        IEnumerable<CustomEntry> CustomEntries { get; }
    }

    public interface IEntryHandler
    {
        bool HandleEntry(Entry e);
    }
}