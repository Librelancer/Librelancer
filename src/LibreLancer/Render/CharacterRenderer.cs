// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using LibreLancer.Utf.Dfm;
namespace LibreLancer
{
	public class CharacterRenderer : ObjectRenderer
	{
		public DfmFile Head;
		public DfmFile Body;
		public DfmFile LeftHand;
		public DfmFile RightHand;

		public CharacterRenderer(DfmFile head, DfmFile body, DfmFile leftHand, DfmFile rightHand)
		{
			Head = head;
			Body = body;
			LeftHand = leftHand;
			RightHand = rightHand;
		}

		Matrix4 transform;
		public override void Update(TimeSpan time, Vector3 position, Matrix4 transform)
		{
			this.transform = transform;
		}

		public override void Draw(ICamera camera, CommandBuffer commands, SystemLighting lights, NebulaRenderer nr)
		{
			Body.Update(camera, TimeSpan.Zero, TimeSpan.Zero);
			var bhps = Body.GetHardpoints().Where((arg) => arg.Hp.Name.ToLowerInvariant() == "hp_head").First();
			Body.DrawBuffer(commands, transform, ref Lighting.Empty);
			Head.Update(camera, TimeSpan.Zero, TimeSpan.Zero);
			Head.DrawBuffer(commands, bhps.GetTransform(transform), ref Lighting.Empty);
			var hhps = Head.GetHardpoints().ToArray();
			LeftHand.Update(camera, TimeSpan.Zero, TimeSpan.Zero);
			var lhhps = LeftHand.GetHardpoints().ToArray();
			RightHand.Update(camera, TimeSpan.Zero, TimeSpan.Zero);
			var rhhps = RightHand.GetHardpoints().ToArray();
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
