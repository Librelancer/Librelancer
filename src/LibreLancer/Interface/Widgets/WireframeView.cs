// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
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
        private RenderState? drawState;

        private readonly record struct RenderState(
            RigidModel Model,
            Matrix4x4 Matrix,
            List<TargetShipWireframe.ChildModel> ChildModels,
            Dictionary<RigidModelPart, TargetShipWireframe.PartModel> Parts);

        public WireframeView()
        {
            OrbitPan = Vector2.Zero;
            CanRotate = false;
        }

        public void SetWireframe(TargetShipWireframe? target)
        {
            this.target = target;
        }

        public override bool MouseWanted(UiContext context, float x, float y) =>
            Visible && target?.Model != null && ClientRectangle.Contains(x, y);

        private static float GetZoom(RigidModel model) => model.Source == RigidModelSource.Sphere
            ? SPHERE_OFFSET
            : -model.GetRadius() * 2.05f;

        public override void OnMouseClick(UiContext context)
        {
            var currentTarget = target;
            var model = currentTarget?.Model;
            if (model == null || !ClientRectangle.Contains(context.MouseX, context.MouseY))
            {
                return;
            }

            var pxRect = context.PointsToPixels(ClientRectangle);
            if (pxRect.Width <= 0 || pxRect.Height <= 0)
            {
                return;
            }

            var mouse = new Vector2(
                (context.MouseX - ClientRectangle.X) / ClientRectangle.Width * pxRect.Width,
                (context.MouseY - ClientRectangle.Y) / ClientRectangle.Height * pxRect.Height);
            var camera = GetCamera(GetZoom(model), context, ClientRectangle);
            var viewport = new Vector2(pxRect.Width, pxRect.Height);
            var start = Vector3Ex.UnProject(new Vector3(mouse, 0), camera.Projection, camera.View, viewport);
            var end = Vector3Ex.UnProject(new Vector3(mouse, 1), camera.Projection, camera.View, viewport);
            var direction = (end - start).Normalized();

            uint? selected = null;
            var closest = float.MaxValue;
            foreach (var (part, selectable) in currentTarget!.Parts)
            {
                if (!part.Active || part.Mesh == null)
                {
                    continue;
                }

                var partMatrix = part.LocalTransform.Matrix() * currentTarget.Matrix;
                if (!Matrix4x4.Invert(partMatrix, out var inverse))
                {
                    continue;
                }

                var localStart = Vector3.Transform(start, inverse);
                var localDirection = Vector3.TransformNormal(direction, inverse).Normalized();
                var distance = new Ray(localStart, localDirection).Intersects(part.Mesh.BoundingBox);
                if (distance is >= 0 && distance < closest)
                {
                    closest = distance.Value;
                    selected = selectable.CRC;
                }
            }
            currentTarget.PartSelected?.Invoke(selected);
        }

        public override void Render(UiContext context, double delta, DrawList2D drawList)
        {
            if (!Visible) return;
            if (ClientRectangle.Width <= 0 || ClientRectangle.Height <= 0) return;
            Background?.Draw(context, drawList, ClientRectangle);

            var currentTarget = target;
            if (currentTarget?.Model is { } model)
            {
                var state = new RenderState(
                    model,
                    currentTarget.Matrix,
                    currentTarget.ChildModels,
                    currentTarget.Parts);
                var rect = ClientRectangle;
                drawList.AddCallback(_ =>
                {
                    drawState = state;
                    try
                    {
                        Draw3DViewport(context, rect);
                    }
                    finally
                    {
                        drawState = null;
                    }
                });
            }

            Border?.Draw(context, drawList, ClientRectangle);
        }

        private void DrawWires(UiContext context, RenderState state)
        {
            DrawModelWires(context, state.Model, state.Matrix, selectableParts: state.Parts);

            foreach (var child in state.ChildModels)
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

        private void DrawModelWires(UiContext context, RigidModel model, Matrix4x4 matrix,
            Color4? colorOverride = null,
            Dictionary<RigidModelPart, TargetShipWireframe.PartModel>? selectableParts = null)
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
                if (!part.Active || part.Wireframe == null)
                {
                    continue;
                }

                var partColor = colorOverride;
                if (selectableParts?.TryGetValue(part, out var selectable) == true)
                {
                    partColor = selectable.Selected
                        ? Color4.Orange
                        : GetHealthColor(selectable.Health, context.GlobalTime);
                }
                DrawVMeshWire(context, part.Wireframe, part.LocalTransform.Matrix() * matrix, partColor);
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
            if (drawState is not { } state)
            {
                return;
            }

            var cam = GetCamera(GetZoom(state.Model), context, rect);
            context.RenderContext.SetCamera(cam);
            context.Lines.StartFrame(context.RenderContext);
            DrawWires(context, state);
            context.Lines.Render();
        }
    }
}
