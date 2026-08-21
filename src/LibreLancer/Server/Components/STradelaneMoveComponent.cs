// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

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

        private float totalTime;
        private TradelaneMoveState moveState = TradelaneMoveState.Transit;
        private float targetSpeed = TradelaneMotion.Speed;

        private GameObject? penultimateRing;
        private GameObject? finalRing;
        private float slowdownRouteDistance;
        private float slowdownProgress;
        private bool automaticSlowdownArmed;

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

        public STradelaneMoveComponent(GameObject parent, GameObject tradelane, string lane) : base(parent)
        {
            currenttradelane = tradelane;
            this.lane = lane;
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

            var (position, direction) = CalculateNextTradelane(tradelaneComponent);
            var distanceToTradelane = direction.Length();

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

            if (distanceToTradelane > TradelaneMotion.SlowdownStartDistance)
            {
                automaticSlowdownArmed = true;
            }

            TryBeginSlowdown(tradelaneComponent, distanceToTradelane, world);

            if (distanceToTradelane < 200)
            {
                // Ensure the final ring is reached at the target speed before
                // handing the ship back to normal physics.
                if (moveState == TradelaneMoveState.Slowdown && tradelaneComponent == finalRing)
                {
                    slowdownProgress = 1;
                    targetSpeed = TradelaneMotion.SlowdownSpeed(1, NormalThrottleSpeed());
                    MoveShip(CalculateCurrentTradelane(), position, direction);
                }

                currenttradelane = tradelaneComponent;
                if (!LaneEntered())
                {
                    ExitTradelane();
                }

                automaticSlowdownArmed = false;
                return;
            }

            UpdateSlowdownProgress(tradelaneComponent, distanceToTradelane);
            MoveShip(CalculateCurrentTradelane(), position, direction);
            totalTime += (float)time;
        }

        private void TryBeginSlowdown(GameObject nextRing, float distanceToNextRing, GameWorld world)
        {
            if (moveState != TradelaneMoveState.Transit ||
                !TradelaneMotion.CanStartAutomaticSlowdown(
                    distanceToNextRing,
                    automaticSlowdownArmed,
                    IsPenultimate(nextRing, world)))
            {
                return;
            }

            var lastRing = NextRing(nextRing, world);
            if (lastRing == null)
            {
                return;
            }

            penultimateRing = nextRing;
            finalRing = lastRing;
            var penultimatePosition = CalculateTradelanePosition(penultimateRing);
            var finalPosition = CalculateTradelanePosition(finalRing);
            slowdownRouteDistance = distanceToNextRing + Vector3.Distance(penultimatePosition, finalPosition);

            if (slowdownRouteDistance > float.Epsilon)
            {
                moveState = TradelaneMoveState.Slowdown;
            }
        }

        private void UpdateSlowdownProgress(GameObject nextRing, float distanceToNextRing)
        {
            if (moveState != TradelaneMoveState.Slowdown ||
                penultimateRing == null ||
                finalRing == null ||
                slowdownRouteDistance <= float.Epsilon)
            {
                return;
            }

            var remaining = distanceToNextRing;
            if (nextRing == penultimateRing)
            {
                remaining += Vector3.Distance(
                    CalculateTradelanePosition(penultimateRing),
                    CalculateTradelanePosition(finalRing));
            }

            var progress = 1 - MathHelper.Clamp(remaining / slowdownRouteDistance, 0, 1);
            slowdownProgress = progress;
            targetSpeed = TradelaneMotion.SlowdownSpeed(progress, NormalThrottleSpeed());
        }

        private void MoveShip(Vector3 sourcePoint, Vector3 targetPoint, Vector3 direction)
        {
            if (direction.LengthSquared() <= float.Epsilon)
            {
                return;
            }

            direction.Normalize();
            var speed = moveState == TradelaneMoveState.Slowdown
                ? targetSpeed
                : Easing.Ease(EasingTypes.EaseIn, MathHelper.Clamp(totalTime, 0, 3), 0, 3, 0, TradelaneMotion.Speed);

            Parent.PhysicsComponent!.Body.LinearVelocity = direction * speed;
            Parent.PhysicsComponent.Body.AngularVelocity = Vector3.Zero;
            Parent.PhysicsComponent.Body.SetOrientation(QuaternionEx.LookAt(sourcePoint, targetPoint));

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

        private Vector3 CalculateCurrentTradelane() => CalculateTradelanePosition(currenttradelane);

        private Vector3 CalculateTradelanePosition(GameObject tradelane)
        {
            var offset = Parent.Formation is not null
                ? Parent.Formation.GetShipOffset(Parent)
                : Vector3.Zero;

            return (tradelane.GetHardpoint(lane)!.TransformNoRotate * tradelane.WorldTransform)
                .Transform(offset);
        }

        private (Vector3, Vector3) CalculateNextTradelane(GameObject tradelaneComponent)
        {
            var targetPosition = CalculateTradelanePosition(tradelaneComponent);
            var direction = targetPosition - Parent.PhysicsComponent!.Body.Position;
            return (targetPosition, direction);
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

        private bool IsPenultimate(GameObject ring, GameWorld world)
        {
            var final = NextRing(ring, world);
            return final != null && NextRing(final, world) == null;
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
            }

            if (TryGetMissionRuntime(out var msn, out var isPlayer))
            {
                msn.TradelaneExited(isPlayer ? "Player" : Parent.Nickname!, currenttradelane.Nickname!);
            }

            Parent.RemoveComponent(this);
        }
    }
}
