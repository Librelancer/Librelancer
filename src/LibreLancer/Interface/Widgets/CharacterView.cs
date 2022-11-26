// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Numerics;
using LibreLancer.Render;
using WattleScript.Interpreter;

//TODO: Implement
namespace LibreLancer.Interface
{
    [UiLoadable]
    [WattleScriptUserData]
    public class CharacterView : Widget3D    
    {
        DfmSkeletonManager Skeleton;
        private string _costume;
        private string _idle;

        string setCostume = null;
        public void LoadCostume(string costume, string idle)
        {
            _costume = costume;
            _idle = idle;
        }

        void LoadCostumeInternal()
        {
            if (string.IsNullOrWhiteSpace(_costume) || string.IsNullOrWhiteSpace(_idle)) {
                Skeleton = null;
                setCostume = null;
                return;
            }
            if (_costume != setCostume)
            {
                   
            }
        }
        public override void Render(UiContext context, RectangleF parentRectangle)
        {
            if (!Visible) return;
            if (Width < 2 || Height < 2) return;
            var rect = GetMyRectangle(context, parentRectangle);
            Background?.Draw(context, rect);
            LoadCostumeInternal();
            if (Skeleton != null) {
                Draw3DViewport(context, rect);
            }
            Border?.Draw(context, rect);
        }

        protected override void Draw3DContent(UiContext context, RectangleF rect)
        {
            var cam = GetCamera(3f, context, rect);
            context.CommandBuffer.StartFrame(context.RenderContext);
            Skeleton.GetTransforms(Matrix4x4.Identity, 
                out var headTransform, 
                out var leftTransform, 
                out var rightTransform
            );
            var lighting = Lighting.Empty;
            Skeleton.UploadBoneData(context.CommandBuffer.BonesBuffer);
            Skeleton.Body.SetSkinning(Skeleton.BodySkinning);
            Skeleton.Body.Update(cam, 0.0, 0.0);
            Skeleton.Body.DrawBuffer(context.CommandBuffer, Matrix4x4.Identity, ref lighting);
            if (Skeleton.Head != null)
            {
                Skeleton.Head.SetSkinning(Skeleton.HeadSkinning);
                Skeleton.Head.Update(cam, 0.0, 0.0);
                Skeleton.Head.DrawBuffer(context.CommandBuffer, headTransform, ref lighting);
            }
            if (Skeleton.LeftHand != null)
            {
                Skeleton.LeftHand.SetSkinning(Skeleton.LeftHandSkinning);
                Skeleton.LeftHand.Update(cam, 0.0, 0.0);
                Skeleton.LeftHand.DrawBuffer(context.CommandBuffer, leftTransform, ref lighting);
            }
            if (Skeleton.RightHand != null)
            {
                Skeleton.RightHand.SetSkinning(Skeleton.RightHandSkinning);
                Skeleton.RightHand.Update(cam, 0.0, 0.0);
                Skeleton.RightHand.DrawBuffer(context.CommandBuffer, rightTransform, ref lighting);
            }
            context.CommandBuffer.DrawOpaque(context.RenderContext);
            context.RenderContext.DepthWrite = false;
            context.CommandBuffer.DrawTransparent(context.RenderContext);
            context.RenderContext.DepthWrite = true;
        }
    }
}