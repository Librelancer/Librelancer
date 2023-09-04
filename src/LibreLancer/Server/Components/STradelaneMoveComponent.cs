// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Numerics;
using LibreLancer.Client.Components;
using LibreLancer.Missions;
using LibreLancer.World;
using LibreLancer.World.Components;

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

        public void LaneEntered()
        {
            if (TryGetMissionRuntime(out var msn, out var player))
            {
                var cmp = currenttradelane.GetComponent<SDockableComponent>();

                msn.TradelaneEntered(
                    player ? "Player" : Parent.Nickname,
                    currenttradelane.Nickname,
                    lane == "HpRightLane" ? cmp.Action.Target : cmp.Action.TargetLeft
                );
            }
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
            bool tradeLaneDisrupted = tradelaneComponent.GetChildComponents<SShieldComponent>()
                .Any(c => c.Health == 0 && distance < 3000);

            var offset = Vector3.Zero;
            if (Parent.Formation != null)
            {
                offset = Parent.Formation.GetShipOffset(Parent);
            }

            var eng = Parent.GetComponent<CEngineComponent>();
            if (eng != null) eng.Speed = 0.9f;

            var targetPoint = Vector3.Transform(Vector3.Zero + offset, tradelaneComponent.GetHardpoint(lane).Transform * tradelaneComponent.WorldTransform);
            var direction = targetPoint - Parent.PhysicsComponent.Body.Position;
            var distance = direction.Length();
            if (distance < 200)
            {
                currenttradelane = tradelaneComponent;
                LaneEntered();
                return;
            }
            direction.Normalize();
            Parent.PhysicsComponent.Body.LinearVelocity = direction * 3000;

            //var currRot = Quaternion.From(Parent.PhysicsComponent.Body.Transform.ClearTranslation());
            var targetRot = QuaternionEx.LookAt(Parent.PhysicsComponent.Body.Position, targetPoint);
            //var slerped = Quaternion.Slerp(currRot, targetRot, 0.02f); //TODO: Slerp doesn't work?
            Parent.PhysicsComponent.Body.SetTransform(Matrix4x4.CreateFromQuaternion(targetRot) *
                                                      Matrix4x4.CreateTranslation(Parent.PhysicsComponent.Body.Position));

            HandleTradelaneDisruption(tradelaneComponent, distance);

        }

        private void HandleTradelaneDisruption(GameObject tradelaneComponent, float distance)
        {

            if(Parent.TryGetComponent<SPlayerComponent>(out var pc))
                TradeLaneDisruption(pc);
        }

        private void TradeLaneDisruption(SPlayerComponent pc)
        {
            TurnOnEngine();
            pc.Player.TradelaneDisrupted();
            Parent.Components.Remove(this);
        }

        private void ExitTradelane()
        {
            TurnOnEngine();
            if (Parent.TryGetComponent<SPlayerComponent>(out var player))
                player.Player.EndTradelane();

            if (TryGetMissionRuntime(out var msn, out var isPlayer) && msn is not null)
                msn.TradelaneExited(isPlayer ? "Player" : Parent.Nickname, currenttradelane.Nickname);

            Parent.Components.Remove(this);
        }

        private void TurnOnEngine()
        {
            var ctrl = Parent.GetComponent<ShipPhysicsComponent>();
            if (ctrl != null)
            {
                ctrl.EnginePower = 0.4f;
                ctrl.Active = true;
            }
        }
    }
}
