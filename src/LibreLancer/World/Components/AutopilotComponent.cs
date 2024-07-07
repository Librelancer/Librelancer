// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Numerics;
using LibreLancer.Client.Components;
using LibreLancer.GameData.World;

namespace LibreLancer.World.Components
{
	public enum AutopilotBehaviors
	{
		None,
		Goto,
		Dock,
        Formation
	}
	public class AutopilotComponent : GameComponent
    {
        private AutopilotBehaviors _behavior;
        public bool LocalPlayer = false;
        public AutopilotBehaviors CurrentBehavior
        {
            get => _behavior;
            private set
            {
                _behavior = value;
                BehaviorChanged?.Invoke(_behavior);
            }
        }

        public PIDController PitchControl = new PIDController();
		public PIDController YawControl = new PIDController();


		public AutopilotComponent(GameObject parent) : base(parent)
		{
			PitchControl.P = 4;
			YawControl.P = 4;
		}

		bool hasTriggeredAnimation = false;
		int lastTargetHp = 0;
        private string tlDockHP = null;
        bool haveSetCruise = false;
        void ResetDockState()
		{
			hasTriggeredAnimation = false;
			lastTargetHp = 0;
            tlDockHP = null;
            haveSetCruise = false;
        }

        public Action<AutopilotBehaviors> BehaviorChanged;


        private GameObject _targetObject;
        private Vector3 _targetPosition;
        private float _targetRadius;
        private float _maxThrottle;
        public bool CanCruise = false;
        private float gotoRange = 40;
        public void GotoVec(Vector3 vec, bool cruise, float maxThrottle = 1, float gotoRange = 40)
        {
            _targetObject = null;
            _targetPosition = vec;
            _targetRadius = 5;
            _maxThrottle = maxThrottle;
            CurrentBehavior = AutopilotBehaviors.Goto;
            CanCruise = cruise;
            this.gotoRange = gotoRange;
            if (Parent.TryGetComponent<ShipSteeringComponent>(out var comp))
                comp.InThrottle = maxThrottle;
        }

        public void GotoObject(GameObject obj, bool cruise = true, float maxThrottle = 1, float gotoRange = 40)
        {
            _targetObject = obj;
            CurrentBehavior = AutopilotBehaviors.Goto;
            CanCruise = cruise;
            _maxThrottle = maxThrottle;
            this.gotoRange = gotoRange;
            if (Parent.TryGetComponent<ShipSteeringComponent>(out var comp))
                comp.InThrottle = maxThrottle;
        }

        public void Cancel()
        {
            _targetObject = null;
            CurrentBehavior = AutopilotBehaviors.None;
        }

        public void StartDock(GameObject target)
        {
            _targetObject = target;
            var docking = target.GetComponent<CDockComponent>();
            if (docking != null)
            {
                if (docking.Action.Kind == DockKinds.Tradelane)
                {
                    var hpend = docking.GetDockHardpoints(Parent.PhysicsComponent.Body.Position).Last();
                    tlDockHP = hpend.Name;
                }
            }
            _maxThrottle = 1;
            CurrentBehavior = AutopilotBehaviors.Dock;
            CanCruise = true;
            gotoRange = 40;
        }

        Vector3 GetTargetPoint()
        {
            if (_targetObject == null) return _targetPosition;
            return _targetObject.WorldTransform.Position;
        }

        float GetTargetRadius()
        {
            if (_targetObject == null) return _targetRadius;
            return _targetObject.PhysicsComponent.Body.Collider.Radius;
        }

        public float OutPitch;
        public float OutYaw;

        public void StartFormation()
        {
            CurrentBehavior = AutopilotBehaviors.Formation;
        }

