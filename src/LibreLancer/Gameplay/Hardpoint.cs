/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
using System;
using LibreLancer.Utf;
using LibreLancer.Utf.Cmp;
namespace LibreLancer
{
	public class Hardpoint
	{
		Matrix4 transform;
		public AbstractConstruct parent;
		public string Name;
        public RevoluteHardpointDefinition Revolute;
        public float CurrentRevolution;
        Matrix4 rotation = Matrix4.Identity;
        public void Revolve(float val)
        {
            var clamped = MathHelper.Clamp(val, Revolute.Min, Revolute.Max);
            CurrentRevolution = clamped;
            rotation = Matrix4.CreateFromAxisAngle(Revolute.Axis, clamped);
        }
		public Hardpoint(HardpointDefinition def, AbstractConstruct parent)
		{
			this.parent = parent;
			if (def != null)
				this.transform = def.Transform;
			else
				this.transform = Matrix4.Identity;
			Name = def == null ? "Dummy Hardpoint" : def.Name;
            Revolute = def as RevoluteHardpointDefinition;
            IsStatic = parent is FixConstruct && def is FixedHardpointDefinition;
		}
        public bool IsStatic { get; private set; }

        public Matrix4 HpTransformInfo
        {
            get {
                return transform;
            }
        }
        public Matrix4 TransformNoRotate
        {
            get
            {
                if (parent != null)
                    return transform * parent.Transform;
                else
                    return transform;
            }
        }

		public Matrix4 Transform
		{
			get
			{
                var tr = (rotation * transform);
				if (parent != null)
					return tr * parent.Transform;
				else
					return tr;
			}
		}
		public override string ToString()
		{
			return string.Format("[{0}, IsStatic={1}]", Name, IsStatic);
		}
	}
}

