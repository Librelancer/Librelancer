using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Render;
using LibreLancer.Resources;
using LibreLancer.World.Components;

namespace LibreLancer.World.Equipments;

public abstract class AbstractWeapon : EquipmentModelObject<Equipment>
{
    public double CurrentCooldown = 0;

    public Vector2 Angles = new(0, 0);

    protected abstract float TurnRate { get; }

    public abstract float MaxRange { get; }

    private List<ParticleEffectRenderer> flashFx = [];


    protected AbstractWeapon(
        GameObject parent,
        Hardpoint? attachment,
        Equipment equipment,
        EquipmentType type,
        ResourceManager resources) : base(parent, attachment, equipment, type, resources, true)
    {
    }


    protected void AddFlashEffect(ResolvedFx? fx, ResourceManager resources)
    {
        var effect = fx?.GetEffect(resources);
        if (effect == null)
            return;

        var hpfires = GetHardpoints()
            .Where(x => x.Name.StartsWith("hpfire", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        foreach (var fire in hpfires)
        {
            var pr = new ParticleEffectRenderer(effect)
            {
                Active = false,
                Attachment = fire
            };
            flashFx.Add(pr);
        }
    }

    protected void RunMuzzleFlash()
    {
        foreach (var fire in flashFx)
        {
            fire.Active = true;
            fire.Restart();
        }
    }

    public override void PrepareRender(ICamera camera, NebulaRenderer? nr, SystemRenderer sys, bool parentCull)
    {
        base.PrepareRender(camera, nr, sys, parentCull);
        foreach (var fx in flashFx)
        {
            fx.PrepareRender(camera, nr, sys, parentCull);
        }
    }

    public override void RenderUpdate(double delta)
    {
        base.RenderUpdate(delta);
        if (flashFx.Count > 0)
        {
            var tr = GetTransform();
            foreach (var fx in flashFx)
            {
                fx.Update(delta, tr);
            }
        }
    }

    public override void OnUpdate(double time, GameWorld world)
    {
        CurrentCooldown -= time;
        if (CurrentCooldown < 0)
        {
            CurrentCooldown = 0;
        }

        if (_targetX > -1000)
        {
            DoRotation(_targetX, _targetY, time);
        }
    }

    private void DoRotation(float x, float y, double time)
    {
        if (Model == null)
            return;
        var hp = Attachment!;
        var rads = MathHelper.DegreesToRadians(TurnRate);
        var delta = (float)(time * rads);

        if (hp.Revolute)
        {
            var target = MathHelper.Clamp(x, hp.RevolveMin, hp.RevolveMax);
            var current = MoveTowards(hp.CurrentRevolution, target, delta);

            if (Math.Abs(target - current) < float.Epsilon)
            {
                NeedUpdateTransform();
            }

            hp.Revolve(current);
            Angles.X = hp.CurrentRevolution;
        }

        // TODO: Finding barrel construct properly?
        Utf.RevConstruct? barrel = null;
        foreach (var mdl in Model.RigidModel.AllParts)
        {
            if (mdl.Construct is Utf.RevConstruct revCon)
            {
                barrel = revCon;
            }
        }

        if (barrel != null)
        {
            var target = MathHelper.Clamp(y, barrel.Min, barrel.Max);
            var current = MoveTowards(barrel.Current, target, delta);

            if (Math.Abs(target - current) < float.Epsilon)
            {
                NeedUpdateTransform();
            }

            barrel.Update(current, Quaternion.Identity);
            Angles.Y = barrel.Current;
            Model.RigidModel.UpdateTransform();
        }
    }

    private static float MoveTowards(float current, float target, float maxDelta)
    {
        var difference = target - current;
        if (MathF.Abs(difference) <= maxDelta)
            return target;
        return current + MathF.CopySign(maxDelta, difference);
    }

    private float _targetX = -1000;
    private float _targetY = -1000;

    public void RotateTowards(float x, float y)
    {
        _targetX = x;
        _targetY = y;
    }

    public void AimTowards(Vector3 point, double time)
    {
        var hp = Attachment!;
        // Parent is the gun itself rotated
        var br = (hp.TransformNoRotate * Parent.Transform).Matrix();
        // Inverse Transform
        Matrix4x4.Invert(br, out var beforeRotate);
        var local = TransformGL(point, beforeRotate);
        var localProper = local.Normalized();
        var x = -localProper.X * (float)Math.PI;
        var y = localProper.Y * (float)Math.PI;
        DoRotation(x, y, time);
    }

    private static Vector3 TransformGL(Vector3 position, Matrix4x4 matrix)
    {
        return new Vector3(
            position.X * matrix.M11 + position.Y * matrix.M21 + position.Z * matrix.M31 + matrix.M41,
            position.X * matrix.M12 + position.Y * matrix.M22 + position.Z * matrix.M32 + matrix.M42,
            position.X * matrix.M13 + position.Y * matrix.M23 + position.Z * matrix.M33 + matrix.M43);
    }

    protected static float GetAngle(Vector3 pointA, Vector3 pointB)
    {
        var angle = MathF.Acos(Vector3.Dot(pointA.Normalized(), pointB.Normalized()));
        return angle;
    }

    protected abstract bool OnFire(Vector3 point, GameWorld world, GameObject? target, bool server);

    public bool Fire(Vector3 point, GameWorld world, GameObject? target = null, bool fromServer = false)
    {
        if (!fromServer && Parent.TryGetComponent<ShipPhysicsComponent>(out var flight) &&
            flight.EngineState is EngineStates.Cruise or EngineStates.CruiseCharging)
        {
            return false;
        }

        if (CurrentCooldown > 0 && !fromServer)
        {
            return false;
        }

        // Cloaked ships can't fire weapons
        return !Parent.Flags.HasFlag(GameObjectFlags.Cloaked) && OnFire(point, world, target, fromServer);
    }
}
