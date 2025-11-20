using System;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ContentEdit.Model;
using LibreLancer.ImUI;
using LibreLancer.Utf.Anm;

namespace LancerEdit;

public class JointMapEditor : PopupWindow
{
    public override string Title { get; set; }

    public override Vector2 InitSize => new Vector2(400, 400) * ImGuiHelper.Scale;

    private RefList<JointMap> _srcList;
    private int _srcIndex;
    ref JointMap DestMap => ref _srcList[_srcIndex];

    private EditableAnmBuffer buffer;
    private JointMap jointMap;

    public JointMapEditor(RefList<JointMap> jms, int index, string scriptName)
    {
        _srcList = jms;
        _srcIndex = index;
        Title = $"{DestMap.ChildName} ({scriptName})";

        buffer = new EditableAnmBuffer(ref DestMap.Channel);
        jointMap = DestMap;
        jointMap.Channel.SetBuffer(buffer);
    }

    private const int SUPPORTED_MASK = 0x1 | 0x2 | 0x4;

    bool IsChannelType(int c) => (jointMap.Channel.ChannelType & c) == c;
    bool IsNewChannelType(int c, ref Channel ch) => (ch.ChannelType & c) == c;

    void DeleteFrame(int i)
    {
        int count = jointMap.Channel.FrameCount;
        var stride = jointMap.Channel.Stride;
        if (i != count - 1)
        {
            // Delete element
            Array.Copy(buffer.Buffer, (i + 1) * stride, buffer.Buffer, i * stride,
                (count - i - 1) * stride);
        }
        jointMap.Channel.FrameCount--;
    }

    (Channel, EditableAnmBuffer) RebuildChannel(float interval, int channelType)
    {
        var ch2 = new Channel()
        {
            FrameCount = jointMap.Channel.FrameCount,
            Interval = interval,
            ChannelType = channelType
        };
        var buf2 = new EditableAnmBuffer(ch2.Stride * ch2.FrameCount);
        ch2.SetBuffer(buf2);
        for (int i = 0; i < jointMap.Channel.FrameCount; i++)
        {
            if (interval < 0)
            {
                buf2.SetTime(ref ch2, i, jointMap.Channel.GetTime(i));
            }
            if (IsNewChannelType(0x1, ref ch2) &&
                IsChannelType(0x1))
            {
                buf2.SetAngle(ref ch2, i, jointMap.Channel.GetAngle(i));
            }
            if (IsNewChannelType(0x2, ref ch2) &&
                IsChannelType(0x2))
            {
                buf2.SetVector(ref ch2, i, jointMap.Channel.GetPosition(i));
            }
            if (IsNewChannelType(0x4, ref ch2))
            {
                if (IsChannelType(0x4))
                {
                    buf2.SetQuaternion(ref ch2, i, jointMap.Channel.GetQuaternion(i));
                }
                else
                {
                    buf2.SetQuaternion(ref ch2, i, Quaternion.Identity);
                }
            }
        }
        return (ch2, buf2);
    }

    void RemoveTime()
    {
        (jointMap.Channel, buffer) = RebuildChannel(0.33f, jointMap.Channel.ChannelType);
    }

    void AddTime()
    {
        (jointMap.Channel, buffer) = RebuildChannel(-1f, jointMap.Channel.ChannelType);
    }

    public override void Draw(bool appearing)
    {
        if ((jointMap.Channel.ChannelType & ~SUPPORTED_MASK) != 0)
        {
            ImGui.Text($"Joint Map unsupported kind: 0x{jointMap.Channel.ChannelType:X}");
            return;
        }
        if (ImGui.Button("Save"))
        {
            DestMap = jointMap;
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Discard"))
        {
            ImGui.CloseCurrentPopup();
        }
        ImGui.Separator();

        if (jointMap.Channel.Interval < 0)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Manual Time.");
            ImGui.SameLine();
            if (ImGui.Button("Use Interval"))
            {
                RemoveTime();
            }
        }
        else
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Interval: ");
            ImGui.SameLine();
            ImGui.PushItemWidth(120 * ImGuiHelper.Scale);
            ImGui.InputFloat("##interval", ref jointMap.Channel.Interval);
            if (jointMap.Channel.Interval < 0.0001f)
                jointMap.Channel.Interval = 0.0001f;
            ImGui.PopItemWidth();
            ImGui.SameLine();
            if (ImGui.Button("Use Manual Time"))
            {
                AddTime();
            }
        }

