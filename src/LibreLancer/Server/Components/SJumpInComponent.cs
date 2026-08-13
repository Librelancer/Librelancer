// MIT License - Copyright (c) LibreLancer contributors
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Data.GameData;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Server.Components;

internal sealed class SJumpInComponent(
    GameObject parent,
    Vector3[] path,
    float duration,
    Action<bool>? onFinished = null) : GameComponent(parent)
{
    private double elapsed;
    private bool complete;

    private void Finish(bool arrived)
    {
        if (complete)
            return;
        complete = true;
        var callback = onFinished;
        onFinished = null;
        callback?.Invoke(arrived);
    }

    public override void Update(double time, GameWorld world)
    {
        if (complete)
            return;
        elapsed += Math.Max(0, time);
        var progress = duration <= 0
            ? 1
            : Math.Clamp((float)(elapsed / duration), 0, 1);
        var sample = JumpTunnelMotion.SamplePath(path, progress);
        var transform = new Transform3D(
            sample.Position,
            QuaternionEx.LookAt(sample.Position, sample.Position + sample.Direction));
        Parent.SetLocalTransform(transform);
        if (Parent.PhysicsComponent?.Body != null)
        {
            Parent.PhysicsComponent.Body.LinearVelocity = progress < 1
                ? sample.Direction * JumpTunnelMotion.JumpArrivalSpeed
                : Vector3.Zero;
            Parent.PhysicsComponent.Body.AngularVelocity = Vector3.Zero;
        }
        if (progress >= 1)
        {
            Finish(true);
            Parent.RemoveComponent(this);
        }
    }

    // Physics components are unregistered before this component. Report a
    // cancellation without allowing cleanup code to touch the invalid Bepu body.
    public override void Unregister(GameWorld world) => Finish(false);
}
