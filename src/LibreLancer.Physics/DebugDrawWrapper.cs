// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using BulletSharp;

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

        public override void DrawLine(ref Vector3 from, ref Vector3 to, ref Vector3 color)
        {
            ren.DrawLine(from, to, new Color4(color.X, color.Y, color.Z, 1));
        }

        public override void Draw3DText(ref Vector3 location, string textString)
        {
            
        }

        public override void ReportErrorWarning(string warningString)
        {
          
        }
    }
}
