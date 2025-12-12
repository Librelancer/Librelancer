// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Numerics;
using LibreLancer.Client.Components;
using LibreLancer.Physics;

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
        protected bool hasTriggeredCruise = false;
        protected double cruiseDelayTimer = 0;
        protected const double CruiseDelay = 3.0; // Vanilla seems to have a 3 second delay before cruise activates when calling a using cruise

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


        protected bool CanCruise;

        protected GameObject TargetObject;
        protected Vector3 _targetPosition;
        protected float _targetRadius;

        protected float MaxThrottle;
        protected float GotoRadius;

        protected void SetThrottle(float throttle, ShipSteeringComponent control, ShipInputComponent input)
        {
            if (input != null) input.AutopilotThrottle = throttle;
            control.InThrottle = throttle;
        }

        protected void SetCruiseLimit(float throttle, ShipSteeringComponent control, ShipInputComponent input)
        {
            float clampedThrottle = MathHelper.Clamp(throttle, 0, 1);
            control.CruiseLimit = clampedThrottle;
            FLLog.Debug("Autopilot", $"SetCruiseLimit: throttle={throttle}, clamped={clampedThrottle}");
            if (input != null) input.AutopilotThrottle = clampedThrottle;
        }


        protected void TriggerCruise(ShipSteeringComponent control, bool shouldCruise, double time)
        {
            if (!hasTriggeredCruise && CanCruise && shouldCruise)
            {
                // Start the cruise delay timer
                cruiseDelayTimer += time;
                if (cruiseDelayTimer >= CruiseDelay)
                {
                    control.Cruise = true;
                    hasTriggeredCruise = true;
                }
                // Don't set cruise yet if delay hasn't elapsed
            }
            else if (!CanCruise)
            {
                control.Cruise = false;
                hasTriggeredCruise = true;
                cruiseDelayTimer = 0;
            }
            else if (!shouldCruise)
            {
                control.Cruise = false;
                hasTriggeredCruise = false;
                cruiseDelayTimer = 0;
            }
            else if (hasTriggeredCruise)
            {
                // Cruise is already active, keep it active
                control.Cruise = true;
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
            
            if (directionSatisfied)
            {
                Component.OutYaw = 0;
                Component.OutPitch = 0;
            }

            return directionSatisfied;
        }

        protected bool MoveToPoint(
            double time,
            Vector3 point,
            float radius,
            float range,
            float maxSpeed,
            bool shouldStop,
            ShipSteeringComponent control,
            ShipInputComponent input,
            bool keepCruiseNearTarget = false)
        {
            float targetPower = 0;
            //Bring ship to within GotoRange metres of target
            var targetRadius = GetTargetRadius();
            var myRadius = Parent.PhysicsComponent.Body.Collider.Radius;
            var distance = (point - Parent.PhysicsComponent.Body.Position).Length();

            //When keepCruiseNearTarget is true, we should keep cruise active even at small distances except for formation points.
            bool shouldCruise = (distance - range) > 2000 || (keepCruiseNearTarget && this is GotoBehavior);
            TriggerCruise(control, shouldCruise, time);
            FLLog.Info("Autopilot", $"[{Parent.Nickname}] Distance to target: {distance:F1}m, range: {range:F1}m, keepCruiseNearTarget: {keepCruiseNearTarget}, shouldCruise: {shouldCruise}");
            if ((distance - range) < 500 && !keepCruiseNearTarget)
            {
                FLLog.Info("Autopilot", $"[{Parent.Nickname}] Disabling cruise at small distance (normal behavior)");
                control.Cruise = false;
                hasTriggeredCruise = false;
                cruiseDelayTimer = 0;
            }
            else if ((distance - range) < 500 && keepCruiseNearTarget)
            {
                FLLog.Info("Autopilot", $"[{Parent.Nickname}] Keeping cruise active near target because next target also uses cruise (CRUISE PERSISTENCE)");
            }

            var completionDistance = range*3; // Use the gotoRange parameter as the primary completion distance
            var distrad = radius < 0 ? (targetRadius + myRadius + range) : radius + myRadius;
            bool distanceSatisfied = completionDistance >= distance;
            FLLog.Info("Autopilot", $"[{Parent.Nickname}] GotoVec completion check - distance: {distance:F1}m, completionDistance: {completionDistance:F1}m, range: {range:F1}m, targetRadius: {targetRadius:F1}m, myRadius: {myRadius:F1}m, radius: {radius:F1}m, distanceSatisfied: {distanceSatisfied}");
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

        public void Start(GotoKind kind, GameObject target, float maxThrottle, float gotoRadius)
        {
            CanCruise = kind != GotoKind.GotoNoCruise;
            MaxThrottle = maxThrottle;
            GotoRadius = gotoRadius;
            TargetObject = target;
        }

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

        public void Start(GotoKind kind, GameObject target, float maxThrottle, float gotoRadius)
        {
            CanCruise = kind != GotoKind.GotoNoCruise;
            MaxThrottle = maxThrottle;
            GotoRadius = gotoRadius;
            TargetObject = target;
        }

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
        public GameObject PlayerReference;
        public float MinDistance;
        public float MaxDistance;
        public int PlayerDistanceBehavior;
        public bool KeepCruiseNearTarget = false;

        public void Start(GotoKind kind, GameObject targetObject, float maxThrottle, float gotoRadius,
            GameObject playerReference = null, float minDistance = 0, float maxDistance = 0,
            int playerDistanceBehavior = 0, bool keepCruiseNearTarget = false)
        {
            CanCruise = kind != GotoKind.GotoNoCruise;
            MaxThrottle = maxThrottle;
            GotoRadius = gotoRadius;
            TargetObject = targetObject;
            PlayerReference = playerReference;
            MinDistance = minDistance;
            MaxDistance = maxDistance;
            PlayerDistanceBehavior = playerDistanceBehavior;
            KeepCruiseNearTarget = keepCruiseNearTarget;
        }

        public void Start(GotoKind kind, Vector3 targetPosition, float maxThrottle, float gotoRadius,
            GameObject playerReference = null, float minDistance = 0, float maxDistance = 0,
            int playerDistanceBehavior = 0, bool keepCruiseNearTarget = false)
        {
            CanCruise = kind != GotoKind.GotoNoCruise;
            MaxThrottle = maxThrottle;
            GotoRadius = gotoRadius;
            _targetPosition = targetPosition;
            _targetRadius = 5;
            PlayerReference = playerReference;
            MinDistance = minDistance;
            MaxDistance = maxDistance;
            PlayerDistanceBehavior = playerDistanceBehavior;
            KeepCruiseNearTarget = keepCruiseNearTarget;
        }

        public override bool Update(ShipSteeringComponent control, ShipInputComponent input, double time)
        {
            if (!TargetValid())
                return true;

            // Check if it needs to maintain distance from player
            if (PlayerReference != null && PlayerReference.Flags.HasFlag(GameObjectFlags.Exists))
            {
                var playerPos = PlayerReference.PhysicsComponent.Body.Position;
                var myPos = Parent.PhysicsComponent.Body.Position;
                var distanceToPlayer = Vector3.Distance(playerPos, myPos);

                // If player is too far (only check max distance, ignore min distance)
                if (distanceToPlayer > MaxDistance)
                {
                    // Player is too far, stop the ship
                    SetThrottle(0, control, input);
                    SetCruiseLimit(0, control, input);
                    return false; // Don't continue to MoveToPoint
                }
                // If player is within range, continue normally
                else
                {
                    // Player is within range, continue normally
                    SetCruiseLimit(1, control, input);
                    MaxThrottle = Math.Clamp(MaxThrottle, 0.1f, 1.0f);
                }
            }

            bool moveToPointResult = MoveToPoint(time, GetTargetPoint(), GetTargetRadius(), GotoRadius, MaxThrottle, true, control, input, KeepCruiseNearTarget);
            FLLog.Info("Autopilot", $"[{Parent.Nickname}] GotoBehavior.Update - MoveToPoint returned: {moveToPointResult}");
            return moveToPointResult;
        }
    }

    sealed class FormationBehavior(AutopilotComponent c) : AutopilotBehavior(c)
    {
        public override AutopilotBehaviors Behavior => AutopilotBehaviors.Formation;

        // Formation stability constants
        private bool isOvertakingLeader = false;
        private const float OVERTAKING_DISTANCE = 150f;
        private const float OVERTAKING_SPEED_REDUCTION = 0.7f; // 70% throttle when overtaking
        private double leaderCruiseActivationTime = -1; // Track when leader activated cruise

        public void Start(GotoKind kind, Vector3 targetPosition, float maxThrottle, float gotoRadius)
        {
            CanCruise = kind != GotoKind.GotoNoCruise;
            MaxThrottle = maxThrottle;
            GotoRadius = gotoRadius;
            _targetPosition = targetPosition;
            _targetRadius = 5;
        }

        public override bool Update(ShipSteeringComponent control, ShipInputComponent input, double time)
        {
            if (Parent.Formation == null ||
                Parent.Formation.LeadShip == Parent)
            {
                // Clean up when leaving formation
                return true;
            }

            // Ensure CanCruise is set appropriately for formation behavior
            // Formation members should be able to cruise when following the leader
            CanCruise = true;

            var targetPoint = Parent.Formation.GetShipPosition(Parent, Component.LocalPlayer);
            var distance = (targetPoint - Parent.PhysicsComponent.Body.Position).Length();
            var lead = Parent.Formation.LeadShip;

            // Check overtaking status
            bool currentlyOvertakingLeader = CheckOvertakingLeader(lead, distance);

            // Debug logging for formation behavior analysis
            FLLog.Debug("Formation", $"[{Parent.Nickname}] Formation Update - distance: {distance:F1}m, overtaking: {currentlyOvertakingLeader}->{isOvertakingLeader}");

            // Smooth overtaking detection with hysteresis
            if (currentlyOvertakingLeader && !isOvertakingLeader)
            {
                isOvertakingLeader = true;
                FLLog.Debug("Formation", $"[{Parent.Nickname}] Overtaking leader detected - reducing speed");
            }
            else if (!currentlyOvertakingLeader && isOvertakingLeader)
            {
                // Only reset overtaking flag if we're clearly not overtaking anymore
                if (distance > OVERTAKING_DISTANCE + 20f)
                {
                    isOvertakingLeader = false;
                    FLLog.Debug("Formation", $"[{Parent.Nickname}] Overtaking resolved - restoring normal speed");
                }
            }

            // Apply existing formation logic with modifications
            var minThrottle = distance > 100 ? (isOvertakingLeader ? OVERTAKING_SPEED_REDUCTION : 1f) : 0;

            FLLog.Debug("Formation", $"[{Parent.Nickname}] Throttle calculation - minThrottle: {minThrottle:F2}, overtaking: {isOvertakingLeader}");

            bool shouldCruise = false;
            bool leaderIsCruising = false;

            // Check if leader is cruising (this should work at any distance)
            if (lead.TryGetComponent<ShipPhysicsComponent>(out var tempLeadControl))
            {
                leaderIsCruising = tempLeadControl.CruiseEnabled;
                FLLog.Debug("Formation", $"[{Parent.Nickname}] Leader CruiseEnabled: {leaderIsCruising}");

                // When leader activates cruise, record the time and synchronize all formation members
                if (leaderIsCruising && leaderCruiseActivationTime < 0)
                {
                    // Use Server.TotalTime which is accessible from the world
                    if (Parent.World.Server != null)
                    {
                        leaderCruiseActivationTime = Parent.World.Server.Server.TotalTime;
                        FLLog.Debug("Formation", $"[{Parent.Nickname}] Leader activated cruise at time {leaderCruiseActivationTime:F2}");
                    }
                }
            }
            else if (lead.TryGetComponent<CEngineComponent>(out var eng))
            {
                leaderIsCruising = eng.Speed > 0.9f;
                FLLog.Debug("Formation", $"[{Parent.Nickname}] Leader engine speed: {eng.Speed:F2}, inferred cruise: {leaderIsCruising}");
            }

            shouldCruise = true;
            FLLog.Debug("Formation", $"[{Parent.Nickname}] Far from formation ({distance:F1}m) - shouldCruise = true (catching up)");

            if (distance < 200)
            {
                shouldCruise = leaderIsCruising;
                FLLog.Debug("Formation", $"[{Parent.Nickname}] Formation distance ({distance:F1}m) - shouldCruise = {shouldCruise} (following leader cruise)");
            }

            if (leaderCruiseActivationTime >= 0 && shouldCruise)
            {
                double timeSinceLeaderCruise = Parent.World.Server?.Server?.TotalTime ?? 0 - leaderCruiseActivationTime;

                if (timeSinceLeaderCruise >= CruiseDelay)
                {
                    control.Cruise = true;
                    this.hasTriggeredCruise = true;
                    this.cruiseDelayTimer = 0;
                    FLLog.Debug("Formation", $"[{Parent.Nickname}] Synchronized cruise activation with leader (time since leader cruise: {timeSinceLeaderCruise:F2}s)");
                }
                else
                {
                    // Leader hasn't finished cruise activation yet, wait synchronously
                    FLLog.Debug("Formation", $"[{Parent.Nickname}] Waiting for leader cruise activation (time since leader cruise: {timeSinceLeaderCruise:F2}s)");
                }
            }
            else
            {
                // Use the standard TriggerCruise method for non-synchronized cases
                TriggerCruise(control, shouldCruise, time);
                FLLog.Debug("Formation", $"[{Parent.Nickname}] TriggerCruise called with shouldCruise={shouldCruise}, control.Cruise={control.Cruise}");
            }

            // Apply throttle based on leader's throttle and overtaking status
            if (lead.TryGetComponent<ShipPhysicsComponent>(out var leadControl))
            {
                float leaderThrottle = leadControl.EnginePower;

                FLLog.Debug("Formation", $"[{Parent.Nickname}] Following ShipPhysics - leaderCruise: {leadControl.CruiseEnabled}, leaderThrottle: {leaderThrottle:F2}, ourCruise: {shouldCruise}");

                // Apply overtaking reduction to leader's throttle with smoothing
                if (isOvertakingLeader)
                {
                    // Gradual speed reduction instead of abrupt change
                    float reductionFactor = OVERTAKING_SPEED_REDUCTION + (0.3f * (1f - Math.Min(distance / OVERTAKING_DISTANCE, 1f)));
                    leaderThrottle = Math.Max(leaderThrottle * reductionFactor, minThrottle);
                    FLLog.Debug("Formation", $"[{Parent.Nickname}] Applied overtaking reduction - leaderThrottle: {leaderThrottle:F2}, reductionFactor: {reductionFactor:F2}");
                }

                float finalThrottle = MathF.Max(minThrottle, leaderThrottle);

                // Add distance-based throttle smoothing for close formation flying
                if (distance < 200f)
                {
                    // Reduce throttle more aggressively when very close to prevent overshooting
                    finalThrottle = Math.Min(finalThrottle, 0.6f);
                    FLLog.Debug("Formation", $"[{Parent.Nickname}] Close formation adjustment - reduced throttle to: {finalThrottle:F2}");
                }

                SetThrottle(finalThrottle, control, input);
                FLLog.Debug("Formation", $"[{Parent.Nickname}] Final throttle set to: {finalThrottle:F2}");
            }
            else if (distance > 100)
            {
                // Apply overtaking speed reduction if needed when we don't have leader ShipPhysics
                float targetThrottle = isOvertakingLeader ? OVERTAKING_SPEED_REDUCTION : 1f;
                SetThrottle(targetThrottle, control, input);
                FLLog.Debug("Formation", $"[{Parent.Nickname}] No ShipPhysics on leader - using default throttle: {targetThrottle:F2}");
            }
            else if (lead.TryGetComponent<CEngineComponent>(out var eng))
            {
                var pThrottle = MathHelper.Clamp(eng.Speed / 0.9f, 0, 1);

                FLLog.Debug("Formation", $"[{Parent.Nickname}] Following CEngine - leaderSpeed: {eng.Speed:F2}, pThrottle: {pThrottle:F2}, ourCruise: {shouldCruise}");

                // Apply overtaking reduction with smoothing
                if (isOvertakingLeader)
                {
                    // Gradual speed reduction instead of abrupt change
                    float reductionFactor = OVERTAKING_SPEED_REDUCTION + (0.3f * (1f - Math.Min(distance / OVERTAKING_DISTANCE, 1f)));
                    pThrottle = Math.Max(pThrottle * reductionFactor, minThrottle);
                    FLLog.Debug("Formation", $"[{Parent.Nickname}] Applied overtaking reduction - pThrottle: {pThrottle:F2}, reductionFactor: {reductionFactor:F2}");
                }

                float finalThrottle = MathF.Max(minThrottle, pThrottle);

                // Add distance-based throttle smoothing for close formation flying
                if (distance < 200f)
                {
                    // Reduce throttle more aggressively when very close to prevent overshooting
                    finalThrottle = Math.Min(finalThrottle, 0.6f);
                    FLLog.Debug("Formation", $"[{Parent.Nickname}] Close formation adjustment - reduced throttle to: {finalThrottle:F2}");
                }

                SetThrottle(finalThrottle, control, input);
                FLLog.Debug("Formation", $"[{Parent.Nickname}] Final throttle set to: {finalThrottle:F2}");
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
        private bool CheckOvertakingLeader(GameObject lead, float distanceToFormation)
        {
            if (distanceToFormation > OVERTAKING_DISTANCE)
                return false;

            if (!lead.TryGetComponent<ShipPhysicsComponent>(out var leadPhysics))
                return false;

            var myPhysics = Parent.PhysicsComponent;
            if (myPhysics == null)
                return false;

            // Calculate relative velocity
            var myVelocity = myPhysics.Body.LinearVelocity;
            var leaderVelocity = lead.PhysicsComponent.Body.LinearVelocity;

            // Calculate vector from leader to wingman
            var relativePosition = myPhysics.Body.Position - lead.PhysicsComponent.Body.Position;

            // Check if wingman is moving faster than leader in the direction of the leader
            float mySpeedInLeaderDirection = Vector3.Dot(myVelocity, Vector3.Normalize(relativePosition));
            float leaderSpeedInLeaderDirection = Vector3.Dot(leaderVelocity, Vector3.Normalize(relativePosition));

            // Overtaking if wingman is closer than threshold AND moving faster than leader
            return distanceToFormation < OVERTAKING_DISTANCE &&
                   mySpeedInLeaderDirection > leaderSpeedInLeaderDirection;
        }

    }



	public class AutopilotComponent : GameComponent
	   {
	       private AutopilotBehavior instance;
	       public bool LocalPlayer = false;

	       // Obstacle Avoidance Configuration
	       public bool ObstacleAvoidanceEnabled = true;
	       public float ObstacleAvoidanceRange = 1000f;
	       public float ObstacleAvoidanceRayAngle = 45f; // degrees from center
	       public float ObstacleAvoidanceRayWidth = 2f;  // meters between rays

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

            // Reset strafing when changing or clearing autopilot behavior
            if (i == null || old != (i?.Behavior ?? AutopilotBehaviors.None))
            {
                var shipPhysics = Parent.GetComponent<ShipPhysicsComponent>();
                if (shipPhysics != null)
                {
                    shipPhysics.CurrentStrafe = StrafeControls.None;
                }
            }

            instance = i;
            if(CurrentBehavior != old)
                BehaviorChanged?.Invoke(CurrentBehavior, old);
        }

        public void GotoVec(Vector3 vec, GotoKind kind, float maxThrottle = 1, float gotoRange = 40,
            GameObject playerReference = null, float minDistance = 0, float maxDistance = 0, int playerDistanceBehavior = 0,
            bool keepCruiseNearTarget = false)
        {
            var gotoBehavior = new GotoBehavior(this);
            SetInstance(gotoBehavior);
            gotoBehavior.Start(kind, vec, maxThrottle, gotoRange, playerReference, minDistance, maxDistance, playerDistanceBehavior, keepCruiseNearTarget);
        }

        public void GotoObject(GameObject obj, GotoKind kind, float maxThrottle = 1, float gotoRange = 40,
            GameObject playerReference = null, float minDistance = 0, float maxDistance = 0, int playerDistanceBehavior = 0,
            bool keepCruiseNearTarget = false)
        {
            var gotoBehavior = new GotoBehavior(this);
            SetInstance(gotoBehavior);
            gotoBehavior.Start(kind, obj, maxThrottle, gotoRange, playerReference, minDistance, maxDistance, playerDistanceBehavior, keepCruiseNearTarget);
        }

        public void ResetStrafing()
        {
            var shipPhysics = Parent.GetComponent<ShipPhysicsComponent>();
            if (shipPhysics != null)
            {
                shipPhysics.CurrentStrafe = StrafeControls.None;
            }
        }

        public void Cancel()
        {
            SetInstance(null);
        }

        public void StartDock(GameObject target, GotoKind kind)
        {
            var dockBehavior = new DockBehavior(this);
            SetInstance(dockBehavior);
            dockBehavior.Start(kind, target, 1, 40);
        }

        public void Undock(GameObject target, int index)
        {
            var undockBehavior = new UndockBehavior(this, index);
            SetInstance(undockBehavior);
            undockBehavior.Start(GotoKind.GotoNoCruise, target, 1, 10);
        }


        public void StartFormation()
        {
            var formationBehavior = new FormationBehavior(this);
            SetInstance(formationBehavior);
            formationBehavior.Start(GotoKind.Goto, Vector3.Zero, 1, 10);
        }
		// Performs obstacle avoidance using raycasting and applies strafing if needed
		private void UpdateObstacleAvoidance(ShipPhysicsComponent shipPhysics)
		{
		    if (!ObstacleAvoidanceEnabled || shipPhysics == null)
		      return;

		    // Get ship's current position and forward direction
		    var shipPosition = Parent.PhysicsComponent.Body.Position;
		    var shipForward = Vector3.Transform(-Vector3.UnitZ, Parent.PhysicsComponent.Body.Orientation);
		    var shipUp = Vector3.Transform(Vector3.UnitY, Parent.PhysicsComponent.Body.Orientation);

		    // Convert angles to radians
		    float leftAngle = -MathHelper.DegreesToRadians(ObstacleAvoidanceRayAngle);
		    float rightAngle = MathHelper.DegreesToRadians(ObstacleAvoidanceRayAngle);

		    // Create ray directions
		    var centerDirection = shipForward;
		    var leftDirection = Vector3.Transform(shipForward, Quaternion.CreateFromAxisAngle(shipUp, leftAngle));
		    var rightDirection = Vector3.Transform(shipForward, Quaternion.CreateFromAxisAngle(shipUp, rightAngle));

		    // Perform raycasts
		    var centerHit = PerformRaycast(shipPosition, centerDirection);
		    var leftHit = PerformRaycast(shipPosition, leftDirection);
		    var rightHit = PerformRaycast(shipPosition, rightDirection);

		    // Determine optimal avoidance direction
		    StrafeControls strafeDirection = StrafeControls.None;

		    if (centerHit.Hit && centerHit.Distance < ObstacleAvoidanceRange)
		    {
		              // Center ray hit an obstacle, need to avoid
		       if (!leftHit.Hit && !rightHit.Hit)
		       {
		                  // Both sides clear, choose direction with more space
		            strafeDirection = leftHit.Distance > rightHit.Distance ? StrafeControls.Left : StrafeControls.Right;
		        }
		        else if (!leftHit.Hit)
		        {
		            // Left side clear
		            strafeDirection = StrafeControls.Left;
		        }
		        else if (!rightHit.Hit)
		        {
		            // Right side clear
		            strafeDirection = StrafeControls.Right;
		        }
		        else
		        {
		                  // Both sides have obstacles, choose side with farther obstacle
		            strafeDirection = leftHit.Distance > rightHit.Distance ? StrafeControls.Left : StrafeControls.Right;
		        }
		    }

		    // Apply strafing
		    shipPhysics.CurrentStrafe = strafeDirection;
		}

		// Performs a single raycast and returns hit information
		private (bool Hit, float Distance, PhysicsObject Object) PerformRaycast(Vector3 origin, Vector3 direction)
		{
		    if (Parent.World?.Physics == null)
		        return (false, float.MaxValue, null);

		    // Normalize direction
		    direction = Vector3.Normalize(direction);

		    // Perform raycast
		    bool hit = Parent.World.Physics.PointRaycast(
		        Parent.PhysicsComponent.Body,
		        origin,
		        direction,
		        ObstacleAvoidanceRange,
		        out Vector3 contactPoint,
		        out PhysicsObject hitObject
		    );

		    if (hit)
		    {
		        float distance = Vector3.Distance(origin, contactPoint);
		        return (true, distance, hitObject);
		    }

		    return (false, float.MaxValue, null);
		    }

		public override void Update(double time)
		{
			var control = Parent.GetComponent<ShipSteeringComponent>();
		          var input = Parent.GetComponent<ShipInputComponent>();
		          var shipPhysics = Parent.GetComponent<ShipPhysicsComponent>();

		          if (input != null)
		          {
		              input.AutopilotThrottle = 0;
		              input.InFormation = CurrentBehavior == AutopilotBehaviors.Formation;
		          }

		          if (control == null) return;

		          // Update obstacle avoidance before normal autopilot logic
		          if (shipPhysics != null && instance != null)
		          {
		              UpdateObstacleAvoidance(shipPhysics);
		          }

		          if (instance != null)
		          {
		              bool behaviorCompleted = instance.Update(control, input, time);
		              if (behaviorCompleted)
		              {
		                  FLLog.Info("Autopilot", $"[{Parent.Nickname}] Behavior completed: {instance.Behavior}, transitioning to next directive");
		                  SetInstance(null);
		                  FLLog.Info("Autopilot", $"[{Parent.Nickname}] SetInstance(null) called - CurrentBehavior should now be None");
		              }
		              else
		              {
		                  FLLog.Debug("Autopilot", $"[{Parent.Nickname}] Behavior still active: {instance.Behavior}");
		              }
		          }
		          else
		          {
		              FLLog.Debug("Autopilot", $"[{Parent.Nickname}] No active autopilot instance");
		          }
		}

	}
}
