using LibreLancer.Data.GameData.Items;
using LibreLancer.Render;
using LibreLancer.Resources;

namespace LibreLancer.World.Components;

public class CloakComponent(GameObject parent, CloakEquipment equipment, bool withEffects) : GameComponent(parent)
{
    public CloakEquipment Equipment = equipment;

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
        parent.Flags |= GameObjectFlags.Cloaked;
        StartInFx();
        world.Server?.OnCloak(Parent);
    }

    public void SetInitCloaked()
    {
        currentState = CloakState.Cloaked;
        parent.Flags |= (GameObjectFlags.Cloaked | GameObjectFlags.Hidden);
    }

    public void Uncloak(GameWorld world)
    {
        if (currentState == CloakState.Uncloaking ||
            currentState == CloakState.Off)
            return;
        parent.Flags &= ~GameObjectFlags.Hidden;
        parent.Flags &= ~GameObjectFlags.Cloaked;
        stateTime = 0;
        currentState = CloakState.Uncloaking;
        StartOutFx();
        SetOpacity(0f);
        world.Server?.OnUncloak(Parent);
    }

    private ParticleEffectRenderer? inFx = null;
    private ParticleEffectRenderer? outFx = null;

    public override void Register(GameWorld world)
    {
        if (withEffects)
        {
            var resources = GetResourceManager(world)!;
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

    void StartInFx()
    {
        if (inFx == null)
            return;
        inFx.Restart();
        Parent.ExtraRenderers.Add(inFx);
    }

    void StopInFx()
    {
        if (inFx == null)
            return;
        Parent.ExtraRenderers.Remove(inFx);
    }

    void StartOutFx()
    {
        if(outFx == null)
            return;
        outFx.Restart();
        Parent.ExtraRenderers.Add(outFx);
    }

    void StopOutFx()
    {
        if (outFx == null)
            return;
        Parent.ExtraRenderers.Remove(outFx);
    }

    void SetOpacity(float opacity)
    {
        if (!withEffects)
            return;
        SetOpacity(Parent, opacity);
    }

    void SetOpacity(GameObject obj, float opacity)
    {
        if (obj.RenderComponent != null)
        {
            obj.RenderComponent.OpacityMultiplier = opacity;
        }
        for (int i = 0; i < obj.Children.Count; i++)
        {
            SetOpacity(obj.Children[i], opacity);
        }
    }


    public override void Update(double time, GameWorld world)
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
                    SetOpacity(1.0f - (float)(stateTime / Equipment.CloakOutTime));
                }
                break;
            case CloakState.Uncloaking:
                stateTime += time;
                if (stateTime >= Equipment.CloakOutTime)
                {
                    currentState = CloakState.Off;
                    StopOutFx();
                    SetOpacity(1f);
                }
                else
                {
                    SetOpacity((float)(stateTime / Equipment.CloakOutTime));
                }
                break;
        }
    }
}
