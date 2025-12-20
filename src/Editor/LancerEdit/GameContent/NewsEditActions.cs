using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.World;

namespace LancerEdit.GameContent;

public abstract class NewsObjectModification<T>(NewsItem target, T old, T updated, string name)
    : EditorModification<T>(old, updated)
{
    protected NewsItem Target = target;

    string Print(T obj) => obj?.ToString() ?? "NULL";

    public override string ToString() =>
        $"{name}\nOld: {Print(Old)}\nUpdated: {Print(Updated)}";
}

public sealed class NewsRemoveBase(NewsItem item, Base location, NewsCollection collection) : EditorAction
{
    public override string ToString() => $"RemoveBase: {location.Nickname}";

    public override void Commit() => collection.RemoveFromBase(item, location);
    public override void Undo() => collection.AddToBase(item, location);
}

public sealed class NewsAddBase(NewsItem item, Base location, NewsCollection collection) : EditorAction
{
    public override string ToString() => $"AddBase: {location.Nickname}";
    public override void Commit() => collection.AddToBase(item, location);
    public override void Undo() => collection.RemoveFromBase(item, location);
}

public sealed class NewsNew(NewsItem item, NewsCollection collection) : EditorAction
{
    public override string ToString() => "New News";

    public override void Commit() => collection.AddNewsItem(item);
    public override void Undo() => collection.DeleteNewsItem(item);
}


public sealed class NewsDelete(NewsItem item, NewsCollection collection, NewsEditorTab tab) : EditorAction
{
    public override string ToString() => "Delete News";

    private int index = -1;
    private Base[] bases;

    public override void Commit()
    {
        bases = collection.GetBases(item);
        index = collection.DeleteNewsItem(item);
        tab.CheckDeleted(item);
    }

    public override void Undo()
    {
        collection.AddNewsItem(item, index);
        foreach(var b in bases)
            collection.AddToBase(item, b);
    }
}

public sealed class NewsSetFrom(NewsItem target, StoryIndex old, StoryIndex updated) :
    NewsObjectModification<StoryIndex>(target, old, updated, "SetVisibleFrom")
{
    public override void Set(StoryIndex value)
    {
        Target.From = value;
    }
}

public sealed class NewsSetTo(NewsItem target, StoryIndex old, StoryIndex updated) :
    NewsObjectModification<StoryIndex>(target, old, updated, "SetVisibleTo")
{
    public override void Set(StoryIndex value)
    {
        Target.To = value;
    }
}

public sealed class NewsSetHeadline(NewsItem target, int old, int updated) :
    NewsObjectModification<int>(target, old, updated, "SetHeadline")
{
    public override void Set(int value)
    {
        Target.Headline = value;
    }
}

public sealed class NewsSetText(NewsItem target, int old, int updated):
    NewsObjectModification<int>(target, old, updated, "SetText")
{
    public override void Set(int value)
    {
        Target.Text = value;
    }
}

public sealed class NewsSetIcon(NewsItem target, string old, string updated):
    NewsObjectModification<string>(target, old, updated, "SetIcon")
{
    public override void Set(string value)
    {
        Target.Icon = value;
    }
}

public sealed class NewsSetLogo(NewsItem target, string old, string updated):
    NewsObjectModification<string>(target, old, updated, "SetImage")
{
    public override void Set(string value)
    {
        Target.Logo = value;
    }
}

public sealed class NewsSetAutoselect(NewsItem target, bool old, bool updated):
    NewsObjectModification<bool>(target, old, updated, "SetAutoselect")
{
    public override void Set(bool value)
    {
        Target.Autoselect = value;
    }
}

