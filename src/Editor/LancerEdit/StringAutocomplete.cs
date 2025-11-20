using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer.ImUI;

namespace LancerEdit;

public class StringAutocomplete
{
    private string id;
    private string inputText;
    private string currentValue;
    private string lastFilter = "";
    private (int Index, string Value)[] allChoices;
    private (int Index, string Value)[] currentChoices;
    private Action<string> committed;

    public StringAutocomplete(
        string id,
        string[] choices,
        string initial,
        Action<string> commit
    )
    {
        this.id = id;
        inputText = currentValue = initial;
        allChoices = choices.Select((x, y) => (y, x)).ToArray();
        currentChoices = allChoices;
        this.committed = commit;
    }

    // Temporal value, used for selecting from list with keyboard
    private int selectedIndex = -1;

    public bool IsOpen { get; private set; }
    public string Hovered { get; private set; }


    private int nextAction = 0;
    private const int ACT_UP = 1;
    private const int ACT_DOWN = 2;

    private bool visibleLastFrame = false;
    private int popupShown = 0;
    private bool activate = false;
    private bool buttonClosed = false;

    public void Activate()
    {
        activate = true;
    }

    void OnValueChange(string value)
    {
        if (value != currentValue)
        {
            currentValue = inputText = value;
            committed(currentValue);
        }
    }

    public unsafe void Draw()
    {
        IsOpen = false;
        Hovered = null;
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
                currentChoices = allChoices.Where(x => x.Value
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
                Hovered = currentChoices[selectedIndex].Value;
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
                bool isSelected = currentChoices[i].Index == selectedIndex;
                if (ImGui.Selectable(currentChoices[i].Value, isSelected))
                {
                    inputText = currentChoices[i].Value;
                    OnValueChange(currentChoices[i].Value);
                    ImGui.CloseCurrentPopup();
                }
                if (ImGui.IsItemHovered())
                {
                    Hovered = currentChoices[i].Value;
                }
                if ((popupShown > 0  || nextAction != 0) && isSelected) {
                    ImGui.SetScrollHereY();
                }
            }
            if (enterPressed || (!textActive && !ImGui.IsWindowFocused()))
            {
                if (enterPressed)
                {
                    if(selectedIndex != -1)
                        inputText = allChoices[selectedIndex].Value;
                    OnValueChange(inputText);
                }
                ImGui.CloseCurrentPopup();
            }
            if (ImGui.IsKeyPressed(ImGuiKey.Escape))
            {
                inputText = currentValue;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
        else
        {
            visibleLastFrame = false;
            if (!textActive && !textActivated)
            {
                OnValueChange(inputText);
            }
        }
        //open/close button
        //we need to handle the state of the button along with the ImGui auto closing of the popup
        //when clicking this button
        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - ImGui.GetStyle().ItemSpacing.X - 2 * ImGuiHelper.Scale);
        if (ImGuiExt.ButtonRounding("##activate", new Vector2(szButton), ImDrawFlags.RoundCornersRight)) {
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

    private unsafe int TextCallback(ImGuiInputTextCallbackData* cb)
    {
        if (cb->EventKey == ImGuiKey.UpArrow) {
            nextAction = ACT_UP;
        }
        if (cb->EventKey == ImGuiKey.DownArrow) {
            nextAction = ACT_DOWN;
        }
        return 0;
    }

    void UpdateSelectedIndex()
    {
        selectedIndex = -1;
        for (int i = 0; i < allChoices.Length; i++)
        {
            if (allChoices[i].Value.Equals(inputText))
            {
                selectedIndex = i;
                break;
            }
        }
    }

    int GetCurrentIndex() => Array.FindIndex(currentChoices, x => x.Index == selectedIndex);

    public void SetValue(string text)
    {
        inputText = currentValue = text;
    }

}
