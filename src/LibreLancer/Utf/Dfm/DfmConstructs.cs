using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.IO;
    
namespace LibreLancer.Utf.Dfm
{
    public class DfmConstructs
    {
        public List<DfmConstruct> Constructs = new List<DfmConstruct>();

        public void AddNode(IntermediateNode root)
        {
            foreach (LeafNode conNode in root)
            {
                using (BinaryReader reader = new BinaryReader(conNode.DataSegment.GetReadStream()))
                {
                    switch (conNode.Name.ToLowerInvariant())
                    {
                        case "sphere":
                            while (reader.BaseStream.Position < reader.BaseStream.Length) Constructs.Add(new DfmSphere(reader));
                            break;
                        case "loose":
                            while (reader.BaseStream.Position < reader.BaseStream.Length) Constructs.Add(new DfmLoose(reader));
                            break;
                    }
                }
            }
        }
    }
    public class DfmConstruct
    {
        public string ParentName;
        public string ChildName;
        public Vector3 Origin;
        public Matrix4x4 Rotation;
        public DfmConstruct(BinaryReader reader)
        {
            int ln = 64;
            byte[] buffer = new byte[64];

            reader.Read(buffer, 0, 64);
            for (int i = 0; i < 64; i++) if (buffer[i] == 0) { ln = i; break; }
            ParentName = Encoding.ASCII.GetString(buffer, 0, ln);
            for (int i = 22; i < 64; i++) buffer[i] = 0;

            ln = 24;
            reader.Read(buffer, 0, 64);
            for(int i = 0; i < 64; i++) if(buffer[i] == 0) { ln = i;  break; }
            ChildName = Encoding.ASCII.GetString(buffer, 0, ln);

            Origin = ConvertData.ToVector3(reader);
        }
    }
    public class DfmLoose : DfmConstruct
    {
        public DfmLoose(BinaryReader reader) : base(reader)
        {
            Rotation = ConvertData.ToMatrix3x3(reader);
        }
    }
    public class DfmSphere : DfmConstruct
    {
        public Vector3 Offset { get; set; }
        public float Min1 { get; set; }
        public float Max1 { get; set; }
        public float Min2 { get; set; }
        public float Max2 { get; set; }
        public float Min3 { get; set; }
        public float Max3 { get; set; }

        public DfmSphere(BinaryReader reader) : base(reader)
        {
            Offset = ConvertData.ToVector3(reader);
            Rotation = ConvertData.ToMatrix3x3(reader);

            Min1 = reader.ReadSingle();
            Max1 = reader.ReadSingle();
            Min2 = reader.ReadSingle();
            Max2 = reader.ReadSingle();
            Min3 = reader.ReadSingle();
            Max3 = reader.ReadSingle();
        }
    }
}
