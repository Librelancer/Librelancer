// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer;
using LibreLancer.Fx;
using LibreLancer.ImUI;

namespace LancerEdit
{
    public partial class AleEditor
    {

        static string TypeName(FxNode n) => n switch
        {
            FxSphereEmitter => "Sphere Emitter",
            FxConeEmitter => "Cone Emitter",
            FxCubeEmitter => "Cube Emitter",
            //Appearances
            FLBeamAppearance => "Beam Appearance",
            FxParticleAppearance => "Particle Appearance",
            FxRectAppearance => "Rect Appearance",
            FxOrientedAppearance => "Oriented Appearance",
            FxPerpAppearance => "Perp Appearance",
            FxBasicAppearance =>  "Basic Appearance",
            //Fields
            FLBeamField => "Beam Field",
            FLDustField => "Dust Field",
            FxAirField => "Air Field",
            FxCollideField => "Collide Field",
            FxGravityField => "Gravity Field",
            FxRadialField => "Radial Field",
            FxTurbulenceField => "Turbulence Field",
            _ when n.GetType() == typeof(FxNode) => "Empty Node",
            _ => n.NodeName
        };


        static char NodeIcon(FxNode n) => n switch
        {
            //Emitters
            FxSphereEmitter => Icons.SphereEmitter,
            FxConeEmitter => Icons.ConeEmitter,
            FxCubeEmitter => Icons.CubeEmitter,
            //Appearances
            FLBeamAppearance => Icons.BeamAppearance,
            FxParticleAppearance => Icons.ParticleAppearance,
            FxRectAppearance => Icons.RectAppearance,
            FxOrientedAppearance => Icons.RectAppearance,
            FxPerpAppearance => Icons.PerpAppearance,
            FxBasicAppearance => Icons.BasicAppearance,
            //Fields
            FLBeamField => Icons.Bolt,
            FLDustField => Icons.Cloud,
            FxAirField => Icons.Wind,
            FxCollideField => Icons.CarCrash,
            FxGravityField => Icons.AngleDoubleDown,
            FxRadialField => Icons.Bullseye,
            FxTurbulenceField => Icons.Fan,
            //FxNode/Unimplemented
            _ => Icons.Leaf
        };
    }
}
