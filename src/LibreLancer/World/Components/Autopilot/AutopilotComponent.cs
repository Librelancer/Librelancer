// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer.Client.Components;
using LibreLancer.Data.GameData.World;
using LibreLancer.Net.Protocol;

namespace LibreLancer.World.Components
{
    public enum AutopilotBehaviors
    {
        None,
        Goto,
        Dock,
        Formation,
        Undock
    }

    internal abstract class AutopilotBehavior
    {
        private bool hasTriggeredCruise = false;
        protected readonly AutopilotObstacleAvoidance avoidance = new();

        public abstract AutopilotBehaviors Behavior { get; }

        public virtual bool DockCameraActive => false;

        protected AutopilotBehavior(AutopilotComponent component)
        {
            Component = component;
        }

        protected readonly AutopilotComponent Component;
        protected GameObject Parent => Component.Parent;

        public abstract void ImGuiDebug();

        public abstract bool Update(
            ShipSteeringComponent control,
            ShipInputComponent? input,
            double time,
            GameWorld world);

        private void Start(GotoKind kind, float maxThrottle, float gotoRadius)
        {
            CanCruise = kind != GotoKind.GotoNoCruise;
            MaxThrottle = maxThrottle;
            GotoRadius = gotoRadius;
        }

        public void Start(GotoKind kind,
            GameObject targetObject,
            float maxThrottle,
            float gotoRadius,
            bool shouldStopAtTarget = true)
        {
            TargetObject = targetObject;
            ShouldStopAtTarget = shouldStopAtTarget;
            Start(kind, maxThrottle, gotoRadius);
        }

        public void Start(GotoKind kind,
            Vector3 targetPosition,
            float maxThrottle,
            float gotoRadius,
            bool shouldStopAtTarget = true)
        {
            _targetPosition = targetPosition;
            _targetRadius = 5;
            ShouldStopAtTarget = shouldStopAtTarget;
            Start(kind, maxThrottle, gotoRadius);
        }

        protected bool CanCruise;

        protected GameObject? TargetObject;
        private Vector3 _targetPosition;
        private float _targetRadius;

        protected float MaxThrottle;
        protected float GotoRadius;
        protected bool ShouldStopAtTarget = true;

        protected void SetThrottle(float throttle, ShipSteeringComponent control, ShipInputComponent? input)
        {
            input?.AutopilotThrottle = throttle;
            control.InThrottle = throttle;
        }

        protected void TriggerCruise(ShipSteeringComponent control, bool shouldCruise)
        {
            if (hasTriggeredCruise)
            {
                return;
            }

            hasTriggeredCruise = true;
            control.Cruise = CanCruise && shouldCruise;
            hasTriggeredCruise = true;
        }

        protected bool TargetValid() => TargetObject == null ||
                                        TargetObject.Flags.HasFlag(GameObjectFlags.Exists);

        protected bool Dockable(out DockInfoComponent? dockInfoComponent)
        {
            if (TargetObject != null)
            {
                return TargetObject.TryGetComponent(out dockInfoComponent);
            }

            dockInfoComponent = null;
            return false;
        }

        protected Vector3 GetTargetPoint()
        {
            return TargetObject == null ? _targetPosition : TargetObject.WorldTransform.Position;
        }

        protected float GetTargetRadius()
        {
            if (TargetObject == null)
            {
                return _targetRadius;
            }

            return TargetObject.PhysicsComponent!.Body!.Collider.Radius;
        }

        protected Hardpoint? GetTargetHardpoint(DockInfoComponent docking, bool reverse, int index, int dockIndex = 0)
        {
            var hps = docking.GetDockHardpoints(Parent.PhysicsComponent!.Body!.Position, dockIndex);

            if (reverse)
            {
                hps = hps.Reverse();
            }

            return hps.Skip(index).FirstOrDefault();
        }

        protected bool TurnTowards(double time, Vector3 targetPoint)
        {
            // Orientation
            var dt = time;
            var vec = Parent.InverseTransformPoint(targetPoint);
            // normalize it
            vec.Normalize();

            var directionSatisfied = (Math.Abs(vec.X) < 0.0015f && Math.Abs(vec.Y) < 0.0015f);

            if (!directionSatisfied)
            {
                Component.OutYaw = MathHelper.Clamp((float)Component.YawControl.Update(0, vec.X, dt), -1, 1);
                Component.OutPitch = MathHelper.Clamp((float)Component.PitchControl.Update(0, -vec.Y, dt), -1, 1);
                return false;
            }

            Component.OutYaw = 0;
            Component.OutPitch = 0;
            return true;
        }

