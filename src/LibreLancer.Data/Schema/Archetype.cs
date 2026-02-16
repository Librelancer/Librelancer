// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Solar;
using LibreLancer.Data.Schema.Ships;

namespace LibreLancer.Data.Schema;

[ParsedSection]
public partial class Archetype
{
    [Entry("nickname")]
    public string Nickname = "";
    [Entry("ids_name")]
    public int IdsName;
    [Entry("ids_info", Multiline = true)]
    public List<int>? IdsInfo;
    [Entry("material_library", Multiline = true)]
    public List<string> MaterialPaths = [];
    [Entry("envmap_material")]
    public string? EnvmapMaterial;
    [Entry("explosion_arch")]
    public string? ExplosionArch;
    [Entry("mass")]
    public float? Mass;
    [Entry("shape_name")]
    public string? ShapeName;
    [Entry("solar_radius")]
    public float? SolarRadius;
    [Entry("da_archetype")]
    public string? DaArchetypeName;
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
    public string? LoadoutName;
    //Set from parent ini
    [Section("collisiongroup", Child = true)]
    public List<CollisionGroup> CollisionGroups = [];
    //Handled manually
    public List<DockSphere> DockingSpheres = [];
    [Entry("open_anim")]
    public string? OpenAnim;
    [Entry("open_sound")]
    public string? OpenSound;
    [Entry("close_sound")]
    public string? CloseSound;
    [Entry("docking_camera")]
    public int DockingCamera;
    [Entry("jump_out_hp")]
    public string? JumpOutHp;
    [Entry("lodranges")]
    public float[]? LODRanges;
    [Entry("distance_render")]
    public float DistanceRender;
    [Entry("nomad")]
    public bool Nomad;
    [Entry("animated_textures")]
    public bool AnimatedTextures;

    public ShieldLink? ShieldLink;
    public List<ObjectFuse> Fuses = [];
    public List<SurfaceHitEffects> SurfaceHitEffects = [];

    [EntryHandler("surface_hit_effects", MinComponents = 2, Multiline = true)]
    private void HandleSurfaceHitEffect(Entry e) => SurfaceHitEffects.Add(new SurfaceHitEffects(e));

    [EntryHandler("fuse", MinComponents = 3, Multiline = true)]
    private void HandleFuse(Entry e) => Fuses.Add(new ObjectFuse(e));

    [EntryHandler("shield_link", MinComponents = 3)]
    private void HandleShieldLink(Entry e) => ShieldLink = new ShieldLink(e);

    [EntryHandler("docking_sphere", MinComponents = 3, Multiline = true)]
    private void HandleDockingSphere(Entry e)
    {
        string? scr = e.Count == 4 ? e[3].ToString() : null;
        if (!Enum.TryParse<DockSphereType>(e[0].ToString(), out var type))
        {
            IniDiagnostic.InvalidEnum(e, e.Section);
        }

        DockingSpheres.Add(new DockSphere() { Type = type, Hardpoint = e[1].ToString(), Radius = e[2].ToInt32(), Script = scr });
    }

}
