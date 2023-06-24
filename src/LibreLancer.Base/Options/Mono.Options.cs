//
// Options.cs
//
// Authors:
//  Jonathan Pryor <jpryor@novell.com>, <Jonathan.Pryor@microsoft.com>
//  Federico Di Gregorio <fog@initd.org>
//  Rolf Bjarne Kvinge <rolf@xamarin.com>
//
// Copyright (C) 2008 Novell (http://www.novell.com)
// Copyright (C) 2009 Federico Di Gregorio.
// Copyright (C) 2012 Xamarin Inc (http://www.xamarin.com)
// Copyright (C) 2017 Microsoft Corporation (http://www.microsoft.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// this is a shortened version of the original file - https://github.com/mono/mono/blob/main/mcs/class/Mono.Options/Mono.Options/Options.cs

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using MessageLocalizerConverter = System.Converter<string, string>;

namespace LibreLancer.Options;

internal static class StringCoda
{
    public static IEnumerable<string> WrappedLines(string self, params int[] widths)
    {
        IEnumerable<int> w = widths;
        return WrappedLines(self, w);
    }

    public static IEnumerable<string> WrappedLines(string self, IEnumerable<int> widths)
    {
        if (widths == null)
            throw new ArgumentNullException(nameof(widths));
        return CreateWrappedLinesIterator(self, widths);
    }

    private static IEnumerable<string> CreateWrappedLinesIterator(string self, IEnumerable<int> widths)
    {
        if (string.IsNullOrEmpty(self))
        {
            yield return string.Empty;
            yield break;
        }

        using IEnumerator<int> ewidths = widths.GetEnumerator();
        bool? hw = null;
        int width = GetNextWidth(ewidths, int.MaxValue, ref hw);
        int start = 0;
        do
        {
            int end = GetLineEnd(start, width, self);
            // endCorrection is 1 if the line end is '\n', and might be 2 if the line end is '\r\n'.
            int endCorrection = 1;
            if (end >= 2 && self.Substring(end - 2, 2).Equals("\r\n"))
                endCorrection = 2;
            char c = self[end - endCorrection];
            if (char.IsWhiteSpace(c))
                end -= endCorrection;
            bool needContinuation = end != self.Length && !IsEolChar(c);
            string continuation = "";
            if (needContinuation)
            {
                --end;
                continuation = "-";
            }

            string line = string.Concat(self.AsSpan(start, end - start), continuation);
            yield return line;
            start = end;
            if (char.IsWhiteSpace(c))
                start += endCorrection;
            width = GetNextWidth(ewidths, width, ref hw);
        } while (start < self.Length);
    }

    private static int GetNextWidth(IEnumerator<int> ewidths, int curWidth, ref bool? eValid)
    {
        if (!eValid.HasValue || eValid.Value)
        {
            curWidth = (eValid = ewidths.MoveNext()).Value ? ewidths.Current : curWidth;
            // '.' is any character, - is for a continuation
            const string minWidth = ".-";
            if (curWidth < minWidth.Length)
                throw new ArgumentOutOfRangeException("widths",
                    $"Element must be >= {minWidth.Length}, was {curWidth}.");
            return curWidth;
        }

        // no more elements, use the last element.
        return curWidth;
    }

    private static bool IsEolChar(char c)
    {
        return !char.IsLetterOrDigit(c);
    }

    private static int GetLineEnd(int start, int length, string description)
    {
        int end = Math.Min(start + length, description.Length);
        int sep = -1;
        for (int i = start; i < end; ++i)
        {
            if (i + 2 <= description.Length && description.Substring(i, 2).Equals("\r\n"))
                return i + 2;
            if (description[i] == '\n')
                return i + 1;
            if (IsEolChar(description[i]))
                sep = i + 1;
        }

        if (sep == -1 || end == description.Length)
            return end;
        return sep;
    }
}

public class OptionValueCollection : IList, IList<string>
{
    private readonly List<string> values = new();
    private readonly OptionContext c;

    internal OptionValueCollection(OptionContext c)
    {
        this.c = c;
    }
    
