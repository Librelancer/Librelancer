// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.IO;
using System.Text;
using System.Numerics;

namespace LibreLancer.Utf
{
    public abstract class AbstractConstruct
    {
        const int STR_LENGTH = 64;
        public string ParentName { get; set; }
        public string ChildName { get; set; }
        public Vector3 Origin { get; set; }
        public Quaternion Rotation { get; set; }

        public abstract Transform3D LocalTransform { get; }

        public Transform3D? OverrideTransform;

        protected AbstractConstruct()
        {
        }

        protected AbstractConstruct(BinaryReader reader)
        {
            if (reader == null) throw new ArgumentNullException("reader");

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

        public abstract AbstractConstruct Clone();

        protected Transform3D internalGetTransform(Transform3D tr)
        {
            if (OverrideTransform != null)
                tr = OverrideTransform.Value;
            return tr;
        }
        public abstract void Reset();
        public abstract void Update(float distance, Quaternion quat);
    }
}
