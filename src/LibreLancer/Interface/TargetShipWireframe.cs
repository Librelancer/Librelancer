using System;
using System.Numerics;
using System.Collections.Generic;
using WattleScript.Interpreter;

namespace LibreLancer.Interface;

[WattleScriptUserData]
public class TargetShipWireframe
{
    internal record struct ChildModel(RigidModel Model, Matrix4x4 Matrix, float Health);
    internal record struct PartModel(uint CRC, float Health, bool Selected);

    internal RigidModel? Model;
    internal Matrix4x4 Matrix;
    internal List<ChildModel> ChildModels = [];
    internal Dictionary<RigidModelPart, PartModel> Parts = [];
    internal Action<uint?>? PartSelected;
}
