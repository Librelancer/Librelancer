// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;

namespace LibreLancer.Render
{
	public class CharacterRenderer : ObjectRenderer
    {
        // jacobi in prison cutscene needs this
        // investigate how to calculate a culling radius
        // in the skeleton manager?

        public DfmSkeletonManager Skeleton;
		public CharacterRenderer(DfmSkeletonManager skeleton)
		{
			this.Skeleton = skeleton;
		}

		Matrix4x4 transform;
        public override void Update(double time, Vector3 position, Matrix4x4 transform)
        {
            this.transform = transform;
        }

        private SystemRenderer sysren;

        public override void Draw(ICamera camera, CommandBuffer commands, SystemLighting lights, NebulaRenderer nr)
        {
            Skeleton.GetTransforms(transform,
                out var headTransform,
                out var leftTransform,
                out var rightTransform
                );
            var radius = Vector3.Distance(Skeleton.Bounds.Min, Skeleton.Bounds.Max) / 2.0f;
            var center = (Skeleton.Bounds.Min + Skeleton.Bounds.Max) / 2.0f;
            if (sysren.DfmMode < DfmDrawMode.DebugBones)
            {
                var lighting = RenderHelpers.ApplyLights(
                    lights, LightGroup,
                    Vector3.Transform(center, transform), radius, nr,
                    LitAmbient, LitDynamic, NoFog
                );
                Skeleton.Body.SetSkinning(Skeleton.BodySkinning);
                Skeleton.Body.DrawBuffer(commands, transform, ref lighting);
                if (Skeleton.Head != null)
                {
                    Skeleton.Head.SetSkinning(Skeleton.HeadSkinning);
                    Skeleton.Head.DrawBuffer(commands, headTransform, ref lighting);
                }

                if (Skeleton.LeftHand != null)
                {
                    Skeleton.LeftHand.SetSkinning(Skeleton.LeftHandSkinning);
                    Skeleton.LeftHand.DrawBuffer(commands, leftTransform, ref lighting);
                }

                if (Skeleton.RightHand != null)
                {
                    Skeleton.RightHand.SetSkinning(Skeleton.RightHandSkinning);
                    Skeleton.RightHand.DrawBuffer(commands, rightTransform, ref lighting);
                }
            }
            if (sysren.DfmMode != DfmDrawMode.Normal)
            {
                Skeleton.DebugDraw(sysren.DebugRenderer, transform, sysren.DfmMode);
            }
        }
        public override bool OutOfView(ICamera camera)
        {
            var bounds = BoundingBox.TransformAABB(Skeleton.Bounds, transform);
            return !camera.FrustumCheck(bounds);
        }
        public override bool PrepareRender(ICamera camera, NebulaRenderer nr, SystemRenderer sys, bool forceCull)
        {
            var bounds = BoundingBox.TransformAABB(Skeleton.Bounds, transform);
            if (camera.FrustumCheck(bounds))
            {
                sys.AddObject(this);
                this.sysren = sys;
                if (sysren.DfmMode < DfmDrawMode.DebugBones)
                {
                    Skeleton.UploadBoneData(sys.Commands.BonesBuffer, ref sys.Commands.BonesOffset, ref sys.Commands.BonesMax);
                }
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
