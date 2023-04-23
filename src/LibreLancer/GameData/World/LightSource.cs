using System;
using System.Numerics;
using System.Text;
using LibreLancer.Data;
using LibreLancer.Render;

namespace LibreLancer.GameData.World;

public class LightSource
{
    public string Nickname;
    public string AttenuationCurveName;
    public RenderLight Light;

    public string Serialize()
    {
        var sb = new StringBuilder();
        sb.AppendSection("LightSource")
            .AppendEntry("nickname", Nickname)
            .AppendEntry("pos", Light.Position)
            .AppendEntry("color", Light.Color)
            .AppendEntry("range", Light.Range);
        if (Light.Direction != Vector3.UnitZ)
            sb.AppendEntry("direction", Light.Direction);
        sb.AppendEntry("type", Light.Kind == LightKind.Directional ? "DIRECTIONAL" : "POINT");
        if (!string.IsNullOrWhiteSpace(AttenuationCurveName))
            sb.AppendEntry("atten_curve", AttenuationCurveName);
        else
            sb.AppendEntry("attenuation", Light.Attenuation);
        return sb.ToString();
    }
}