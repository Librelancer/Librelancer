// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer;
using LibreLancer.Fx;
namespace LancerEdit
{
    public partial class AleViewer
    {
        void NodeIcon(FxNode n, out string icon, out Color4 color)
        {
            //Default
            icon = "fxnode";
            color = Color4.White;
            //Emitters
            if (n is FxSphereEmitter)
            {
                icon = "sphere";
            }
            if (n is FxConeEmitter)
            {
                icon = "cone";
            }
            if (n is FxCubeEmitter)
            {
                icon = "fix";
            }
            //Fields
            if (n is FLBeamField)
            {
                icon = "fieldbeam";
            }
            if (n is FLDustField)
            {
                icon = "fielddust";
            }
            if (n is FxAirField)
            {
                icon = "fieldair";
            }
            if (n is FxCollideField)
            {
                icon = "fieldcollide";
            }
            if (n is FxGravityField)
            {
                icon = "fieldgravity";
            }
            if (n is FxRadialField)
            {
                icon = "fieldradial";
            }
            if (n is FxTurbulenceField)
            {
                icon = "pris";
            }
            //Appearances
            if (n is FLBeamAppearance)
            {
                icon = "appbeam";
            }
            else if (n is FxParticleAppearance)
            {
                icon = "appparticle";
            }
            else if (n is FxRectAppearance)
            {
                icon = "apprect";
            }
            else if (n is FxPerpAppearance)
            {
                icon = "appperp";
            }
            else if (n is FxBasicAppearance)
            {
                icon = "appbasic";
            }
        }
    }
}
