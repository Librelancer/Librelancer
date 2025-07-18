using System;
using System.Numerics;
using ImGuiNET;

namespace LibreLancer.ImUI;

public enum MessageBoxResponse
{
    Ok,
    Cancel,
    Yes,
    No
}

public enum MessageBoxButtons
{
    Ok,
    OkCancel,
    YesNo
}

sealed class MessageBoxPopup : PopupWindow
{
    public override string Title { get; set; }

    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;
    public override bool NoClose => true;

    private string text;
    private MessageBoxButtons buttons;
    private Action<MessageBoxResponse> callback;
    private MessageBoxResponse response;
    private bool multiline;

    public MessageBoxPopup(string title, string text,
        bool multiline = false,
        MessageBoxButtons buttons = MessageBoxButtons.OkCancel,
        Action<MessageBoxResponse> callback = null)
    {
        Title = title;
        this.text = text;
        this.buttons = buttons;
        this.callback = callback;
        this.multiline = multiline;
        response = buttons switch
        {
            MessageBoxButtons.YesNo => MessageBoxResponse.No,
            MessageBoxButtons.Ok => MessageBoxResponse.Ok,
            _ => MessageBoxResponse.Cancel
        };
    }

    public override void Draw(bool appearing)
    {
        if (multiline)
        {
            ImGui.InputTextMultiline(
                "##label",
                ref text,
                uint.MaxValue,
                new Vector2(350, 150) * ImGuiHelper.Scale,
                ImGuiInputTextFlags.ReadOnly
            );
        }
        else
        {
            ImGui.Text(text);
        }
        if (buttons == MessageBoxButtons.Ok)
        {
            if (ImGui.Button("Ok"))
            {
                response = MessageBoxResponse.Ok;
                ImGui.CloseCurrentPopup();
            }
        }
        else if (buttons == MessageBoxButtons.OkCancel)
        {
            if (ImGui.Button("Ok"))
            {
                response = MessageBoxResponse.Ok;
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                response = MessageBoxResponse.Cancel;
                ImGui.CloseCurrentPopup();
            }
        }
        else if (buttons == MessageBoxButtons.YesNo)
        {
            if (ImGui.Button("Yes"))
            {
                response = MessageBoxResponse.Yes;
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("No"))
            {
                response = MessageBoxResponse.No;
                ImGui.CloseCurrentPopup();
            }
        }

    }

    public override void OnClosed()
    {
        callback?.Invoke(response);
    }
}
