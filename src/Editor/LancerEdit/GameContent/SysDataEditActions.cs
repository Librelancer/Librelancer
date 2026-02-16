using System;
using LibreLancer;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.World;

namespace LancerEdit.GameContent;

public abstract class SysDataModification<T>(StarSystem target, T old, T updated, string name)
    : EditorModification<T>(old, updated)
{
    protected StarSystem Target = target;

    string Print(T obj) => obj?.ToString() ?? "NULL";

    public override string ToString() =>
        $"{name}: {target.Nickname ?? "No Nickname"}\nOld: {Print(Old)}\nUpdated: {Print(Updated)}";
}

public class SysDataSetStars(StarSystem target, ResolvedModel old, ResolvedModel updated, string kind, SystemEditorTab tab)
    : SysDataModification<ResolvedModel>(target, old, updated, "SetStars" + kind)
{
    public override void Set(ResolvedModel value)
    {
        switch (kind)
        {
            case "Basic":
                Target.StarsBasic = value;
                break;
            case "Complex":
                Target.StarsComplex = value;
                break;
            case "Nebula":
                Target.StarsNebula = value;
                break;
            default: throw new InvalidOperationException();
        }
        tab.ReloadStarspheres();
    }
}
