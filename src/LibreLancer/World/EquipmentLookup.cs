using LibreLancer.World.Equipments;

namespace LibreLancer.World;

public class EquipmentLookup
{
    public Shield? Shield;
    public PowerCore? Power;
    public Engine? Engine;
    public Tractor? Tractor;
    public Scanner? Scanner;

    public void Set(EquipmentObject obj)
    {
        if (obj is Shield sh)
        {
            Shield ??= sh;
        }
        else if (obj is PowerCore pc)
        {
            Power ??= pc;
        }
        else if (obj is Engine eg)
        {
            Engine ??= eg;
        }
        else if (obj is Tractor tc)
        {
            Tractor ??= tc;
        }
        else if (obj is Scanner sc)
        {
            Scanner ??= sc;
        }
    }

    public void Unset(EquipmentObject obj)
    {
        if (obj is Shield sh && sh == Shield)
        {
            Shield = null;
        }
        else if (obj is PowerCore pc && pc == Power)
        {
            Power = null;
        }
        else if (obj is Engine eg && eg == Engine)
        {
            Engine = null;
        }
        else if (obj is Tractor tc && tc == Tractor)
        {
            Tractor = null;
        }
        else if (obj is Scanner sc && sc == Scanner)
        {
            Scanner = null;
        }
    }

    public void Clear()
    {
        Shield = null;
        Power = null;
        Engine = null;
        Tractor = null;
        Scanner = null;
    }
}
