// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer.Ini
{
    public class EntryAttribute : Attribute
    {
        public string Name;
        public bool MinMax = false;
        public bool Multiline = false;
        public bool Presence = false;
        public bool Required = false;

        public EntryAttribute(string name)
        {
            Name = name;
        }
    }
}
