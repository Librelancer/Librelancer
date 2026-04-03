using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Client.Components;
using LibreLancer.Render;
using LibreLancer.World;

namespace LibreLancer.Thn;

public class ThnSceneObject
{
    public string Name = null!;
    public Vector3 Translate;
    public Quaternion Rotate;
    public string? Actor;
    public GameObject? Object;
    public DynamicLight? Light;
    public ThnEntity Entity = null!;
    public ThnCameraProps? Camera;
    public Vector3 LightDir;
    public Hardpoint? HpMount;
    public ThnSound? Sound;
    public bool Animating = false;
    public bool PosFromObject = false;
    public CEngineComponent? Engine;
    public int MonitorIndex = 0;

    public List<ThnAttachment> Attachments = new();

    public Transform3D GetTransform()
    {
        var result = GetTransformInternal();
        return result;
    }


    Transform3D GetTransformInternal()
    {
        if (PosFromObject)
        {
            return Object!.WorldTransform;
        }

        Transform3D self = new(Translate, Rotate);
        foreach (var t in Attachments.Where(x => !x.LookAt))
        {
            var parent = t.Parent.GetTransform(t.PathLookAt);
            if (t.Orientation)
            {
                if (t.OrientationRelative)
                {
                    var qCurrent = parent.Orientation;
                    var diff = qCurrent * Quaternion.Inverse(t.LastRotate);
                    var qChild = self.Orientation;
                    self.Orientation = qChild * diff;
                    t.LastRotate = qCurrent;
                }
                else
                {
                    self.Orientation = parent.Orientation;
                }
            }

            if (t.Position)
            {
                if (t.Offset != Vector3.Zero)
                {
                    var off = t.Offset;
                    if (t.EntityRelative)
                    {
                        off = Vector3.Transform(t.Offset, parent.Orientation);
                    }

                    var tr = parent * new Transform3D(off, Quaternion.Identity);
                    self.Position = tr.Position;
                }
                else
                {
                    self.Position = parent.Position;
                }
            }
        }

        // LookAt must be processed after position/orientation
        foreach (var t in Attachments.Where(x => x.LookAt))
        {
            var parent = t.Parent.GetTransform(t.PathLookAt);
            self.Orientation =
                QuaternionEx.LookRotation(Vector3.Normalize(self.Position - parent.Position), Vector3.UnitY);
        }

        return self;
    }

    public void Update()
    {
        var self = GetTransform();
        Translate = self.Position;
        Rotate = self.Orientation;
        UpdateEngineObjects();
    }


    void UpdateEngineObjects()
    {
        if (!PosFromObject && Object != null)
        {
            if (Object.RenderComponent is CharacterRenderer charRen)
            {
                if (charRen.Skeleton.ApplyRootMotion)
                {
                    var accum = Rotate * charRen.Skeleton.RootRotation;
                    var movement = Quaternion.Inverse(charRen.Skeleton.RootRotationAccumulator) * accum;
                    Translate += Vector3.Transform(charRen.Skeleton.RootTranslation, movement);
                    Rotate = accum;
                }

                Translate.Y = charRen.Skeleton.FloorHeight + charRen.Skeleton.RootHeight;
            }

            if (HpMount == null)
            {
                Object.SetLocalTransform(new Transform3D(Translate, Rotate));
            }
            else
            {
                Object.SetLocalTransform(HpMount.Transform.Inverse() * new Transform3D(Translate, Rotate));
            }
        }

        if (Light != null)
        {
            Light.Light.Position = Translate;
            Light.Light.Direction = Vector3.Transform(LightDir.Normalized(), Rotate);
        }
    }
}

public class ThnAttachment(ThnAttachParent parent)
{
    public Vector3 Offset;
    public Quaternion LastRotate;
    public bool Position;
    public bool Orientation;
    public bool OrientationRelative;
    public bool EntityRelative;
    public bool LookAt;
    public bool PathLookAt;

    public ThnAttachParent Parent = parent;
}

public abstract class ThnAttachParent
{
    public abstract Transform3D GetTransform(bool pathLookAt);
}

public class ThnPathParent(ThnSceneObject Path)
    : ThnAttachParent
{
    public float T = 0;
    public bool Reverse = false;
    public Vector3 Offset;

    public override Transform3D GetTransform(bool pathLookAt)
    {
        var path = Path.Entity.Path!;
        var parent = Path.GetTransform();
        Quaternion orient = Quaternion.Identity;
        if (pathLookAt)
        {
            orient = QuaternionEx.LookRotation(path.GetDirection(T, Reverse), Vector3.UnitY) * parent.Orientation;
        }
        else if (path.HasOrientation)
        {
            orient = path.GetOrientation(T) * parent.Orientation;
        }

        var pos = parent.Transform(path.GetPosition(T) + Offset);
        return new(pos, orient);
    }
}

public class ThnObjectParent(ThnSceneObject obj, IRenderHardpoint? hardpoint, RigidModelPart? part)
    : ThnAttachParent
{
    public override Transform3D GetTransform(bool pathLookAt)
    {
        var tr = obj.GetTransform();
        if (part != null)
        {
            return part.LocalTransform * tr;
        }

        if (hardpoint != null)
        {
            return hardpoint.Transform * tr;
        }

        return tr;
    }
}
