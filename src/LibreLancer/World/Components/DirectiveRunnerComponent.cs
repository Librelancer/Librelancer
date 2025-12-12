using System;
using System.Numerics;
using LibreLancer.Client.Components;
using LibreLancer.Missions;
using LibreLancer.Missions.Directives;
using LibreLancer.Server;
using LibreLancer.Server.Components;
using LibreLancer.World.Components;

namespace LibreLancer.World;

public class DirectiveRunnerComponent(GameObject parent) : GameComponent(parent)
{
    public bool Active => index >= 0 && currentDirectives != null;

    private int index = -1;
    private int splineIndex = -1;
    private MissionDirective[] currentDirectives;

    public void SetDirectives(MissionDirective[] directives)
    {
        currentDirectives = directives;
        index = directives == null ? -1 : 0;
        if (directives != null && index < directives.Length)
        {
            StartDirective(directives[index]);
        }
    }

    private double currentDelay = 0;

    public override void Update(double time)
    {
        if (CheckDirective())
        {
            UpdateDirective(currentDirectives[index], time);
        }
    }

    static float Throttle(float inThrottle) =>
        inThrottle <= 0
            ? 1
            : inThrottle / 100.0f;

    void StartDirective(MissionDirective directive)
    {
        splineIndex = -1;
        FLLog.Debug("ObjList", $"{Parent.Nickname} running '{directive}'");
        switch (directive)
        {
            case DockDirective dock:
            {
                var tgt = Parent.World.GetObject(dock.Target);
                if (Parent.TryGetComponent<AutopilotComponent>(out var ap))
                {
                    if (tgt.TryGetComponent<SDockableComponent>(out var sd))
                    {
                        sd.StartDock(Parent, 0);
                    }
                    else if (tgt.TryGetComponent<CLocalPlayerComponent>(out var pl))
                    {
                        pl.Dock(tgt);
                    }

                    ap.StartDock(tgt, GotoKind.Goto);
                }

                break;
            }
            case GotoShipDirective ship:
            {
                var tgt = Parent.World.GetObject(ship.Target);
                if (Parent.TryGetComponent<AutopilotComponent>(out var ap))
                {
                    ap.GotoObject(tgt, ship.CruiseKind, Throttle(ship.MaxThrottle),
                        ship.Range);
                }

                break;
            }
            case GotoSplineDirective spline:
            {
                if (Parent.TryGetComponent<AutopilotComponent>(out var ap))
                {
                    splineIndex = 0;
                    // Check if next directive is also a GotoVec with cruise
                    bool keepCruiseNearTarget = false;
                    if (index + 1 < currentDirectives.Length)
                    {
                        var nextDirective = currentDirectives[index + 1];
                        if (nextDirective is GotoVecDirective nextVec &&
                            (nextVec.CruiseKind == GotoKind.GotoCruise || nextVec.CruiseKind == GotoKind.Goto))
                        {
                            keepCruiseNearTarget = true;
                        }
                    }

                    ap.GotoVec(EvalSpline(0, spline),
                        spline.CruiseKind,
                        Throttle(spline.MaxThrottle),
                        spline.Range,
                        null, 0, 0, 0, keepCruiseNearTarget);
                }

                break;
            }
            case GotoVecDirective vec:
            {
                if (Parent.TryGetComponent<AutopilotComponent>(out var ap))
                {
                    // Get player reference if specified
                    GameObject playerRef = Parent.GetWorld().GetObject(vec.PlayerReference);
                    // Check if next directive is also a GotoVec with cruise
                    bool keepCruiseNearTarget = false;
                    if (index + 1 < currentDirectives.Length)
                    {
                        var nextDirective = currentDirectives[index + 1];
                        FLLog.Info("Autopilot", $"[{Parent.Nickname}] Next directive type: {nextDirective.GetType().Name}");
                        if (nextDirective is GotoVecDirective nextVec)
                        {
                            FLLog.Info("Autopilot", $"[{Parent.Nickname}] Next GotoVec cruise kind: {nextVec.CruiseKind}");
                            if (nextVec.CruiseKind == GotoKind.GotoCruise || nextVec.CruiseKind == GotoKind.Goto)
                            {
                                keepCruiseNearTarget = true;
                                FLLog.Info("Autopilot", $"[{Parent.Nickname}] Setting keepCruiseNearTarget = true - cruise will persist between targets");
                            }
                        }
                    }
                    else
                    {
                        FLLog.Debug("Autopilot", "No next directive");
                    }

                    FLLog.Info("Autopilot", $"[{Parent.Nickname}] GotoVec called with keepCruiseNearTarget: {keepCruiseNearTarget}, Range: {vec.Range}");
                    ap.GotoVec(vec.Target, vec.CruiseKind, Throttle(vec.MaxThrottle), vec.Range,
                        playerRef, vec.MinDistance, vec.MaxDistance, vec.PlayerDistanceBehavior, keepCruiseNearTarget);
                }

                break;
            }
            case BreakFormationDirective:
            {
                if (Parent.TryGetComponent<CLocalPlayerComponent>(out var pl))
                {
                    pl.BreakFormation();
                }
                else
                {
                    Parent.Formation?.Remove(Parent);
                }

                NextDirective();
                break;
            }
            case FollowDirective follow:
            {
                if (Parent.TryGetComponent<CLocalPlayerComponent>(out var pl))
                {
                    pl.RunDirectiveIndex(index);
                }
                else
                {
                    var tgtObject = Parent.World.GetObject(follow.Target);
                    FormationTools.EnterFormation(Parent, tgtObject, follow.Offset);
                }

                NextDirective();
                break;
            }
            case FollowPlayerDirective followPlayer:
            {
                if (Parent.TryGetComponent<CLocalPlayerComponent>(out var pl))
                {
                    pl.RunDirectiveIndex(index);
                }
                else
                {
                    FormationTools.MakeNewFormation(Parent.World.GetObject("Player"), followPlayer.Formation,
                        followPlayer.Ships);
                }

                NextDirective();
                break;
            }
            case MakeNewFormationDirective newFormation:
            {
                if (Parent.TryGetComponent<CLocalPlayerComponent>(out var pl))
                {
                    pl.RunDirectiveIndex(index);
                }
                else
                {
                    FormationTools.MakeNewFormation(Parent, newFormation.Formation, newFormation.Ships);
                }

                NextDirective();
                break;
            }
            case DelayDirective delay:
                currentDelay = delay.Time;
                break;
            case IdleDirective:
            {
                // STOP
                if (Parent.TryGetComponent<AutopilotComponent>(out var ap))
                {
                    ap.Cancel();
                }

                if (Parent.TryGetComponent<ShipSteeringComponent>(out var st))
                {
                    st.InThrottle = 0;
                    st.Cruise = false;
                }

                if (Parent.TryGetComponent<ShipInputComponent>(out var si))
                {
                    si.Throttle = 0;
                }

                // this may be hacky
                if (Parent.TryGetComponent<CLocalPlayerComponent>(out var pl))
                {
                    pl.BreakFormation();
                }
                else
                {
                    Parent.Formation?.Remove(Parent);
                }

                NextDirective();
                break;
            }
            default:
                NextDirective();
                break;
        }
    }

