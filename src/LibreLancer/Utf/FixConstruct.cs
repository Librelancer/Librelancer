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

using System;
using System.Collections.Generic;
using System.IO;

using OpenTK;

//using FLParser;
//using FLCommon;
namespace LibreLancer.Utf
{
    public class FixConstruct : AbstractConstruct
    {
        public override Matrix4 Transform { get { return internalGetTransform(Rotation * Matrix4.CreateTranslation(Origin)); } }

        public FixConstruct(BinaryReader reader, ConstructCollection constructs)
            : base(reader, constructs)
        {
            Rotation = ConvertData.ToMatrix3x3(reader);
        }

        public override void Update(float distance)
        {
            throw new NotImplementedException();
        }
    }
}