    void ICollection.CopyTo(Array array, int index)
    {
        (values as ICollection).CopyTo(array, index);
    }

    bool ICollection.IsSynchronized => (values as ICollection).IsSynchronized;
    object ICollection.SyncRoot => (values as ICollection).SyncRoot;

    public void Add(string item)
    {
        values.Add(item);
    }

    public void Clear()
    {
        values.Clear();
    }

    public bool Contains(string item)
    {
        return values.Contains(item);
    }

    public void CopyTo(string[] array, int arrayIndex)
    {
        values.CopyTo(array, arrayIndex);
    }

    public bool Remove(string item)
    {
        return values.Remove(item);
    }

    public int Count => values.Count;
    public bool IsReadOnly => false;

    IEnumerator IEnumerable.GetEnumerator()
    {
        return values.GetEnumerator();
    }
    
    public IEnumerator<string> GetEnumerator()
    {
        return values.GetEnumerator();
    }

    int IList.Add(object value)
    {
        return (values as IList).Add(value);
    }

    bool IList.Contains(object value)
    {
        return (values as IList).Contains(value);
    }

    int IList.IndexOf(object value)
    {
        return (values as IList).IndexOf(value);
    }

    void IList.Insert(int index, object value)
    {
        (values as IList).Insert(index, value);
    }

    void IList.Remove(object value)
    {
        (values as IList).Remove(value);
    }

    void IList.RemoveAt(int index)
    {
        (values as IList).RemoveAt(index);
    }

    bool IList.IsFixedSize => false;

    object IList.this[int index]
    {
        get => this[index];
        set => (values as IList)[index] = value;
    }

    public int IndexOf(string item)
    {
        return values.IndexOf(item);
    }

    public void Insert(int index, string item)
    {
        values.Insert(index, item);
    }

    public void RemoveAt(int index)
    {
        values.RemoveAt(index);
    }

    private void AssertValid(int index)
    {
        if (c.Option == null)
            throw new InvalidOperationException("OptionContext.Option is null.");
        if (index >= c.Option.MaxValueCount)
            throw new ArgumentOutOfRangeException(nameof(index));
        if (c.Option.OptionValueType == OptionValueType.Required &&
            index >= values.Count)
            throw new OptionException(string.Format(
                    c.OptionSet.MessageLocalizer("Missing required value for option '{0}'."), c.OptionName),
                c.OptionName);
    }

    public string this[int index]
    {
        get
        {
            AssertValid(index);
            return index >= values.Count ? null : values[index];
        }
        set => values[index] = value;
    }

    public override string ToString()
    {
        return string.Join(", ", values.ToArray());
    }
}

public class OptionContext
{
    public OptionContext(OptionSet set)
    {
        OptionSet = set;
        OptionValues = new OptionValueCollection(this);
    }

    public Option Option { get; set; }

    public string OptionName { get; set; }

    public int OptionIndex { get; set; }

    public OptionSet OptionSet { get; }

    public OptionValueCollection OptionValues { get; }
}

public enum OptionValueType
{
    None,
    Optional,
    Required
}

public abstract class Option
{
    protected Option(string prototype, string description, int maxValueCount = 1, bool hidden = false)
    {
        if (prototype == null)
            throw new ArgumentNullException(nameof(prototype));
        if (prototype.Length == 0)
            throw new ArgumentException("Cannot be the empty string.", nameof(prototype));
        if (maxValueCount < 0)
            throw new ArgumentOutOfRangeException(nameof(maxValueCount));

        Prototype = prototype;
        Description = description;
        MaxValueCount = maxValueCount;
        Names = this is OptionSet.Category
            // append GetHashCode() so that "duplicate" categories have distinct
            // names, e.g. adding multiple "" categories should be valid.
            ? new[] {prototype + GetHashCode()}
            : prototype.Split('|');

        if (this is OptionSet.Category)
            return;

        OptionValueType = ParsePrototype();
        Hidden = hidden;

        if (MaxValueCount == 0 && OptionValueType != OptionValueType.None)
            throw new ArgumentException(
                "Cannot provide maxValueCount of 0 for OptionValueType.Required or " +
                "OptionValueType.Optional.",
                nameof(maxValueCount));
        if (OptionValueType == OptionValueType.None && maxValueCount > 1)
            throw new ArgumentException(
                $"Cannot provide maxValueCount of {maxValueCount} for OptionValueType.None.",
                nameof(maxValueCount));
        if (Array.IndexOf(Names, "<>") >= 0 &&
            ((Names.Length == 1 && OptionValueType != OptionValueType.None) ||
             (Names.Length > 1 && MaxValueCount > 1)))
            throw new ArgumentException(
                "The default option handler '<>' cannot require values.",
                nameof(prototype));
    }

