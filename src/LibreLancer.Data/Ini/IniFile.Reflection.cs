// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
namespace LibreLancer.Ini
{
    //Class for constructing ini through Reflection
    public abstract partial class IniFile
    {
        class ReflectionInfo
        {
            public Type Type;
            public List<ReflectionField> Fields = new List<ReflectionField>();
            public MethodInfo HandleEntry;
        }

        class ReflectionSection
        {
            public string Name;
            public MethodInfo Add;
            public FieldInfo Field;
            public ReflectionInfo Type;
        }

        class ReflectionField
        {
            public EntryAttribute Attr;
            public FieldInfo Field;
        }

        static Dictionary<Type, List<ReflectionSection>> containerclasses = new Dictionary<Type, List<ReflectionSection>>();
        static Dictionary<Type, ReflectionInfo> sectionclasses = new Dictionary<Type, ReflectionInfo>();
        static string FormatLine(string file, int line) => string.Format(" at {0}, line {1}", file, line);
        static string FormatLine(string file, int line, string section) => string.Format(" at section {2}: {0}, line {1}", file, line, section);

        const BindingFlags F_CLASSMEMBERS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        static object _sLock = new object();
        static object _cLock = new object();

        static ReflectionInfo GetSectionInfo(Type t)
        {
            lock (_sLock)
            {
                ReflectionInfo info;
                if (sectionclasses.TryGetValue(t, out info)) return info;
                info = new ReflectionInfo() { Type = t };
                foreach (var field in t.GetFields(F_CLASSMEMBERS))
                {
                    var attrs = field.GetCustomAttributes<EntryAttribute>();
                    foreach (var a in attrs)
                        info.Fields.Add(new ReflectionField() { Attr = a, Field = field });
                }
                //This should never be tripped
                if (info.Fields.Count > 64) throw new Exception("Too many fields!! Edit bitmask code & raise limit");
                foreach (var mthd in t.GetMethods(F_CLASSMEMBERS))
                {
                    if (mthd.Name == "HandleEntry")
                    {
                        info.HandleEntry = mthd;
                        break;
                    }
                }
                sectionclasses.Add(t, info);
                return info;
            }
        }

        static List<ReflectionSection> GetContainerInfo(Type t)
        {
            lock (_cLock)
            {
                List<ReflectionSection> sections;
                if (containerclasses.TryGetValue(t, out sections)) return sections;
                sections = new List<ReflectionSection>();
                foreach (var field in t.GetFields(F_CLASSMEMBERS))
                {
                    var attr = field.GetCustomAttributes<SectionAttribute>().FirstOrDefault();
                    if (attr == null) continue;
                    var s = new ReflectionSection() { Name = attr.Name };
                    var fieldType = field.FieldType;
                    //Handle lists
                    if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        s.Add = fieldType.GetMethod("Add", F_CLASSMEMBERS);
                        if (s.Add == null) throw new Exception();
                        fieldType = fieldType.GetGenericArguments()[0]; // use this...
                    }
                    s.Type = GetSectionInfo(fieldType);
                    s.Field = field;
                    sections.Add(s);
                }
                containerclasses.Add(t, sections);
                return sections;
            }
        }

        public static T FromSection<T>(Section s)
        {
            return (T)GetFromSection(s, GetSectionInfo(typeof(T)));
        }

