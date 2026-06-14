// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Graphics;
using LibreLancer.Utf.Cmp;
using WattleScript.Interpreter;

namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public partial class WireframeView : Widget3D
    {
        public InterfaceColor? WireframeColor { get; set; }
        private TargetShipWireframe? target;

        public WireframeView()
        {
            OrbitPan = Vector2.Zero;
            CanRotate = false;
        }

        public void SetWireframe(TargetShipWireframe target)
        {
            this.target = target;
        }

        public override void Render(UiContext context, double delta, DrawList2D drawList)
        {
            if (!Visible) return;
            if (ClientRectangle.Width <= 0 || ClientRectangle.Height <= 0) return;
            Background?.Draw(context, drawList, ClientRectangle);

            if (target != null)
            {
                drawList.AddCallback(_ => Draw3DViewport(context, ClientRectangle));
            }

            Border?.Draw(context, drawList, ClientRectangle);
        }

        private void DrawWires(UiContext context)
        {
            if (target is null)
            {
                return;
            }

            DrawModelWires(context, target.Model!, target.Matrix);

            foreach (var child in target.ChildModels)
            {
                DrawModelWires(context, child.Model, child.Matrix, GetHealthColor(child.Health, context.GlobalTime));
            }
        }

        private Color4 GetHealthColor(float health, double time)
        {
            if (health >= 0.8f)
            {
                return Color4.Blue;
            }

            if (health >= 0.6f)
            {
                return Color4.White;
            }

            if (health >= 0.4f)
            {
                return Color4.Yellow;
            }

            if (health >= 0.2f)
            {
                return Color4.Red;
            }

            var pulse = (float)(time % 1.0);
            return Color4.Lerp(Color4.Red, Color4.Black, pulse);
        }

        private void DrawModelWires(UiContext context, RigidModel model, Matrix4x4 matrix, Color4? colorOverride = null)
        {
            if (model.Source == RigidModelSource.Sphere)
            {
                var color = colorOverride ?? (WireframeColor ?? InterfaceColor.White).GetColor(context.GlobalTime);

                for (int i = 0; i < sphereWireframe.Length / 2; i++)
                {
                    context.Lines.DrawLine(
                        Vector3.Transform(sphereWireframe[i * 2], matrix),
                        Vector3.Transform(sphereWireframe[i * 2 + 1], matrix),
                        color
                    );
                }

                return;
            }

            foreach (var part in model.AllParts)
            {
                if (part.Wireframe != null)
                {
                    DrawVMeshWire(context, part.Wireframe, part.LocalTransform.Matrix() * matrix, colorOverride);
                }
            }
        }

        private void DrawVMeshWire(UiContext context, VMeshWire wire, Matrix4x4 mat, Color4? colorOverride = null)
        {
            var color = colorOverride ?? (WireframeColor ?? InterfaceColor.White).GetColor(context.GlobalTime);
            var mesh = context.Data.ResourceManager.FindMesh(wire.MeshCRC);
            if (mesh != null)
                context.Lines.DrawVWire(wire, mesh.VertexResource!, mat, color);
        }

        protected override void Draw3DContent(UiContext context, RectangleF rect)
        {
            float zoom;
            if (target!.Model!.Source == RigidModelSource.Sphere)
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
