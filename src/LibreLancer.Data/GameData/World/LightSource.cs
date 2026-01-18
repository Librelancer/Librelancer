namespace LibreLancer.Data.GameData.World;

public class LightSource : NicknameItem
{
    public required string? AttenuationCurveName;
    public bool Disabled; // Editor use only, not saved.
    public RenderLight Light;

    public LightSource Clone() => new()
        { Nickname = Nickname, AttenuationCurveName = AttenuationCurveName, Light = Light };
}
