// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.IO;
using System.Text;

namespace LibreLancer.Utf
{
    public abstract class AbstractConstruct
    {
        const int STR_LENGTH = 64;
        protected ConstructCollection constructs;

        public string ParentName { get; set; }
        public string ChildName { get; set; }
        public Vector3 Origin { get; set; }
        public Matrix4 Rotation { get; set; }

        public abstract Matrix4 Transform { get; }
		bool parentExists = true;

        public Matrix4? OverrideTransform;

        protected AbstractConstruct(ConstructCollection constructs)
        {
            this.constructs = constructs;
        }

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
        public void ClearParent()
        {
            parent = null;
            parentExists = true;
        }
		public abstract AbstractConstruct Clone(ConstructCollection newcol);

		AbstractConstruct parent;
        protected Matrix4 internalGetTransform(Matrix4 matrix)
        {
            if (OverrideTransform != null)
                matrix = OverrideTransform.Value;
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
        public abstract void Reset();
        public abstract void Update(float distance);
    }
}
