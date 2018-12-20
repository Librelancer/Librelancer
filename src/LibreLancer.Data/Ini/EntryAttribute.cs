using System;
namespace LibreLancer.Ini
{
    public class EntryAttribute : Attribute
    {
        public string Name;
        public bool MinMax = false;
        public bool Multiline = false;

        public EntryAttribute(string name)
        {
            Name = name;
        }
    }
}
