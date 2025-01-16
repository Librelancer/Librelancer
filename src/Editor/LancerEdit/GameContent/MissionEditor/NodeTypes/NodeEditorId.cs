using System.Threading;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes;

public static class NodeEditorId
{
    private static int _id = 1;
    public static int Next() => Interlocked.Increment(ref _id);
}
