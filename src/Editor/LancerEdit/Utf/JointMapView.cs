// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ContentEdit;
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
            var b = new AnmBuffer();
            map = new JointMap(EditableUtf.NodeToEngine(node), b);
            b.Shrink();
        }

        private bool open = true;
        public bool Draw()
        {
            ImGui.SetNextWindowSize(new Vector2(420,420), ImGuiCond.FirstUseEver);
            if (ImGui.Begin(ImGuiExt.IDWithExtra($"{name}: {scriptName}", unique), ref open))
            {
                if (string.IsNullOrEmpty(map.ChildName))
                {
                    ImGui.Text($"Target: {map.ParentName}");
                }
                else
                {
                    ImGui.Text($"Parent Name: {map.ParentName}");
                    ImGui.Text($"Child Name: {map.ChildName}");
                }
                ImGui.Text($"Interval: {map.Channel.Interval}");
                ImGui.Text($"Frame Count: {map.Channel.FrameCount}");
                ImGui.Text($"Type: {map.Channel.InterpretedType} (0x{map.Channel.ChannelType.ToString("x")})");
                if (map.Channel.InterpretedType == FrameType.Quaternion ||
                    map.Channel.InterpretedType == FrameType.VecWithQuat)
                {
                    ImGui.Text($"Quaternion Storage: {map.Channel.QuaternionMethod}");
                }
                ImGui.Separator();
                ImGui.BeginChild("##values");
                ImGui.Columns(2);
                float iT = 0;
                for(int i = 0; i < map.Channel.FrameCount; i++)
                {
                    float t = 0;
                    if (map.Channel.Interval > 0)
                    {
                        t = iT;
                        iT += map.Channel.Interval;
                    }
                    else
                        t = map.Channel.GetTime(i);
                    ImGui.Text(t.ToString());
                    ImGui.NextColumn();
                    switch (map.Channel.InterpretedType)
                    {
                        case FrameType.Float:
                            ImGui.Text(map.Channel.GetAngle(i).ToString());
                            break;
                        case FrameType.Vector3:
                            ImGui.Text(map.Channel.GetPosition(i).ToString());
                            break;
                        case FrameType.Quaternion:
                            ImGui.Text(map.Channel.GetQuaternion(i).ToString());
                            break;
                        case FrameType.VecWithQuat:
                            ImGui.Text($"{map.Channel.GetPosition(i)} {map.Channel.GetQuaternion(i)}");
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
