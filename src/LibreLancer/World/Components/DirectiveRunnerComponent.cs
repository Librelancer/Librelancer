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
                    if(tgt.TryGetComponent<SDockableComponent>(out var sd))
                    {
                        sd.StartDock(Parent, 0);
                    }
                    else if (tgt.TryGetComponent<CLocalPlayerComponent>(out var pl))
                    {
                        pl.Dock(tgt);
                    }
                    ap.CanCruise = false;
                    ap.StartDock(tgt);
                }
                break;
            }
            case GotoShipDirective ship:
            {
                var tgt = Parent.World.GetObject(ship.Target);
                if (Parent.TryGetComponent<AutopilotComponent>(out var ap))
                {
                    ap.GotoObject(tgt, ship.CruiseKind != GotoKind.GotoNoCruise, Throttle(ship.MaxThrottle), ship.Range);
                    if (ship.CruiseKind == GotoKind.GotoCruise)
                        Parent.GetComponent<ShipSteeringComponent>().Cruise = true;
                }
                break;
            }
            case GotoSplineDirective spline:
            {
                if (Parent.TryGetComponent<AutopilotComponent>(out var ap))
                {
                    splineIndex = 0;
                    ap.GotoVec(EvalSpline(0, spline),
                        spline.CruiseKind != GotoKind.GotoNoCruise,
                        Throttle(spline.MaxThrottle));
                }
                break;
            }
            case GotoVecDirective vec:
            {
                if (Parent.TryGetComponent<AutopilotComponent>(out var ap))
                {
                    ap.GotoVec(vec.Target,
                        vec.CruiseKind != GotoKind.GotoNoCruise,
                        Throttle(vec.MaxThrottle));
                    if (vec.CruiseKind == GotoKind.GotoCruise)
                        Parent.GetComponent<ShipSteeringComponent>().Cruise = true;
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
                    FormationTools.MakeNewFormation(Parent.World.GetObject("Player"), followPlayer.Formation, followPlayer.Ships);
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
                            ap.GotoVec(
                                EvalSpline(times[splineIndex], spline),
                                spline.CruiseKind != GotoKind.GotoNoCruise,
                                Throttle(spline.MaxThrottle));
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
                    if (ap.CurrentBehavior == AutopilotBehaviors.None)
                        NextDirective();
                }
                else
                {
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
        float t1 = GetT( t0, alpha, p0, p1 );
        float t2 = GetT( t1, alpha, p1, p2 );
        float t3 = GetT( t2, alpha, p2, p3 );
        t = MathHelper.Lerp(t1, t2, t);
        var A1 = ( t1-t )/( t1-t0 )*p0 + ( t-t0 )/( t1-t0 )*p1;
        var A2 = ( t2-t )/( t2-t1 )*p1 + ( t-t1 )/( t2-t1 )*p2;
        var A3 = ( t3-t )/( t3-t2 )*p2 + ( t-t2 )/( t3-t2 )*p3;
        var B1 = ( t2-t )/( t2-t0 )*A1 + ( t-t0 )/( t2-t0 )*A2;
        var B2 = ( t3-t )/( t3-t1 )*A2 + ( t-t1 )/( t3-t1 )*A3;
        var C  = ( t2-t )/( t2-t1 )*B1 + ( t-t1 )/( t2-t1 )*B2;
        return C;
    }

    private static float[] times = {0, 0.3333f, 0.6667f, 1f};

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
        index++;
        if(CheckDirective())
        {
            StartDirective(currentDirectives[index]);
        }
    }
}
