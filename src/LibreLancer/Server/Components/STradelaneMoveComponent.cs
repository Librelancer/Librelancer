// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Client.Components;
using LibreLancer.Missions;
using LibreLancer.World;
using LibreLancer.World.Components;
using System.Linq;
using System.Numerics;

namespace LibreLancer.Server.Components
{
    public class STradelaneMoveComponent : GameComponent
    {
        private const float TRADELANE_SPEED = 2500;
        private const float DISRUPTION_DISTANCE = 3000;

        GameObject currenttradelane;
        string lane;

        public STradelaneMoveComponent(GameObject parent, GameObject tradelane, string lane) : base(parent)
        {
            currenttradelane = tradelane;
            this.lane = lane;
        }

        bool TryGetMissionRuntime(out MissionRuntime msn, out bool player)
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
            if (TryGetMissionRuntime(out var msn, out var isPlayer) && msn is not null)
            {
                SDockableComponent cmp = currenttradelane.GetComponent<SDockableComponent>();
                if (cmp is null)
                    return false;

                msn.TradelaneEntered(
                    isPlayer ? "Player" : Parent.Nickname,
                    currenttradelane.Nickname,
                    lane == "HpRightLane" ? cmp.Action.Target : cmp.Action.TargetLeft
                );
            }

            return true;
        }

        void DisruptOther(GameObject go)
        {
            if (go.TryGetComponent<STradelaneMoveComponent>(out var tlmov) &&
                tlmov.currenttradelane == currenttradelane)
            {
                tlmov.TradeLaneDisruption();
            }
        }

        private float totalTime = 0;
        public override void Update(double time)
        {
            var cmp = currenttradelane.GetComponent<SDockableComponent>();
            var tradelaneComponent = Parent.GetWorld().GetObject(lane == "HpRightLane" ? cmp.Action.Target : cmp.Action.TargetLeft);

            if (tradelaneComponent is null)
            {
                ExitTradelane();
                return;
            }


            var (position, direction) = CalculateNextTradelane(tradelaneComponent);
            var distanceToTradelane = direction.Length();

            if (TradelaneDisrupted(distanceToTradelane, tradelaneComponent))
            {
                //Do it to all the ships
                if (Parent.Formation != null)
                {
                    if (Parent.Formation.LeadShip != Parent) {
                        DisruptOther(Parent.Formation.LeadShip);
                    }
                    foreach (var f in Parent.Formation.Followers) {
                        if(f != Parent)
                            DisruptOther(f);
                    }
                }
                TradeLaneDisruption();
                // tradelaneComponent.Parent.Formation.Remove(tradelaneComponent.Parent); TODO: Once formation triggers work or wandering npcs are added this can be tested.
                return;
            }
            else if (distanceToTradelane < 200)
            {
                currenttradelane = tradelaneComponent;
                if (!LaneEntered())
                    ExitTradelane();

                return;
            }

            MoveShip(CalculateCurrentTradelane(), position, direction);

            totalTime += (float)time;
        }

        private void MoveShip(Vector3 sourcePoint, Vector3 targetPoint, Vector3 direction)
        {
            direction.Normalize();
            var speed = Easing.Ease(EasingTypes.EaseIn, MathHelper.Clamp(totalTime, 0, 3), 0, 3, 0, TRADELANE_SPEED);
            Parent.PhysicsComponent.Body.LinearVelocity = direction * speed;
            Parent.PhysicsComponent.Body.AngularVelocity = Vector3.Zero;
            var targetRot = QuaternionEx.LookAt(sourcePoint, targetPoint);
            Parent.PhysicsComponent.Body.SetOrientation(targetRot);
        }

        private Vector3 CalculateCurrentTradelane()
        {
            var offset = Vector3.Zero;
            if (Parent.Formation is not null)
            {
                offset = Parent.Formation.GetShipOffset(Parent);
            }
            return (currenttradelane.GetHardpoint(lane).TransformNoRotate * currenttradelane.WorldTransform)
                .Transform(offset);
        }
        private (Vector3, Vector3) CalculateNextTradelane(GameObject tradelaneComponent)
        {
            var offset = Vector3.Zero;
            if (Parent.Formation is not null)
            {
                offset = Parent.Formation.GetShipOffset(Parent);
            }

            CEngineComponent eng = Parent.GetComponent<CEngineComponent>();
            if (eng is not null)
                eng.Speed = 0.9f;

            var targetPosition =
                (tradelaneComponent.GetHardpoint(lane).TransformNoRotate * tradelaneComponent.WorldTransform)
                .Transform(offset);
            var direction = (targetPosition - Parent.PhysicsComponent.Body.Position);

            return (targetPosition, direction);
        }

        private static bool TradelaneDisrupted(float distance, GameObject tradelaneComponent) =>
            distance < DISRUPTION_DISTANCE &&
            tradelaneComponent.GetChildComponents<SShieldComponent>()
                .Any(c => c.Health == 0);


        private void TradeLaneDisruption()
        {
            ExitTradelane();
            if (Parent.TryGetComponent<SPlayerComponent>(out var pc))
                pc.Player.TradelaneDisrupted();
        }

        private void ExitTradelane()
        {
            var ctrl = Parent.GetComponent<ShipPhysicsComponent>();
            if (ctrl != null)
            {
                ctrl.EnginePower = 0.4f;
                ctrl.Active = true;
            }
            if (Parent.TryGetComponent<SPlayerComponent>(out var player))
                player.Player.EndTradelane();
            if(Parent.TryGetComponent<AutopilotComponent>(out var ap))
                ap.Cancel();
            if (TryGetMissionRuntime(out var msn, out var isPlayer) && msn is not null)
                msn.TradelaneExited(isPlayer ? "Player" : Parent.Nickname, currenttradelane.Nickname);
            Parent.RemoveComponent(this);
        }
    }
}
