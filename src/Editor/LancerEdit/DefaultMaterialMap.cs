// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer;
using LibreLancer.Data.Schema;

namespace LancerEdit
{
    public class DefaultMaterialMap
    {
        public static void Init()
        {
            var map = new MaterialMap();
            map.AddMap("EcEtOcOt", "DcDtOcOt");
            map.AddMap("DcDtEcEt", "DcDtEt");
            
            map.AddRegex("^nomad.*$","NomadMaterialNoBendy");
            map.AddRegex("^n-texture.*$","NomadMaterialNoBendy");
            map.AddRegex("^ui_.*","HUDIconMaterial");
            map.AddRegex("^exclusion_.*","ExclusionZoneMaterial");

            map.AddRegex("^c_glass$","HighGlassMaterial");
            map.AddRegex("^cv_glass$","HighGlassMaterial");
            map.AddRegex("^b_glass$","HighGlassMaterial");
            map.AddRegex("^k_glass$","HighGlassMaterial");
            map.AddRegex("^l_glass$","HighGlassMaterial");
            map.AddRegex("^r_glass$","HighGlassMaterial");
            
            map.AddRegex("^planet.*_glass$", "GFGlassMaterial");
            map.AddRegex("^bw_glass$", "HighGlassMaterial");
            map.AddRegex("^o_glass$", "HighGlassMaterial");
            map.AddRegex("^anim_hud.*$", "HUDAnimMaterial");
            map.AddRegex("^sea_anim.*$", "PlanetWaterMaterial");
            map.AddRegex("^null$", " NullMaterial");
        }
    }
}