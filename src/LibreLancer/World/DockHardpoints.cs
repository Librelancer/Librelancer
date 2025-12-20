using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using LibreLancer.Data.GameData.World;
using LibreLancer.Server.Components;

namespace LibreLancer.World;

public class DockHardpoints
{
    private DockAction DockAction;
    private DockingPoint[] DockPoints;
    private (Hardpoint[] Dock, Hardpoint[] Undock)[] Hardpoints;

    public DockHardpoints(DockAction action, DockingPoint[] points)
    {
        DockAction = action;
        DockPoints = points;
        Hardpoints = new (Hardpoint[] Dock, Hardpoint[] Undock)[DockPoints.Length];
    }

    Hardpoint[] CacheDockHardpoints(GameObject parent, int index, bool reverse)
    {
        var hpname = DockPoints[index].DockSphere.Hardpoint.Replace("DockMount", "DockPoint");
        var hp0 = parent.GetHardpoint(DockPoints[index].DockSphere.Hardpoint);
        var hp1 = parent.GetHardpoint(hpname + "01");
        var hp2 = parent.GetHardpoint(hpname + "02");
        if (reverse)
        {
            return ((Hardpoint[]) [hp0, hp1, hp2]).Where(x => x != null).ToArray();
        }
        else
        {
            return ((Hardpoint[])[hp2, hp1, hp0]).Where(x => x != null).ToArray();
        }
    }

    private Hardpoint[] leftLane;
    Hardpoint[] rightLane;

    public Hardpoint[] GetDockHardpoints(GameObject parent, int index, Vector3 position, bool reverse)
    {
        if (DockAction.Kind != DockKinds.Tradelane)
        {
            if(reverse)
            {
                Hardpoints[index].Undock ??= CacheDockHardpoints(parent, index, true);
                return Hardpoints[index].Undock;
            }
            else
            {
                Hardpoints[index].Dock ??= CacheDockHardpoints(parent, index, false);
                return Hardpoints[index].Dock;
            }
        }
        else if (DockAction.Kind == DockKinds.Tradelane)
        {
            var heading = position - parent.PhysicsComponent.Body.Position;
            var fwd = Vector3.Transform(-Vector3.UnitZ, parent.PhysicsComponent.Body.Orientation);
            var dot = Vector3.Dot(heading, fwd);
            if (dot > 0)
            {
                leftLane ??= [parent.GetHardpoint("HpLeftLane")];
                return leftLane;
            }
            else
            {
                rightLane ??= [parent.GetHardpoint("HpRightLane")];
                return rightLane;
            }
        }
        throw new InvalidOperationException();
    }
}