        protected bool MoveToPoint(
            double time,
            Vector3 point,
            float radius,
            float range,
            float maxSpeed,
            bool shouldStop,
            ShipSteeringComponent control,
            ShipInputComponent? input,
            GameWorld world,
            bool includeTargetRadius = true)
        {
            float targetPower = 0;
            // Bring ship to within GotoRange metres of target
            var targetRadius = includeTargetRadius ? GetTargetRadius() : 0;
            var myRadius = Parent.PhysicsComponent!.Body!.Collider.Radius;
            var distance = (point - Parent.PhysicsComponent.Body.Position).Length();

            TriggerCruise(control, (distance - range) > 2000);

            if (shouldStop && (distance - range) < 500)
            {
                control.Cruise = false; // Disable cruise at small distance
            }

            var distrad = targetRadius + myRadius + radius + range;
            var distanceSatisfied = distrad >= distance;

            if (distanceSatisfied && shouldStop)
            {
                targetPower = 0;
            }
            else
            {
                targetPower = maxSpeed;
            }

            if (targetPower > maxSpeed)
            {
                targetPower = maxSpeed;
            }

            input?.AutopilotThrottle = targetPower;
            control.InThrottle = targetPower;

            if (distanceSatisfied)
                return true;

            if (Behavior != AutopilotBehaviors.Undock)
            {
                var avoidancePlan = avoidance.GetPlan(world, Parent, TargetObject, point, targetRadius + radius + range,
                    time);
                Component.SetAutopilotStrafe(avoidancePlan.Strafe, avoidancePlan.StrafeVector);
            }

            var directionSatisfied = TurnTowards(time, point);


            return distanceSatisfied && directionSatisfied;
        }

    }

    internal sealed class DockBehavior(AutopilotComponent c, int dockIndex) : AutopilotBehavior(c)
    {
        public override AutopilotBehaviors Behavior => AutopilotBehaviors.Dock;
        public override bool DockCameraActive => ringDocking;

        private int lastTargetHp = 0;
        private bool ringDocking = false;
        private double ringDockTime = 0;

        private static bool IsDockingRingIndex(DockInfoComponent docking, int index) =>
            docking.Action.Kind == DockKinds.Base &&
            index == 0 &&
            index < docking.Spheres.Length &&
            docking.Spheres[index].Type == Data.Schema.Solar.DockSphereType.ring;

        private void StartRingFlyThrough(ShipSteeringComponent control, ShipInputComponent? input)
        {
            if (Parent.TryGetComponent<ShipPhysicsComponent>(out var physics))
            {
                physics.Active = false;
            }

            Parent.PhysicsComponent!.Collidable = false;
            Parent.PhysicsComponent.Body.Collidable = false;
            Parent.PhysicsComponent.Body.LinearVelocity =
                Vector3.Transform(-Vector3.UnitZ, Parent.PhysicsComponent.Body.Orientation) * 80;
            ringDocking = true;
            ringDockTime = 3.0;
            SetThrottle(1, control, input);
        }

        public override void ImGuiDebug()
        {
            if (Dockable(out var docking))
            {
                ImGui.Text($"Docking with: {docking?.Parent}");
                var hp = GetTargetHardpoint(docking!, false, lastTargetHp, dockIndex);
                ImGui.Text($"Target hardpoint: {hp?.Name}");
            }
        }

        public override bool Update(ShipSteeringComponent control, ShipInputComponent? input, double time, GameWorld world)
        {
            if (!TargetValid() || !Dockable(out var docking))
            {
                return true; // finished
            }

            if (ringDocking)
            {
                SetThrottle(1, control, input);
                ringDockTime -= time;
                return ringDockTime <= 0;
            }

            var dock = docking!;
            var hp = GetTargetHardpoint(dock, false, lastTargetHp, dockIndex);

            if (hp == null)
            {
                // No dock hardpoints available, cancel docking
                FLLog.Error("Autopilot", "No dock hardpoints available for docking");
                return true; // finished
            }

            var isTradelane = dock.Action.Kind == DockKinds.Tradelane;
            var radius = isTradelane || lastTargetHp == 2 ? dock.GetTriggerRadius(dockIndex) : 5;
            var targetPoint = (hp.Transform * TargetObject!.WorldTransform).Position;
            var isDockingRing = IsDockingRingIndex(dock, dockIndex);

            var d2 = (targetPoint - Parent.PhysicsComponent!.Body!.Position).Length();

            if (isDockingRing &&
                hp.Name.Equals(dock.Spheres[dockIndex].Hardpoint, StringComparison.OrdinalIgnoreCase) &&
                d2 <= Math.Max(250, dock.GetTriggerRadius(dockIndex) + Parent.PhysicsComponent.Body.Collider.Radius))
            {
                StartRingFlyThrough(control, input);
                return false;
            }

            var maxSpeed = lastTargetHp > 0 || d2 < 80 ? 0.3f : 1f;
            if (!MoveToPoint(time, targetPoint, radius, 0, maxSpeed, true, control, input, world, false))
            {
                return false; // not finished
            }

            if (isTradelane)
            {
                return false; // wait for the server to start the tradelane
            }

            if (lastTargetHp < 2)
            {
                lastTargetHp++;
            }
            else if (isDockingRing)
            {
                StartRingFlyThrough(control, input);
            }
            else
            {
                SetThrottle(1, control, input);
            }

            return false; // not finished
        }
    }

