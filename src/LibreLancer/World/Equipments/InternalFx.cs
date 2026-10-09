using LibreLancer.Data.GameData.Items;
using LibreLancer.Sounds;

namespace LibreLancer.World.Equipments;

public class InternalFx : EquipmentObject<InternalFxEquipment>
{
    private bool needTriggerAnimation;
    private AttachedSound? sfx;

    public InternalFx(
        GameObject parent,
        Hardpoint? attachment,
        InternalFxEquipment equipment,
        SoundManager? sounds,
        EquipmentType type) : base(parent, attachment, equipment, type, UpdatesEnabled(equipment, type), false)
    {
        needTriggerAnimation = (type == EquipmentType.Server || type == EquipmentType.Cutscene || type == EquipmentType.Editor) &&
                               equipment.Animation != null;
        if (equipment.Sound != null && sounds != null)
        {
            sfx = new(sounds, equipment.Sound);
        }
    }

    public override void OnUpdate(double delta, GameWorld world)
    {
        if (sfx != null)
        {
            var tr = GetTransform();
            sfx.Position = tr.Position;
            sfx.PlayIfInactive(true);
        }
    }

    public void StartAnimation()
    {
        if (Equipment.Animation != null)
        {
            Parent.AnimationComponent?.StartAnimation(Equipment.Animation);
        }
    }

    public override void ResolveReferences()
    {
        if (needTriggerAnimation)
        {
            StartAnimation();
            needTriggerAnimation = false;
        }
    }

    static bool UpdatesEnabled(InternalFxEquipment equip, EquipmentType type)
    {
        if (type == EquipmentType.LocalPlayer || type == EquipmentType.RemoteObject)
            return equip.Sound != null;
        return false;
    }
}
