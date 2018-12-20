// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

using LibreLancer.Ini;
using LibreLancer.Data.Solar;

namespace LibreLancer.Data
{
	public class Archetype 
	{
        [Entry("nickname")]
        public string Nickname = "";
        [Entry("ids_name")]
        public int IdsName;
        [Entry("ids_info", Multiline = true)]
        public List<int> IdsInfo;
        [Entry("material_library", Multiline = true)]
		public List<string> MaterialPaths = new List<string>();
        [Entry("envmap_material")]
        public string EnvmapMaterial;
        [Entry("explosion_arch")]
        public string ExplosionArch;
        [Entry("mass")]
        public float? Mass;
        [Entry("shape_name")]
        public string ShapeName;
        [Entry("solar_radius")]
        public float? SolarRadius;
        [Entry("da_archetype")]
		public string DaArchetypeName;
        [Entry("hit_pts")]
        public float? Hitpoints;
        [Entry("destructible")]
        public bool Destructible;
        [Entry("type")]
        public ArchetypeType Type;
        //TODO: I don't know what this is or what it does
        [Entry("phantom_physics")]
        public bool? PhantomPhysics;
        [Entry("loadout")]
		public string LoadoutName;
        //Set from parent ini
        public List<CollisionGroup> CollisionGroups = new List<CollisionGroup>();
        //Handled manually
        public List<DockSphere> DockingSpheres = new List<DockSphere>();
        [Entry("open_anim")]
        public string OpenAnim;
        [Entry("open_sound")]
        public string OpenSound;
        [Entry("close_sound")]
        public string CloseSound;
        [Entry("docking_camera")]
        public int DockingCamera;
        [Entry("jump_out_hp")]
        public string JumpOutHp;
        [Entry("lodranges")]
        public float[] LODRanges;
        [Entry("distance_render")]
        public float DistanceRender;
        [Entry("nomad")]
        public bool Nomad;

        protected bool HandleEntry(Entry e)
        {
            if(e.Name.Equals("docking_sphere", StringComparison.InvariantCultureIgnoreCase)) {
                string scr = e.Count == 4 ? e[3].ToString() : null;
                DockingSpheres.Add(new DockSphere() { Name = e[0].ToString(), Hardpoint = e[1].ToString(), Radius = e[2].ToInt32(), Script = scr });
                return true;
            }
            switch(e.Name.ToLowerInvariant())
            {
                case "animated_textures":
                case "surface_hit_effects":
                case "fuse":
                case "shield_link":
                    return true;
            }
            return false;
        }
	}
}