    internal sealed class UndockBehavior(AutopilotComponent c, int index, double initialDelay) : AutopilotBehavior(c)
    {
        public override AutopilotBehaviors Behavior => AutopilotBehaviors.Undock;

        private const double MAX_TIME_UNDOCK = 30.0;

        private double totalTime = 0.0;
        private double delay = initialDelay;
        private int targetHp = 1;

        public override void ImGuiDebug()
        {
            ImGui.Text("Undocking");
        }

        public override bool Update(ShipSteeringComponent control,
            ShipInputComponent? input,
            double time,
            GameWorld world)
        {
            if (delay > 0)
            {
                SetThrottle(0, control, input);
                Component.OutPitch = 0;
                Component.OutYaw = 0;
                delay -= time;
                return false; // not finished
            }

            totalTime += time;

            if (totalTime > MAX_TIME_UNDOCK)
            {
                FLLog.Warning("Autopilot", $"Undock force quit at {totalTime}");
                return true; // finished
            }

            if (!TargetValid() ||
                !Dockable(out var docking))
            {
                return true; // finished
            }

            var hps = docking!.GetDockHardpoints(Parent.PhysicsComponent!.Body!.Position, index).Reverse().ToArray();
            if (hps.Length < 2)
            {
                var info = docking.GetUndockInfo(index);
                var fallbackPoint = (info.End!.Transform * TargetObject!.WorldTransform).Position;
                return MoveToPoint(time, fallbackPoint, 25, 0, 1f, false, control, input, world, false);
            }

            if (targetHp >= hps.Length)
            {
                return true;
            }

            var targetPoint = (hps[targetHp].Transform * TargetObject!.WorldTransform).Position;
            if (!MoveToPoint(time, targetPoint, 25, 0, 1f, false, control, input, world, false))
            {
                return false;
            }

            targetHp++;
            if (targetHp < hps.Length)
            {
                return false;
            }

            return true;
        }
    }

    internal sealed class GotoBehavior(AutopilotComponent c) : AutopilotBehavior(c)
    {
        public override AutopilotBehaviors Behavior => AutopilotBehaviors.Goto;

        public override bool Update(ShipSteeringComponent control, ShipInputComponent? input, double time, GameWorld world)
        {
            if (!TargetValid())
            {
                return true;
            }

            return MoveToPoint(time, GetTargetPoint(), GetTargetRadius(), GotoRadius, MaxThrottle, ShouldStopAtTarget, control,
                input, world);
        }

        public override void ImGuiDebug()
        {
            ImGui.Text($"Goto: {GetTargetPoint()}");
        }
    }

    internal sealed class FormationBehavior(AutopilotComponent c) : AutopilotBehavior(c)
    {
        public override AutopilotBehaviors Behavior => AutopilotBehaviors.Formation;

        private const float CruiseDampenDistance = 250;
        private const float ThrottleDampenDistance = 125;
        private const float LeaderCruiseCatchupDistance = 450;
        private const float FormationSpeedBoost = 0.2f;
        private const float MaxFormationCruiseSpeedReduction = 5;
        private const float FormationFacingDistance = 100;
        private const float ThrottleMatchDistance = 100;
        private const float MinCatchupThrottle = 0.35f;