    void UpdateDirective(MissionDirective directive, double time)
    {
        switch (directive)
        {
            case DockDirective:
            {
                if (Parent.TryGetComponent<AutopilotComponent>(out var ap))
                {
                    if (ap.CurrentBehavior != AutopilotBehaviors.Dock)
                    {
                        NextDirective();
                    }
                }

                break;
            }
            case DelayDirective:
            {
                currentDelay -= time;
                if (currentDelay <= 0)
                {
                    NextDirective();
                }

                break;
            }
            case GotoSplineDirective spline:
            {
                if (Parent.TryGetComponent<AutopilotComponent>(out var ap))
                {
                    if (ap.CurrentBehavior == AutopilotBehaviors.None)
                    {
                        if (splineIndex + 1 < 4)
                        {
                            splineIndex++;
                            // Check if next directive is also a GotoVec with cruise
                            bool keepCruiseNearTarget = false;
                            if (index + 1 < currentDirectives.Length)
                            {
                                var nextDirective = currentDirectives[index + 1];
                                if (nextDirective is GotoVecDirective nextVec &&
                                    (nextVec.CruiseKind == GotoKind.GotoCruise || nextVec.CruiseKind == GotoKind.Goto))
                                {
                                    keepCruiseNearTarget = true;
                                }
                            }

                            ap.GotoVec(EvalSpline(times[splineIndex], spline), spline.CruiseKind, Throttle(spline.MaxThrottle), spline.Range,
                                null, 0, 0, 0, keepCruiseNearTarget);
                        }
                        else
                        {
                            NextDirective();
                        }
                    }
                }
                else
                {
                    NextDirective();
                }

                break;
            }
            case GotoShipDirective:
            case GotoVecDirective:
            {
                if (Parent.TryGetComponent<AutopilotComponent>(out var ap))
                {
                    FLLog.Info("Autopilot", $"[{Parent.Nickname}] UpdateDirective for GotoVec/GotoShip - CurrentBehavior: {ap.CurrentBehavior}");

                    if (ap.CurrentBehavior == AutopilotBehaviors.None)
                    {
                        FLLog.Info("Autopilot", $"[{Parent.Nickname}] Autopilot behavior is None, directive completed, calling NextDirective()");
                        NextDirective();
                    }
                    else
                    {
                        FLLog.Info("Autopilot", $"[{Parent.Nickname}] Autopilot behavior is active: {ap.CurrentBehavior}, directive still in progress");
                    }
                }
                else
                {
                    FLLog.Info("Autopilot", $"[{Parent.Nickname}] No AutopilotComponent found, calling NextDirective()");
                    NextDirective();
                }

                break;
            }
        }
    }

