using WattleScript.Interpreter;

namespace LibreLancer.Interface;

[WattleScriptUserData]
public class UiEquippedWeapon
{
    public bool Enabled { get; set; }
    public int Strid { get; set; }
    public int Ammo { get; set; }

    public UiEquippedWeapon(bool enabled, int strid, int ammo = -1)
    {
        Enabled = enabled;
        Strid = strid;
        Ammo = ammo;
    }
}
