using System.Numerics;
using LibreLancer.Data.GameData.Items;

namespace LibreLancer.World.Equipments;

public class Scanner(GameObject parent, Hardpoint? attachment, ScannerEquipment equipment, EquipmentType type)
    : EquipmentObject<ScannerEquipment>(parent, attachment, equipment, type, false, false)
{
    public bool CanScan(GameObject obj) =>
        obj.Flags.HasFlag(GameObjectFlags.Exists) &&
        obj.Kind == GameObjectKind.Ship &&
        Vector3.Distance(Parent.Transform.Position, obj.Transform.Position) <= Equipment.Def.CargoScanRange;
}
