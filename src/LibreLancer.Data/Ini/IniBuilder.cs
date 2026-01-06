using System.Collections.Generic;
using System.Numerics;

namespace LibreLancer.Data.Ini;

public class IniBuilder
{
    public List<Section> Sections = [];

    public IniSectionBuilder Section(string name)
    {
        var s = new Section(name);
        var builder = new IniSectionBuilder() { Section = s, Parent = Sections };
        Sections.Add(s);
        return builder;
    }

    public class IniSectionBuilder
    {
        public Section Section = null!;
        public List<Section> Parent = null!;

        public void RemoveIfEmpty()
        {
            if (Section.Count == 0)
                Parent.Remove(Section);
        }

        public IniSectionBuilder OptionalEntry(string name, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                var e = new Entry(Section, name);
                e.Add(new StringValue(value));
                Section.Add(e);
            }

            return this;
        }

        public IniSectionBuilder OptionalEntry(string name, string[]? value)
        {
            if (value is not { Length: > 0 })
            {
                return this;
            }

            Entry(name, value);
            return this;
        }

        public IniSectionBuilder OptionalEntry(string name, int value, int defaultValue = 0)
        {
            if (value != defaultValue)
            {
                var e = new Entry(Section, name);
                e.Add(new Int32Value(value));
                Section.Add(e);
            }

            return this;
        }

        public IniSectionBuilder OptionalEntry(string name, uint value, uint defaultValue = 0)
        {
            if (value != defaultValue)
            {
                var e = new Entry(Section, name);
                e.Add(new Int32Value((int)value));
                Section.Add(e);
            }

            return this;
        }

        public IniSectionBuilder OptionalEntry(string name, float value, float defaultValue = 0)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (value != defaultValue)
            {
                var e = new Entry(Section, name);
                e.Add(new SingleValue(value, null));
                Section.Add(e);
            }

            return this;
        }

        public IniSectionBuilder OptionalEntry(string name, bool value, bool defaultValue = false)
        {
            if (value != defaultValue)
            {
                var e = new Entry(Section, name);
                e.Add(new BooleanValue(value));
                Section.Add(e);
            }

            return this;
        }

        public IniSectionBuilder OptionalEntry(string name, HashValue value) => value is { Hash: 0, String: null } ? this : Entry(name, value);

        public IniSectionBuilder Entry(string name, Vector4 value)
            => Entry(name, value.X, value.Y, value.Z, value.W);

        public IniSectionBuilder Entry(string name, Color4 value, bool alpha = false)
            => alpha
                ? Entry(name, (int)(value.R * 255), (int)(value.G * 255), (int)(value.B * 255), (int)(value.A * 255))
                : Entry(name, (int)(value.R * 255), (int)(value.G * 255), (int)(value.B * 255));

        public IniSectionBuilder Entry(string name, Color3f value)
            => Entry(name, (int)(value.R * 255), (int)(value.G * 255), (int)(value.B * 255));


        public IniSectionBuilder Entry(string name, Vector2 value)
            => Entry(name, value.X, value.Y);

        public IniSectionBuilder Entry(string name, Vector3 value)
            => Entry(name, value.X, value.Y, value.Z);

        public IniSectionBuilder Entry(string name, Quaternion value) =>
            Entry(name, value.W, value.X, value.Y, value.Z);

        public IniSectionBuilder Entry(string name, IEnumerable<string> values)
        {
            var e = new Entry(Section, name);
            foreach (var v in values)
                e.Add(new StringValue(v));
            Section.Add(e);
            return this;
        }

        public IniSectionBuilder Entry(string name, ValueBase value)
        {
            var e = new Entry(Section, name);
            e.Add(value);
            Section.Add(e);
            return this;
        }

        public IniSectionBuilder Entry(string name, params ValueBase[]? values)
        {
            var e = new Entry(Section, name);
            if (values != null)
            {
                foreach (var v in values)
                {
                    e.Add(v);
                }
            }

            Section.Add(e);
            return this;
        }
    }
}
