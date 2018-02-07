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
using BulletSharp;
using BM = BulletSharp.Math;

namespace LibreLancer.Physics
{
    class DebugDrawWrapper : DebugDraw
    {
        IDebugRenderer ren;
        public DebugDrawWrapper(IDebugRenderer ren)
        {
            this.ren = ren;
        }

        DebugDrawModes drawMode = DebugDrawModes.DrawWireframe;
        public override DebugDrawModes DebugMode { get => drawMode; set => drawMode = value; }

        public override void Draw3dText(ref BM.Vector3 location, string textString)
        {
            
        }

        public override void DrawLine(ref BM.Vector3 from, ref BM.Vector3 to, ref BM.Vector3 color)
        {
            ren.DrawLine(from.Cast(), to.Cast(), new Color4(color.X, color.Y, color.Z, 1));
        }

        public override void ReportErrorWarning(string warningString)
        {
          
        }
    }
}