    public string Prototype { get; }

    public string Description { get; }

    public OptionValueType OptionValueType { get; }

    public int MaxValueCount { get; }

    public bool Hidden { get; }

    protected static T Parse<T>(string value, OptionContext c)
    {
        Type tt = typeof(T);
        bool nullable =
            tt.IsValueType &&
            tt.IsGenericType &&
            !tt.IsGenericTypeDefinition &&
            tt.GetGenericTypeDefinition() == typeof(Nullable<>);
            Type targetType = nullable ? tt.GetGenericArguments()[0] : tt;
        T t = default;
        try
        {
            if (value != null)
            {
                TypeConverter conv = TypeDescriptor.GetConverter(targetType);
                t = (T) conv.ConvertFromString(value);
            }
        }
        catch (Exception e)
        {
            throw new OptionException(
                string.Format(
                    c.OptionSet.MessageLocalizer("Could not convert string `{0}' to type {1} for option `{2}'."),
                    value, targetType.Name, c.OptionName),
                c.OptionName, e);
        }

        return t;
    }

    internal string[] Names { get; }
    internal string[] ValueSeparators { get; private set; }

    private static readonly char[] NameTerminator = {'=', ':'};

    private OptionValueType ParsePrototype()
    {
        char type = '\0';
        List<string> seps = new();
        for (int i = 0; i < Names.Length; ++i)
        {
            string name = Names[i];
            if (name.Length == 0)
                throw new ArgumentException("Empty option names are not supported.", "prototype");

            int end = name.IndexOfAny(NameTerminator);
            if (end == -1)
                continue;
            Names[i] = name[..end];
            if (type == '\0' || type == name[end])
                type = name[end];
            else
                throw new ArgumentException(
                    $"Conflicting option types: '{type}' vs. '{name[end]}'.",
                    "prototype");
            AddSeparators(name, end, seps);
        }

        if (type == '\0')
            return OptionValueType.None;

        if (MaxValueCount <= 1 && seps.Count != 0)
            throw new ArgumentException(
                $"Cannot provide key/value separators for Options taking {MaxValueCount} value(s).",
                "prototype");
        if (MaxValueCount > 1)
        {
            if (seps.Count == 0)
                ValueSeparators = new[] {":", "="};
            else if (seps.Count == 1 && seps[0].Length == 0)
                ValueSeparators = null;
            else
                ValueSeparators = seps.ToArray();
        }

        return type == '=' ? OptionValueType.Required : OptionValueType.Optional;
    }

    private static void AddSeparators(string name, int end, ICollection<string> seps)
    {
        int start = -1;
        for (int i = end + 1; i < name.Length; ++i)
            switch (name[i])
            {
                case '{':
                    if (start != -1)
                        throw new ArgumentException(
                            $"Ill-formed name/value separator found in \"{name}\".",
                            "prototype");
                    start = i + 1;
                    break;
                case '}':
                    if (start == -1)
                        throw new ArgumentException(
                            $"Ill-formed name/value separator found in \"{name}\".",
                            "prototype");
                    seps.Add(name.Substring(start, i - start));
                    start = -1;
                    break;
                default:
                    if (start == -1)
                        seps.Add(name[i].ToString());
                    break;
            }

        if (start != -1)
            throw new ArgumentException(
                $"Ill-formed name/value separator found in \"{name}\".",
                "prototype");
    }

    public void Invoke(OptionContext c)
    {
        OnParseComplete(c);
        c.OptionName = null;
        c.Option = null;
        c.OptionValues.Clear();
    }

