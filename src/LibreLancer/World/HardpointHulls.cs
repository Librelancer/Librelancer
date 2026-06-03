namespace LibreLancer.World;

public static class HardpointHulls
{
    public static void Activate(GameComponent childComponent)
    {
        var eq = childComponent.Parent;

        if ((GameObject?)eq == null)
        {
            FLLog.Warning("Game", "Failed to activate hardpoint hull, equipment object missing");
            return;
        }

        var p = childComponent.Parent?.Parent;

        if (p == null)
        {
            FLLog.Warning("Game", "Failed to activate hardpoint hull, parent missing");
            return;
        }

        if (p.PhysicsComponent == null)
        {
            // Not in physics mode.
            return;
        }

        if (childComponent.Parent?.Attachment == null)
        {
            FLLog.Warning("Game", $"Failed to activate hardpoint hull on {p}, no attachment");
            return;
        }

        p.PhysicsComponent!.ActivateHardpoint(eq.Attachment!, eq);
    }

    public static void Deactivate(GameComponent childComponent)
    {
        var eq = childComponent.Parent;
        if ((GameObject?)eq == null)
        {
            return;
        }

        var p = childComponent.Parent?.Parent;

        if (eq.Attachment == null)
        {
            return;
        }

        if (p == null)
        {
            return;
        }

        p.PhysicsComponent?.DeactivateHardpoint(eq.Attachment);
        FLLog.Info("HARDPOINT", $"Deactivate {eq.Attachment} on {p}");
    }
}
