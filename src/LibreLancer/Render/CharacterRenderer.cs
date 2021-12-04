// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Utf.Anm;
using LibreLancer.Utf.Dfm;
namespace LibreLancer
{
	public class CharacterRenderer : ObjectRenderer
    {
        public const float RADIUS = 1.5f;
        
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
        public override void Draw(ICamera camera, CommandBuffer commands, SystemLighting lights, NebulaRenderer nr)
        {
            Skeleton.GetTransforms(transform, 
                out var headTransform, 
                out var leftTransform, 
                out var rightTransform
                );
            Skeleton.UploadBoneData(commands.BonesBuffer);
            var lighting = RenderHelpers.ApplyLights(
                lights, LightGroup, 
                Vector3.Transform(Vector3.Zero, transform), RADIUS, nr,
                LitAmbient, LitDynamic, NoFog
                );
            Skeleton.Body.SetSkinning(Skeleton.BodySkinning);
            Skeleton.Body.Update(camera, 0.0, 0.0);
            Skeleton.Body.DrawBuffer(commands, transform, ref lighting);
            if (Skeleton.Head != null)
            {
                Skeleton.Head.SetSkinning(Skeleton.HeadSkinning);
                Skeleton.Head.Update(camera, 0.0, 0.0);
                Skeleton.Head.DrawBuffer(commands, headTransform, ref lighting);
            }
            if (Skeleton.LeftHand != null)
            {
                Skeleton.LeftHand.SetSkinning(Skeleton.LeftHandSkinning);
                Skeleton.LeftHand.Update(camera, 0.0, 0.0);
                Skeleton.LeftHand.DrawBuffer(commands, leftTransform, ref lighting);
            }
            if (Skeleton.RightHand != null)
            {
                Skeleton.RightHand.SetSkinning(Skeleton.RightHandSkinning);
                Skeleton.RightHand.Update(camera, 0.0, 0.0);
                Skeleton.RightHand.DrawBuffer(commands, rightTransform, ref lighting);
            }
        }
        public override bool OutOfView(ICamera camera)
        {
            var position = Vector3.Transform(Vector3.Zero, transform);
            var bsphere = new BoundingSphere(position, RADIUS);
            return !camera.Frustum.Intersects(bsphere);
        }
        public override bool PrepareRender(ICamera camera, NebulaRenderer nr, SystemRenderer sys)
        {
            var position = Vector3.Transform(Vector3.Zero, transform);
            var bsphere = new BoundingSphere(position, RADIUS);
            if (camera.Frustum.Intersects(bsphere))
            {
                sys.AddObject(this);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