    protected abstract void OnParseComplete(OptionContext c);

    internal void InvokeOnParseComplete(OptionContext c)
    {
        OnParseComplete(c);
    }

    public override string ToString()
    {
        return Prototype;
    }
}

public abstract class ArgumentSource
{
    public abstract string[] GetNames();
    public abstract string Description { get; }
    public abstract bool GetArguments(string value, out IEnumerable<string> replacement);

    public static IEnumerable<string> GetArgumentsFromFile(string file)
    {
        return GetArguments(File.OpenText(file), true);
    }


    public static IEnumerable<string> GetArguments(TextReader reader)
    {
        return GetArguments(reader, false);
    }

    // Cribbed from mcs/driver.cs:LoadArgs(string)
    private static IEnumerable<string> GetArguments(TextReader reader, bool close)
    {
        try
        {
            StringBuilder arg = new();

            while (reader.ReadLine() is { } line)
            {
                int t = line.Length;

                for (int i = 0; i < t; i++)
                {
                    char c = line[i];

                    switch (c)
                    {
                        case '"':
                        case '\'':
                        {
                            char end = c;

                            for (i++; i < t; i++)
                            {
                                c = line[i];

                                if (c == end)
                                    break;
                                arg.Append(c);
                            }

                            break;
                        }
                        case ' ':
                        {
                            if (arg.Length > 0)
                            {
                                yield return arg.ToString();
                                arg.Length = 0;
                            }

                            break;
                        }
                        default:
                            arg.Append(c);
                            break;
                    }
                }

                if (arg.Length <= 0) continue;
                yield return arg.ToString();
                arg.Length = 0;
            }
        }
        finally
        {
            if (close)
                reader.Dispose();
        }
    }
}

[Serializable]
public class OptionException : Exception
{
    private string option;

    public OptionException()
    {
    }

    public OptionException(string message, string optionName)
        : base(message)
    {
        option = optionName;
    }

    public OptionException(string message, string optionName, Exception innerException)
        : base(message, innerException)
    {
        option = optionName;
    }

    protected OptionException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        option = info.GetString("OptionName");
    }
}

public delegate void OptionAction<in TKey, in TValue>(TKey key, TValue value);

public class OptionSet : KeyedCollection<string, Option>
{
    public OptionSet()
        : this(null, null)
    {
    }

    public OptionSet(StringComparer comparer)
        : this(null, comparer)
    {
    }

    public OptionSet(MessageLocalizerConverter localizer, StringComparer comparer = null)
        : base(comparer)
    {
        ArgumentSources = new ReadOnlyCollection<ArgumentSource>(sources);
        MessageLocalizer = localizer ?? (f => f);
    }

    public MessageLocalizerConverter MessageLocalizer { get; internal set; }

    private readonly List<ArgumentSource> sources = new();

    public ReadOnlyCollection<ArgumentSource> ArgumentSources { get; }


    protected override string GetKeyForItem(Option item)
    {
        if (item == null)
            throw new ArgumentNullException("option");
        if (item.Names != null && item.Names.Length > 0)
            return item.Names[0];
        // This should never happen, as it's invalid for Option to be
        // constructed w/o any names.
        throw new InvalidOperationException("Option has no names!");
    }