        private static bool LeadIsCruising(GameObject lead)
        {
            if (lead.TryGetComponent<ShipPhysicsComponent>(out var leadControl) &&
                (leadControl.CruiseEnabled ||
                 leadControl.EngineState is EngineStates.Cruise or EngineStates.CruiseCharging))
            {
                return true;
            }

            return lead.TryGetComponent<CEngineComponent>(out var eng) &&
                   eng.CruiseThrust is CruiseThrustState.Cruising or CruiseThrustState.CruiseCharging;
        }

        private static float LeadThrottle(GameObject lead)
        {
            if (lead.TryGetComponent<ShipPhysicsComponent>(out var leadControl))
            {
                return leadControl.EnginePower;
            }

            if (lead.TryGetComponent<CEngineComponent>(out var eng))
            {
                return MathHelper.Clamp(eng.Speed / 0.9f, 0, 1);
            }

            return 0;
        }

        private static float ApproachThrottle(float distance, float leadThrottle)
        {
            if (distance > ThrottleDampenDistance)
            {
                return 1 + FormationSpeedBoost;
            }

            if (distance > ThrottleMatchDistance)
            {
                var throttle = MathHelper.Lerp(MinCatchupThrottle, 1, distance / ThrottleDampenDistance);
                return MathF.Max(leadThrottle, throttle);
            }

            return leadThrottle;
        }

        private static float CruiseSpeedOffset(float distance)
        {
            var dampen = 1 - MathHelper.Clamp(distance / CruiseDampenDistance, 0, 1);
            return -MaxFormationCruiseSpeedReduction * dampen;
        }

        private static Vector3 ShipPosition(GameObject ship) =>
            ship.PhysicsComponent?.Body?.Position ?? ship.WorldTransform.Position;

        private static float ShipSpeed(GameObject ship)
        {
            return ship.PhysicsComponent?.Body?.LinearVelocity.Length() ?? 0;
        }

        private bool ShouldCruise(GameObject lead)
        {
            var leaderDistance = Vector3.Distance(ShipPosition(lead), Parent.PhysicsComponent!.Body!.Position);
            return LeadIsCruising(lead) || leaderDistance > LeaderCruiseCatchupDistance;
        }

        private static Vector3? FormationFacingPoint(GameObject self, GameObject lead, Vector3 targetPoint, float distance)
        {
            if (distance > FormationFacingDistance)
            {
                return targetPoint;
            }

            if (ShipSpeed(self) > ShipSpeed(lead) + 20)
            {
                return targetPoint;
            }

            var leadVelocity = lead.PhysicsComponent?.Body?.LinearVelocity ?? Vector3.Zero;
            if (leadVelocity.LengthSquared() > 400)
            {
                return self.WorldTransform.Position + Vector3.Normalize(leadVelocity) * 1000;
            }

            return null;
        }

        public override void ImGuiDebug()
        {
            ImGui.Text("In Formation");
        }

        public override bool Update(ShipSteeringComponent control, ShipInputComponent? input, double time, GameWorld world)
        {
            if (Parent.Formation == null ||
                Parent.Formation.LeadShip == Parent)
            {
                return true;
            }

            var body = Parent.PhysicsComponent!.Body!;
            var targetPoint = Parent.Formation.GetShipPosition(Parent, Component.LocalPlayer);
            var distance = (targetPoint - body.Position).Length();
            var lead = Parent.Formation.LeadShip;
            var leadThrottle = LeadThrottle(lead);
            var selfPhysics = Parent.GetComponent<ShipPhysicsComponent>();

            control.Cruise = ShouldCruise(lead);
            control.CruiseSpeedOffset =
                control.Cruise &&
                selfPhysics?.EngineState == EngineStates.Cruise &&
                distance < CruiseDampenDistance
                    ? CruiseSpeedOffset(distance)
                    : 0;
            SetThrottle(ApproachThrottle(distance, leadThrottle), control, input);

            var avoidancePlan = avoidance.GetPlan(world, Parent, TargetObject, targetPoint, ThrottleDampenDistance,
                time);
            Component.SetAutopilotStrafe(avoidancePlan.Strafe, avoidancePlan.StrafeVector);

            Vector3? facingPoint = FormationFacingPoint(Parent, lead, targetPoint, distance);
            if (facingPoint != null)
            {
                TurnTowards(time, facingPoint.Value);
            }
            else
            {
                Component.OutYaw = 0;
                Component.OutPitch = 0;
            }

            return false;
        }
    }


    public class AutopilotComponent : GameComponent
    {
        private AutopilotBehavior? instance;
        public bool LocalPlayer = false;
        internal StrafeControls AutopilotStrafe { get; private set; } = StrafeControls.None;

