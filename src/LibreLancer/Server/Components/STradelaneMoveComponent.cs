// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using LibreLancer.Missions;
using LibreLancer.Net.Protocol;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Server.Components
{
    public class STradelaneMoveComponent : GameComponent
    {
        private GameObject currenttradelane;
        private readonly string lane;
        private readonly bool skipAutomaticSlowdown;

        private float entryOrientationTime;
        private readonly Quaternion entryStartOrientation;
        private readonly Quaternion entryTargetOrientation;

        private float totalTime;
        private TradelaneMoveState moveState = TradelaneMoveState.Transit;
        private float targetSpeed;

        private float slowdownTime;
        private float slowdownProgress;

        private float manualExitTime;
        private float manualStartSpeed;
        private Quaternion manualStartOrientation;
        private Quaternion manualTargetOrientation;

        public TradelaneMoveState MoveState => moveState;
        public float TargetSpeed => targetSpeed;
        public float Progress => moveState switch
        {
            TradelaneMoveState.Slowdown => slowdownProgress,
            TradelaneMoveState.ManualExit => MathHelper.Clamp(
                manualExitTime / TradelaneMotion.ManualExitDuration, 0, 1),
            _ => 0
        };

        public STradelaneMoveComponent(
            GameObject parent,
            GameObject tradelane,
            string lane,
            Quaternion targetOrientation,
            bool skipAutomaticSlowdown) : base(parent)
        {
            currenttradelane = tradelane;
            this.lane = lane;
            this.skipAutomaticSlowdown = skipAutomaticSlowdown;
            entryStartOrientation = parent.PhysicsComponent!.Body.Orientation;
            entryTargetOrientation = targetOrientation;
            targetSpeed = NormalThrottleSpeed();
        }

        private bool TryGetMissionRuntime([MaybeNullWhen(false)] out MissionRuntime msn, out bool player)
        {
            if (Parent.TryGetComponent<SPlayerComponent>(out var p) &&
                p.Player.MissionRuntime != null)
            {
                msn = p.Player.MissionRuntime;
                player = true;
                return true;
            }

            if (Parent.TryGetComponent<SNPCComponent>(out var npc) &&
                npc.MissionRuntime != null)
            {
                msn = npc.MissionRuntime;
                player = false;
                return true;
            }

            player = false;
            msn = null;
            return false;
        }

        public bool LaneEntered()
        {
            if (TryGetMissionRuntime(out var msn, out var isPlayer))
            {
                if (!currenttradelane.TryGetComponent<SDockableComponent>(out var cmp))
                {
                    return false;
                }

                msn.TradelaneEntered(
                    isPlayer ? "Player" : Parent.Nickname!,
                    currenttradelane.Nickname!,
                    (lane == "HpRightLane" ? cmp.Action.Target : cmp.Action.TargetLeft)!);
            }

            return true;
        }

        public bool RequestFreeFlight()
        {
            if (moveState == TradelaneMoveState.None || moveState == TradelaneMoveState.ManualExit)
            {
                return false;
            }

            // Story missions must remain on the scripted tradelane route.
            if (Parent.TryGetComponent<SPlayerComponent>(out var player) &&
                player.Player.Story?.CurrentMission != null)
            {
                return false;
            }

            var body = Parent.PhysicsComponent?.Body;
            if (body == null)
            {
                return false;
            }

            moveState = TradelaneMoveState.ManualExit;
            manualExitTime = 0;
            manualStartSpeed = body.LinearVelocity.Length();
            manualStartOrientation = body.Orientation;
            manualTargetOrientation = TradelaneMotion.TurnRight(
                manualStartOrientation, TradelaneMotion.ManualTurnDegrees);
            targetSpeed = NormalThrottleSpeed();
            return true;
        }

        private void DisruptOther(GameObject go)
        {
            if (go.TryGetComponent<STradelaneMoveComponent>(out var tlmov) &&
                tlmov.currenttradelane == currenttradelane)
            {
                tlmov.TradeLaneDisruption();
            }
        }

        public override void Update(double time, GameWorld world)
        {
            UpdateEntryOrientation(time);

            if (moveState == TradelaneMoveState.ManualExit)
            {
                UpdateManualExit(time);
                return;
            }

            var cmp = currenttradelane.GetComponent<SDockableComponent>()!;
            var tradelaneComponent = world.GetObject(lane == "HpRightLane" ? cmp.Action.Target : cmp.Action.TargetLeft);

            if (tradelaneComponent is null)
            {
                ExitTradelane();
                return;
            }

            var (laneDirection, distanceToTradelane) = CalculateNextTradelane(tradelaneComponent);

            if (TradelaneDisrupted(distanceToTradelane, tradelaneComponent))
            {
                // Do it to all the ships
                if (Parent.Formation != null)
                {
                    if (Parent.Formation.LeadShip != Parent)
                    {
                        DisruptOther(Parent.Formation.LeadShip);
                    }

                    foreach (var f in Parent.Formation.Followers)
                    {
                        if (f != Parent)
                        {
                            DisruptOther(f);
                        }
                    }
                }

                TradeLaneDisruption();
                return;
            }

            TryBeginSlowdown(tradelaneComponent, world);

            if (distanceToTradelane < 200)
            {
                currenttradelane = tradelaneComponent;
                if (NextRing(currenttradelane, world) != null &&
                    Parent.TryGetComponent<SPlayerComponent>(out var player))
                {
                    player.Player.TradelaneRing(currenttradelane);
                }
                var laneContinues = LaneEntered();
                if (!laneContinues)
                {
                    ExitTradelane();
                }

                return;
            }

            UpdateSlowdownProgress(time);
            MoveShip(time, laneDirection);
            totalTime += (float)time;
        }

        private void TryBeginSlowdown(GameObject nextRing, GameWorld world)
        {
            if (moveState != TradelaneMoveState.Transit ||
                !TradelaneMotion.CanStartAutomaticSlowdown(
                    IsFinal(nextRing, world),
                    skipAutomaticSlowdown))
            {
                return;
            }

            slowdownTime = 0;
            moveState = TradelaneMoveState.Slowdown;
        }

        private void UpdateEntryOrientation(double time)
        {
            if (entryOrientationTime >= TradelaneMotion.EntryAlignmentDuration)
            {
                return;
            }

            entryOrientationTime = MathF.Min(
                TradelaneMotion.EntryAlignmentDuration,
                entryOrientationTime + (float)time);
            var orientation = TradelaneMotion.EntryOrientation(
                entryStartOrientation,
                entryTargetOrientation,
                entryOrientationTime);
            var body = Parent.PhysicsComponent!.Body;
            body.SetOrientation(orientation);
            body.AngularVelocity = Vector3.Zero;
            if (Parent.TryGetComponent<SPlayerComponent>(out var player))
            {
                player.Player.Orientation = orientation;
            }
        }

        private void UpdateSlowdownProgress(double time)
        {
            if (moveState != TradelaneMoveState.Slowdown)
            {
                return;
            }

            slowdownTime += (float)time;
            slowdownProgress = MathHelper.Clamp(
                slowdownTime / TradelaneMotion.SlowdownDuration, 0, 1);
            targetSpeed = TradelaneMotion.SlowdownSpeed(slowdownTime);
        }

        private void MoveShip(double time, Vector3 direction)
        {
            if (direction.LengthSquared() <= float.Epsilon)
            {
                return;
            }

            direction.Normalize();
            var speed = moveState == TradelaneMoveState.Slowdown
                ? targetSpeed
                : TradelaneMotion.SpeedupSpeed(totalTime, NormalThrottleSpeed());

            targetSpeed = speed;

            var body = Parent.PhysicsComponent!.Body;
            body.LinearVelocity = direction * speed;
            body.AngularVelocity = Vector3.Zero;

            if (Parent.TryGetComponent<SEngineComponent>(out var engine))
            {
                engine.Speed = MathHelper.Clamp(speed / TradelaneMotion.Speed, 0, 1) * 0.9f;
            }
        }

        private void UpdateManualExit(double time)
        {
            manualExitTime += (float)time;
            var turnProgress = MathHelper.Clamp(manualExitTime / TradelaneMotion.ManualTurnDuration, 0, 1);
            var speedProgress = MathHelper.Clamp(manualExitTime / TradelaneMotion.ManualExitDuration, 0, 1);
            var orientation = TradelaneMotion.ManualExitOrientation(
                manualStartOrientation, manualTargetOrientation, turnProgress);
            var direction = TradelaneMotion.Forward(orientation);
            var speed = TradelaneMotion.ManualExitSpeed(speedProgress, manualStartSpeed, NormalThrottleSpeed());

            Parent.PhysicsComponent!.Body.SetOrientation(orientation);
            Parent.PhysicsComponent.Body.LinearVelocity = direction * speed;
            Parent.PhysicsComponent.Body.AngularVelocity = Vector3.Zero;

            if (Parent.TryGetComponent<SEngineComponent>(out var engine))
            {
                engine.Speed = MathHelper.Clamp(speed / TradelaneMotion.Speed, 0, 1) * 0.9f;
            }

            if (manualExitTime >= TradelaneMotion.ManualExitDuration)
            {
                ExitTradelane();
            }
        }

        private Vector3 CalculateTradelanePosition(GameObject tradelane)
        {
            var offset = Parent.Formation is not null
                ? Parent.Formation.GetShipOffset(Parent)
                : Vector3.Zero;

            return (tradelane.GetHardpoint(lane)!.TransformNoRotate * tradelane.WorldTransform)
                .Transform(offset);
        }

        private (Vector3 Direction, float Distance) CalculateNextTradelane(GameObject tradelaneComponent)
        {
            var targetPosition = CalculateTradelanePosition(tradelaneComponent);
            var laneDirection = targetPosition - CalculateTradelanePosition(currenttradelane);
            var distanceToTarget = Vector3.Distance(
                targetPosition,
                Parent.PhysicsComponent!.Body.Position);
            return (laneDirection, distanceToTarget);
        }

        private string? NextNickname(GameObject ring)
        {
            if (!ring.TryGetComponent<SDockableComponent>(out var dock))
            {
                return null;
            }

            return lane == "HpRightLane" ? dock.Action.Target : dock.Action.TargetLeft;
        }

        private GameObject? NextRing(GameObject ring, GameWorld world)
        {
            var nickname = NextNickname(ring);
            return nickname == null ? null : world.GetObject(nickname);
        }

        private bool IsFinal(GameObject ring, GameWorld world)
        {
            return NextRing(ring, world) == null;
        }

        private float NormalThrottleSpeed()
        {
            if (Parent.TryGetComponent<ShipPhysicsComponent>(out var physics) &&
                Parent.TryGetComponent<SEngineComponent>(out var engine))
            {
                return TradelaneMotion.NormalThrottleSpeed(physics.Ship, engine.Engine);
            }

            return TradelaneMotion.Speed;
        }

        private static bool TradelaneDisrupted(float distance, GameObject tradelaneComponent) =>
            distance < 3000 &&
            tradelaneComponent.TryGetFirstChildComponent<SShieldComponent>(out var comp) &&
            comp.Health < float.Epsilon;

        private void TradeLaneDisruption()
        {
            ExitTradelane();
            if (Parent!.TryGetComponent<SPlayerComponent>(out var pc))
            {
                pc.Player.TradelaneDisrupted();
            }
        }

        private void ExitTradelane()
        {
            if (Parent!.TryGetComponent<ShipPhysicsComponent>(out var ctrl))
            {
                ctrl.Active = true;
            }

            moveState = TradelaneMoveState.None;
            targetSpeed = 0;

            if (Parent.TryGetComponent<SPlayerComponent>(out var player))
            {
                player.Player.EndTradelane();
            }

            if (Parent.TryGetComponent<AutopilotComponent>(out var ap))
            {
                ap.Cancel();
                if (Parent.Formation != null && Parent.Formation.LeadShip != Parent)
                    ap.StartFormation();
            }

            if (TryGetMissionRuntime(out var msn, out var isPlayer))
            {
                msn.TradelaneExited(isPlayer ? "Player" : Parent.Nickname!, currenttradelane.Nickname!);
            }

            Parent.RemoveComponent(this);
        }
    }
}
