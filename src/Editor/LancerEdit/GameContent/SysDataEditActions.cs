using System;
using LibreLancer;
using LibreLancer.Data.GameData;

namespace LancerEdit.GameContent;

public abstract class SysDataModification<T>(SystemEditData target, T old, T updated, string name)
    : EditorModification<T>(old, updated)
{
    protected SystemEditData Target = target;

    string Print(T obj) => obj?.ToString() ?? "NULL";

    public override string ToString() =>
        $"{name}: {target.Nickname ?? "No Nickname"}\nOld: {Print(Old)}\nUpdated: {Print(Updated)}";
}

public class SysDataSetIdsName(SystemEditData target, int old, int updated)
    : SysDataModification<int>(target, old, updated, "SetIdsName")
{
    public override void Set(int value) => Target.IdsName = value;
}

public class SysDataSetIdsInfo(SystemEditData target, int old, int updated)
    : SysDataModification<int>(target, old, updated, "SetIdsInfo")
{
    public override void Set(int value) => Target.IdsInfo = value;
}


public class SysDataSetMusic(SystemEditData target, string old, string updated, string kind)
    : SysDataModification<string>(target, old, updated, "SetMusic" + kind)
{
    public override void Set(string value)
    {
        switch (kind)
        {
            case "Space":
                Target.MusicSpace = value;
                break;
            case "Battle":
                Target.MusicBattle = value;
                break;
            case "Danger":
                Target.MusicDanger = value;
                break;
            default: throw new InvalidOperationException();
        }
    }
}

public class SysDataSetStars(SystemEditData target, ResolvedModel old, ResolvedModel updated, string kind, SystemEditorTab tab)
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

public class SysDataSetSpaceColor(SystemEditData target, Color4 old, Color4 updated)
    : SysDataModification<Color4>(target, old, updated, "SetSpaceColor")
{
    public override void Set(Color4 value) => Target.SpaceColor = value;
}

public class SysDataSetAmbient(SystemEditData target, Color3f old, Color3f updated)
    : SysDataModification<Color3f>(target, old, updated, "SetAmbient")
{
    public override void Set(Color3f value) => Target.Ambient = value;
}
