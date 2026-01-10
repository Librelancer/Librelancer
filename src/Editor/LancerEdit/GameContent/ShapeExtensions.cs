using System;
using System.Numerics;
using LibreLancer.Data.GameData.World;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent;

public static class ShapeExtensions
{
    public static void ChangeShape(this Zone zone, ShapeKind target, SystemEditorTab tab)
    {
        if (target == zone.Shape)
            return;
        Vector3 newSize = zone.Size;
        if (target == ShapeKind.Cylinder)
        {
            newSize = zone.Shape == ShapeKind.Sphere
                ? new Vector3(zone.Size.X)
                : zone.Size;
        }
        else if(target == ShapeKind.Sphere)
        {
            newSize = new Vector3(zone.Size.X, 0, 0);
        }
        else if (target == ShapeKind.Ellipsoid ||
                 target == ShapeKind.Box)
        {
            switch (zone.Shape) {
                case ShapeKind.Box:
                case ShapeKind.Ellipsoid:
                    newSize = zone.Size;
                    break;
                case ShapeKind.Ring:
                case ShapeKind.Cylinder:
                    newSize = new Vector3(zone.Size.X, zone.Size.Y, zone.Size.X);
                    break;
                case ShapeKind.Sphere:
                    newSize = new Vector3(zone.Size.X);
                    break;
            }
        }
        else if (target == ShapeKind.Ring)
        {
            newSize = zone.Shape == ShapeKind.Sphere
                ? new Vector3(zone.Size.X)
                : zone.Size;
            newSize.Z /= 2;
        }
        else
        {
            throw new InvalidOperationException();
        }
        tab.UndoBuffer.Commit(new SysZoneSetShape(zone, tab, zone.Shape, zone.Size, target, newSize));
    }

    public static (Vector3, GuizmoOperation) GetSizeModify(this Zone z)
    {
        switch (z.Shape)
        {
            case ShapeKind.Box:
            case ShapeKind.Ellipsoid:
            case ShapeKind.Ring:
                return (z.Size, GuizmoOperation.SCALE);
            case ShapeKind.Cylinder:
                return (new Vector3(z.Size.X, z.Size.Y, 1), GuizmoOperation.SCALE_X | GuizmoOperation.SCALE_Y);
            case ShapeKind.Sphere:
                return (new Vector3(z.Size.X,1,1), GuizmoOperation.SCALE_X);
        }
        throw new InvalidOperationException();
    }

}
