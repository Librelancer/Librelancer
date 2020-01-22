// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Utf.Anm;
using LibreLancer.Utf.Dfm;
namespace LibreLancer
{
	public class CharacterRenderer : ObjectRenderer
	{
        public DfmSkeletonManager Skeleton;
		public CharacterRenderer(DfmSkeletonManager skeleton)
		{
			this.Skeleton = skeleton;
		}

		Matrix4 transform;
        public override void Update(TimeSpan time, Vector3 position, Matrix4 transform)
        {
            this.transform = transform;
            Skeleton.SetOriginalTransform(transform);
        }
        public override void Draw(ICamera camera, CommandBuffer commands, SystemLighting lights, NebulaRenderer nr)
        {
            Skeleton.GetTransforms(this.transform, 
                out var headTransform, 
                out var leftTransform, 
                out var rightTransform
                );
            var transform = this.transform;
            Skeleton.UploadBoneData(commands.BonesBuffer);
            var lighting = RenderHelpers.ApplyLights(
                lights, LightGroup, 
                transform.Transform(Vector3.Zero), float.MaxValue, nr,
                LitAmbient, LitDynamic, NoFog
                );
            Skeleton.Body.SetSkinning(Skeleton.BodySkinning);
            Skeleton.Body.Update(camera, TimeSpan.Zero, TimeSpan.Zero);
            Skeleton.Body.DrawBuffer(commands, transform, ref lighting);
            if (Skeleton.Head != null)
            {
                Skeleton.Head.SetSkinning(Skeleton.HeadSkinning);
                Skeleton.Head.Update(camera, TimeSpan.Zero, TimeSpan.Zero);
                Skeleton.Head.DrawBuffer(commands, headTransform, ref lighting);
            }
            if (Skeleton.LeftHand != null)
            {
                Skeleton.LeftHand.SetSkinning(Skeleton.LeftHandSkinning);
                Skeleton.LeftHand.Update(camera, TimeSpan.Zero, TimeSpan.Zero);
                Skeleton.LeftHand.DrawBuffer(commands, leftTransform, ref lighting);
            }
            if (Skeleton.RightHand != null)
            {
                Skeleton.RightHand.SetSkinning(Skeleton.RightHandSkinning);
                Skeleton.RightHand.Update(camera, TimeSpan.Zero, TimeSpan.Zero);
                Skeleton.RightHand.DrawBuffer(commands, rightTransform, ref lighting);
            }
        }
        public override bool OutOfView(ICamera camera)
        {
            return false;
        }
        public override bool PrepareRender(ICamera camera, NebulaRenderer nr, SystemRenderer sys)
        {
            sys.AddObject(this);
            return true;
        }
    }
}
