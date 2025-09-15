using System.Runtime.CompilerServices;
using LibreLancer.Utf.Anm;

namespace LibreLancer.ContentEdit.Model;

public static class AnimationWriter
{
    static LUtfNode WriteChannel(ref Channel channel)
    {
        var channelNode = new LUtfNode() { Name = "Channel", Children = [] };
        var channelHeader = new LUtfNode() { Name = "Header", Parent = channelNode, Data = new byte[12] };
        Unsafe.WriteUnaligned(ref channelHeader.Data[0], channel.FrameCount);
        Unsafe.WriteUnaligned(ref channelHeader.Data[4], channel.Interval);
        Unsafe.WriteUnaligned(ref channelHeader.Data[8], channel.ChannelType);
        channelNode.Children.Add(channelHeader);
        channelNode.Children.Add(new LUtfNode() { Name = "Frames", Parent = channelNode, Data = channel.GetDataCopy() });
        return channelNode;
    }

    public static void WriteAnimations(LUtfNode destNode, AnmFile anm)
    {
        var collectionNode = new LUtfNode() { Name = "Script", Parent = destNode, Children = [] };
        foreach (var script in anm.Scripts.Values)
        {
            var scriptNode = new LUtfNode() { Name = script.Name, Children = [], Parent = collectionNode };
            collectionNode.Children.Add(scriptNode);
            if (script.HasRootHeight)
            {
                scriptNode.Children.Add(LUtfNode.FloatNode(scriptNode, "Root Height", script.RootHeight));
            }

            for (int i = 0; i < script.ObjectMaps.Count; i++)
            {
                ref var om =  ref script.ObjectMaps[i];
                var omNode = new LUtfNode() { Name = $"Object map {i}", Parent = scriptNode };
                scriptNode.Children.Add(omNode);
                omNode.Children = [LUtfNode.StringNode(omNode, "Parent Name", om.ParentName)];
                var channelNode = WriteChannel(ref om.Channel);
                channelNode.Parent = omNode;
                omNode.Children.Add(channelNode);
            }

            for (int i = 0; i < script.JointMaps.Count; i++)
            {
                ref var jm = ref script.JointMaps[i];
                var jmNode = new LUtfNode() { Name = $"Joint map {i}", Parent = scriptNode };
                scriptNode.Children.Add(jmNode);
                jmNode.Children = [
                    LUtfNode.StringNode(jmNode, "Parent Name", jm.ParentName),
                    LUtfNode.StringNode(jmNode, "Child Name", jm.ChildName)
                ];
                var channelNode = WriteChannel(ref jm.Channel);
                channelNode.Parent = jmNode;
                jmNode.Children.Add(channelNode);
            }
        }
        destNode.Children = [collectionNode];
    }
}
