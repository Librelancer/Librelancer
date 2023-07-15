// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Utf.Cmp;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public partial class WireframeView : Widget3D
    {
        public WireframeView()
        {
            OrbitPan = Vector2.Zero;
            CanRotate = false;
        }

        public InterfaceColor WireframeColor { get; set; }


        private TargetShipWireframe target;
        public void SetWireframe(TargetShipWireframe target)
        {
            this.target = target;
        }
        
        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            if (!Visible) return;
            base.Render(context, parentRectangle);
            var rect = GetMyRectangle(context, parentRectangle);
            if (rect.Width <= 0 || rect.Height <= 0) return;
            Background?.Draw(context, rect);
            if (target != null) {
                Draw3DViewport(context, rect);
            }
            Border?.Draw(context, rect);
        }

        void DrawWires(UiContext context)
        {
            if (target.Model.Source == RigidModelSource.Sphere)
            {
                DrawVMeshWire(context, sphereWireframe, target.Matrix);
            }
            else
            {
                foreach (var part in target.Model.AllParts)
                {
                    if (part.Wireframe != null)
                    {
                        DrawVMeshWire(context, part.Wireframe.Lines, part.LocalTransform * target.Matrix);
                    }
                }
            }
        }
        void DrawVMeshWire(UiContext context, Vector3[] wires, Matrix4x4 mat)
        {
            var color = (WireframeColor ?? InterfaceColor.White).GetColor(context.GlobalTime); 
            for (int i = 0; i < wires.Length / 2; i++)
            {
                context.Lines.DrawLine(
                    Vector3.Transform(wires[i * 2],mat),
                    Vector3.Transform(wires[i * 2 + 1],mat),
                    color
                );
            }
        }
        
        protected override void Draw3DContent(UiContext context, RectangleF rect)
        {
            float zoom;
            if (target.Model.Source == RigidModelSource.Sphere)
                zoom = SPHERE_OFFSET;
            else
                zoom = -target.Model.GetRadius() * 2.05f;
            var cam = GetCamera(zoom, context, rect);
            context.RenderContext.SetCamera(cam);
            context.Lines.StartFrame(context.RenderContext);
            DrawWires(context);
            context.Lines.Render();
        }
    }
}