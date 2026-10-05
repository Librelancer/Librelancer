using LibreLancer.Data.GameData.Items;
using LibreLancer.Render;
using LibreLancer.Resources;

namespace LibreLancer.World.Equipments;

public class CloakingDevice : EquipmentModelObject<CloakEquipment>
{
    public CloakingDevice(
        GameObject parent,
        Hardpoint? attachment,
        CloakEquipment equipment,
        EquipmentType type,
        ResourceManager resources)
        : base(parent, attachment, equipment, type, resources, true)
    {
        if (type == EquipmentType.LocalPlayer ||
            type == EquipmentType.RemoteObject)
        {
            var inParticles = Equipment.CloakInFx?.GetEffect(resources);
            var outParticles = Equipment.CloakOutFx?.GetEffect(resources);
            if (inParticles != null)
            {
                inFx = new(inParticles);
            }
            if (outParticles != null)
            {
                outFx = new(outParticles);
            }
        }
    }

    private double stateTime = 0;
    private CloakState currentState = CloakState.Off;

    enum CloakState
    {
        Cloaking,
        Uncloaking,
        Cloaked,
        Off
    }

    public void Cloak(GameWorld world)
    {
        if (currentState == CloakState.Cloaking ||
            currentState == CloakState.Cloaked)
            return;
        stateTime = 0;
        currentState = CloakState.Cloaking;
        Parent.Flags |= GameObjectFlags.Cloaked;
        StartInFx();
        world.Server?.OnCloak(Parent);
    }

    public void SetInitCloaked()
    {
        currentState = CloakState.Cloaked;
        Parent.Flags |= (GameObjectFlags.Cloaked | GameObjectFlags.Hidden);
    }

    public void Uncloak(GameWorld world)
    {
        if (currentState == CloakState.Uncloaking ||
            currentState == CloakState.Off)
            return;
        Parent.Flags &= ~GameObjectFlags.Hidden;
        Parent.Flags &= ~GameObjectFlags.Cloaked;
        stateTime = 0;
        currentState = CloakState.Uncloaking;
        StartOutFx();
        SetObjectOpacity(Parent, 0f);
        world.Server?.OnUncloak(Parent);
    }

    private ParticleEffectRenderer? inFx = null;
    private ParticleEffectRenderer? outFx = null;

    void StartInFx()
    {
        inFx?.Restart();
    }

    void StopInFx()
    {
        inFx?.Active = false;
    }

    void StartOutFx()
    {
        outFx?.Restart();
    }

    void StopOutFx()
    {
        outFx?.Active = false;
    }


    static void SetObjectOpacity(GameObject obj, float opacity)
    {
        if (obj.RenderComponent != null)
        {
            obj.RenderComponent.OpacityMultiplier = opacity;
        }

        foreach (var e in obj.AllEquipment)
        {
            e.SetOpacity(opacity);
        }
    }


    public override void OnUpdate(double time, GameWorld world)
    {
        switch (currentState)
        {
            case CloakState.Cloaking:
                stateTime += time;
                if (stateTime >= Equipment.CloakInTime)
                {
                    Parent.Flags |= GameObjectFlags.Hidden;
                    currentState = CloakState.Cloaked;
                    StopInFx();
                }
                else
                {
                    SetObjectOpacity(Parent, 1.0f - (float)(stateTime / Equipment.CloakOutTime));
                }
                break;
            case CloakState.Uncloaking:
                stateTime += time;
                if (stateTime >= Equipment.CloakOutTime)
                {
                    currentState = CloakState.Off;
                    StopOutFx();
                    SetObjectOpacity(Parent, 1f);
                }
                else
                {
                    SetObjectOpacity(Parent, (float)(stateTime / Equipment.CloakOutTime));
                }
                break;
        }
    }

    public override void RenderUpdate(double delta)
    {
        base.RenderUpdate(delta);
        var p = Parent.Transform;
        var mat = Parent.Transform.Matrix();
        inFx?.Update(delta, p.Position, mat);
        outFx?.Update(delta, p.Position, mat);
    }

    public override void PrepareRender(ICamera camera, NebulaRenderer? nr, SystemRenderer sys, bool parentCull)
    {
        base.PrepareRender(camera, nr, sys, parentCull);
        inFx?.PrepareRender(camera, nr, sys, parentCull);
        outFx?.PrepareRender(camera, nr, sys, parentCull);
    }
}
