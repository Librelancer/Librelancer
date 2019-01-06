// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
namespace LibreLancer.Utf.Cmp
{
    public class CmpCameraInfo
    {
        public float Fovx;
        public float Fovy;
        public float Znear;
        public float Zfar;

        public CmpCameraInfo(IntermediateNode node)
        {
            var cameraNode = (node.FirstOrDefault((x) => x.Name.Equals("camera", StringComparison.OrdinalIgnoreCase)) as IntermediateNode);
            if(cameraNode == null) {
                FLLog.Error("Cmp", "Camera does not contain valid camera node"); //This won't be thrown in normal loading
                return;
            }
            foreach(var child in cameraNode)
            {
                var leaf = (child as LeafNode);
                if(leaf == null)
                {
                    FLLog.Error("Cmp", "Invalid node in camera " + child.Name);
                    continue;
                }
                switch (child.Name.ToLowerInvariant())
                {
                    case "znear":
                        Znear = leaf.SingleData.Value;
                        break;
                    case "zfar":
                        Zfar = leaf.SingleData.Value;
                        break;
                    case "fovx":
                        Fovx = leaf.SingleData.Value;
                        break;
                    case "fovy":
                        Fovy = leaf.SingleData.Value;
                        break;
                    default:
                        FLLog.Error("Cmp", "Invalid node in camera " + child.Name);
                        break;
                }
            }
        }
    }
}
