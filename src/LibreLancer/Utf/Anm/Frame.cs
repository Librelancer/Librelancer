/* The contents of this file a
 * re subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * The Original Code is Starchart code (http://flapi.sourceforge.net/).
 * Data structure from Freelancer UTF Editor by Cannon & Adoxa, continuing the work of Colin Sanby and Mario 'HCl' Brito (http://the-starport.net)
 * 
 * The Initial Developer of the Original Code is Malte Rupprecht (mailto:rupprema@googlemail.com).
 * Portions created by the Initial Developer are Copyright (C) 2012
 * the Initial Developer. All Rights Reserved.
 */

using System;
using System.Collections.Generic;
using System.IO;

namespace LibreLancer.Utf.Anm
{
    public class Frame
    {
        public float? Time { get; private set; }
		public float JointValue { get; private set; }
		public Matrix4 ObjectValue { get; private set; }
		public Frame(BinaryReader reader, bool time, bool matrix)
        {
            if (time) Time = reader.ReadSingle();
			if (matrix)
			{
				ObjectValue = ConvertData.ToMatrix3x3(reader);
			}
			else
			{
				JointValue = reader.ReadSingle();
			}
        }

        public override string ToString()
        {
			return "Frame";
        }
    }
}