        int valueCount = BitOperations.PopCount((uint)jointMap.Channel.ChannelType & SUPPORTED_MASK);
        // frame, time, [values], del
        int toDelete = -1;
        var sz = ImGui.GetContentRegionAvail();
        sz.Y -= ImGui.GetFrameHeightWithSpacing();
        if (sz.Y < 10) sz.Y = 10;
        if (jointMap.Channel.FrameCount > 0 &&
            ImGui.BeginTable("##frames", valueCount+3,
                ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoHostExtendY |
                ImGuiTableFlags.RowBg, sz))
        {
            ImGui.TableSetupColumn("Frame", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Time");
            if (IsChannelType(0x1))
            {
                ImGui.TableSetupColumn("Scalar");
            }
            if (IsChannelType(0x2))
            {
                ImGui.TableSetupColumn("Position");
            }
            if (IsChannelType(0x4))
            {
                ImGui.TableSetupColumn("Orientation");
            }

            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupScrollFreeze(0,1);
            ImGui.TableHeadersRow();
            for (int i = 0; i < jointMap.Channel.FrameCount; i++)
            {
                ImGui.PushID(i);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text($"{i}");
                ImGui.TableNextColumn();
                if (jointMap.Channel.Interval < 0)
                {
                    var told = jointMap.Channel.GetTime(i);
                    var t = told;
                    ImGui.InputFloat("##time", ref t);
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if(told != t)
                        buffer.SetTime(ref jointMap.Channel, i, t);
                }
                else
                {
                    ImGui.Text($"{jointMap.Channel.GetTime(i)}");
                }
                if (IsChannelType(0x1))
                {
                    ImGui.TableNextColumn();
                    var fold = jointMap.Channel.GetAngle(i);
                    var f = fold;
                    ImGui.InputFloat("##scalar", ref f);
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if(fold != f)
                        buffer.SetAngle(ref jointMap.Channel, i, f);
                }
                if (IsChannelType(0x2))
                {
                    ImGui.TableNextColumn();
                    var vold = jointMap.Channel.GetPosition(i);
                    var v = vold;
                    ImGui.InputFloat3("##position", ref v);
                    if(vold != v)
                        buffer.SetVector(ref jointMap.Channel, i, v);
                }
                if (IsChannelType(0x4))
                {
                    ImGui.TableNextColumn();
                    var qsrc = jointMap.Channel.GetQuaternion(i);
                    var vold = new Vector4(qsrc.X, qsrc.Y, qsrc.Z, qsrc.W);
                    var v = vold;
                    ImGui.InputFloat4("##orientation", ref v);
                    if (v != vold)
                    {
                        var qnew = Quaternion.Normalize(new Quaternion(v.X, v.Y, v.Z, v.W));
                        buffer.SetQuaternion(ref jointMap.Channel, i, qnew);
                    }
                }
                ImGui.TableNextColumn();
                if (ImGui.Button($"{Icons.TrashAlt}"))
                {
                    toDelete = i;
                }
                ImGui.PopID();
            }
            ImGui.EndTable();
        }

        if (ImGui.Button("Add Frame"))
        {
            buffer.EnsureSize(jointMap.Channel.Stride * (jointMap.Channel.FrameCount + 1));
            if (IsChannelType(0x4))
            {
                buffer.SetQuaternion(ref jointMap.Channel, jointMap.Channel.FrameCount, Quaternion.Identity);
            }
            jointMap.Channel.FrameCount++;
        }

        if (toDelete != -1)
        {
            DeleteFrame(toDelete);
        }
    }
}
