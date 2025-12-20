namespace LibreLancer.Data.GameData.World;

public class LightSource
{
    public string Nickname;
    public string AttenuationCurveName;
    public RenderLight Light;

    public LightSource Clone() => new LightSource()
        { Nickname = Nickname, AttenuationCurveName = AttenuationCurveName, Light = Light };
}
