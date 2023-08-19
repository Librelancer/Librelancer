using System;
using System.Numerics;
using LibreLancer.GameData.World;
using LibreLancer.ImUI;

namespace LancerEdit;

public static class ShapeExtensions
{
    public static T ChangeTo<T>(this ZoneShape shape, Zone zone) where T : ZoneShape
    {
        if (typeof(T) == typeof(ZoneCylinder))
        {
            switch (shape) {
                case ZoneCylinder:
                    return (T)shape;
                case ZoneSphere sph:
                    return (T) (ZoneShape) new ZoneCylinder(zone, sph.Radius, sph.Radius);
                case ZoneEllipsoid ellipsoid:
                    return (T) (ZoneShape) new ZoneCylinder(zone, ellipsoid.Size.X, ellipsoid.Size.Y);
                case ZoneBox box:
                    return (T) (ZoneShape) new ZoneCylinder(zone, box.Size.X, box.Size.Y);
                case ZoneRing ring:
                    return (T) (ZoneShape) new ZoneCylinder(zone, ring.OuterRadius, ring.Height);
            }
        }
        if (typeof(T) == typeof(ZoneSphere))
        {
            switch (shape) {
                case ZoneCylinder cyl:
                    return (T)(ZoneShape)new ZoneSphere(zone, cyl.Radius);
                case ZoneSphere:
                    return (T)shape;
                case ZoneEllipsoid ellipsoid:
                    return (T) (ZoneShape) new ZoneSphere(zone, ellipsoid.Size.X);
                case ZoneBox box:
                    return (T) (ZoneShape) new ZoneSphere(zone, box.Size.X);
                case ZoneRing ring:
                    return (T) (ZoneShape) new ZoneSphere(zone, ring.OuterRadius);
            }
        }
        if (typeof(T) == typeof(ZoneEllipsoid))
        {
            switch (shape) {
                case ZoneCylinder cyl:
                    return (T)(ZoneShape)new ZoneEllipsoid(zone, cyl.Radius, cyl.Height, cyl.Radius);
                case ZoneSphere sph:
                    return (T)(ZoneShape)new ZoneEllipsoid(zone, sph.Radius, sph.Radius, sph.Radius);
                case ZoneEllipsoid:
                    return (T) shape;
                case ZoneBox box:
                    return (T) (ZoneShape) new ZoneEllipsoid(zone, box.Size.X, box.Size.Y, box.Size.Z);
                case ZoneRing ring:
                    return (T) (ZoneShape) new ZoneEllipsoid(zone, ring.OuterRadius, ring.Height, ring.OuterRadius);
            }
        }
        if (typeof(T) == typeof(ZoneBox))
        {
            switch (shape) {
                case ZoneCylinder cyl:
                    return (T)(ZoneShape)new ZoneBox(zone, cyl.Radius, cyl.Height, cyl.Radius);
                case ZoneSphere sph:
                    return (T)(ZoneShape)new ZoneBox(zone, sph.Radius, sph.Radius, sph.Radius);
                case ZoneEllipsoid ellipsoid:
                    return (T) (ZoneShape) new ZoneBox(zone, ellipsoid.Size.X, ellipsoid.Size.Y, ellipsoid.Size.Z);
                case ZoneBox:
                    return (T) shape;
                case ZoneRing ring:
                    return (T) (ZoneShape) new ZoneBox(zone, ring.OuterRadius, ring.Height, ring.OuterRadius);
            }
        }
        if (typeof(T) == typeof(ZoneRing))
        {
            switch (shape) {
                case ZoneCylinder cyl:
                    return (T)(ZoneShape)new ZoneRing(zone, cyl.Radius, cyl.Radius / 2f, cyl.Height);
                case ZoneSphere sph:
                    return (T)(ZoneShape)new ZoneRing(zone, sph.Radius, sph.Radius / 2f, sph.Radius);
                case ZoneEllipsoid ellipsoid:
                    return (T) (ZoneShape) new ZoneRing(zone, ellipsoid.Size.X, ellipsoid.Size.X / 2f, ellipsoid.Size.Y);
                case ZoneBox box:
                    return (T) (ZoneShape) new ZoneRing(zone, box.Size.X, box.Size.X / 2f, box.Size.Y);
                case ZoneRing:
                    return (T) shape;
            }
        }
        throw new InvalidOperationException();
    }
    
    public static (Vector3, GuizmoOperation) GetSize(this ZoneShape shape)
    {
        return shape switch
        {
            ZoneCylinder cyl => (new Vector3(cyl.Radius, cyl.Height, 1),
                GuizmoOperation.SCALE_X | GuizmoOperation.SCALE_Y),
            ZoneSphere sphere => (new Vector3(sphere.Radius, 1, 1), GuizmoOperation.SCALE_X),
            ZoneEllipsoid ellipsoid => (ellipsoid.Size, GuizmoOperation.SCALE),
            ZoneBox box => (box.Size, GuizmoOperation.SCALE),
            ZoneRing ring => (new Vector3(ring.OuterRadius, ring.Height, ring.InnerRadius), GuizmoOperation.SCALE),
            _ => (Vector3.One, 0)
        };
    }

    public static void SetSize(this ZoneShape shape, Vector3 size)
    {
        switch (shape)
        {
            case ZoneCylinder cyl:
                cyl.Radius = size.X;
                cyl.Height = size.Y;
                break;
            case ZoneSphere sphere:
                sphere.Radius = size.X;
                break;
            case ZoneEllipsoid ellipsoid:
                ellipsoid.Size = size;
                break;
            case ZoneBox box:
                box.Size = size;
                break;
            case ZoneRing ring:
                ring.OuterRadius = size.X;
                ring.Height = size.Y;
                ring.InnerRadius = size.Z;
                break;
        }
    }
    
}