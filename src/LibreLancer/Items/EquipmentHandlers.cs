using LibreLancer.Client.Components;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Render;
using LibreLancer.Resources;
using LibreLancer.Server.Components;
using LibreLancer.Sounds;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Items;

public static class EquipmentHandlers
{
    private static bool _registered = false;

    public static void Register()
    {
        if (_registered)
            return;
        _registered = true;

        EquipmentObjectManager.RegisterType<CloakEquipment>(Cloak);
        EquipmentObjectManager.RegisterType<CountermeasureEquipment>(Countermeasure);
        EquipmentObjectManager.RegisterType<EffectEquipment>(Effect);
        EquipmentObjectManager.RegisterType<EngineEquipment>(Engine);
        EquipmentObjectManager.RegisterType<GunEquipment>(Gun);
        EquipmentObjectManager.RegisterType<LightEquipment>(Light);
        EquipmentObjectManager.RegisterType<MissileLauncherEquipment>(MissileLauncher);
        EquipmentObjectManager.RegisterType<PowerEquipment>(Power);
        EquipmentObjectManager.RegisterType<ScannerEquipment>(Scanner);
        EquipmentObjectManager.RegisterType<ShieldEquipment>(Shield);
        EquipmentObjectManager.RegisterType<ThrusterEquipment>(Thruster);
        EquipmentObjectManager.RegisterType<TractorEquipment>(Tractor);
        EquipmentObjectManager.RegisterType<TradelaneEquipment>(Tradelane);
    }

    private static GameObject Countermeasure(GameObject parent, ResourceManager res, SoundManager? snd,
        EquipmentType type, string? hardpoint, Equipment equip)
    {
        var sh = (CountermeasureEquipment) equip;
        var obj = GameObject.WithModel(sh.ModelFile!, type != EquipmentType.Server, res);
        return obj;
    }

    private static GameObject Effect(GameObject parent, ResourceManager res, SoundManager? snd, EquipmentType type,
        string? hardpoint, Equipment equip)
    {
        var obj = new GameObject();

        if (type == EquipmentType.Server)
        {
            return obj;
        }

        var e = (EffectEquipment) equip;

        if (e.Particles is null)
        {
            return obj;
        }

        obj.RenderComponent = new ParticleEffectRenderer(e.Particles.GetEffect(res)!);
        obj.AddComponent(new CUpdateSParamComponent(obj));

        if (e.Particles.Sound is null ||
            snd == null)
        {
            return obj;
        }

        snd.LoadSound(e.Particles.Sound.Nickname);
        obj.AddComponent(new CSoundEffectComponent(parent, snd, e.Particles.Sound.Nickname));

        return obj;
    }

    private static GameObject? Cloak(GameObject parent, ResourceManager res, SoundManager? snd, EquipmentType type,
        string? hardpoint, Equipment equip)
    {
        var clk = (CloakEquipment)equip;
        parent.AddComponent(new CloakComponent(parent, clk, type == EquipmentType.RemoteObject ||
                                                            type == EquipmentType.LocalPlayer));
        return null;
    }

    private static GameObject? Engine(GameObject parent, ResourceManager res, SoundManager? snd, EquipmentType type,
        string? hardpoint, Equipment equip)
    {
        var eng = (EngineEquipment) equip;
        if (type != EquipmentType.Server)
        {
            parent.AddComponent(new CEngineComponent(parent, eng));
        }
        else
        {
            parent.AddComponent(new SEngineComponent(parent, eng));
        }

        if (snd == null)
        {
            return null;
        }

        snd.LoadSound(eng.Def.CruiseLoopSound);
        snd.LoadSound(eng.Def.CruiseStartSound);
        snd.LoadSound(eng.Def.CruiseStopSound);
        snd.LoadSound(eng.Def.CruiseBackfireSound);
        snd.LoadSound(eng.Def.CruiseStopSound);
        snd.LoadSound(eng.Def.EngineKillSound);
        snd.LoadSound(eng.Def.RumbleSound);
        snd.LoadSound(eng.Def.CharacterLoopSound);
        snd.LoadSound(eng.Def.CharacterStartSound);

        return null;
    }

