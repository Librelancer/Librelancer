namespace LibreLancer.Data.GameData.World;

public class LightSource : NicknameItem
{
    public bool Disabled; // Editor use only, not saved.
    public string AttenuationCurveName;
    public RenderLight Light;

    public LightSource Clone() => new LightSource()
        { Nickname = Nickname, AttenuationCurveName = AttenuationCurveName, Light = Light };
}