    static Vector3 EvalSpline(float t, GotoSplineDirective spline)
    {
        var val = CatmullRom(spline.PointA, spline.PointB, spline.PointC, spline.PointD, t);
        FLLog.Debug("GotoSpline", $"heading to point t={t} - {val}");
        return val;
    }

    static float GetT(float t, float alpha, Vector3 p0, Vector3 p1)
    {
        var d = p1 - p0;
        var a = Vector3.Dot(d, d);
        float b = MathF.Pow(a, alpha * 0.5f);
        return (b + t);
    }

    static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t, float alpha = 0.5f)
    {
        float t0 = 0.0f;
        float t1 = GetT(t0, alpha, p0, p1);
        float t2 = GetT(t1, alpha, p1, p2);
        float t3 = GetT(t2, alpha, p2, p3);
        t = MathHelper.Lerp(t1, t2, t);
        var A1 = (t1 - t) / (t1 - t0) * p0 + (t - t0) / (t1 - t0) * p1;
        var A2 = (t2 - t) / (t2 - t1) * p1 + (t - t1) / (t2 - t1) * p2;
        var A3 = (t3 - t) / (t3 - t2) * p2 + (t - t2) / (t3 - t2) * p3;
        var B1 = (t2 - t) / (t2 - t0) * A1 + (t - t0) / (t2 - t0) * A2;
        var B2 = (t3 - t) / (t3 - t1) * A2 + (t - t1) / (t3 - t1) * A3;
        var C = (t2 - t) / (t2 - t1) * B1 + (t - t1) / (t2 - t1) * B2;
        return C;
    }

    private static float[] times = { 0, 0.3333f, 0.6667f, 1f };

    bool CheckDirective()
    {
        if (currentDirectives == null)
        {
            return false;
        }

        if (index >= currentDirectives.Length)
        {
            currentDirectives = null;
            index = -1;
            return false;
        }

        return true;
    }

    void NextDirective()
    {
        FLLog.Info("Autopilot", $"[{Parent.Nickname}] NextDirective called - current index: {index}, moving to next directive");
        index++;
        if (CheckDirective())
        {
            FLLog.Info("Autopilot", $"[{Parent.Nickname}] Starting next directive at index: {index}");
            StartDirective(currentDirectives[index]);
        }
        else
        {
            FLLog.Info("Autopilot", $"[{Parent.Nickname}] No more directives available");
        }
    }
}
