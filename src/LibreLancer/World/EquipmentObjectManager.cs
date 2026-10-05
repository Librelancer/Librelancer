// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Client.Components;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Resources;
using LibreLancer.Sounds;
using LibreLancer.World.Equipments;

namespace LibreLancer.World
{
    public enum EquipmentType
    {
        Server,
        RemoteObject,
        LocalPlayer,
        Cutscene
    }

    public static class EquipmentObjectManager
    {
        public static void InstantiateEquipment(GameObject parent, ResourceManager res, SoundManager? snd, EquipmentType type, string? hardpoint, Equipment equip)
        {
            var obj = Create(parent, res, snd, type, parent.GetHardpoint(hardpoint), equip);
            parent.AddChild(obj);
            HardpointHulls.Activate(obj);
        }

        static EquipmentObject Create(GameObject parent, ResourceManager res, SoundManager? snd, EquipmentType type,
            Hardpoint? hardpoint, Equipment equip) =>
            equip switch
            {
                CargoPodEquipment => CargoPod(parent, res, snd, type, hardpoint, equip),
                CountermeasureEquipment => Countermeasure(parent, res, snd, type, hardpoint, equip),
                EffectEquipment => AttachedEffect(parent, res, snd, type, hardpoint, equip),
                CloakEquipment => Cloak(parent, res, snd, type, hardpoint, equip),
                EngineEquipment => Engine(parent, res, snd, type, hardpoint, equip),
                GunEquipment => Gun(parent, res, snd, type, hardpoint, equip),
                InternalFxEquipment => InternalEffect(parent, res, snd, type, hardpoint, equip),
                LightEquipment => Light(parent, res, snd, type, hardpoint, equip),
                MissileLauncherEquipment => MissileLauncher(parent, res, snd, type, hardpoint, equip),
                MineDropperEquipment => MineDropper(parent, res, snd, type, hardpoint, equip),
                PowerEquipment => Power(parent, res, snd, type, hardpoint, equip),
                ScannerEquipment => Scanner(parent, res, snd, type, hardpoint, equip),
                ShieldEquipment => Shield(parent, res, snd, type, hardpoint, equip),
                ThrusterEquipment => Thruster(parent, res, snd, type, hardpoint, equip),
                TractorEquipment => Tractor(parent, res, snd, type, hardpoint, equip),
                TradelaneEquipment => Tradelane(parent, res, snd, type, hardpoint, equip),
                _ => throw new NotImplementedException()
            };

        private static EquipmentObject CargoPod(GameObject parent, ResourceManager res, SoundManager? snd,
            EquipmentType type, Hardpoint? hardpoint, Equipment equip)
        {
            var pod = (CargoPodEquipment)equip;
            return new CargoPod(parent, hardpoint, pod, type, res);
        }

        private static EquipmentObject Countermeasure(GameObject parent, ResourceManager res, SoundManager? snd,
            EquipmentType type, Hardpoint? hardpoint, Equipment equip)
        {
            var cm = (CountermeasureEquipment)equip;
            snd?.LoadSound(cm.Munition?.Def.OneShotSound);
            return new CountermeasureLauncher(parent, hardpoint, cm, type, res);
        }

        private static EquipmentObject AttachedEffect(GameObject parent, ResourceManager res, SoundManager? snd,
            EquipmentType type,
            Hardpoint? hardpoint, Equipment equip)
        {
            var e = (EffectEquipment)equip;
            snd?.LoadSound(e.Effect?.Sound?.Nickname);
            return new AttachedEffect(parent, hardpoint, e, type, res, snd);
        }

        private static EquipmentObject InternalEffect(GameObject parent, ResourceManager res, SoundManager? snd,
            EquipmentType type,
            Hardpoint? hardpoint, Equipment equip)
        {
            var e = (InternalFxEquipment)equip;
            snd?.LoadSound(e.Sound);
            return new InternalFx(parent, hardpoint, e, snd, type);
        }

        private static EquipmentObject Cloak(GameObject parent, ResourceManager res, SoundManager? snd,
            EquipmentType type,
            Hardpoint? hardpoint, Equipment equip) =>
            new CloakingDevice(parent, hardpoint, (CloakEquipment)equip, type, res);