        static bool ComponentCheck(int c, Section s, Entry e, int min = -1)
        {
            if (min == -1) min = c;
            if (e.Count > c)
                FLLog.Warning("Ini", "Too many components for " + e.Name + FormatLine(e.File, e.Line, s.Name));
            if (e.Count >= min)
                return true;
            FLLog.Error("Ini", "Not enough components for " + e.Name + FormatLine(e.File, e.Line, s.Name));
            return false;
        }
        static object GetFromSection(Section s, ReflectionInfo type)
        {
            var obj = Activator.CreateInstance(type.Type);
            ulong bitmask = 0;
            foreach (var e in s)
            {
                //Find entry
                int idx = -1;
                for (int i = 0; i < type.Fields.Count; i++)
                {
                    if (type.Fields[i].Attr.Name.Equals(e.Name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        idx = i;
                        break;
                    }
                }
                //Special Handling
                if (idx == -1)
                {
                    bool handled = false;
                    if (type.HandleEntry != null)
                        handled = (bool)type.HandleEntry.Invoke(obj, new object[] { e });
                    if (!handled) FLLog.Warning("Ini", "Unknown entry " + e.Name + FormatLine(e.File, e.Line, s.Name));
                    continue;
                }

                var field = type.Fields[idx];
                //Warning for duplicates
                if (!field.Attr.Multiline)
                {
                    if ((bitmask & (1ul << idx)) != 0)
                    {
                        FLLog.Warning("Ini", "Duplicate of " + field.Attr.Name + FormatLine(e.File, e.Line, s.Name));
                    }
                    bitmask |= 1ul << idx;
                }
                var ftype = field.Field.FieldType;
                Type nType;
                if ((nType = Nullable.GetUnderlyingType(ftype)) != null) {
                    ftype = nType;
                }
                //Fill
                if (ftype == typeof(string))
                {
                    if (ComponentCheck(1, s, e)) field.Field.SetValue(obj, e[0].ToString());
                }
                else if (ftype == typeof(float))
                {
                    if (ComponentCheck(1, s, e)) field.Field.SetValue(obj, e[0].ToSingle());
                }
                else if (ftype == typeof(int))
                {
                    if (ComponentCheck(1, s, e)) field.Field.SetValue(obj, e[0].ToInt32());
                }
                else if (ftype == typeof(long))
                {
                    if (ComponentCheck(1, s, e)) field.Field.SetValue(obj, e[0].ToInt64());
                }
                else if (ftype == typeof(bool))
                {
                    if (ComponentCheck(1, s, e)) field.Field.SetValue(obj, e[0].ToBoolean());
                }
                else if (ftype == typeof(Vector3))
                {
                    if (ComponentCheck(3, s, e)) field.Field.SetValue(obj, new Vector3(e[0].ToSingle(), e[1].ToSingle(), e[2].ToSingle()));
                }
                else if(ftype == typeof(Quaternion))
                {
                    if (ComponentCheck(4, s, e)) field.Field.SetValue(obj, new Quaternion(e[1].ToSingle(), e[2].ToSingle(), e[3].ToSingle(), e[0].ToSingle()));
                }
                else if (ftype == typeof(Vector2))
                {
                    if (e.Count == 1 && field.Attr.MinMax)
                        field.Field.SetValue(obj, new Vector2(-1, e[0].ToSingle()));
                    else if (ComponentCheck(2, s, e)) field.Field.SetValue(obj, new Vector2(e[0].ToSingle(), e[1].ToSingle()));
                }
                else if (ftype == typeof(Color4))
                {
                    if (ComponentCheck(4, s, e, 3))
                    {
                        Color4 col;
                        if (e.Count == 3)
                            col = new Color4(e[0].ToInt32() / 255f, e[1].ToInt32() / 255f, e[2].ToInt32() / 255f, 1f);
                        else
                            col = new Color4(e[0].ToInt32() / 255f, e[1].ToInt32() / 255f, e[2].ToInt32() / 255f, e[3].ToInt32() / 255f);
                        field.Field.SetValue(obj, col);
                    }
                }
                else if (ftype == typeof(Color3f))
                {
                    if (ComponentCheck(3, s, e)) field.Field.SetValue(obj, new Color3f(e[0].ToSingle(), e[1].ToSingle(), e[2].ToSingle()));
                }
                else if (ftype == typeof(List<string>))
                {
                    if (field.Attr.Multiline)
                    {
                        bitmask &= ~(1ul << idx); //Avoid duplicate warnings
                        if (ComponentCheck(1, s, e))
                        {
                            var v = (List<string>)field.Field.GetValue(obj);
                            v.Add(e[0].ToString());
                        }
                    }
                    else if (ComponentCheck(int.MaxValue, s, e, 1))
                        field.Field.SetValue(obj, e.Select((x) => x.ToString()).ToList());
                }
                else if (ftype == typeof(float[]))
                {
                    if(ComponentCheck(int.MaxValue,s,e,1)) {
                        var floats = new float[e.Count];
                        for (int i = 0; i < e.Count; i++) floats[i] = e[i].ToSingle();
                        field.Field.SetValue(obj, floats);
                    }
                }
                else if (ftype == typeof(string[]))
                {
                    if (ComponentCheck(int.MaxValue, s, e, 1)) {
                        var strings = new string[e.Count];
                        for (int i = 0; i < e.Count; i++) strings[i] = e[i].ToString();
                        field.Field.SetValue(obj, strings);
                    }
                }
                else if (ftype.IsEnum)
                {
                    if (ComponentCheck(1, s, e))
                    {
                        //TryParse requires generics, wrap in exception handler
                        try
                        {
                            field.Field.SetValue(obj, Enum.Parse(field.Field.FieldType, e[0].ToString(), true));
                        }
                        catch (Exception)
                        {
                            FLLog.Error("Ini", "Invalid value for enum " + e[0].ToString() + FormatLine(e.File, e.Line, s.Name));
                        }
                    }
                }
            }
            return obj;
        }

        public void ParseAndFill(string filename, MemoryStream stream)
        {
            var sections = GetContainerInfo(this.GetType());
            foreach (var section in ParseFile(filename, stream))
            {
                var tgt = sections.FirstOrDefault((x) => x.Name.Equals(section.Name, StringComparison.InvariantCultureIgnoreCase));
                if (tgt == null)
                {
                    FLLog.Warning("Ini", "Unknown section " + section.Name + FormatLine(section.File, section.Line));
                    continue;
                }
                var parsed = GetFromSection(section, tgt.Type);
                if (tgt.Add != null)
                {
                    var list = tgt.Field.GetValue(this);
                    tgt.Add.Invoke(list, new object[] { parsed });
                }
                else
                    tgt.Field.SetValue(this, parsed);
            }

        }
        public void ParseAndFill(string filename)
        {
            var sections = GetContainerInfo(this.GetType());
            foreach (var section in ParseFile(filename))
            {
                var tgt = sections.FirstOrDefault((x) => x.Name.Equals(section.Name, StringComparison.InvariantCultureIgnoreCase));
                if (tgt == null)
                {
                    FLLog.Warning("Ini", "Unknown section " + section.Name + FormatLine(section.File, section.Line));
                    continue;
                }
                var parsed = GetFromSection(section, tgt.Type);
                if (tgt.Add != null)
                {
                    var list = tgt.Field.GetValue(this);
                    tgt.Add.Invoke(list, new object[] { parsed });
                }
                else
                    tgt.Field.SetValue(this, parsed);
            }

        }
    }
}