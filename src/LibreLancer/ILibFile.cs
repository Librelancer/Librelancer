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
        TextureData FindTexture(string name);
        Material FindMaterial(uint materialId);
        VMeshData FindMesh(uint vMeshLibId);
    }
}