        public void ProcessGotoDock(double time, ShipSteeringComponent control, ShipInputComponent input)
        {
            Vector3 targetPoint = Vector3.Zero;
			float radius = -1;
			float maxSpeed = 1f;
            if(_targetObject != null && !_targetObject.Flags.HasFlag(GameObjectFlags.Exists))
            {
                //We're trying to get to an object that has been blown up
                ResetDockState();
                CurrentBehavior = AutopilotBehaviors.None;
                _targetObject = null;
                return;
            }
            if (CurrentBehavior == AutopilotBehaviors.Goto)
            {
                targetPoint = GetTargetPoint();
				ResetDockState();
			}
			else
			{
				var docking = _targetObject.GetComponent<CDockComponent>();
				if (docking == null)
				{
					CurrentBehavior = AutopilotBehaviors.None;
					ResetDockState();
					return;
				}
                var hp = docking.GetDockHardpoints(Parent.PhysicsComponent.Body.Position).Skip(lastTargetHp).First();
				radius = 5;
                targetPoint = (hp.Transform * _targetObject.WorldTransform).Position;
				if (lastTargetHp > 0) maxSpeed = 0.3f;
				if (lastTargetHp == 2) radius = docking.TriggerRadius;
                var d2 = (targetPoint - Parent.PhysicsComponent.Body.Position).Length();
				if (d2 < 80) maxSpeed = 0.3f;
			}

            float targetPower = 0;
            //Bring ship to within GotoRange metres of target
            var targetRadius = GetTargetRadius();
            var myRadius = Parent.PhysicsComponent.Body.Collider.Radius;
			var distance = (targetPoint - Parent.PhysicsComponent.Body.Position).Length();

            if ((distance - gotoRange) > 2000 && CanCruise)
            {
                if (!haveSetCruise) {
                    control.Cruise = true;
                    haveSetCruise = true;
                }
            }
            else if ((distance - gotoRange) < 10)
            {
                control.Cruise = false;
            }
			var distrad = radius < 0 ? (targetRadius + myRadius + gotoRange) : radius + myRadius;
			bool distanceSatisfied =  distrad >= distance;
			if (distanceSatisfied)
				targetPower = 0;
			else
				targetPower = maxSpeed;

            var directionSatisfied = TurnTowards(time, targetPoint);

			if (distanceSatisfied && directionSatisfied && CurrentBehavior == AutopilotBehaviors.Goto)
			{
				CurrentBehavior = AutopilotBehaviors.None;
			}
			if (distanceSatisfied && directionSatisfied && CurrentBehavior == AutopilotBehaviors.Dock)
			{
				if (lastTargetHp < 2) lastTargetHp++;
				else
					targetPower = maxSpeed;
			}

            if (targetPower > _maxThrottle) targetPower = _maxThrottle;
            if(input != null)
                input.AutopilotThrottle = targetPower;
            control.InThrottle = targetPower;
        }

        public bool TurnTowards(double time, Vector3 targetPoint)
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
                OutYaw = MathHelper.Clamp((float)YawControl.Update(0, vec.X, dt), -1, 1);
                OutPitch = MathHelper.Clamp((float)PitchControl.Update(0, -vec.Y, dt), -1, 1);
                return false;
            }
            else
            {
                OutYaw = 0;
                OutPitch = 0;
                return true;
            }
        }

        void SetThrottle(float throttle, ShipSteeringComponent control, ShipInputComponent input)
        {
            if (input != null) input.AutopilotThrottle = throttle;
            control.InThrottle = throttle;

        }
        public void ProcessFormation(double time, ShipSteeringComponent control, ShipInputComponent input)
        {
            if (Parent.Formation == null ||
                Parent.Formation.LeadShip == Parent) {
                CurrentBehavior = AutopilotBehaviors.None;
                return;
            }
            var targetPoint = Parent.Formation.GetShipPosition(Parent, LocalPlayer);
            var distance = (targetPoint - Parent.PhysicsComponent.Body.Position).Length();
            var lead = Parent.Formation.LeadShip;
            if (distance > 2000) {
                control.Cruise = true;
            }
            else if (distance > 100)
            {
                SetThrottle(1, control, input);
            }
            else  if (lead.TryGetComponent<ShipPhysicsComponent>(out var leadControl))
            {
                control.Cruise = leadControl.CruiseEnabled;
                SetThrottle(leadControl.EnginePower, control, input);
            }
            else if (lead.TryGetComponent<CEngineComponent>(out var eng))
            {
                control.Cruise = eng.Speed > 0.9f;
                SetThrottle(MathHelper.Clamp(eng.Speed / 0.9f, 0, 1), control, input);
            }
            if (distance > 30) {
                TurnTowards(time, targetPoint);
            }
            else {
                OutYaw = 0;
                OutPitch = 0;
            }
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
            switch (CurrentBehavior)
            {
                case AutopilotBehaviors.Dock:
                case AutopilotBehaviors.Goto:
                    ProcessGotoDock(time, control, input);
                    break;
                case AutopilotBehaviors.Formation:
                    ProcessFormation(time, control, input);
                    break;
                case AutopilotBehaviors.None:
                    ResetDockState();
                    break;
            }
        }

	}
}
