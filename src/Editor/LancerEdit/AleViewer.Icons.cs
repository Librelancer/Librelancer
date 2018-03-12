/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
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
