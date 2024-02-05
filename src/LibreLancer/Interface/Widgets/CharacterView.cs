// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Numerics;
using LibreLancer.Render;
using LibreLancer.Render.Cameras;
using WattleScript.Interpreter;

//TODO: Implement
namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class CharacterView : Widget3D
    {
        DfmSkeletonManager Skeleton;
        public void SetCharacter(CommAppearance comm)
        {
            if (comm == null)
                Skeleton = null;
            else
            {
                Skeleton = new DfmSkeletonManager(comm.Body, comm.Head);
                foreach (var sc in comm.Scripts)
                    Skeleton.StartScript(sc, 0, 1, 0, true);
            }
        }

        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            if (!Visible) return;
            if (Width < 2 || Height < 2) return;
            var rect = GetMyRectangle(context, parentRectangle);
            Background?.Draw(context, rect);
            if (Skeleton != null) {
                Draw3DViewport(context, rect);
            }
            Border?.Draw(context, rect);
        }

        public bool HeadOnly { get; set; } = true;

        ICamera View(UiContext context, RectangleF rect, Matrix4x4 headTransform)
        {
            if (!HeadOnly)
                return GetCamera(3f, context, rect);
            var headPos = Vector3.Transform(new Vector3(0, 0.05f, 0), headTransform);
            var lookFrom = Vector3.Transform(new Vector3(0, 0.05f, 0.5f), headTransform);
            var lookAt = new LookAtCamera();
            var pxRect = context.PointsToPixels(rect);
            lookAt.Update(pxRect.Width, pxRect.Height, lookFrom, headPos);
            return lookAt;
        }

        protected override void Draw3DContent(UiContext context, RectangleF rect)
        {
            Skeleton.UpdateScripts(context.DeltaTime);
            context.CommandBuffer.StartFrame(context.RenderContext);
            Skeleton.GetTransforms(Matrix4x4.Identity,
                out var headTransform,
                out var leftTransform,
                out var rightTransform
            );
            var cam = View(context, rect, headTransform);
            context.RenderContext.SetCamera(cam);
            context.CommandBuffer.Camera = cam;
            var lighting = Lighting.Empty;
            context.CommandBuffer.BonesBuffer.BeginStreaming();
            int a = 0, b = 0;
            Skeleton.UploadBoneData(context.CommandBuffer.BonesBuffer, ref a, ref b);
            context.CommandBuffer.BonesBuffer.EndStreaming(b);
            Skeleton.Body.SetSkinning(Skeleton.BodySkinning);
            Skeleton.Body.DrawBuffer(context.CommandBuffer, Matrix4x4.Identity, ref lighting);

            if (Skeleton.Head != null)
            {
                Skeleton.Head.SetSkinning(Skeleton.HeadSkinning);
                Skeleton.Head.DrawBuffer(context.CommandBuffer, headTransform, ref lighting);
            }
            if (Skeleton.LeftHand != null)
            {
                Skeleton.LeftHand.SetSkinning(Skeleton.LeftHandSkinning);
                Skeleton.LeftHand.DrawBuffer(context.CommandBuffer, leftTransform, ref lighting);
            }
            if (Skeleton.RightHand != null)
            {
                Skeleton.RightHand.SetSkinning(Skeleton.RightHandSkinning);
                Skeleton.RightHand.DrawBuffer(context.CommandBuffer, rightTransform, ref lighting);
            }
            context.CommandBuffer.DrawOpaque(context.RenderContext);
            context.RenderContext.DepthWrite = false;
            context.CommandBuffer.DrawTransparent(context.RenderContext);
            context.RenderContext.DepthWrite = true;
        }
    }
}
