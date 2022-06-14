using WattleScript.Interpreter;

namespace LibreLancer.Interface;

[WattleScriptUserData]
public class UiEquippedWeapon
{
    public bool Enabled { get; set; }
    public int Strid { get; set; }

    public UiEquippedWeapon(bool enabled, int strid)
    {
        Enabled = enabled;
        Strid = strid;
    }
}