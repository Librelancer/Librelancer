// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Vms;
namespace LibreLancer
{
    public interface ILibFile
    {
        Texture FindTexture(string name);
        Material FindMaterial(uint materialId);
        VMeshData FindMesh(uint vMeshLibId);
    }
}
