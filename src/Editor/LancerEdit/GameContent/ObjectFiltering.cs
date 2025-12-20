using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer;
using LibreLancer.Data;

namespace LancerEdit.GameContent;

public abstract class ObjectFiltering<T>
{
    private Dictionary<string,FilterMethod> filters =
        new(StringComparer.OrdinalIgnoreCase);

    protected delegate IEnumerable<T> FilterMethod(string text, IEnumerable<T> source);

    protected delegate IEnumerable<T> ExtraFilterMethod(IEnumerable<T> source);
    protected abstract IEnumerable<T> DefaultFilter(string text, IEnumerable<T> source);

    private Dictionary<string, ExtraFilterMethod> extraFilterFuncs = 
        new(StringComparer.OrdinalIgnoreCase);

    protected void SetPrefix(string prefix, FilterMethod func)
    {
        filters[prefix] = func;
    }

    protected void SetExtra(string id, ExtraFilterMethod func)
    {
        extraFilterFuncs[id] = func;
    }

    protected static bool NicknameContains(IdentifiableItem item, string nickname)
    {
        if (item == null) return false;
        return item.Nickname.Contains(nickname, StringComparison.OrdinalIgnoreCase);
    }

    IEnumerable<T> FilterByText(string text, IEnumerable<T> source) {
        text = text.Trim();
        if (string.IsNullOrWhiteSpace(text)) return source;
        int idx = text.IndexOf(':');
        if (idx != -1)
        {
            var pfx = text.Substring(0, idx).Trim();
            if (filters.TryGetValue(pfx, out var filterFunc)) {
                return filterFunc(text.Substring(idx + 1).Trim(), source);
            }
        }
        return DefaultFilter(text, source);
    }

    public IEnumerable<T> Filter(string text, IEnumerable<T> source, params string[] extraFilters)
    {
        var filtered = FilterByText(text, source);
        foreach (var e in extraFilters.Where(x => !string.IsNullOrWhiteSpace(x))) {
            filtered = extraFilterFuncs[e](filtered);
        }
        return filtered;
    }
}