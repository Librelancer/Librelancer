namespace LibreLancer.Data.GameData.World;

public class LightSource
{
    public required string Nickname;
    public required string? AttenuationCurveName;
    public RenderLight Light;

    public LightSource Clone() => new()
        { Nickname = Nickname, AttenuationCurveName = AttenuationCurveName, Light = Light };
}
