using System;
using LibreLancer.Resources;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Server.Components;
using LibreLancer.World.Components;

namespace LibreLancer.World.Equipments;

public class Shield : EquipmentModelObject<ShieldEquipment>
{
    public float Health
    {
        get => _health < MinHealth ? 0 : _health;
        set => _health = value;
    }

    private float _health;
    private float MinHealth => Equipment.Def.OfflineThreshold * Equipment.Def.MaxCapacity;
    private double suppressTimer;
    private float suppressedRestoreHealth;
    private bool shieldHpActive = false;
    private readonly bool updateCapacities;

    public Shield(
        GameObject parent,
        Hardpoint? attachment,
        ShieldEquipment equipment,
        EquipmentType type,
        ResourceManager resources) : base(parent, attachment, equipment, type, resources, true)
    {
        Health = equipment.Def.MaxCapacity;
        updateCapacities = type == EquipmentType.Server;
    }

    public void Suppress(double duration, GameWorld world)
    {
        suppressTimer = duration;
        suppressedRestoreHealth = Math.Max(suppressedRestoreHealth, _health);
        _health = 0;
    }

    public bool Damage(float incomingDamage)
    {
        if (_health > MinHealth)
        {
            _health -= incomingDamage;
            if (_health <= MinHealth)
            {
                _health = 0;
            }
            if(Parent.TryGetComponent<SNPCComponent>(out var n))
                n.TakingDamage(incomingDamage);
            if (Parent.TryGetComponent<SSolarComponent>(out var s))
                s.SendSolarUpdate = true;
            return true;
        }
        return false;
    }

    void UpdateCapacities(double delta)
    {
        if (!updateCapacities)
            return;
        if (suppressTimer > 0)
        {
            _health = 0;
            suppressTimer = double.MaxNative(0, suppressTimer - delta);
            if (suppressTimer > 0)
            {
                _health = Math.Max(suppressedRestoreHealth, MinHealth);
                suppressedRestoreHealth = 0;
            }
        }
        else
        {
            if (_health < MinHealth)
            {
                var regenRate = MinHealth / Equipment.Def.OfflineRebuildTime;
                _health += (float) (delta * regenRate);
                if (_health > MinHealth)
                    _health = MinHealth;
            }
            else
            {
                _health += (float)(delta * Equipment.Def.RegenerationRate);
                if (_health > Equipment.Def.MaxCapacity) _health = Equipment.Def.MaxCapacity;
            }
        }
    }

    public override void OnUpdate(double delta, GameWorld world)
    {
        UpdateCapacities(delta);
        if (Health >= MinHealth && !shieldHpActive)
        {
            shieldHpActive = true;
            if (Parent.TryGetComponent<ShipComponent>(out var ship)) {
                ship.ActivateShieldBubble(Attachment!.Name);
            }
        }
        else if (Health < MinHealth && shieldHpActive)
        {
            shieldHpActive = false;
            if (Parent.TryGetComponent<ShipComponent>(out var ship)) {
                ship.DeactivateShieldBubble(Attachment!.Name);
            }
        }
    }
}
