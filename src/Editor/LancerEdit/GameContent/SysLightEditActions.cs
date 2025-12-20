using System.ComponentModel;
using System.Numerics;
using LibreLancer;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.World;
using LibreLancer.Render;

namespace LancerEdit.GameContent;

public abstract class SysLightModification<T>(LightSource target, T old, T updated, string name)
    : EditorModification<T>(old, updated)
{
    protected LightSource Target = target;
    string Print(T obj) => obj?.ToString() ?? "NULL";

    public override string ToString() =>
        $"{name}: {Target.Nickname ?? "No Nickname"}\nOld: {Print(Old)}\nUpdated: {Print(Updated)}";
}


public class SysLightSetKind(LightSource target, LightKind old, LightKind updated, LightSourceList list)
    : SysLightModification<LightKind>(target, old, updated, "SetKind")
{
    public override void Set(LightKind value)
    {
        Target.Light.Kind = value;
        list.CheckDirty();
    }
}

public class SysLightCreate(LightSource newLight, SystemEditorTab tab)
: EditorAction
{
    public override void Commit()
    {
        tab.LightsList.Sources.Add(newLight);
        tab.LightsList.CheckDirty();
    }

    public override void Undo()
    {
        tab.LightsList.Sources.Remove(newLight);
        tab.LightsList.CheckDirty();
    }

    public override string ToString() => $"CreateLight: {newLight.Nickname}";
}

public class SysLightRemove(LightSource existingLight, LightSourceList list)
    : EditorAction
{
    public override void Commit()
    {
        list.Sources.Remove(existingLight);
        list.CheckDirty();
    }

    public override void Undo()
    {
        list.Sources.Add(existingLight);
        list.CheckDirty();
    }

    public override string ToString() => $"RemoveLight: {existingLight.Nickname}";
}

public class SysLightSetAttenuation(
    LightSource target,
    string oldCurve,
    Vector3 oldAtten,
    string newCurve,
    Vector3 newAtten,
    LightSourceList list)
    : SysLightModification<(string Curve, Vector3 Atten)>(target, (oldCurve, oldAtten), (newCurve, newAtten),
        "SetAttenuation")
{
    public override void Set((string Curve, Vector3 Atten) value)
    {
        Target.AttenuationCurveName = value.Curve;
        Target.Light.Attenuation = value.Atten;
        list.CheckDirty();
    }
}

public class SysLightSetNickname(LightSource target, string old, string updated, LightSourceList list)
    : SysLightModification<string>(target, old, updated, "SetNickname")
{
    public override void Set(string value)
    {
        Target.Nickname = value;
        list.CheckDirty();
    }
}

public class SysLightSetPosition(LightSource target, Vector3 old, Vector3 updated, LightSourceList list)
    : SysLightModification<Vector3>(target, old, updated, "SetPosition")
{
    public override void Set(Vector3 value)
    {
        Target.Light.Position = value;
        list.CheckDirty();
    }
}

public class SysLightSetRange(LightSource target, float old, float updated, LightSourceList list)
    : SysLightModification<float>(target, old, updated, "SetRange")
{
    public override void Set(float value)
    {
        Target.Light.Range = value;
        list.CheckDirty();
    }
}

public class SysLightSetColor(LightSource target, Color3f old, Color3f updated, LightSourceList list)
    : SysLightModification<Color3f>(target, old, updated, "SetColor")
{
    public override void Set(Color3f value)
    {
        Target.Light.Color = value;
        list.CheckDirty();
    }
}


