namespace LibreLancer.World;

public static class HardpointHulls
{
    public static void Activate(EquipmentObject child)
    {
        var p = child.Parent;
        if (p.PhysicsComponent == null)
        {
            // Not in physics mode.
            return;
        }
        if (child.Attachment == null)
        {
            return;
        }
        p.PhysicsComponent.ActivateHardpoint(child.Attachment, child);
    }

    public static void Deactivate(EquipmentObject child)
    {
        var p = child.Parent;
        if (p.PhysicsComponent == null)
        {
            // Not in physics mode.
            return;
        }
        if (child.Attachment == null)
        {
            return;
        }

        p.PhysicsComponent.DeactivateHardpoint(child.Attachment);
        FLLog.Info("HARDPOINT", $"Deactivate {child.Attachment} on {p}");
    }
}
