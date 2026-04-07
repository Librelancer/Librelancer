// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Utf.Ale;
namespace LibreLancer.Fx
{
	public class FxField : FxNode
	{
		public FxField (AlchemyNode ale) : base(ale)
		{
		}

        public FxField(string name) : base(name)
        {
        }

        private static readonly AlchemyTransform[] _transforms = new AlchemyTransform[32];

        protected Transform3D GetTransform(NodeReference reference, float sparam, float t)
        {
            var translate = Vector3.Zero;
            var rotate = Quaternion.Identity;

            var idx = -1;
            var pr = reference;

            while (pr != null && !pr.IsAttachmentNode)
            {
                if (pr.Node.Transform.HasTransform)
                {
                    idx++;
                    _transforms[idx] = pr.Node.Transform;
                }

                pr = pr.Parent;
            }

            for (var i = idx; i >= 0; i--)
            {
                translate += _transforms[i].GetTranslation(sparam, t);
                rotate *= _transforms[i].GetRotation(sparam, t);
            }

            return new (translate, rotate);
        }

        public virtual void Update(ParticleEffectInstance instance, FieldReference self,
            int appIdx, Matrix4x4 attachment, float sparam, float delta)
        {
        }
	}
}

