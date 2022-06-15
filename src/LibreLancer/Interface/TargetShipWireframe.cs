using System.Numerics;
using WattleScript.Interpreter;

namespace LibreLancer.Interface;

[WattleScriptUserData]
public class TargetShipWireframe
{
    internal RigidModel Model;
    internal Matrix4x4 Matrix;
}