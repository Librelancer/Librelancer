// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

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
        GameObject currenttradelane;
        string lane;

        public STradelaneMoveComponent(GameObject parent, GameObject tradelane, string lane) : base(parent)
        {
            currenttradelane = tradelane;
            this.lane = lane;
        }

        bool TryGetMissionRuntime(out MissionRuntime? msn, out bool player)
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
            if (TryGetMissionRuntime(out var msn, out var player) && msn is not null)
            {
                SDockableComponent? cmp = currenttradelane.GetComponent<SDockableComponent>();
                if (cmp is null)
                    return false;

                msn.TradelaneEntered(
                    player ? "Player" : Parent.Nickname,
                    currenttradelane.Nickname,
                    lane == "HpRightLane" ? cmp.Action.Target : cmp.Action.TargetLeft
                );
            }

            return true;
        }

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
                TradeLaneDisruption();
                return;
            }
            else if (distanceToTradelane < 200)
            {
                currenttradelane = tradelaneComponent;
                if (!LaneEntered())
                    ExitTradelane();

                return;
            }

            MoveShip(position, direction);
        }

        private void MoveShip(Vector3 targetPoint, Vector3 direction)
        {
            direction.Normalize();
            Parent.PhysicsComponent.Body.LinearVelocity = direction * 3000;

            //var currRot = Quaternion.From(Parent.PhysicsComponent.Body.Transform.ClearTranslation());
            var targetRot = QuaternionEx.LookAt(Parent.PhysicsComponent.Body.Position, targetPoint);
            //var slerped = Quaternion.Slerp(currRot, targetRot, 0.02f); //TODO: Slerp doesn't work?
            Parent.PhysicsComponent.Body.SetTransform(Matrix4x4.CreateFromQuaternion(targetRot) *
                                                      Matrix4x4.CreateTranslation(Parent.PhysicsComponent.Body.Position));
        }

        private (Vector3, Vector3) CalculateNextTradelane(GameObject tradelaneComponent)
        {
            var offset = Vector3.Zero;
            if (Parent.Formation is not null)
                offset = Parent.Formation.GetShipOffset(Parent);

            CEngineComponent? eng = Parent.GetComponent<CEngineComponent>();
            if (eng is not null)
                eng.Speed = 0.9f;

            var targetPosition = Vector3.Transform(Vector3.Zero + offset, tradelaneComponent.GetHardpoint(lane).Transform * tradelaneComponent.WorldTransform);
            var direction = targetPosition - Parent.PhysicsComponent.Body.Position;
            return (targetPosition, direction);
        }

        private static bool TradelaneDisrupted(float distance, GameObject tradelaneComponent) =>
            distance < 3000 && 
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

            if (TryGetMissionRuntime(out var msn, out var isPlayer) && msn is not null)
                msn.TradelaneExited(isPlayer ? "Player" : Parent.Nickname, currenttradelane.Nickname);

            Parent.Components.Remove(this);
        }
    }
}
