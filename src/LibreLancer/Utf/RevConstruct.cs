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
 * The Original Code is FlLApi code (http://flapi.sourceforge.net/).
 * Data structure from Freelancer UTF Editor by Cannon & Adoxa, continuing the work of Colin Sanby and Mario 'HCl' Brito (http://the-starport.net)
 * 
 * The Initial Developer of the Original Code is Malte Rupprecht (mailto:rupprema@googlemail.com).
 * Portions created by the Initial Developer are Copyright (C) 2011, 2012
 * the Initial Developer. All Rights Reserved.
 */

using System.Collections.Generic;
using System.IO;

namespace LibreLancer.Utf
{
    public class RevConstruct : AbstractConstruct
    {
        public Vector3 Offset { get; set; }
        public Vector3 AxisRotation { get; set; }
        public float Min { get; set; }
        public float Max { get; set; }

        private Matrix4 currentTransform = Matrix4.Identity;

		public override Matrix4 Transform { get { return internalGetTransform((Rotation * currentTransform) * Matrix4.CreateTranslation(Origin + Offset)); } }

        public RevConstruct(ConstructCollection constructs) : base(constructs) {}

        public RevConstruct(BinaryReader reader, ConstructCollection constructs)
            : base(reader, constructs)
        {
            Offset = ConvertData.ToVector3(reader);
            Rotation = ConvertData.ToMatrix3x3(reader);
            AxisRotation = ConvertData.ToVector3(reader);

            Min = reader.ReadSingle();
            Max = reader.ReadSingle();
        }

		protected RevConstruct(RevConstruct cf) : base(cf) { }
		public override AbstractConstruct Clone(ConstructCollection newcol)
		{
			var newc = new RevConstruct(this);
			newc.Offset = Offset;
			newc.AxisRotation = AxisRotation;
			newc.Min = Min;
			newc.Max = Max;
			newc.constructs = newcol;
			return newc;
		}
        public override void Reset()
        {
            currentTransform = Matrix4.Identity;
        }
        public float Current = 0;
        public override void Update(float distance)
        {
            Current = MathHelper.Clamp(distance, Min, Max);
			currentTransform = Matrix4.CreateFromAxisAngle(AxisRotation, Current);
        }
    }
}
