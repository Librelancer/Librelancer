using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;

namespace LancerEdit;

public class SearchDropdown<T>
{
    private string id;
    private string inputText = "";
    private string lastFilter = "";

    // Actual selected value
    private T selectedValue;
    // Temporal value, used for selecting from list with keyboard
    private int selectedIndex = -1;
    private Action<T> onSelected;
    private Func<T, string> displayName;

    private (T Item, int Index)[] allChoices;
    private (T Item, int Index)[] currentChoices;

    // Track current highlight state
    public bool IsOpen { get; private set; }
    public T Hovered { get; private set; }


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
        this.allChoices = choices.Select((x, i) => (x, i)).ToArray();
        this.currentChoices = this.allChoices;
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

    int GetCurrentIndex() => Array.FindIndex(currentChoices, x => x.Index == selectedIndex);


    public unsafe void Draw()
    {
        IsOpen = false;
        Hovered = default;

        var szButton = ImGui.GetFrameHeight();
        ImGui.PushID(id);
        if (activate)
        {
            ImGui.SetKeyboardFocusHere();
            activate = false;
        }

        var w = ImGui.CalcItemWidth();
        ImGui.SetNextItemWidth(w - szButton);

        var enterPressed = ImGui.InputText("##input", ref inputText, 300,
            ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CallbackAlways | ImGuiInputTextFlags.CallbackHistory, TextCallback);
        var textActive = ImGui.IsItemActive();
        var textActivated = ImGui.IsItemActivated();
        if (textActivated)
        {
            ImGui.OpenPopup("##choices");
            UpdateSelectedIndex();
        }
        if (textActivated || visibleLastFrame && lastFilter != inputText)
        {
            lastFilter = inputText;
            if (string.IsNullOrWhiteSpace(inputText)) {
                currentChoices = allChoices;
                UpdateSelectedIndex();
            }
            else {
                var st = inputText.Trim();
                currentChoices = allChoices.Where(x => displayName(x.Item)
                        .Contains(st, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                if (GetCurrentIndex() == -1)
                    selectedIndex = -1;
            }
        }
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        int lines = Math.Min(currentChoices.Length + 1, 8);
        ImGui.SetNextWindowPos(new Vector2(min.X, max.Y));
        ImGui.SetNextWindowSize(new Vector2(max.X - min.X + szButton, (max.Y - min.Y) * lines));
        if (ImGui.BeginPopup("##choices",
                ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.ChildWindow))
        {
            IsOpen = true;
            if(selectedIndex > 0 && selectedIndex < currentChoices.Length)
                Hovered = currentChoices[selectedIndex].Item;
            popupShown--;
            if (popupShown < 0) popupShown = 0;
            if (!visibleLastFrame)
                popupShown = 2;
            visibleLastFrame = true;
            if (nextAction == ACT_UP && currentChoices.Length > 0)
            {
                var idx = GetCurrentIndex();
                if (idx - 1 <= 0)
                {
                    selectedIndex = currentChoices[0].Index;
                }
                else {
                    selectedIndex = currentChoices[idx - 1].Index;
                }
            }
            else if (nextAction == ACT_DOWN && currentChoices.Length > 0)
            {
                var idx = GetCurrentIndex();
                if (idx + 1 >= currentChoices.Length)
                {
                    selectedIndex = currentChoices[^1].Index;
                }
                else
                {
                    selectedIndex = currentChoices[idx + 1].Index;
                }
            }
            for (int i = 0; i < currentChoices.Length; i++)
            {
                var n = displayName(currentChoices[i].Item);
                bool isSelected = currentChoices[i].Index == selectedIndex;
                if (ImGui.Selectable(n, isSelected))
                {
                    SetSelected(currentChoices[i].Item);
                    onSelected(currentChoices[i].Item);
                    ImGui.CloseCurrentPopup();
                }
                if (ImGui.IsItemHovered())
                {
                    Hovered = currentChoices[i].Item;
                }
                if ((popupShown > 0  || nextAction != 0) && isSelected) {
                    ImGui.SetScrollHereY();
                }
            }
            if (enterPressed || (!textActive && !ImGui.IsWindowFocused()))
            {
                if (enterPressed)
                {
                    if (selectedIndex >= 0 &&
                        !allChoices[selectedIndex].Item.Equals(selectedValue)) {
                        SetSelected(allChoices[selectedIndex].Item);
                        onSelected(allChoices[selectedIndex].Item);
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
        nextAction = 0;
        var m = ImGui.GetItemRectMin();
        ImGuiExt.igExtRenderArrow(m.X, m.Y);
        ImGui.PopID();
    }

    private int nextAction = 0;
    private const int ACT_UP = 1;
    private const int ACT_DOWN = 2;

    private unsafe int TextCallback(ImGuiInputTextCallbackData* cb)
    {
        if (cb->EventKey == ImGuiKey.UpArrow) {
            nextAction = ACT_UP;
        }
        if (cb->EventKey == ImGuiKey.DownArrow) {
            nextAction = ACT_DOWN;
        }
        // Have to clear text in callback due to imgui usage
        // PopupShown is 2 frames to allow scroll logic to work
        if (popupShown == 2)
        {
            cb->BufDirty = true;
            ((byte*)cb->Buf)![0] = 0;
            cb->BufTextLen = 0;
        }
        return 0;
    }

    public void SetSelected(T value)
    {
        selectedValue = value;
        inputText = displayName(value);
        UpdateSelectedIndex();
    }

    void UpdateSelectedIndex()
    {
        selectedIndex = -1;
        for (int i = 0; i < allChoices.Length; i++)
        {
            if (allChoices[i].Item.Equals(selectedValue))
            {
                selectedIndex = i;
                break;
            }
        }
    }
}