        private static EquipmentObject Engine(GameObject parent, ResourceManager res, SoundManager? snd,
            EquipmentType type,
            Hardpoint? hardpoint, Equipment equip)
        {
            var eng = (EngineEquipment)equip;
            if (snd != null)
            {
                snd.LoadSound(eng.Def.CruiseLoopSound);
                snd.LoadSound(eng.Def.CruiseStartSound);
                snd.LoadSound(eng.Def.CruiseStopSound);
                snd.LoadSound(eng.Def.CruiseBackfireSound);
                snd.LoadSound(eng.Def.CruiseStopSound);
                snd.LoadSound(eng.Def.EngineKillSound);
                snd.LoadSound(eng.Def.RumbleSound);
                snd.LoadSound(eng.Def.CharacterLoopSound);
                snd.LoadSound(eng.Def.CharacterStartSound);
            }

            return new Engine(parent, hardpoint, eng, res, snd, type);
        }

        private static EquipmentObject Gun(GameObject parent, ResourceManager res, SoundManager? snd,
            EquipmentType type,
            Hardpoint? hardpoint, Equipment equip)
        {
            var gn = (GunEquipment)equip;
            snd?.LoadSound(gn.Munition.Def.OneShotSound);
            return new Gun(parent, hardpoint, gn, type, res);
        }

        private static EquipmentObject Light(GameObject parent, ResourceManager res, SoundManager? snd,
            EquipmentType type, Hardpoint? hardpoint, Equipment equip) =>
            new Light(parent, hardpoint, (LightEquipment)equip, type);

        private static EquipmentObject MissileLauncher(GameObject parent, ResourceManager res, SoundManager? snd,
            EquipmentType type, Hardpoint? hardpoint, Equipment equip)
        {
            var gn = (MissileLauncherEquipment)equip;
            snd?.LoadSound(gn.Munition.Def.OneShotSound);
            return new MissileLauncher(parent, hardpoint, gn, type, res);
        }

        private static EquipmentObject MineDropper(GameObject parent, ResourceManager res, SoundManager? snd,
            EquipmentType type, Hardpoint? hardpoint, Equipment equip)
        {
            var md = (MineDropperEquipment)equip;
            snd?.LoadSound(md.Mine?.Def.OneShotSound);
            return new MineDropper(parent, hardpoint, md, type, res);
        }

        private static EquipmentObject Power(GameObject parent, ResourceManager res, SoundManager? snd,
            EquipmentType type,
            Hardpoint? hardpoint, Equipment equip) =>
            new PowerCore(parent, hardpoint, (PowerEquipment)equip, type);

        private static EquipmentObject Scanner(GameObject parent, ResourceManager res, SoundManager? snd,
            EquipmentType type,
            Hardpoint? hardpoint, Equipment equip) =>
            new Scanner(parent, hardpoint, (ScannerEquipment)equip, type);

        private static EquipmentObject Shield(GameObject parent, ResourceManager res, SoundManager? snd,
            EquipmentType type, Hardpoint? hardpoint, Equipment equip)
        {
            return new Shield(parent, hardpoint, (ShieldEquipment)equip, type, res);
        }

        private static EquipmentObject Thruster(GameObject parent, ResourceManager res, SoundManager? snd,
            EquipmentType type,
            Hardpoint? hardpoint, Equipment equip) =>
            new Thruster(parent, hardpoint, (ThrusterEquipment)equip, type, res, snd);


        private static EquipmentObject Tractor(GameObject parent, ResourceManager res, SoundManager? snd,
            EquipmentType type, Hardpoint? hardpoint, Equipment equip)
            => new Tractor(parent, hardpoint, (TractorEquipment)equip, type);

        private static EquipmentObject Tradelane(GameObject parent, ResourceManager res, SoundManager? snd,
            EquipmentType type,
            Hardpoint? hardpoint, Equipment equip)
            => new Tradelane(parent, hardpoint, (TradelaneEquipment)equip, type, res);
    }
}
