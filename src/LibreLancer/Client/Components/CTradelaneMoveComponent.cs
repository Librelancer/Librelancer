// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Net.Protocol;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Client.Components;

public class CTradelaneMoveComponent(GameObject parent) : GameComponent(parent)
{
    public TradelaneMoveState State { get; private set; } = TradelaneMoveState.Transit;
    public float TargetSpeed { get; private set; } = TradelaneMotion.Speed;

    private float manualExitTime;
    private float manualStartSpeed;
    private float manualExitDuration;
    private float manualTurnDuration;
    private Quaternion manualStartOrientation;
    private Quaternion manualTargetOrientation;

    public void ApplyState(PlayerAuthState auth)
    {
        var oldState = State;
        State = auth.TradelaneState;
        TargetSpeed = auth.TradelaneTargetSpeed;

        if (State == TradelaneMoveState.ManualExit && oldState != TradelaneMoveState.ManualExit)
        {
            ResetManualExit(auth);
        }
        else if (State == TradelaneMoveState.None)
        {
            manualExitTime = 0;
        }
    }

    public void ResetToAuthoritative(PlayerAuthState auth)
    {
        State = auth.TradelaneState;
        TargetSpeed = auth.TradelaneTargetSpeed;
        if (State == TradelaneMoveState.ManualExit)
        {
            ResetManualExit(auth);
        }
    }

    public override void Update(double time, GameWorld world)
    {
        Predict(time);
    }

    public void Predict(double time)
    {
        if (State == TradelaneMoveState.None || Parent.PhysicsComponent?.Body == null)
        {
            return;
        }

        if (State == TradelaneMoveState.ManualExit)
        {
            PredictManualExit(time);
            return;
        }

        var body = Parent.PhysicsComponent.Body;
        var direction = body.LinearVelocity.LengthSquared() > float.Epsilon
            ? body.LinearVelocity.Normalized()
            : TradelaneMotion.Forward(body.Orientation);
        var speed = TargetSpeed > float.Epsilon
            ? TargetSpeed
            : body.LinearVelocity.Length();

        body.LinearVelocity = direction * speed;
        if (direction.LengthSquared() > float.Epsilon)
        {
            body.SetOrientation(QuaternionEx.LookAt(body.Position, body.Position + direction));
        }
        body.AngularVelocity = Vector3.Zero;
        SetEngineSpeed(speed);
    }

    private void PredictManualExit(double time)
    {
        manualExitTime += (float)time;
        var turnProgress = MathHelper.Clamp(manualExitTime / manualTurnDuration, 0, 1);
        var speedProgress = MathHelper.Clamp(manualExitTime / manualExitDuration, 0, 1);
        var easedTurnProgress = turnProgress * turnProgress * (3 - 2 * turnProgress);
        var orientation = Quaternion.Slerp(manualStartOrientation, manualTargetOrientation, easedTurnProgress);
        var speed = TradelaneMotion.ManualExitSpeed(speedProgress, manualStartSpeed, TargetSpeed);

        var body = Parent.PhysicsComponent!.Body;
        body.SetOrientation(orientation);
        body.LinearVelocity = TradelaneMotion.Forward(orientation) * speed;
        body.AngularVelocity = Vector3.Zero;
        SetEngineSpeed(speed);
    }

    private void ResetManualExit(PlayerAuthState auth)
    {
        var remaining = 1 - MathHelper.Clamp(auth.TradelaneProgress, 0, 1);
        manualExitTime = 0;
        manualStartSpeed = auth.LinearVelocity.Length();
        manualExitDuration = MathF.Max(1 / 60f, TradelaneMotion.ManualExitDuration * remaining);
        manualTurnDuration = MathF.Max(1 / 60f, TradelaneMotion.ManualTurnDuration * remaining);
        manualStartOrientation = auth.Orientation;
        manualTargetOrientation = TradelaneMotion.TurnRight(
            auth.Orientation, TradelaneMotion.ManualTurnDegrees * remaining);
    }

    private void SetEngineSpeed(float speed)
    {
        if (Parent.TryGetComponent<CEngineComponent>(out var engine))
        {
            engine.Speed = MathHelper.Clamp(speed / TradelaneMotion.Speed, 0, 1) * 0.9f;
        }
    }
}
