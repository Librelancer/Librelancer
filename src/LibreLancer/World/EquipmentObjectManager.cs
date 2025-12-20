// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Render;
using LibreLancer.Resources;
using LibreLancer.Sounds;
using LibreLancer.World.Components;

namespace LibreLancer.World
{
    /// <summary>
    /// Return a GameObject only if you add one to the parent
    /// </summary>
    public delegate GameObject MountEquipmentHandler(GameObject parent, ResourceManager res, SoundManager snd, EquipmentType type, string hardpoint, Equipment equip);

    public enum EquipmentType
    {
        Server,
        RemoteObject,
        LocalPlayer,
        Cutscene
    }
    public class EquipmentObjectManager
    {
        static Dictionary<Type, MountEquipmentHandler> handlers = new Dictionary<Type, MountEquipmentHandler>();
        public static void RegisterType<T>(MountEquipmentHandler handler)
        {
            handlers.Add(typeof(T), handler);
        }
        public static void InstantiateEquipment(GameObject parent, ResourceManager res, SoundManager snd, EquipmentType type, string hardpoint, Equipment equip)
        {
            var etype = equip.GetType();
            if (!handlers.TryGetValue(etype, out var handle))
            {
                FLLog.Error("Equipment", $"Cannot instantiate {etype}");
                return;
            }
            var obj = handle(parent, res, snd, type, hardpoint, equip);
            //Do setup of child attachment, hardpoint, lod inheriting, static position etc.
            if (obj != null)
            {
                obj.Parent = parent;
                obj.AddComponent(new EquipmentComponent(equip, obj));
                parent.Children.Add(obj);
                if (equip.LODRanges != null && obj.RenderComponent is ModelRenderer mrender)
                    mrender.LODRanges = equip.LODRanges;
                if(equip.HPChild != null)
                {
                    Hardpoint hpChild = obj.GetHardpoint(equip.HPChild);
                    if (hpChild != null)
                    {
                        obj.SetLocalTransform(hpChild.Transform.Inverse());
                    }
                }
                var hp = parent.GetHardpoint(hardpoint);
                obj.Attachment = hp;
                if(obj.RenderComponent is ModelRenderer && parent.RenderComponent != null)
                {
                    if (parent.RenderComponent is ModelRenderer m && m.LODRanges != null)
                    {
                        obj.RenderComponent.InheritCull = true;
                    }
                    else if (parent.RenderComponent is ModelRenderer)
                    {
                        var mr = (ModelRenderer)parent.RenderComponent;
                        //if (mr.Model.Mesh != null && mr.Model.Switch2 != null)
                         //  obj. RenderComponent.InheritCull = true;
                        //if(mr.CmpParts != null)
                        //{
                            /*Part parentPart = null;
                            if (hp.parent != null)
                                parentPart = mr.CmpParts.Find((o) => o.ObjectName == hp.parent.ChildName);
                            else
                                parentPart = mr.CmpParts.Find((o) => o.ObjectName == "Root");
                            if (parentPart.Model.Switch2 != null)
                                obj.RenderComponent.InheritCull = true;*/
                        //}
                    }
                }

            }
            else
            {
                parent.AddComponent(new EquipmentComponent(equip, parent));
            }
        }
    }
}
