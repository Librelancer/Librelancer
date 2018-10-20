// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibreLancer.Utf
{
    public class LeafNode : Node
    {
        private byte[] data;

        public bool PossibleCompression { get; private set; }

        public byte[] ByteArrayData { get { return data; } }

        public int? Int32Data
        {
            get
            {
                if (data.Length % sizeof(int) == 0) return BitConverter.ToInt32(data, 0);
                else return null;
            }
        }

        public int[] Int32ArrayData
        {
            get
            {
                List<int> result = new List<int>();

                using (BinaryReader reader = new BinaryReader(new MemoryStream(data)))
                {
                    while (reader.BaseStream.Position <= reader.BaseStream.Length - sizeof(int))
                    {
                        result.Add(reader.ReadInt32());
                    }
                }

                return result.ToArray();
            }
        }

        public ushort? UInt16Data
        {
            get
            {
                if (data.Length == sizeof(ushort)) return BitConverter.ToUInt16(data, 0);
                else return null;
            }
        }

        public ushort[] UInt16ArrayData
        {
            get
            {
                List<ushort> result = new List<ushort>();

                using (BinaryReader reader = new BinaryReader(new MemoryStream(data)))
                {
                    while (reader.BaseStream.Position <= reader.BaseStream.Length - sizeof(ushort))
                    {
                        result.Add(reader.ReadUInt16());
                    }
                }

                return result.ToArray();
            }
        }

        public float? SingleData
        {
            get
            {
                if (data.Length % sizeof(float) == 0) return BitConverter.ToSingle(data, 0);
                else return null;
            }
        }

        public float[] SingleArrayData
        {
            get
            {
                List<float> result = new List<float>();

                using (BinaryReader reader = new BinaryReader(new MemoryStream(data)))
                {
                    while (reader.BaseStream.Position <= reader.BaseStream.Length - sizeof(float))
                    {
                        result.Add(reader.ReadSingle());
                    }
                }

                return result.ToArray();
            }
        }

        public string StringData
        {
            get
            {
                return Encoding.ASCII.GetString(data).TrimEnd('\0');
            }
        }

        public Vector2? Vector2Data
        {
            get
            {
                if (data.Length == sizeof(float) * 2) return ConvertData.ToVector2(data);
                else return null;
            }
        }

        public Vector2[] Vector2ArrayData
        {
            get
            {
                return ConvertData.ToVector2Array(data);
            }
        }

        public Vector3? Vector3Data
        {
            get
            {
                if (data.Length == sizeof(float) * 3) return ConvertData.ToVector3(data);
                else return null;
            }
        }

        public Vector3[] Vector3ArrayData
        {
            get
            {
                return ConvertData.ToVector3Array(data);
            }
        }

        public Matrix4? MatrixData3x3
        {
            get
            {
                if (data.Length == sizeof(float) * 9) return ConvertData.ToMatrix3x3(data);
                else return null;
            }
        }

        public Matrix4? MatrixData4x3
        {
            get
            {
                if (data.Length == sizeof(float) * 12) return ConvertData.ToMatrix4x3(data);
                else return null;
            }
        }

        public Color4? ColorData
        {
            get
            {
                if (data.Length == sizeof(float) * 3) return ConvertData.ToColor(data);
                else return null;
            }
        }

		public LeafNode(string name, byte[] data) : base(name)
		{
			this.data = data;
		}

        public LeafNode(int peerOffset, string name, BinaryReader reader, byte[] dataBlock)
            : base(peerOffset, name)
        {
            if (reader == null) throw new ArgumentNullException("reader");
            if (dataBlock == null) throw new ArgumentNullException("dataBlock");

            //int zero = reader.ReadInt32();
            reader.BaseStream.Seek(sizeof(int), SeekOrigin.Current);

            int dataOffset = reader.ReadInt32();

            //int allocatedSize = reader.ReadInt32();
            reader.BaseStream.Seek(sizeof(int), SeekOrigin.Current);

            int size = reader.ReadInt32();
            if (size == 0) data = new byte[0];
            else
            {
                data = new byte[size];
                Array.Copy(dataBlock, dataOffset, data, 0, size);
            }

            int size2 = reader.ReadInt32();
            PossibleCompression = size != size2;

            //int timestamp1 = reader.ReadInt32();
            //int timestamp2 = reader.ReadInt32();
            //int timestamp3 = reader.ReadInt32();
        }

        public override string ToString()
        {
            return "{Leaf: " + base.ToString() + ", Data: " + data.Length + "B}";
        }
    }
}
