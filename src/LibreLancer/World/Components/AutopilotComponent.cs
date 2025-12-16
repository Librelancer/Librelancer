// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Numerics;
using LibreLancer.Client.Components;

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

    abstract class AutopilotBehavior
    {
        private bool hasTriggeredCruise = false;

        public abstract AutopilotBehaviors Behavior { get; }

        protected AutopilotBehavior(AutopilotComponent component)
        {
            Component = component;
        }

        protected AutopilotComponent Component;
        protected GameObject Parent => Component.Parent;

        public abstract bool Update(
            ShipSteeringComponent control,
            ShipInputComponent input,
            double time);

        private void Start(GotoKind kind, float maxThrottle, float gotoRadius)
        {
            CanCruise = kind != GotoKind.GotoNoCruise;
            MaxThrottle = maxThrottle;
            GotoRadius = gotoRadius;
        }

        public void Start(GotoKind kind,
            GameObject targetObject,
            float maxThrottle,
            float gotoRadius)
        {
            TargetObject = targetObject;
            Start(kind, maxThrottle, gotoRadius);
        }

        public void Start(GotoKind kind,
            Vector3 targetPosition,
            float maxThrottle,
            float gotoRadius)
        {
            _targetPosition = targetPosition;
            _targetRadius = 5;
            Start(kind, maxThrottle, gotoRadius);
        }

        protected bool CanCruise;

        protected GameObject TargetObject;
        private Vector3 _targetPosition;
        private float _targetRadius;

        protected float MaxThrottle;
        protected float GotoRadius;

        protected void SetThrottle(float throttle, ShipSteeringComponent control, ShipInputComponent input)
        {
            if (input != null) input.AutopilotThrottle = throttle;
            control.InThrottle = throttle;
        }

        protected void TriggerCruise(ShipSteeringComponent control, bool shouldCruise)
        {
            if (!hasTriggeredCruise)
            {
                hasTriggeredCruise = true;
                if (!CanCruise)
                    control.Cruise = false;
                else
                    control.Cruise = shouldCruise;
                hasTriggeredCruise = true;
            }
        }

        protected bool TargetValid() => TargetObject == null ||
                              TargetObject.Flags.HasFlag(GameObjectFlags.Exists);

        protected bool Dockable(out DockInfoComponent dockInfoComponent)
        {
            if (TargetObject == null)
            {
                dockInfoComponent = null;
                return false;
            }
            return TargetObject.TryGetComponent<DockInfoComponent>(out dockInfoComponent);
        }

        protected Vector3 GetTargetPoint()
        {
            if (TargetObject == null) return _targetPosition;
            return TargetObject.WorldTransform.Position;
        }

        protected float GetTargetRadius()
        {
            if (TargetObject == null) return _targetRadius;
            return TargetObject.PhysicsComponent.Body.Collider.Radius;
        }

        protected Hardpoint GetTargetHardpoint(DockInfoComponent docking, bool reverse, int index)
        {
            var hps =
                docking.GetDockHardpoints(Parent.PhysicsComponent.Body.Position);
            if (reverse)
                hps = hps.Reverse();
            return hps.Skip(index).FirstOrDefault();
        }

        protected bool TurnTowards(double time, Vector3 targetPoint)
        {
            //Orientation
            var dt = time;
            var vec = Parent.InverseTransformPoint(targetPoint);
            //normalize it
            vec.Normalize();
            //
            bool directionSatisfied = (Math.Abs(vec.X) < 0.0015f && Math.Abs(vec.Y) < 0.0015f);
            if (!directionSatisfied)
            {
                Component.OutYaw = MathHelper.Clamp((float)Component.YawControl.Update(0, vec.X, dt), -1, 1);
                Component.OutPitch = MathHelper.Clamp((float)Component.PitchControl.Update(0, -vec.Y, dt), -1, 1);
                return false;
            }
            else
            {
                Component.OutYaw = 0;
                Component.OutPitch = 0;
                return true;
            }
        }

        protected bool MoveToPoint(
            double time,
            Vector3 point,
            float radius,
            float range,
            float maxSpeed,
            bool shouldStop,
            ShipSteeringComponent control,
            ShipInputComponent input)
        {
            float targetPower = 0;
            //Bring ship to within GotoRange metres of target
            var targetRadius = GetTargetRadius();
            var myRadius = Parent.PhysicsComponent.Body.Collider.Radius;
            var distance = (point - Parent.PhysicsComponent.Body.Position).Length();

            TriggerCruise(control, (distance - range) > 2000);
            if ((distance - range) < 500)
            {
                control.Cruise = false; // Disable cruise at small distance
            }
            var distrad = radius < 0 ? (targetRadius + myRadius + range) : radius + myRadius;
            bool distanceSatisfied =  distrad >= distance;
            if (distanceSatisfied && shouldStop)
                targetPower = 0;
            else
                targetPower = maxSpeed;

            var directionSatisfied = TurnTowards(time, point);

            if (targetPower > maxSpeed) targetPower = maxSpeed;
            if(input != null)
                input.AutopilotThrottle = targetPower;
            control.InThrottle = targetPower;

            return distanceSatisfied && directionSatisfied;
        }

    }

    sealed class DockBehavior(AutopilotComponent c) : AutopilotBehavior(c)
    {
        public override AutopilotBehaviors Behavior => AutopilotBehaviors.Dock;

        private int lastTargetHp = 0;

        public override bool Update(ShipSteeringComponent control, ShipInputComponent input, double time)
        {
            if (!TargetValid() ||
                !Dockable(out var docking))
            {
                return true; //finished
            }

            var hp = GetTargetHardpoint(docking, true, lastTargetHp);
            if (hp == null)
            {
                // No dock hardpoints available, cancel docking
                FLLog.Error("Autopilot", $"No dock hardpoints available for {Parent.Nickname} docking");
                return true; // finished
            }

            float radius = 5;
            float maxSpeed = 1f;
            var targetPoint = (hp.Transform * TargetObject.WorldTransform).Position;
            if (lastTargetHp > 0) maxSpeed = 0.3f;
            if (lastTargetHp == 2) radius = docking.GetTriggerRadius();
            var d2 = (targetPoint - Parent.PhysicsComponent.Body.Position).Length();
            if (d2 < 80) maxSpeed = 0.3f;

            if (MoveToPoint(time, targetPoint, radius, GotoRadius, maxSpeed, true, control, input))
            {
                if (lastTargetHp < 2)
                    lastTargetHp++;
                else
                   SetThrottle(1, control, input);
            }

            return false; //not finished
        }
    }

    sealed class UndockBehavior(AutopilotComponent c, int index) : AutopilotBehavior(c)
    {
        public override AutopilotBehaviors Behavior => AutopilotBehaviors.Undock;

        private const double MAX_TIME_UNDOCK = 8.0;

        private double totalTime = 0.0;
        private double delay = 1.2;


        public override bool Update(
            ShipSteeringComponent control,
            ShipInputComponent input,
            double time)
        {
            if (delay > 0)
            {
                SetThrottle(0, control, input);
                Component.OutPitch = 0;
                Component.OutYaw = 0;
                delay -= time;
                return false; //not finished
            }
            totalTime += time;
            if (totalTime > MAX_TIME_UNDOCK)
            {
                FLLog.Warning("Autopilot", $"Undock force quit at {totalTime}");
                return true; //finished
            }
            if (!TargetValid() ||
                !Dockable(out var docking))
            {
                return true; //finished
            }

            var info = docking.GetUndockInfo(index);
            var targetPoint = (info.End.Transform * TargetObject.WorldTransform).Position;
            var startPoint = (info.Start.Transform * TargetObject.WorldTransform).Position;
            if (MoveToPoint(time, targetPoint, 25, 10, 1f, false, control, input) ||
                Vector3.Distance(startPoint, targetPoint) - 20 < Vector3.Distance(Parent.LocalTransform.Position, startPoint))
            {
                return true;
            }

            return false; //not finished
        }
    }

    sealed class GotoBehavior(AutopilotComponent c) : AutopilotBehavior(c)
    {
        public override AutopilotBehaviors Behavior => AutopilotBehaviors.Goto;

        public override bool Update(ShipSteeringComponent control, ShipInputComponent input, double time)
        {
            if (!TargetValid())
                return true;

            return MoveToPoint(time, GetTargetPoint(), GetTargetRadius(), GotoRadius, MaxThrottle, true, control, input);
        }
    }

    sealed class FormationBehavior(AutopilotComponent c) : AutopilotBehavior(c)
    {
        public override AutopilotBehaviors Behavior => AutopilotBehaviors.Formation;
        public override bool Update(ShipSteeringComponent control, ShipInputComponent input, double time)
        {
            if (Parent.Formation == null ||
                Parent.Formation.LeadShip == Parent)
            {
                return true;
            }
            var targetPoint = Parent.Formation.GetShipPosition(Parent, Component.LocalPlayer);
            var distance = (targetPoint - Parent.PhysicsComponent.Body.Position).Length();
            var lead = Parent.Formation.LeadShip;
            if (distance > 2000)
            {
                control.Cruise = true;
            }
            else if (distance > 100)
            {
                SetThrottle(1, control, input);
            }

            var minThrottle = distance > 100 ? 1 : 0;

            if (lead.TryGetComponent<ShipPhysicsComponent>(out var leadControl))
            {
                control.Cruise = distance > 2000 || leadControl.CruiseEnabled;
                SetThrottle(MathF.Max(minThrottle, leadControl.EnginePower), control, input);
            }
            else if (lead.TryGetComponent<CEngineComponent>(out var eng))
            {
                control.Cruise = distance > 2000 || eng.Speed > 0.9f;
                var pThrottle = MathHelper.Clamp(eng.Speed / 0.9f, 0, 1);
                SetThrottle(MathF.Max(minThrottle, pThrottle), control, input);
            }
            if (distance > 30) {
                TurnTowards(time, targetPoint);
            }
            else {
                Component.OutYaw = 0;
                Component.OutPitch = 0;
            }

            return false;
        }
    }



	public class AutopilotComponent : GameComponent
    {
        private AutopilotBehavior instance;
        public bool LocalPlayer = false;

        public AutopilotBehaviors CurrentBehavior
            => instance?.Behavior ?? AutopilotBehaviors.None;

        public PIDController PitchControl = new PIDController();
		public PIDController YawControl = new PIDController();

        public float OutPitch;
        public float OutYaw;


		public AutopilotComponent(GameObject parent) : base(parent)
		{
			PitchControl.P = 4;
			YawControl.P = 4;
		}

        public delegate void BehaviorChangedCallback(AutopilotBehaviors newBehavior, AutopilotBehaviors oldBehavior);

        public BehaviorChangedCallback BehaviorChanged;

        void SetInstance(AutopilotBehavior i)
        {
            var old = CurrentBehavior;
            instance = i;
            if(CurrentBehavior != old)
                BehaviorChanged?.Invoke(CurrentBehavior, old);
        }

        public void GotoVec(Vector3 vec, GotoKind kind, float maxThrottle = 1, float gotoRange = 40)
        {
            SetInstance(new GotoBehavior(this));
            instance.Start(kind, vec, maxThrottle, gotoRange);
        }

        public void GotoObject(GameObject obj, GotoKind kind, float maxThrottle = 1, float gotoRange = 40)
        {
            SetInstance(new GotoBehavior(this));
            instance.Start(kind, obj, maxThrottle, gotoRange);
        }

        public void Cancel()
        {
            SetInstance(null);
        }

        public void StartDock(GameObject target, GotoKind kind)
        {
            SetInstance(new DockBehavior(this));
            instance.Start(kind, target, 1, 40);
        }

        public void Undock(GameObject target, int index)
        {
            SetInstance(new UndockBehavior(this, index));
            instance.Start(GotoKind.GotoNoCruise,
                target, 1, 10);
        }


        public void StartFormation()
        {
            SetInstance(new FormationBehavior(this));
            instance.Start(GotoKind.Goto, Vector3.Zero, 1, 10);
        }

		public override void Update(double time)
		{
			var control = Parent.GetComponent<ShipSteeringComponent>();
            var input = Parent.GetComponent<ShipInputComponent>();

            if (input != null)
            {
                input.AutopilotThrottle = 0;
                input.InFormation = CurrentBehavior == AutopilotBehaviors.Formation;
            }

            if (control == null) return;

            if (instance != null)
            {
                if (instance.Update(control, input, time))
                {
                    SetInstance(null);
                }
            }
        }

	}
}
