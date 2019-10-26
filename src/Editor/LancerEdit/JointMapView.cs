// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;
using LibreLancer.Utf.Anm;

namespace LancerEdit
{
    public class JointMapView
    {
        private static int uniqueCount = int.MaxValue;
        private int unique;
        private JointMap map;
        private string name;
        private string scriptName;
        public static JointMapView Create(LUtfNode node)
        {
            try
            {
                return new JointMapView(node);
            }
            catch (Exception)
            {
                return null;
            }   
        }
        
        private JointMapView(LUtfNode node)
        {
            unique = uniqueCount--;
            name = node.Name;
            scriptName = node.Parent.Name;
            map = new JointMap(EditableUtf.NodeToEngine(node));
        }
        
        private bool open = true;
        public bool Draw()
        {
            ImGui.SetNextWindowSize(new Vector2(420,420), ImGuiCond.FirstUseEver);
            if (ImGui.Begin(ImGuiExt.IDWithExtra($"{name}: {scriptName}", unique), ref open))
            {
                ImGui.Text($"Parent Name: {map.ParentName}");
                ImGui.Text($"Child Name: {map.ChildName}");
                ImGui.Text($"Interval: {map.Channel.Interval}");
                ImGui.Text($"Frame Count: {map.Channel.FrameCount}");
                ImGui.Text($"Type: {map.Channel.InterpretedType} (0x{map.Channel.ChannelType.ToString("x")})");
                ImGui.Separator();
                ImGui.BeginChild("##values");
                ImGui.Columns(2);
                float iT = 0;
                foreach (var frame in map.Channel.Frames)
                {
                    float t = 0;
                    if (map.Channel.Interval > 0)
                    {
                        t = iT;
                        iT += map.Channel.Interval;
                    }
                    else
                        t = frame.Time.Value;
                    ImGui.Text(t.ToString());
                    ImGui.NextColumn();
                    switch (map.Channel.InterpretedType)
                    {
                        case FrameType.Float:
                            ImGui.Text(frame.JointValue.ToString());
                            break;
                        case FrameType.Normal:
                            ImGui.Text(frame.NormalValue.ToString());
                            break;
                        case FrameType.Vector3:
                            ImGui.Text(frame.VectorValue.ToString());
                            break;
                        case FrameType.Quaternion:
                            ImGui.Text(frame.QuatValue.ToString());
                            break;
                        case FrameType.VecWithQuat:
                            ImGui.Text($"{frame.VectorValue} {frame.QuatValue}");
                            break;
                    }
                    ImGui.NextColumn();
                }
                ImGui.EndChild();
                ImGui.End();
            }
            else
                return false;
            return open;
        }
        
    }
}