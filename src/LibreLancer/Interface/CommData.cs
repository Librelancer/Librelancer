using WattleScript.Interpreter;

namespace LibreLancer.Interface;

[WattleScriptUserData]
public class CommData
{
    public CommAppearance Appearance = null!;
    public string Source = "";
    public string? Affiliation;
}
