using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer.ImUI;

namespace LancerEdit;

public class SearchDropdown<T>
{
    private string id;
    private string inputText = "";
    private string lastFilter = "";

    private T selectedValue;
    private Action<T> onSelected;
    private Func<T, string> displayName;

    private T[] choices;
    private T[] currentChoices;

    public SearchDropdown(
        string id,
        Func<T, string> displayName,
        Action<T> selected,
        T initial,
        T[] choices)
    {
        this.id = id;
        this.displayName = displayName;
        this.onSelected = selected;
        this.choices = choices;
        this.currentChoices = choices;
        SetSelected(initial);
    }

    private bool visibleLastFrame = false;
    private int popupShown = 0;
    private bool activate = false;
    private bool buttonClosed = false;

    public void Activate()
    {
        activate = true;
    }

    public unsafe void Draw()
    {
        var szButton = ImGui.GetFrameHeight();
        ImGui.PushID(id);
        if (activate)
        {
            ImGui.SetKeyboardFocusHere();
            activate = false;
        }

        var w = ImGui.CalcItemWidth();
        ImGui.SetNextItemWidth(w - szButton);
        var enterPressed = ImGui.InputText("##input", ref inputText, 300, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CallbackAlways,
            cb =>
            {
                // Have to clear text in callback due to imgui usage
                // PopupShown is 2 frames to allow scroll logic to work
                if (popupShown == 2)
                {
                    cb->BufDirty = 1;
                    cb->Buf[0] = 0;
                    cb->BufTextLen =  0;
                }
                return 0;
            });
        var textActive = ImGui.IsItemActive();
        var textActivated = ImGui.IsItemActivated();
        if (textActivated)
        {
            ImGui.OpenPopup("##choices");
        }
        if (textActivated || visibleLastFrame && lastFilter != inputText)
        {
            lastFilter = inputText;
            if (string.IsNullOrWhiteSpace(inputText)) {
                currentChoices = choices;
            }
            else {
                var st = inputText.Trim();
                currentChoices = choices.Where(x => displayName(x).Contains(st, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
            }
        }
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        ImGui.SetNextWindowPos(new Vector2(min.X, max.Y));
        ImGui.SetNextWindowSize(new Vector2(max.X - min.X + szButton, (max.Y - min.Y) * 8));
        if (ImGui.BeginPopup("##choices",
                ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.ChildWindow))
        {
            popupShown--;
            if (popupShown < 0) popupShown = 0;
            if (!visibleLastFrame)
                popupShown = 2;
            visibleLastFrame = true;
            for (int i = 0; i < currentChoices.Length; i++)
            {
                var n = displayName(currentChoices[i]);
                bool isSelected = choices[i].Equals(selectedValue);
                if (ImGui.Selectable(n, isSelected))
                {
                    SetSelected(currentChoices[i]);
                    onSelected(currentChoices[i]);
                    ImGui.CloseCurrentPopup();
                }
                if (popupShown > 0 && isSelected) {
                    ImGui.SetScrollHereY();
                }
            }
            if (enterPressed || (!textActive && !ImGui.IsWindowFocused()))
            {
                if (enterPressed)
                {
                    if (currentChoices.Length == 1 && !string.IsNullOrWhiteSpace(inputText)) {
                        SetSelected(currentChoices[0]);
                        onSelected(currentChoices[0]);
                    }
                }
                inputText = displayName(selectedValue);
                ImGui.CloseCurrentPopup();
            }
            if (ImGui.IsKeyPressed(ImGuiKey.Escape))
            {
                inputText = displayName(selectedValue);
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
        else
        {
            visibleLastFrame = false;
        }
        //open/close button
        //we need to handle the state of the button along with the ImGui auto closing of the popup
        //when clicking this button
        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - ImGui.GetStyle().ItemSpacing.X);
        if (ImGui.Button("##activate", new Vector2(szButton))) {
            if(!buttonClosed)
                activate = true;
            buttonClosed = false;
        }
        if (visibleLastFrame && ImGui.IsItemActive()) {
            buttonClosed = true;
        }
        if (ImGui.IsItemDeactivated() && !activate) {
            buttonClosed = false;
        }
        var m = ImGui.GetItemRectMin();
        ImGuiExt.igExtRenderArrow(m.X, m.Y);
        ImGui.PopID();
    }

    public void SetSelected(T value)
    {
        selectedValue = value;
        inputText = displayName(value);
    }
}
