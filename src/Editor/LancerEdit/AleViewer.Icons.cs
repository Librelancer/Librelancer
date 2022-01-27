// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer;
using LibreLancer.Fx;
using LibreLancer.ImUI;

namespace LancerEdit
{
    public partial class AleViewer
    {
        void NodeIcon(FxNode n, out char icon)
        {
            //Default
            icon = Icons.Leaf;
            //Emitters
            if (n is FxSphereEmitter)
            {
                icon = Icons.Globe;
            }
            if (n is FxConeEmitter)
            {
                icon = Icons.IceCream;
            }
            if (n is FxCubeEmitter)
            {
                icon = Icons.Cube;
            }
            //Fields
            if (n is FLBeamField)
            {
                icon = Icons.Bolt;
            }
            if (n is FLDustField)
            {
                icon = Icons.Cloud;
            }
            if (n is FxAirField)
            {
                icon = Icons.Wind;
            }
            if (n is FxCollideField)
            {
                icon = Icons.CarCrash;
            }
            if (n is FxGravityField)
            {
                icon = Icons.AngleDoubleDown;
            }
            if (n is FxRadialField)
            {
                icon = Icons.Bullseye;
            }
            if (n is FxTurbulenceField)
            {
                icon = Icons.Fan;
            }
            //Appearances
            if (n is FLBeamAppearance)
            {
                icon = Icons.Bolt;
            }
            else if (n is FxParticleAppearance)
            {
                icon = Icons.SprayCan;
            }
            else if (n is FxRectAppearance)
            {
                icon = Icons.Stop;
            }
            else if (n is FxPerpAppearance)
            {
                icon = Icons.Splotch;
            }
            else if (n is FxBasicAppearance)
            {
                icon = Icons.Images;
            }
        }
    }
}
