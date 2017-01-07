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
 * The Original Code is FLApi code (http://flapi.sourceforge.net/).
 * 
 * The Initial Developer of the Original Code is Malte Rupprecht (mailto:rupprema@googlemail.com).
 * Portions created by the Initial Developer are Copyright (C) 2011, 2012
 * the Initial Developer. All Rights Reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;


using LibreLancer.Utf.Anm;

namespace LibreLancer.Utf
{
    public abstract class AbstractConstruct
    {
        const int STR_LENGTH = 64;
        protected ConstructCollection constructs;

        public string ParentName { get; private set; }
        public string ChildName { get; private set; }
        public Vector3 Origin { get; set; }
        public Matrix4 Rotation { get; set; }

        public abstract Matrix4 Transform { get; }
		bool parentExists = true;
        protected AbstractConstruct(BinaryReader reader, ConstructCollection constructs)
        {
            if (reader == null) throw new ArgumentNullException("reader");
            if (constructs == null) throw new ArgumentNullException("construct");

            this.constructs = constructs;

            byte[] buffer = new byte[STR_LENGTH];

            reader.Read(buffer, 0, STR_LENGTH);
            ParentName = Encoding.ASCII.GetString(buffer);
            ParentName = ParentName.Substring(0, ParentName.IndexOf('\0'));

            reader.Read(buffer, 0, STR_LENGTH);
            ChildName = Encoding.ASCII.GetString(buffer);
            ChildName = ChildName.Substring(0, ChildName.IndexOf('\0'));

            Origin = ConvertData.ToVector3(reader);
        }

		protected AbstractConstruct(AbstractConstruct cloneFrom)
		{
			ParentName = cloneFrom.ParentName;
			ChildName = cloneFrom.ChildName;
			Origin = cloneFrom.Origin;
			Rotation = cloneFrom.Rotation;
		}

		public abstract AbstractConstruct Clone(ConstructCollection newcol);

		AbstractConstruct parent;
        protected Matrix4 internalGetTransform(Matrix4 matrix)
        {
			if (parentExists)
			{
				if(parent == null)
					parent = constructs.Find(ParentName);
				if (parent != null)
					matrix = matrix * parent.Transform;
				else
					parentExists = false;
			}

            return matrix;
        }

        public abstract void Update(float distance);
    }
}
