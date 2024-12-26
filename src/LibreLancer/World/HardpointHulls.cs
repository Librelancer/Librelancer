namespace LibreLancer.World;

public static class HardpointHulls
{
    public static void Activate(GameComponent childComponent)
    {
        var eq = childComponent.Parent;
        if (eq == null) {
            FLLog.Warning("Game", "Failed to activate hardpoint hull, equipment object missing");
            return;
        }
        var p = childComponent.Parent?.Parent;
        if (p == null) {
            FLLog.Warning("Game", "Failed to activate hardpoint hull, parent missing");
            return;
        }
        if (childComponent.Parent.Attachment == null) {
            FLLog.Warning("Game", $"Failed to activate hardpoint hull on {p}, no attachment");
            return;
        }
        if (!p.PhysicsComponent.ActivateHardpoint(eq.Attachment))
        {
            FLLog.Warning("Game", $"Failed to activate hardpoint hull for {eq.Attachment} on {p}, no hull found");
        }
    }

    public static void Deactivate(GameComponent childComponent)
    {
        var eq = childComponent.Parent;
        if (eq == null) {
            return;
        }
        var p = childComponent.Parent?.Parent;
        if (eq.Attachment == null) {
            return;
        }
        if (p == null) {
            return;
        }
        p.PhysicsComponent.DeactivateHardpoint(eq.Attachment);
        FLLog.Info("HARDPOINT", $"Deactivate {eq.Attachment} on {p}");
    }
}