        public AutopilotBehaviors CurrentBehavior
            => instance?.Behavior ?? AutopilotBehaviors.None;

        public bool DockCameraActive => instance?.DockCameraActive ?? false;

        public readonly PIDController PitchControl = new();
        public readonly PIDController YawControl = new();

        public float OutPitch;
        public float OutYaw;

        public AutopilotComponent(GameObject parent) : base(parent)
        {
            PitchControl.P = 4;
            YawControl.P = 4;
        }

        public delegate void BehaviorChangedCallback(AutopilotBehaviors newBehavior, AutopilotBehaviors oldBehavior);

        public BehaviorChangedCallback? BehaviorChanged;

        private void SetInstance(AutopilotBehavior? i)
        {
            var old = CurrentBehavior;
            instance = i;

            if (CurrentBehavior != old)
            {
                BehaviorChanged?.Invoke(CurrentBehavior, old);
            }
        }

        public void ImGuiDebug()
        {
            ImGui.Text($"Autopilot: {CurrentBehavior}");
            instance?.ImGuiDebug();
        }

        public void GotoVec(Vector3 vec, GotoKind kind, float maxThrottle = 1, float gotoRange = 40, bool shouldStopAtTarget = true)
        {
            SetInstance(new GotoBehavior(this));
            instance?.Start(kind, vec, maxThrottle, gotoRange, shouldStopAtTarget);
        }

        public void GotoObject(GameObject obj, GotoKind kind, float maxThrottle = 1, float gotoRange = 40, bool shouldStopAtTarget = true)
        {
            SetInstance(new GotoBehavior(this));
            instance?.Start(kind, obj, maxThrottle, gotoRange, shouldStopAtTarget);
        }

        public void Cancel()
        {
            SetInstance(null);
            SetAutopilotStrafe(StrafeControls.None, Vector2.Zero);
        }

        public void StartDock(GameObject target, GotoKind kind, int dockIndex = 0)
        {
            SetInstance(new DockBehavior(this, dockIndex));
            instance?.Start(kind, target, 1, 40);
        }

        public void Undock(GameObject target, int index)
        {
            SetInstance(new UndockBehavior(this, index, GetUndockDelay(target, index)));
            instance?.Start(GotoKind.GotoNoCruise,
                target, 1, 10);
        }

        private static double GetUndockDelay(GameObject target, int index)
        {
            if (!target.TryGetComponent<DockInfoComponent>(out var docking) ||
                index < 0 ||
                index >= docking.Spheres.Length ||
                docking.Spheres[index].Type != Data.Schema.Solar.DockSphereType.berth)
            {
                return 1.2;
            }

            var duration = target.AnimationComponent?.GetAnimationDuration(docking.Spheres[index].Script) ?? 0;
            return Math.Max(1.2, duration);
        }

        public void StartFormation()
        {
            if (Parent.TryGetComponent<ShipPhysicsComponent>(out var physics))
            {
                physics.StopCruise();
            }

            if (Parent.TryGetComponent<ShipSteeringComponent>(out var steering))
            {
                steering.Cruise = false;
                steering.CruiseSpeedOffset = 0;
                steering.InThrottle = 1;
            }

            if (Parent.TryGetComponent<ShipInputComponent>(out var input))
            {
                input.AutopilotThrottle = 1;
                input.InFormation = true;
            }

            SetInstance(new FormationBehavior(this));
            instance?.Start(GotoKind.Goto, Vector3.Zero, 1, 10);
        }

        public override void Update(double time, GameWorld world)
        {
            var control = Parent?.GetComponent<ShipSteeringComponent>();
            var input = Parent?.GetComponent<ShipInputComponent>();
            SetAutopilotStrafe(StrafeControls.None, Vector2.Zero);

            if (input != null)
            {
                input.AutopilotThrottle = 0;
                input.InFormation = CurrentBehavior == AutopilotBehaviors.Formation;
            }

            if (control == null)
            {
                return;
            }

            control.CruiseSpeedOffset = 0;

            if (instance == null)
            {
                return;
            }

            if (instance.Update(control, input, time, world))
            {
                SetInstance(null);
                SetAutopilotStrafe(StrafeControls.None, Vector2.Zero);
            }
        }

        internal void SetAutopilotStrafe(StrafeControls strafe)
        {
            SetAutopilotStrafe(strafe, Vector2.Zero);
        }

        internal void SetAutopilotStrafe(StrafeControls strafe, Vector2 _)
        {
            AutopilotStrafe = strafe;
        }
    }
}
