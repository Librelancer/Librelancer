using System.Numerics;
using LibreLancer.Data.GameData.Items;

namespace LibreLancer.World.Components;

public class ScannerComponent : GameComponent
{
    public ScannerEquipment Equipment;

    public ScannerComponent(GameObject parent, ScannerEquipment def) : base(parent)
    {
        Equipment = def;
    }

    public bool CanScan(GameObject obj) =>
        obj.Flags.HasFlag(GameObjectFlags.Exists) &&
        obj.Kind == GameObjectKind.Ship &&
        Vector3.Distance(Parent.WorldTransform.Position, obj.WorldTransform.Position) <= Equipment.Def.CargoScanRange;
}
