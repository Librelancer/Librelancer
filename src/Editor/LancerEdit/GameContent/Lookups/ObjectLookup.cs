using System;
using System.Collections.Generic;
using System.Linq;

namespace LancerEdit.GameContent.Lookups;

public abstract class ObjectLookup<T> where T : class
{
    private record Display(string Name, T Value);

    private Display sel;

    public T Selected => sel?.Value;
    public T Hovered => dropdown?.Hovered?.Value;
    public bool IsOpen => dropdown.IsOpen;

    private SearchDropdown<Display> dropdown;

    protected void CreateDropdown(string id, IEnumerable<T> values, Func<T,string> name, T initial)
    {
        var options = values
            .Select(x => new Display( name(x), x))
            .ToArray();
        if(initial != null)
            sel = options.FirstOrDefault(x => x.Value == initial);
        dropdown = new SearchDropdown<Display>(id,
             x => x?.Name ?? "(none)",
            x => sel = x,
            sel, options);
    }

    public void Draw()
    {
        dropdown.Draw();
    }
}
