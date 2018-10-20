// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.GameData.Items;
namespace LibreLancer
{
    public class WeaponComponent : GameComponent
    {
        public GunEquipment Definition;
        public WeaponComponent(GameObject parent, GunEquipment def) : base(parent)
        {
            Definition = def;
        }

        public void AimTowards(Vector3 point, TimeSpan time)
        {
            var hp = Parent.Attachment;
            //Parent is the gun itself rotated
            var beforeRotate = hp.TransformNoRotate * Parent.Parent.GetTransform();
            //Inverse Transform
            beforeRotate.Invert();
            var local = beforeRotate.Transform(point);
            var localProper = local.Normalized();
            var delta = (float)(time.TotalSeconds * Definition.TurnRateRadians);
            if(hp.Revolute != null) {
                var target = localProper.X * (float)Math.PI;
                var current = Parent.Attachment.CurrentRevolution;

                if(current > target) {
                    current -= delta;
                    if (current <= target) current = target;
                }
                if(current < target) {
                    current += delta;
                    if (current >= target) current = target;
                }
                hp.Revolve(current);
            }
            //TODO: Finding barrel construct properly?
            Utf.RevConstruct barrel = null;
            foreach (var construct in Parent.CmpConstructs)
                if (construct is Utf.RevConstruct)
                    barrel = (Utf.RevConstruct)construct;
            if(barrel != null) {
                var target = -localProper.Y * (float)Math.PI;
                var current = barrel.Current;
                if (current > target)
                {
                    current -= delta;
                    if (current <= target) current = target;
                }
                if (current < target)
                {
                    current += delta;
                    if (current >= target) current = target;
                }
                barrel.Update(target);
            }
        }
        public void Fire(Vector3 point)
        {
            
        }
    }
}
