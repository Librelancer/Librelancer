// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Ini
{
    //Class for constructing ini through Reflection
    public abstract partial class IniFile
    {
        private static uint Hash(string s)
        {
            uint num = 0x811c9dc5;
            for (int i = 0; i < s.Length; i++)
            {
                var c = (int) s[i];
                if ((c >= 65 && c <= 90))
                    c ^= (1 << 5);
                num = ((uint)c ^ num) * 0x1000193;
            }
            return num;
        }

        private class ReflectionInfo
        {
            public Type Type;
            public bool IsChildSection;
            public List<ReflectionField> Fields = new List<ReflectionField>();
            public uint[] FieldHashes;
            public List<ReflectionMethod> Methods = new List<ReflectionMethod>();
            public uint[] MethodHashes;
            public List<ReflectionSection> ChildSections = new List<ReflectionSection>();
            public ulong RequiredFields = 0;
        }

        private class ReflectionMethod
        {
            public EntryHandlerAttribute Attr;
            public MethodInfo Method;
        }

        private class ReflectionSection
        {
            public string Name;
            public string[] Delimiters;
            public FieldInfo Field;
            public bool IsList;
            public ReflectionInfo Type;
            public bool AttachToParent;
            public bool IsInheritSection;
        }

        private class ContainerClass
        {
            public ReflectionSection[] Sections;
            public uint[] IgnoreHashes;
            public uint[] SectionHashes;

            public ReflectionSection GetSection(string s)
            {
                var hash = Hash(s);
                for (int i = 0; i < SectionHashes.Length; i++)
                {
                    if (SectionHashes[i] == hash && Sections[i].Name.Equals(s, StringComparison.OrdinalIgnoreCase))
                        return Sections[i];
                }
                return null;
            }
        }

        private class ReflectionField
        {
            public EntryAttribute Attr;
            public FieldInfo Field;
            public Type NullableType;
        }

        private static Dictionary<Type, ContainerClass> containerclasses = new Dictionary<Type, ContainerClass>();
        private static Dictionary<Type, ReflectionInfo> sectionclasses = new Dictionary<Type, ReflectionInfo>();

        private static string FormatLine(string file, int line)
        {
            if (line >= 0)
                return $" at {file}, line {line}";
            else
                return $" in {file} (line not available)";
        }

        private static string FormatLine(string file, int line, string section)
        {
            if (line >= 0)
                return $" at section {section}: {file}, line {line}";
            else
                return $" in section {section}: {file} (line not available)";
        }

        private const BindingFlags F_CLASSMEMBERS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static object _sLock = new object();
        private static object _cLock = new object();

        private static ReflectionInfo GetSectionInfo(Type t)
        {
            lock (_sLock)
            {
                ReflectionInfo info;
                if (sectionclasses.TryGetValue(t, out info)) return info;
                info = new ReflectionInfo() { Type = t };
                foreach (var field in t.GetFields(F_CLASSMEMBERS))
                {
                    var attrs = field.GetCustomAttributes<EntryAttribute>();
                    foreach (var a in attrs) {
                        info.Fields.Add(new ReflectionField()
                        {
                            Attr = a, Field = field, NullableType =
                                Nullable.GetUnderlyingType(field.FieldType)
                        });
                        if (a.Required) info.RequiredFields |= (1ul << (info.Fields.Count - 1));
                    }
                    foreach (var attr in field.GetCustomAttributes<SectionAttribute>())
                    {
                        if(!attr.Child) continue;
                        var fieldType = field.FieldType;
                        //Handle lists
                        if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            var s = new ReflectionSection() {Name = attr.Name};
                            s.Field = field;
                            s.IsList = true;
                            info.ChildSections.Add(s);
                        }
                        else
                        {
                            throw new Exception("Child sections can only be lists");
                        }
                    }
                }
                foreach (var method in t.GetMethods(F_CLASSMEMBERS))
                {
                    var attrs = method.GetCustomAttributes<EntryHandlerAttribute>();
                    foreach (var a in attrs) {
                        info.Methods.Add(new ReflectionMethod()
                        {
                            Attr = a, Method = method
                        });
                    }
                }
                //This should never be tripped
                if ((info.Fields.Count + info.Methods.Count) > 64) throw new Exception("Too many fields!! Edit bitmask code & raise limit");
                info.FieldHashes = new uint[info.Fields.Count];
                for (int i = 0; i < info.Fields.Count; i++) {
                    info.FieldHashes[i] = Hash(info.Fields[i].Attr.Name);
                }
                info.MethodHashes = new uint[info.Methods.Count];
                for (int i = 0; i < info.Methods.Count; i++) {
                    info.MethodHashes[i] = Hash(info.Methods[i].Attr.Name);
                }
                sectionclasses.Add(t, info);
                return info;
            }
        }

        private static ContainerClass GetContainerInfo(Type t)
        {
            lock (_cLock)
            {
                ContainerClass cinfo;
                if (containerclasses.TryGetValue(t, out cinfo)) return cinfo;
                cinfo = new ContainerClass();
                var sections = new List<ReflectionSection>();
                cinfo.IgnoreHashes =
                    t.GetCustomAttributes<IgnoreSectionAttribute>().Select(x => Hash(x.Name)).ToArray();
                foreach (var field in t.GetFields(F_CLASSMEMBERS))
                {
                    foreach (var attr in field.GetCustomAttributes<SectionAttribute>())
                    {
                        var s = new ReflectionSection() {Name = attr.Name};
                        s.AttachToParent = attr.Child;
                        var fieldType = field.FieldType;
                        //Handle lists
                        if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            fieldType = fieldType.GetGenericArguments()[0]; // use this...
                            s.IsList = true;
                        }
                        if (attr.Type != null)
                        {
                            s.Type = GetSectionInfo(attr.Type);
                            s.IsInheritSection = attr.Type.GetCustomAttributes<InheritSectionAttribute>(true).Any();
                        }
                        else
                        {
                            if (fieldType.IsAbstract) {
                                throw new Exception(t.Name + " section " + attr.Name + " inits abstract class " + fieldType.Name);
                            }
                            s.Type = GetSectionInfo(fieldType);
                            s.IsInheritSection = fieldType.GetCustomAttributes<InheritSectionAttribute>(true).Any();
                        }
                        s.Field = field;
                        s.Delimiters = attr.Delimiters;
                        sections.Add(s);
                    }
                }
                foreach (var attr in t.GetCustomAttributes<SelfSectionAttribute>())
                {
                    var s = new ReflectionSection() {Name = attr.Name};
                    s.Type = GetSectionInfo(t);
                    sections.Add(s);
                }

                cinfo.Sections = sections.ToArray();
                cinfo.SectionHashes = new uint[cinfo.Sections.Length];
                for (int i = 0; i < cinfo.SectionHashes.Length; i++) {
                    cinfo.SectionHashes[i] = Hash(cinfo.Sections[i].Name);
                }
                return cinfo;
            }
        }

        protected static T FromSection<T>(Section s)
        {
            return (T)GetFromSection(s, GetSectionInfo(typeof(T)));
        }

        protected void SelfFromSection(Section s)
        {
            GetFromSection(s, GetSectionInfo(GetType()), this);
        }

        protected virtual void OnSelfFilled(string datapath, FileSystem vfs) {}

        private static bool ComponentCheck(int c, Section s, Entry e, int min = -1)
        {
            if (min == -1) min = c;
            if (e.Count > c)
                FLLog.Warning("Ini", "Too many components for " + e.Name + FormatLine(s.File, e.Line, s.Name));
            if (e.Count >= min)
                return true;
            FLLog.Error("Ini", "Not enough components for " + e.Name + FormatLine(s.File, e.Line, s.Name));
            return false;
        }
        private static object GetFromSection(Section s, ReflectionInfo type, object obj = null, string datapath = null, FileSystem vfs = null, bool checkRequired = true)
        {
            if(obj == null) obj = Activator.CreateInstance(type.Type);
            ulong bitmask = 0;
            ulong requiredBits = type.RequiredFields;
            foreach (var e in s)
            {
                //Find entry
                var eHash = Hash(e.Name);
                int idx = -1;
                for (int i = 0; i < type.FieldHashes.Length; i++)
                {
                    if (eHash == type.FieldHashes[i] &&
                        type.Fields[i].Attr.Name.Equals(e.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        idx = i;
                        break;
                    }
                }
                // Use method
                for (int i = 0; i < type.MethodHashes.Length; i++)
                {
                    if (eHash == type.MethodHashes[i] &&
                        type.Methods[i].Attr.Name.Equals(e.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        var j = i + type.FieldHashes.Length;
                        var method = type.Methods[i];
                        if (!method.Attr.Multiline)
                        {
                            if ((bitmask & (1ul << j)) != 0)
                            {
                                IniWarning.DuplicateEntry(e, s);
                            }
                            bitmask |= 1ul << j;
                        }
                        if (ComponentCheck(int.MaxValue, s, e, method.Attr.MinComponents))
                            method.Method.Invoke(obj, new[] {e});
                        idx = -2;
                        break;
                    }
                }
                if (idx == -2) continue;
                //Special Handling: Use sparingly
                if (idx == -1)
                {
                    if(obj is not IEntryHandler eh || !eh.HandleEntry(e))
                        IniWarning.UnknownEntry(e,s);
                    continue;
                }

                var field = type.Fields[idx];
                //Warning for duplicates
                if (!field.Attr.Multiline)
                {
                    if ((bitmask & (1ul << idx)) != 0)
                    {
                        IniWarning.DuplicateEntry(e, s);
                    }
                    bitmask |= 1ul << idx;
                }
                requiredBits &= ~(1ul << idx);
                //Handle by type
                var ftype = field.Field.FieldType;
                Type nType;
                if (field.NullableType != null) {
                    ftype = field.NullableType;
                }
                //Fill
                if (ftype == typeof(string))
                {
                    if (field.Attr.Presence && e.Count < 1) { /* Allow for blank strings (hack)*/}
                    else if (ComponentCheck(1, s, e)) field.Field.SetValue(obj, e[0].ToString());
                }
                else if (ftype == typeof(HashValue))
                {
                    if (ComponentCheck(1, s, e))
                    {
                        field.Field.SetValue(obj, new HashValue(e[0]));
                    }
                }
                else if (ftype == typeof(float))
                {
                    if (ComponentCheck(1, s, e)) field.Field.SetValue(obj, e[0].ToSingle());
                }
                else if (ftype == typeof(int))
                {
                    if (ComponentCheck(1, s, e)) field.Field.SetValue(obj, e[0].ToInt32());
                }
                else if (ftype == typeof(ValueRange<int>))
                {
                    if (ComponentCheck(2, s, e)) field.Field.SetValue(obj, new ValueRange<int>(e[0].ToInt32(), e[1].ToInt32()));
                }
                else if (ftype == typeof(ValueRange<float>))
                {
                    if (ComponentCheck(2, s, e)) field.Field.SetValue(obj, new ValueRange<float>(e[0].ToSingle(), e[1].ToSingle()));

                }
                else if (ftype == typeof(long))
                {
                    if (ComponentCheck(1, s, e)) field.Field.SetValue(obj, e[0].ToInt64());
                }
                else if (ftype == typeof(bool))
                {
                    if (field.Attr.Presence) field.Field.SetValue(obj, true);
                    else if (ComponentCheck(1, s, e)) field.Field.SetValue(obj, e[0].ToBoolean());
                }
                else if (ftype == typeof(Vector3))
                {
                    if(e.Count == 1 && e[0].ToSingle() == 0)
                        field.Field.SetValue(obj, Vector3.Zero);
                    else if (field.Attr.Mode == Vec3Mode.None) {
                        if (ComponentCheck(3, s, e)) field.Field.SetValue(obj, new Vector3(e[0].ToSingle(), e[1].ToSingle(), e[2].ToSingle()));
                    } else if (ComponentCheck(3, s, e, 1))
                    {
                        if (field.Attr.Mode == Vec3Mode.Size)
                        {
                            if(e.Count == 1)
                                field.Field.SetValue(obj, new Vector3(e[0].ToSingle()));
                            else if(e.Count == 2)
                                field.Field.SetValue(obj, new Vector3(e[0].ToSingle(), e[1].ToSingle(), 0));
                            else
                                field.Field.SetValue(obj, new Vector3(e[0].ToSingle(), e[1].ToSingle(), e[2].ToSingle()));
                        }
                        else
                        {
                            //optional components
                            var v3 = Vector3.Zero;
                            v3.X = e[0].ToSingle();
                            if (e.Count > 1) v3.Y = e[1].ToSingle();
                            if (e.Count > 2) v3.Z = e[2].ToSingle();
                            field.Field.SetValue(obj, v3);
                        }
                    }
                }
                else if (ftype == typeof(List<Vector3>))
                {
                    bitmask &= ~(1ul << idx); //Avoid duplicate warnings
                    if (ComponentCheck(3, s, e))
                    {
                        var v = (List<Vector3>)field.Field.GetValue(obj);
                        v.Add(new Vector3(e[0].ToSingle(), e[1].ToSingle(), e[2].ToSingle()));
                    }
                }
                else if(ftype == typeof(Quaternion))
                {
                    if (ComponentCheck(4, s, e)) field.Field.SetValue(obj, new Quaternion(e[1].ToSingle(), e[2].ToSingle(), e[3].ToSingle(), e[0].ToSingle()));
                }
                else if(ftype == typeof(Vector4))
                {
                    if (ComponentCheck(4, s, e)) field.Field.SetValue(obj, new Vector4(e[0].ToSingle(), e[1].ToSingle(), e[2].ToSingle(), e[3].ToSingle()));
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
                    if (ComponentCheck(3, s, e))
                    {
                        if (field.Attr.FloatColor)
                        {
                            field.Field.SetValue(obj, new Color3f(e[0].ToSingle(), e[1].ToSingle(), e[2].ToSingle()));
                        }
                        else
                        {
                            field.Field.SetValue(obj, new Color3f(e[0].ToSingle() / 255f, e[1].ToSingle() / 255f, e[2].ToSingle() / 255f));
                        }
                    }
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
                else if (ftype == typeof(List<int>))
                {
                    bitmask &= ~(1ul << idx); //Avoid duplicate warnings
                    if (ComponentCheck(1, s, e))
                    {
                        var v = (List<int>)field.Field.GetValue(obj);
                        if (v == null) {
                            v = new List<int>();
                            field.Field.SetValue(obj, v);
                        }
                        v.Add(e[0].ToInt32());
                    }
                }
                else if (ftype == typeof(float[]))
                {
                    if(ComponentCheck(int.MaxValue,s,e,1)) {
                        var floats = new float[e.Count];
                        for (int i = 0; i < e.Count; i++) floats[i] = e[i].ToSingle();
                        field.Field.SetValue(obj, floats);
                    }
                }
                else if (ftype == typeof(int[]))
                {
                    if(ComponentCheck(int.MaxValue,s,e,1)) {
                        var ints = new int[e.Count];
                        for (int i = 0; i < e.Count; i++) ints[i] = e[i].ToInt32();
                        field.Field.SetValue(obj, ints);
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
                else if (ftype == typeof(List<string[]>))
                {
                    if (ComponentCheck(int.MaxValue, s, e, 1)) {
                        var strings = new string[e.Count];
                        for (int i = 0; i < e.Count; i++) strings[i] = e[i].ToString();
                        var lst = (List<string[]>)field.Field.GetValue(obj);
                        lst.Add(strings);
                    }
                }
                else if (ftype == typeof(Guid))
                {
                    if (ComponentCheck(1, s, e, 1))
                    {
                        if (Guid.TryParse(e[0].ToString(), out var g))
                            field.Field.SetValue(obj, g);
                        else
                            FLLog.Warning("Ini", "Unable to parse GUID" + FormatLine(s.File, e.Line, s.Name));
                    }
                }
                else if (ftype.IsEnum)
                {
                    if (ComponentCheck(1, s, e))
                    {
                        //TryParse requires generics, wrap in exception handler
                        try
                        {
                            field.Field.SetValue(obj, Enum.Parse(ftype, e[0].ToString(), true));
                        }
                        catch (Exception)
                        {
                            FLLog.Error("Ini", "Invalid value for enum " + e[0].ToString() + FormatLine(s.File, e.Line, s.Name));
                        }
                    }
                }
            }
            if(requiredBits != 0 && checkRequired)
            {
                //These sections crash the game if they don't have required fields
                //So don't let them be added to lists
                for(int i = 0; i < 64; i++) {
                    if ((requiredBits & (1ul << i)) != 0)
                    {
                        FLLog.Error("Ini", string.Format("Missing required field {0}{1}", type.Fields[i].Attr.Name, FormatLine(s.File, s.Line, s.Name)));
                    }
                }
                return null;
            }
            else
            {
                if (datapath != null && obj is IniFile ini)
                    ini.OnSelfFilled(datapath, vfs);
                return obj;
            }
        }

        protected void ParseAndFill(string filename, Stream stream, bool preparse = true)
        {
            var sections = GetContainerInfo(GetType());
            var deferred = new Dictionary<ReflectionSection, List<DeferredSection>>();
            DeferredSection lastDeferred = null;
            foreach (var section in ParseFile(filename, stream, preparse))
            {
                lastDeferred = ProcessSection(section, sections, lastDeferred, deferred);
            }
            foreach(var kv in deferred)
                ProcessDeferred(kv.Key, kv.Value);
        }

        protected void ParseAndFill(string filename, string datapath, FileSystem vfs)
        {
            var sections = GetContainerInfo(GetType());
            var deferred = new Dictionary<ReflectionSection, List<DeferredSection>>();
            DeferredSection lastDeferred = null;
            foreach (var section in ParseFile(filename, vfs))
            {
                lastDeferred = ProcessSection(section, sections, lastDeferred, deferred, datapath, vfs);
            }
            foreach(var kv in deferred)
                ProcessDeferred(kv.Key, kv.Value, datapath, vfs);
        }

        protected void ParseAndFill(IEnumerable<string> filenames, FileSystem vfs)
        {
            var sections = GetContainerInfo(GetType());
            var deferred = new Dictionary<ReflectionSection, List<DeferredSection>>();
            foreach (var f in filenames)
            {
                DeferredSection lastDeferred = null;
                foreach (var section in ParseFile(f, vfs))
                {
                    lastDeferred = ProcessSection(section, sections, lastDeferred, deferred);
                }
            }
            foreach(var kv in deferred)
                ProcessDeferred(kv.Key, kv.Value);
        }

        protected void ParseAndFill(string filename, FileSystem vfs)
        {
            var sections = GetContainerInfo(GetType());
            var deferred = new Dictionary<ReflectionSection, List<DeferredSection>>();
            DeferredSection lastDeferred = null;
            foreach (var section in ParseFile(filename, vfs))
            {
                lastDeferred = ProcessSection(section, sections, lastDeferred, deferred);
            }
            foreach(var kv in deferred)
                ProcessDeferred(kv.Key, kv.Value);
        }

        private static readonly uint nicknameHash = Hash("nickname");
        private static readonly uint inheritHash = Hash("inherit");

        private string GetProperty(Section section, uint hash)
        {
            var n = section.LastOrDefault(x => Hash(x.Name) == hash);
            if (n == null || n.Count < 1) return null;
            return n[0].ToString();
        }

        private void ProcessDeferred(ReflectionSection tgt, List<DeferredSection> data, string datapath = null, FileSystem vfs = null)
        {
            //Collect names
            Dictionary<string, Section> byNickname = new Dictionary<string, Section>();
            foreach (var s in data)
            {
                string nick;
                if ((nick = GetProperty(s.Section, nicknameHash)) == null) {
                    FLLog.Warning("Ini", $"Section {s.Section.Name} has no nickname {FormatLine(s.Section.File, s.Section.Line)})");
                }
                else {
                    byNickname[nick] = s.Section;
                }
            }
            //Walk hierarchy and fill objects
            foreach (var s in data)
            {
                List<Section> toApply = new List<Section>();
                Section p = s.Section;
                while (true) {
                    toApply.Add(p);
                    string inherit;
                    if ((inherit = GetProperty(p, inheritHash)) == null)
                        break;
                    if (!byNickname.TryGetValue(inherit, out p))
                        break;
                }
                var list = (IList)tgt.Field.GetValue(this);
                var parsed = GetFromSection(toApply[toApply.Count - 1], tgt.Type, null, datapath, vfs, toApply.Count - 1 == 0);
                for (int i = toApply.Count - 2; i >= 0; i--) {
                    parsed = GetFromSection(toApply[i], tgt.Type, parsed, datapath, vfs, i == 0);
                }
                if (parsed != null)
                    list.Add(parsed);
                foreach (var (childSection, chtgt) in s.Children)
                {
                    var parsedChild = GetFromSection(childSection, chtgt.Type, null, datapath, vfs);
                    if (parsedChild != null) {
                        AddSectionToParent(parsedChild, parsed, childSection);
                    }
                }
            }

        }

        private record DeferredSection(Section Section, List<(Section, ReflectionSection)> Children);

        private void AddSectionToParent(object parsed, object parent, Section section)
        {
            bool success = false;
            var parentInfo = GetSectionInfo(parent.GetType());
            foreach (var cs in parentInfo.ChildSections) {
                if (cs.Name.Equals(section.Name, StringComparison.OrdinalIgnoreCase))
                {
                    var ls2 = (IList)cs.Field.GetValue(parent);
                    ls2.Add(parsed);
                    success = true;
                    break;
                }
            }
            if (!success)
            {
                FLLog.Warning("Ini",
                    $"Type {parentInfo.GetType().Name} does not accept child section {section.Name} {FormatLine(section.File, section.Line)}");
            }
        }


        private DeferredSection ProcessSection(Section section,
            ContainerClass sections,
            DeferredSection lastDeferred,
            Dictionary<ReflectionSection,
            List<DeferredSection>> deferredSections,
            string datapath = null,
            FileSystem vfs = null)
        {
            if (sections.IgnoreHashes.Contains(Hash(section.Name))) return null;
            var tgt = sections.GetSection(section.Name);
            if (tgt == null)
            {
                IniWarning.UnknownSection(section);
                return null;
            }
            if (tgt.Field == null)
            {
                GetFromSection(section, tgt.Type, this);
            }
            else
            {
                if (tgt.Delimiters != null)
                {
                    foreach (var ch in Chunk(tgt.Delimiters, section))
                    {
                        var childObject = GetFromSection(ch, tgt.Type, null, datapath, vfs);
                        if (childObject != null)
                        {
                            var list = (IList)tgt.Field.GetValue(this);
                            list.Add(childObject);
                        }
                    }
                }
                else if (tgt.IsInheritSection && tgt.IsList && !tgt.AttachToParent)
                {
                    if (!deferredSections.TryGetValue(tgt, out var dlist))
                    {
                        dlist = new List<DeferredSection>();
                        deferredSections[tgt] = dlist;
                    }
                    var def = new DeferredSection(section, new List<(Section, ReflectionSection)>());
                    dlist.Add(def);
                    return def;
                }
                else if (tgt.AttachToParent && lastDeferred != null)
                {
                    lastDeferred.Children.Add((section, tgt));
                    return lastDeferred;
                }
                else
                {
                    var parsed = GetFromSection(section, tgt.Type, null, datapath, vfs);
                    if (parsed != null)
                    {
                        if (tgt.IsList)
                        {
                            var list = (IList)tgt.Field.GetValue(this);
                            if (tgt.AttachToParent)
                            {
                                var count = list.Count;
                                if (count <= 0) {
                                    FLLog.Warning("Ini", $"Section {section.Name} has no parent {FormatLine(section.File, section.Line)}");
                                    return null;
                                }
                                var parent = list[count - 1];
                                AddSectionToParent(parsed, parent, section);
                            }
                            else
                            {
                                list.Add(parsed);
                            }
                        }
                        else
                            tgt.Field.SetValue(this, parsed);
                    }
                }
            }
            return null;
        }

        private static bool HasIgnoreCase(string[] array, string value)
        {
            for(int i = 0; i < array.Length; i++)
                if (array[i].Equals(value, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
        private static IEnumerable<Section> Chunk(string[] delimiters, Section parent)
        {
            Section currentSection = null;
            foreach (var e in parent) {
                if (HasIgnoreCase(delimiters, e.Name))
                {
                    if (currentSection != null)
                        yield return currentSection;
                    currentSection = new Section(parent.Name)
                    {
                        File = parent.File,
                        Line = parent.Line
                    };
                }
                if (currentSection != null)
                    currentSection.Add(e);
                else
                    FLLog.Warning("Ini", $"Entry without object '{e.Name}' {FormatLine(parent.File, e.Line, parent.Name)}");
            }
            if (currentSection != null) yield return currentSection;
        }
    }
}