    [Obsolete("Use KeyedCollection.this[string]")]
    protected Option GetOptionForName(string option)
    {
        if (option == null)
            throw new ArgumentNullException(nameof(option));
        try
        {
            return base[option];
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
    }

    protected override void InsertItem(int index, Option item)
    {
        base.InsertItem(index, item);
        AddImpl(item);
    }

    protected override void RemoveItem(int index)
    {
        Option p = Items[index];
        base.RemoveItem(index);
        // KeyedCollection.RemoveItem() handles the 0th item
        for (int i = 1; i < p.Names.Length; ++i) Dictionary?.Remove(p.Names[i]);
    }

    protected override void SetItem(int index, Option item)
    {
        base.SetItem(index, item);
        AddImpl(item);
    }

    private void AddImpl(Option option)
    {
        if (option == null)
            throw new ArgumentNullException(nameof(option));
        List<string> added = new(option.Names.Length);
        try
        {
            // KeyedCollection.InsertItem/SetItem handle the 0th name.
            for (int i = 1; i < option.Names.Length; ++i)
            {
                Dictionary?.Add(option.Names[i], option);
                added.Add(option.Names[i]);
            }
        }
        catch (Exception)
        {
            foreach (string name in added)
                Dictionary?.Remove(name);
            throw;
        }
    }

    public OptionSet Add(string header)
    {
        if (header == null)
            throw new ArgumentNullException(nameof(header));
        Add(new Category(header));
        return this;
    }

    internal sealed class Category : Option
    {
        // Prototype starts with '=' because this is an invalid prototype
        // (see Option.ParsePrototype(), and thus it'll prevent Category
        // instances from being accidentally used as normal options.
        public Category(string description)
            : base("=:Category:= " + description, description)
        {
        }

        protected override void OnParseComplete(OptionContext c)
        {
            throw new NotSupportedException("Category.OnParseComplete should not be invoked.");
        }
    }


    public new OptionSet Add(Option option)
    {
        base.Add(option);
        return this;
    }

    private sealed class ActionOption : Option
    {
        private readonly Action<OptionValueCollection> action;

        public ActionOption(string prototype, string description, int count, Action<OptionValueCollection> action, bool hidden = false)
            : base(prototype, description, count, hidden)
        {
            this.action = action ?? throw new ArgumentNullException(nameof(action));
        }

        protected override void OnParseComplete(OptionContext c)
        {
            action(c.OptionValues);
        }
    }

    public OptionSet Add(string prototype, Action<string> action)
    {
        return Add(prototype, null, action);
    }

    public OptionSet Add(string prototype, string description, Action<string> action)
    {
        return Add(prototype, description, action, false);
    }

    public OptionSet Add(string prototype, string description, Action<string> action, bool hidden)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));
        Option p = new ActionOption(prototype, description, 1,
            delegate(OptionValueCollection v) { action(v[0]); }, hidden);
        base.Add(p);
        return this;
    }

    public OptionSet Add(string prototype, OptionAction<string, string> action)
    {
        return Add(prototype, null, action);
    }

    public OptionSet Add(string prototype, string description, OptionAction<string, string> action)
    {
        return Add(prototype, description, action, false);
    }

    public OptionSet Add(string prototype, string description, OptionAction<string, string> action, bool hidden)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));
        Option p = new ActionOption(prototype, description, 2,
            delegate(OptionValueCollection v) { action(v[0], v[1]); }, hidden);
        base.Add(p);
        return this;
    }

    private sealed class ActionOption<T> : Option
    {
        private readonly Action<T> action;

        public ActionOption(string prototype, string description, Action<T> action)
            : base(prototype, description)
        {
            this.action = action ?? throw new ArgumentNullException(nameof(action));
        }

        protected override void OnParseComplete(OptionContext c)
        {
            action(Parse<T>(c.OptionValues[0], c));
        }
    }

    private sealed class ActionOption<TKey, TValue> : Option
    {
        private readonly OptionAction<TKey, TValue> action;

        public ActionOption(string prototype, string description, OptionAction<TKey, TValue> action)
            : base(prototype, description, 2)
        {
            this.action = action ?? throw new ArgumentNullException(nameof(action));
        }

        protected override void OnParseComplete(OptionContext c)
        {
            action(
                Parse<TKey>(c.OptionValues[0], c),
                Parse<TValue>(c.OptionValues[1], c));
        }
    }

    public OptionSet Add<T>(string prototype, Action<T> action)
    {
        return Add(prototype, null, action);
    }

    public OptionSet Add<T>(string prototype, string description, Action<T> action)
    {
        return Add(new ActionOption<T>(prototype, description, action));
    }

    public OptionSet Add<TKey, TValue>(string prototype, OptionAction<TKey, TValue> action)
    {
        return Add(prototype, null, action);
    }

    public OptionSet Add<TKey, TValue>(string prototype, string description, OptionAction<TKey, TValue> action)
    {
        return Add(new ActionOption<TKey, TValue>(prototype, description, action));
    }

    public OptionSet Add(ArgumentSource source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        sources.Add(source);
        return this;
    }

    protected virtual OptionContext CreateOptionContext()
    {
        return new OptionContext(this);
    }

    public List<string> Parse(IEnumerable<string> arguments)
    {
        if (arguments == null)
            throw new ArgumentNullException(nameof(arguments));
        OptionContext c = CreateOptionContext();
        c.OptionIndex = -1;
        bool process = true;
        List<string> unprocessed = new();
        Option def = Contains("<>") ? this["<>"] : null;
        ArgumentEnumerator ae = new(arguments);
        foreach (string argument in ae)
        {
            ++c.OptionIndex;
            if (argument == "--")
            {
                process = false;
                continue;
            }

            if (!process)
            {
                Unprocessed(unprocessed, def, c, argument);
                continue;
            }

            if (AddSource(ae, argument))
                continue;
            if (!Parse(argument, c))
                Unprocessed(unprocessed, def, c, argument);
        }

        c.Option?.Invoke(c);
        return unprocessed;
    }

    private class ArgumentEnumerator : IEnumerable<string>
    {
        private readonly List<IEnumerator<string>> sources = new();

        public ArgumentEnumerator(IEnumerable<string> arguments)
        {
            sources.Add(arguments.GetEnumerator());
        }

        public void Add(IEnumerable<string> arguments)
        {
            sources.Add(arguments.GetEnumerator());
        }

        public IEnumerator<string> GetEnumerator()
        {
            do
            {
                IEnumerator<string> c = sources[^1];
                if (c.MoveNext())
                {
                    yield return c.Current;
                }
                else
                {
                    c.Dispose();
                    sources.RemoveAt(sources.Count - 1);
                }
            } while (sources.Count > 0);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private bool AddSource(ArgumentEnumerator ae, string argument)
    {
        foreach (ArgumentSource source in sources)
        {
            if (!source.GetArguments(argument, out IEnumerable<string> replacement))
                continue;
            ae.Add(replacement);
            return true;
        }

        return false;
    }

    private static bool Unprocessed(ICollection<string> extra, Option def, OptionContext c, string argument)
    {
        if (def == null)
        {
            extra.Add(argument);
            return false;
        }

        c.OptionValues.Add(argument);
        c.Option = def;
        c.Option.Invoke(c);
        return false;
    }

    private readonly Regex ValueOption = new(
        @"^(?<flag>--|-|/)(?<name>[^:=]+)((?<sep>[:=])(?<value>.*))?$");

    private bool GetOptionParts(string argument, out string flag, out string name, out string sep, out string value)
    {
        if (argument == null)
            throw new ArgumentNullException(nameof(argument));

        flag = name = sep = value = null;
        Match m = ValueOption.Match(argument);
        if (!m.Success) return false;
        flag = m.Groups["flag"].Value;
        name = m.Groups["name"].Value;
        if (m.Groups["sep"].Success && m.Groups["value"].Success)
        {
            sep = m.Groups["sep"].Value;
            value = m.Groups["value"].Value;
        }

        return true;
    }

    protected bool Parse(string argument, OptionContext c)
    {
        if (c.Option != null)
        {
            ParseValue(argument, c);
            return true;
        }

        string f, n, s, v;
        if (!GetOptionParts(argument, out f, out n, out s, out v))
            return false;

        Option p;
        if (Contains(n))
        {
            p = this[n];
            c.OptionName = f + n;
            c.Option = p;
            switch (p.OptionValueType)
            {
                case OptionValueType.None:
                    c.OptionValues.Add(n);
                    c.Option.Invoke(c);
                    break;
                case OptionValueType.Optional:
                case OptionValueType.Required:
                    ParseValue(v, c);
                    break;
            }

            return true;
        }

        // no match; is it a bool option?
        return ParseBool(argument, n, c) || ParseBundledValue(f, string.Concat(n + s + v), c);
    }

    private void ParseValue(string option, OptionContext c)
    {
        if (option != null)
            foreach (string o in c.Option.ValueSeparators != null
                         ? option.Split(c.Option.ValueSeparators, c.Option.MaxValueCount - c.OptionValues.Count, StringSplitOptions.None)
                         : new[] {option})
                c.OptionValues.Add(o);
        if (c.OptionValues.Count == c.Option.MaxValueCount ||
            c.Option.OptionValueType == OptionValueType.Optional)
            c.Option.Invoke(c);
        else if (c.OptionValues.Count > c.Option.MaxValueCount)
            throw new OptionException(MessageLocalizer(string.Format(
                    "Error: Found {0} option values when expecting {1}.",
                    c.OptionValues.Count, c.Option.MaxValueCount)),
                c.OptionName);
    }

    private bool ParseBool(string option, string n, OptionContext c)
    {
        string rn;
        if (n.Length >= 1 && (n[^1] == '+' || n[^1] == '-') &&
            Contains(rn = n[..^1]))
        {
            Option p = this[rn];
            string v = n[^1] == '+' ? option : null;
            c.OptionName = option;
            c.Option = p;
            c.OptionValues.Add(v);
            p.Invoke(c);
            return true;
        }

        return false;
    }

    private bool ParseBundledValue(string f, string n, OptionContext c)
    {
        if (f != "-")
            return false;
        for (int i = 0; i < n.Length; ++i)
        {
            string opt = f + n[i];
            string rn = n[i].ToString();
            if (!Contains(rn))
            {
                if (i == 0)
                    return false;
                throw new OptionException(string.Format(MessageLocalizer(
                    "Cannot use unregistered option '{0}' in bundle '{1}'."), rn, f + n), null);
            }

            Option p = this[rn];
            switch (p.OptionValueType)
            {
                case OptionValueType.None:
                    Invoke(c, opt, n, p);
                    break;
                case OptionValueType.Optional:
                case OptionValueType.Required:
                {
                    string v = n[(i + 1)..];
                    c.Option = p;
                    c.OptionName = opt;
                    ParseValue(v.Length != 0 ? v : null, c);
                    return true;
                }
                default:
                    throw new InvalidOperationException("Unknown OptionValueType: " + p.OptionValueType);
            }
        }

        return true;
    }

    private static void Invoke(OptionContext c, string name, string value, Option option)
    {
        c.OptionName = name;
        c.Option = option;
        c.OptionValues.Add(value);
        option.Invoke(c);
    }

    private const int OptionWidth = 29;
    private const int Description_FirstWidth = 80 - OptionWidth;
    private const int Description_RemWidth = 80 - OptionWidth - 2;
    
    public void WriteOptionDescriptions(TextWriter o)
    {
        foreach (Option p in this)
        {
            int written = 0;

            if (p.Hidden)
                continue;

            switch (p)
            {
                case Category:
                    WriteDescription(o, p.Description, "", 80, 80);
                    continue;
            }

            if (!WriteOptionPrototype(o, p, ref written))
                continue;

            if (written < OptionWidth)
            {
                o.Write(new string(' ', OptionWidth - written));
            }
            else
            {
                o.WriteLine();
                o.Write(new string(' ', OptionWidth));
            }

            WriteDescription(o, p.Description, new string(' ', OptionWidth + 2),
                Description_FirstWidth, Description_RemWidth);
        }

        foreach (ArgumentSource s in sources)
        {
            string[] names = s.GetNames();
            if (names == null || names.Length == 0)
                continue;

            int written = 0;

            Write(o, ref written, "  ");
            Write(o, ref written, names[0]);
            for (int i = 1; i < names.Length; ++i)
            {
                Write(o, ref written, ", ");
                Write(o, ref written, names[i]);
            }

            if (written < OptionWidth)
            {
                o.Write(new string(' ', OptionWidth - written));
            }
            else
            {
                o.WriteLine();
                o.Write(new string(' ', OptionWidth));
            }

            WriteDescription(o, s.Description, new string(' ', OptionWidth + 2),
                Description_FirstWidth, Description_RemWidth);
        }
    }
    

    private void WriteDescription(TextWriter o, string value, string prefix, int firstWidth, int remWidth)
    {
        bool indent = false;
        foreach (string line in GetLines(MessageLocalizer(GetDescription(value)), firstWidth, remWidth))
        {
            if (indent)
                o.Write(prefix);
            o.WriteLine(line);
            indent = true;
        }
    }

    private bool WriteOptionPrototype(TextWriter o, Option p, ref int written)
    {
        string[] names = p.Names;

        int i = GetNextOptionIndex(names, 0);
        if (i == names.Length)
            return false;

        if (names[i].Length == 1)
        {
            Write(o, ref written, "  -");
            Write(o, ref written, names[0]);
        }
        else
        {
            Write(o, ref written, "      --");
            Write(o, ref written, names[0]);
        }

        for (i = GetNextOptionIndex(names, i + 1);
             i < names.Length;
             i = GetNextOptionIndex(names, i + 1))
        {
            Write(o, ref written, ", ");
            Write(o, ref written, names[i].Length == 1 ? "-" : "--");
            Write(o, ref written, names[i]);
        }

        if (p.OptionValueType is OptionValueType.Optional or OptionValueType.Required)
        {
            if (p.OptionValueType == OptionValueType.Optional) Write(o, ref written, MessageLocalizer("["));
            Write(o, ref written, MessageLocalizer("=" + GetArgumentName(0, p.MaxValueCount, p.Description)));
            string sep = p.ValueSeparators != null && p.ValueSeparators.Length > 0
                ? p.ValueSeparators[0]
                : " ";
            for (int c = 1; c < p.MaxValueCount; ++c) Write(o, ref written, MessageLocalizer(sep + GetArgumentName(c, p.MaxValueCount, p.Description)));
            if (p.OptionValueType == OptionValueType.Optional) Write(o, ref written, MessageLocalizer("]"));
        }

        return true;
    }

    private static int GetNextOptionIndex(string[] names, int i)
    {
        while (i < names.Length && names[i] == "<>") ++i;
        return i;
    }

    private static void Write(TextWriter o, ref int n, string s)
    {
        n += s.Length;
        o.Write(s);
    }

    private static string GetArgumentName(int index, int maxIndex, string description)
    {
        MatchCollection matches = Regex.Matches(description ?? "", @"(?<=(?<!\{)\{)[^{}]*(?=\}(?!\}))"); // ignore double braces 
        string argName = "";
        foreach (Match match in matches)
        {
            string[] parts = match.Value.Split(':');
            // for maxIndex=1 it can be {foo} or {0:foo}
            if (maxIndex == 1) argName = parts[^1];
            // look for {i:foo} if maxIndex > 1
            if (maxIndex > 1 && parts.Length == 2 &&
                parts[0] == index.ToString(CultureInfo.InvariantCulture))
                argName = parts[1];
        }

        if (string.IsNullOrEmpty(argName)) argName = maxIndex == 1 ? "VALUE" : "VALUE" + (index + 1);
        return argName;
    }

    private static string GetDescription(string description)
    {
        if (description == null)
            return string.Empty;
        StringBuilder sb = new(description.Length);
        int start = -1;
        for (int i = 0; i < description.Length; ++i)
            switch (description[i])
            {
                case '{':
                    if (i == start)
                    {
                        sb.Append('{');
                        start = -1;
                    }
                    else if (start < 0)
                    {
                        start = i + 1;
                    }

                    break;
                case '}':
                    if (start < 0)
                    {
                        if (i + 1 == description.Length || description[i + 1] != '}')
                            throw new InvalidOperationException("Invalid option description: " + description);
                        ++i;
                        sb.Append('}');
                    }
                    else
                    {
                        sb.Append(description.AsSpan(start, i - start));
                        start = -1;
                    }

                    break;
                case ':':
                    if (start < 0)
                        goto default;
                    start = i + 1;
                    break;
                default:
                    if (start < 0)
                        sb.Append(description[i]);
                    break;
            }

        return sb.ToString();
    }

    private static IEnumerable<string> GetLines(string description, int firstWidth, int remWidth)
    {
        return StringCoda.WrappedLines(description, firstWidth, remWidth);
    }
}