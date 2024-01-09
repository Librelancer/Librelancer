using System;
using LancerEdit.GameContent.MissionEditor.NodeTypes;
using LibreLancer.Data.Missions;

namespace LancerEdit.GameContent.MissionEditor.Registers;

internal static partial class Registers
{
    internal static void RegisterNodeIo<T>(Node node, ref int pinId) where T : class
    {
        var type = typeof(T);
        _ = true switch
        {
            _ when type == typeof(MissionShip) => RegisterMissionShipIo(node, ref pinId),
            _ when type == typeof(MissionSolar) => RegisterMissionSolarIo(node, ref pinId),
            _ when type == typeof(MissionFormation) => RegisterMissionFormationIo(node, ref pinId),
            _ when type == typeof(MissionDialog) => RegisterDialogFormationIo(node, ref pinId),
            _ when type == typeof(NNObjective) => RegisterMissionNNObjectiveIo(node, ref pinId),
            _ => throw new InvalidOperationException("A type that has not been setup has been registered.")
        };
    }
}
