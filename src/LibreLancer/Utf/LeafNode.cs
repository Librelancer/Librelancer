// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Numerics;


namespace LibreLancer.Utf
{
    public class LeafNode : Node
    {
        private byte[] dataArray;
        private int dataStart;
        private int dataLength;

        public bool PossibleCompression { get; private set; }

        public ArraySegment<byte> DataSegment => new ArraySegment<byte>(dataArray, dataStart, dataLength);
        /// <summary>
        /// Returns a COPY of the data. Use LeafNode.DataSegment where possible
        /// </summary>
        public byte[] ByteArrayData
        {
            get
            {
                var result = new byte[dataLength];
                Buffer.BlockCopy(dataArray, dataStart, result, 0, dataLength);
                return result;
            }
        }

        public int? Int32Data
        {
            get
            {
                if (dataLength % sizeof(int) == 0) return BitConverter.ToInt32(dataArray, dataStart);
                else return null;
            }
        }

        public unsafe int[] Int32ArrayData
        {
            get
            {
                var result = new int[dataLength / sizeof(int)];
                int len = result.Length * sizeof(int);
                fixed (byte* pinnedSrc = dataArray)
                {
                    fixed (int* pinnedDst = result)
                    {
                        Buffer.MemoryCopy((&pinnedSrc[dataStart]), pinnedDst, len, len);
                    }
                }
                return result;
            }
        }

        public unsafe ushort[] UInt16ArrayData
        {
            get
            {
                var result = new ushort[dataLength / sizeof(ushort)];
                int len = result.Length * sizeof(ushort);
                fixed (byte* pinnedSrc = dataArray)
                {
                    fixed (ushort* pinnedDst = result)
                    {
                        Buffer.MemoryCopy((&pinnedSrc[dataStart]), pinnedDst, len, len);
                    }
                }
                return result;
            }
        }

        public float? SingleData
        {
            get
            {
                if (dataLength % sizeof(float) == 0) return BitConverter.ToSingle(dataArray, dataStart);
                else return null;
            }
        }

        public unsafe float[] SingleArrayData
        {
            get
            {
                var result = new float[dataLength / sizeof(float)];
                int len = result.Length * sizeof(float);
                fixed (byte* pinnedSrc = dataArray)
                {
                    fixed (float* pinnedDst = result)
                    {
                        Buffer.MemoryCopy((&pinnedSrc[dataStart]), pinnedDst, len, len);
                    }
                }
                return result;
            }
        }

        public string StringData
        {
            get
            {
                int len = dataLength;
                for (int i = 0; i < dataLength; i++)  {
                    if (dataArray[dataStart + i] == 0) {
                        len = i;
                        break;
                    }
                }
                return Encoding.ASCII.GetString(dataArray, dataStart, len);
            }
        }

        public unsafe Vector2? Vector2Data
        {
            get
            {
                if (dataLength == sizeof(float) * 2)
                {
                    fixed (byte* pinnedData = dataArray)
                    {
                        return *(Vector2*) (&pinnedData[dataStart]);
                    }
                }
                else return null;
            }
        }

        public unsafe Vector2[] Vector2ArrayData
        {
            get
            {
                var result = new Vector2[dataLength / (sizeof(float) * 2)];
                int len = result.Length * (sizeof(float) * 2);
                fixed (byte* pinnedSrc = dataArray)
                {
                    fixed (Vector2* pinnedDst = result)
                    {
                        Buffer.MemoryCopy((&pinnedSrc[dataStart]), pinnedDst, len, len);
                    }
                }
                return result;
            }
        }

        public Vector3? Vector3Data
        {
            get
            {
                if (dataLength == sizeof(float) * 3) return ConvertData.ToVector3(dataArray, dataStart);
                else return null;
            }
        }

        public Vector3[] Vector3ArrayData => ConvertData.ToVector3Array(dataArray, dataStart, dataLength);

        public Matrix4x4? MatrixData3x3
        {
            get
            {
                if (dataLength == sizeof(float) * 9) return ConvertData.ToMatrix3x3(dataArray, dataStart);
                else return null;
            }
        }

        public Matrix4x4? MatrixData4x3
        {
            get
            {
                if (dataLength == sizeof(float) * 12) return ConvertData.ToMatrix4x3(dataArray, dataStart);
                else return null;
            }
        }

        public Color4? ColorData
        {
            get
            {
                if (dataLength == sizeof(float) * 3) return ConvertData.ToColor(dataArray, dataStart);
                else return null;
            }
        }

		public LeafNode(string name, byte[] data) : base(name)
		{
			this.dataArray = data;
            dataStart = 0;
            dataLength = data.Length;
        }

        public LeafNode(string name, byte[] data, int start, int len) : base(name)
        {
            this.dataArray = data;
            dataStart = start;
            dataLength = len;
        }

        private static readonly byte[] empty = new byte[0];

        internal static LeafNode LeafV2(string name, BinaryReader reader, byte[] dataBlock)
        {
            int start = (int)reader.ReadVarUInt64();
            int len = (int)reader.ReadVarUInt64();
            return new LeafNode(name, dataBlock, start, len);
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

            this.dataStart = dataOffset;

            int size = reader.ReadInt32();
            int size2 = reader.ReadInt32();
            dataArray = dataBlock;
            PossibleCompression = size != size2;
            this.dataLength = size;
            //int timestamp1 = reader.ReadInt32();
            //int timestamp2 = reader.ReadInt32();
            //int timestamp3 = reader.ReadInt32();
        }

        public override string ToString()
        {
            return "{Leaf: " + base.ToString() + ", Data: " + dataStart + ": " + dataLength + "B}";
        }
    }
}