    private static GameObject Gun(GameObject parent, ResourceManager res, SoundManager? snd, EquipmentType type,
        string? hardpoint, Equipment equip)
    {
        var gn = (GunEquipment) equip;
        var child = GameObject.WithModel(gn.ModelFile!, type != EquipmentType.Server, res);
        if (type != EquipmentType.RemoteObject &&
            type != EquipmentType.Cutscene)
            child.AddComponent(new GunComponent(child, gn));
        if (type is EquipmentType.LocalPlayer or EquipmentType.RemoteObject)
            child.AddComponent(new CMuzzleFlashComponent(child, gn));

        snd?.LoadSound(gn.Munition.Def.OneShotSound);

        return child;
    }

    private static GameObject Light(GameObject parent, ResourceManager res, SoundManager? snd, EquipmentType type,
        string? hardpoint, Equipment equip)
    {
        var lq = (LightEquipment) equip;
        var obj = new GameObject();

        if (type != EquipmentType.Server &&
            type != EquipmentType.Cutscene)
        {
            obj.RenderComponent = new LightEquipRenderer(lq) { LightOn = !lq.DockingLight };
        }

        return obj;
    }

    private static GameObject MissileLauncher(GameObject parent, ResourceManager res, SoundManager? snd,
        EquipmentType type, string? hardpoint, Equipment equip)
    {
        var gn = (MissileLauncherEquipment) equip;
        var child = GameObject.WithModel(gn.ModelFile!, type != EquipmentType.Server, res);
        if (type != EquipmentType.RemoteObject &&
            type != EquipmentType.Cutscene)
            child.AddComponent(new MissileLauncherComponent(child, gn));
        snd?.LoadSound(gn.Munition.Def.OneShotSound);
        return child;
    }

    private static GameObject? Power(GameObject parent, ResourceManager res, SoundManager? snd, EquipmentType type,
        string? hardpoint, Equipment equip)
    {
        var pc = new PowerCoreComponent(((PowerEquipment) equip).Def, parent);
        parent.AddComponent(pc);
        return null;
    }

    private static GameObject? Scanner(GameObject parent, ResourceManager res, SoundManager? snd, EquipmentType type,
        string? hardpoint, Equipment equip)
    {
        var scan = new ScannerComponent(parent, (ScannerEquipment) equip);
        parent.AddComponent(scan);
        return null;
    }

    private static GameObject Shield(GameObject parent, ResourceManager res, SoundManager? snd, EquipmentType type,
        string? hardpoint, Equipment equip)
    {
        var sh = (ShieldEquipment) equip;
        var obj = GameObject.WithModel(sh.ModelFile!, type != EquipmentType.Server, res);

        switch (type)
        {
            case EquipmentType.Server:
                obj.AddComponent(new SShieldComponent(sh, obj));
                break;
            case EquipmentType.LocalPlayer:
            case EquipmentType.RemoteObject:
                obj.AddComponent(new CShieldComponent(sh, obj));
                break;
        }

        return obj;
    }

    private static GameObject Thruster(GameObject parent, ResourceManager res, SoundManager? snd, EquipmentType type,
        string? hardpoint, Equipment equip)
    {
        var th = (ThrusterEquipment) equip;
        var obj = GameObject.WithModel(th.ModelFile!, type != EquipmentType.Server, res);

        switch (type)
        {
            case EquipmentType.LocalPlayer or EquipmentType.RemoteObject:
                obj.AddComponent(new CThrusterComponent(obj, th));
                break;
            case EquipmentType.Server:
                obj.AddComponent(new ThrusterComponent(obj, th));
                break;
        }

        return obj;
    }

    private static GameObject? Tractor(GameObject parent, ResourceManager res, SoundManager? snd, EquipmentType type,
        string? hardpoint, Equipment equip)
    {
        if (type == EquipmentType.Server)
        {
            var tc = new STractorComponent((TractorEquipment) equip, parent);
            parent.AddComponent(tc);
        }
        else
        {
            var tc = new CTractorComponent((TractorEquipment) equip, parent);
            parent.AddComponent(tc);
        }

        return null;
    }

    private static GameObject? Tradelane(GameObject parent, ResourceManager res, SoundManager? snd, EquipmentType type,
        string? hardpoint, Equipment equip)
    {
        if (type != EquipmentType.Server)
        {
            parent.AddComponent(new CTradelaneComponent(parent, (TradelaneEquipment) equip));
        }

        return null;
    }
}
