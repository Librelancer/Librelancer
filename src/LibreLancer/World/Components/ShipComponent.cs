using System;
using LibreLancer.Data.GameData;

namespace LibreLancer.World.Components;

public class ShipComponent : GameComponent
{
    public Ship Ship;

    public ShipComponent(Ship ship, GameObject parent) : base(parent)
    {
        Ship = ship;
    }

    public void ActivateShieldBubble(string hardpoint)
    {
        if (string.IsNullOrWhiteSpace(Ship.ShieldLinkSource) ||
            string.IsNullOrWhiteSpace(Ship.ShieldLinkHull) ||
            Parent.PhysicsComponent == null)
            return;
        if (hardpoint.Equals(Ship.ShieldLinkSource, StringComparison.OrdinalIgnoreCase))
        {
            var hp = Parent.GetHardpoint(Ship.ShieldLinkHull);
            Parent.PhysicsComponent?.ActivateHardpoint(hp);
        }
    }

    public void DeactivateShieldBubble(string hardpoint)
    {
        if (string.IsNullOrWhiteSpace(Ship.ShieldLinkSource) ||
            string.IsNullOrWhiteSpace(Ship.ShieldLinkHull) ||
            Parent.PhysicsComponent == null)
            return;
        if (hardpoint.Equals(Ship.ShieldLinkSource, StringComparison.OrdinalIgnoreCase))
        {
            var hp = Parent.GetHardpoint(Ship.ShieldLinkHull);
            Parent.PhysicsComponent?.DeactivateHardpoint(hp);
        }
    }
}
