// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Numerics;
using LibreLancer.Data.GameData;
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
        private Accessory accessory;
        private RigidModel accessoryModel;
        private bool male = true;
        public void SetCharacter(CommAppearance comm)
        {
            if (comm == null)
                Skeleton = null;
            else
            {
                male = comm.Male;
                Skeleton = new DfmSkeletonManager(comm.Body, comm.Head);
                foreach (var sc in comm.Scripts)
                    Skeleton.StartScript(sc, 0, 1, 0, true);
                accessory = comm.Accessory;
                accessoryModel = comm.AccessoryModel;
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

        ICamera View(UiContext context, RectangleF rect)
        {
            if (!HeadOnly)
                return GetCamera(3f, context, rect);
            return new HeadCamera();
        }

        private static Lighting commLighting;
        private static SystemLighting commSource;

        static CharacterView()
        {
            commSource = new SystemLighting();
            commSource.Lights.Add(new DynamicLight()
            {
                Active = true,
                Light = new RenderLight()
                {
                    Ambient = Color3f.Black,
                    Attenuation = Vector3.UnitX,
                    Color = new Color3f(1f, 1f, 1f),
                    Position = new Vector3(0.23f, 0.31f, 0.92f),
                    Range = 1000000000,
                    Kind = LightKind.Point
                }
            });
            commLighting = new Lighting();
            commLighting.Enabled = true;
            commLighting.NumberOfTilesX = -1;
            commLighting.Lights.SourceLighting = commSource;
            commLighting.Lights.SourceEnabled[0] = true;
            commLighting.Ambient = new Color3f(0.079f, 0.079f, 0.079f);
        }

        class HeadCamera : ICamera
        {
            public Matrix4x4 ViewProjection => Projection;

            public Matrix4x4 Projection => new Matrix4x4(
                11.43f, 0, 0, 0,
                0, 11.43f, 0, 0,
                0, 0, 1.029f, 1,
                0, 0, -1.44f, 0
            );
            public Matrix4x4 View => Matrix4x4.Identity;
            public Vector3 Position => Vector3.Zero;
            public bool FrustumCheck(BoundingSphere sphere) => true;

            public bool FrustumCheck(BoundingBox box) => true;
        }

        static readonly Matrix4x4 TransformMale = new Matrix4x4(
            -1f, 0f, 0.003f, 0f,
            0f, 1.0f, 0.0f, 0.0f,
            0.003f, 0.0f, 1.0f, 0.000f,
            -0.001f, -0.702f, 2.148f, 1.000f
        );

        private static readonly Matrix4x4 TransformFemale = new Matrix4x4(
            -1f, 0, -0.005f, 0f,
            0f, 1f, 0, 0f,
            -0.005f, 0f, 1f, 0f,
            0f, -0.615f, 2.15f, 1f
        );

        protected override void Draw3DContent(UiContext context, RectangleF rect)
        {
            Skeleton.UpdateScripts(context.DeltaTime);
            context.CommandBuffer.StartFrame(context.RenderContext);
            var bodyTransform = !HeadOnly ? Matrix4x4.Identity :
                male ? TransformMale : TransformFemale;
            Skeleton.GetTransforms(bodyTransform,
                out var headTransform,
                out var leftTransform,
                out var rightTransform
            );
            var cam = View(context, rect);
            context.RenderContext.SetCamera(cam);
            context.CommandBuffer.Camera = cam;
            var lighting = commLighting;
            context.CommandBuffer.BonesBuffer.BeginStreaming();
            int a = 0, b = 0;
            Skeleton.UploadBoneData(context.CommandBuffer.BonesBuffer, ref a, ref b);
            context.CommandBuffer.BonesBuffer.EndStreaming(b);
            Skeleton.Body.SetSkinning(Skeleton.BodySkinning);
            Skeleton.Body.DrawBuffer(context.CommandBuffer, bodyTransform, ref lighting);

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

            if (accessoryModel != null && Skeleton.GetAccessoryTransform(
                    accessoryModel,
                    accessory.Hardpoint,
                    accessory.BodyHardpoint,
                    bodyTransform,
                        out var accessoryTransform)) {
                accessoryModel.Update(context.GlobalTime);
                accessoryModel.DrawBuffer(0, context.CommandBuffer, context.Data.ResourceManager, accessoryTransform, ref lighting);
            }

            context.CommandBuffer.DrawOpaque(context.RenderContext);
            context.RenderContext.DepthWrite = false;
            context.CommandBuffer.DrawTransparent(context.RenderContext);
            context.RenderContext.DepthWrite = true;
        }
    }
}
