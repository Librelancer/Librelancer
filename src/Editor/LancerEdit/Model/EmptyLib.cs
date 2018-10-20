// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Vms;

namespace LancerEdit
{
    //Empty class for constructing VMeshData etc. for dumping
    public class EmptyLib : ILibFile
    {
        public EmptyLib()
        {
        }

        public Material FindMaterial(uint materialId)
        {
            throw new InvalidOperationException("EmptyLib usage");
        }

        public VMeshData FindMesh(uint vMeshLibId)
        {
            throw new InvalidOperationException("EmptyLib usage");
        }

        public Texture FindTexture(string name)
        {
            throw new InvalidOperationException("EmptyLib usage");
        }
    }
}
