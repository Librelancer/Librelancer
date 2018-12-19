using System;
namespace LibreLancer.Ini
{
    public class SectionAttribute : Attribute
    {
        public string Name;
        public SectionAttribute(string name)
        {
            Name = name;
        }
    }
}
