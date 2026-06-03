using System.Numerics;
using System.Collections.Generic;
using WattleScript.Interpreter;

namespace LibreLancer.Interface;

[WattleScriptUserData]
public class TargetShipWireframe
{
    internal record struct ChildModel(RigidModel Model, Matrix4x4 Matrix, float Health);

    internal RigidModel? Model;
    internal Matrix4x4 Matrix;
    internal List<ChildModel> ChildModels = [];
}
