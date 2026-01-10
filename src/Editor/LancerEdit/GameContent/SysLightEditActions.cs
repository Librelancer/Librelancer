using System.Numerics;
using LibreLancer;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.World;

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
    }
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
    }
}

public class SysLightSetPosition(LightSource target, Vector3 old, Vector3 updated, LightSourceList list)
    : SysLightModification<Vector3>(target, old, updated, "SetPosition")
{
    public override void Set(Vector3 value)
    {
        Target.Light.Position = value;
    }
}

public class SysLightSetColor(LightSource target, Color3f old, Color3f updated, LightSourceList list)
    : SysLightModification<Color3f>(target, old, updated, "SetColor")
{
    public override void Set(Color3f value)
    {
        Target.Light.Color = value;
    }
}


