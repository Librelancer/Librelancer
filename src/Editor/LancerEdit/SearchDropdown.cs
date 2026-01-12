using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer.ImUI;

namespace LancerEdit;

public static unsafe class SearchDropdown<T>
{
    private const int ACT_UP = 1;
    private const int ACT_DOWN = 2;

    private static DropdownState _state;
    private static uint _activate;
    // Returns true if open

    private static string GetName(T value, Func<T, string> displayName)
    {
        return displayName == null
            ? (value?.ToString() ?? "(none)")
            : displayName(value);
    }

    private static int TextCallback(ImGuiInputTextCallbackData* cb)
    {
        var state = (CallbackState*)cb->UserData;
        if (cb->EventKey == ImGuiKey.UpArrow)
            state->NextAction = ACT_UP;

        if (cb->EventKey == ImGuiKey.DownArrow)
            state->NextAction = ACT_DOWN;

        // Have to clear text in callback due to imgui usage
        // PopupShown is 2 frames to allow scroll logic to work
        if (state->PopupShown == 2)
        {
            cb->BufDirty = true;
            ((byte*)cb->Buf)![0] = 0;
            cb->BufTextLen = 0;
            cb->SelectionStart = cb->SelectionEnd = cb->CursorPos = 0;
        }

        return 0;
    }

    public static bool DrawUndo(string name,
        EditorUndoBuffer undoBuffer,
        FieldAccessor<T> accessor,
        T[] choices,
        Func<T, string> displayName = null,
        bool allowNull = false)
    {
        ref var sel = ref accessor();
        return Draw(name, ref sel, choices, displayName,
            (o, u) => undoBuffer.Set(name, accessor, o, u));
    }

    public static bool Draw(
        string id,
        ref T selectedValue,
        T[] choices,
        Func<T, string> displayName = null,
        Action<T, T> onSelected = null,
        bool allowNull = false)
    {
        return Draw(id, ref selectedValue, out _, choices, displayName, onSelected, allowNull);
    }

    public static bool Draw(
        string id,
        ref T selectedValue,
        out T hovered,
        T[] choices,
        Func<T, string> displayName = null,
        Action<T, T> onSelected = null,
        bool allowNull = false)
    {
        hovered = default;
        var szButton = ImGui.GetFrameHeight();
        ImGui.PushID(id);
        var uid = ImGui.GetID("##input");

        var inputText = uid == _state.ID ? _state.InputText : GetName(selectedValue, displayName);
        var w = ImGui.CalcItemWidth();
        ImGui.SetNextItemWidth(w - szButton);

        var cb = uid == _state.ID ? _state.Frame : default;
        cb.NextAction = 0;
        if (_activate == uid)
        {
            inputText = "";
            ImGui.SetKeyboardFocusHere();
            _activate = 0;
        }

        var enterPressed = ImGui.InputText("##input", ref inputText, 300,
            ImGuiInputTextFlags.EnterReturnsTrue |
            ImGuiInputTextFlags.CallbackAlways |
            ImGuiInputTextFlags.CallbackHistory, TextCallback, (IntPtr)(&cb));

        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        var textActive = ImGui.IsItemActive();
        var textActivated = ImGui.IsItemActivated();

        //open/close button
        //we need to handle the state of the button along with the ImGui auto closing of the popup
        //when clicking this button
        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - ImGui.GetStyle().ItemSpacing.X - 2 * ImGuiHelper.Scale);
        var buttonClosed = ImGui.GetStateStorage().GetBool(uid);
        if (ImGuiExt.ButtonRounding("##activate", new Vector2(szButton), ImDrawFlags.RoundCornersRight))
        {
            if (!buttonClosed)
                _activate = uid;
            buttonClosed = false;
        }

        if (_state.ID == uid && ImGui.IsItemActive())
            buttonClosed = true;

        if (ImGui.IsItemDeactivated() && _activate != uid)
            buttonClosed = false;

        ImGui.GetStateStorage().SetBool(uid, buttonClosed);
        var buttonMin = ImGui.GetItemRectMin();
        ImGuiExt.igExtRenderArrow(buttonMin.X, buttonMin.Y);

        void Select(T item, ref T selected)
        {
            var old = selected;
            selected = item;
            onSelected?.Invoke(old, selected);
        }

        if (textActivated)
        {
            ImGui.OpenPopup("##choices");
            inputText = "";
            _state = new DropdownState
            {
                ID = uid,
                InputText = "",
                FilterText = "",
                AllChoices = allowNull
                    ? choices.Select((x, i) => (x, i + 1)).Prepend((default, 0)).ToArray()
                    : choices.Select((x, i) => (x, i)).ToArray()
            };
            _state.CurrentChoices = _state.AllChoices;
            _state.UpdateSelectedIndex(ref selectedValue);
        }

        if (_state.ID != uid)
        {
            ImGui.PopID();
            return false;
        }

        _state.InputText = inputText;
        _state.Frame = cb;
        _state.UpdateFilter(ref selectedValue, displayName);
        var lines = Math.Min(_state.CurrentChoices.Length + 1, 8);
        ImGui.SetNextWindowPos(new Vector2(min.X, max.Y));
        ImGui.SetNextWindowSize(new Vector2(max.X - min.X + szButton, (max.Y - min.Y) * lines));
        if (ImGui.BeginPopup("##choices",
                ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.ChildWindow))
        {
            if (_state.SelectedIndex > 0 && _state.SelectedIndex <
                _state.CurrentChoices.Length)
                hovered = _state.CurrentChoices[_state.SelectedIndex].Item;
            _state.Frame.PopupShown--;
            if (_state.Frame.PopupShown < 0) _state.Frame.PopupShown = 0;
            if (!_state.VisibleLastFrame)
                _state.Frame.PopupShown = 2;
            _state.VisibleLastFrame = true;
            if (cb.NextAction == ACT_UP && _state.CurrentChoices.Length > 0)
            {
                var idx = _state.GetCurrentIndex();
                if (idx - 1 <= 0)
                    _state.SelectedIndex = _state.CurrentChoices[0].Index;
                else
                    _state.SelectedIndex = _state.CurrentChoices[idx - 1].Index;
            }
            else if (cb.NextAction == ACT_DOWN && _state.CurrentChoices.Length > 0)
            {
                var idx = _state.GetCurrentIndex();
                if (idx + 1 >= _state.CurrentChoices.Length)
                    _state.SelectedIndex = _state.CurrentChoices[^1].Index;
                else
                    _state.SelectedIndex = _state.CurrentChoices[idx + 1].Index;
            }

            for (var i = 0; i < _state.CurrentChoices.Length; i++)
            {
                var n = GetName(_state.CurrentChoices[i].Item, displayName);
                var isSelected = _state.CurrentChoices[i].Index == _state.SelectedIndex;
                if (ImGui.Selectable(n, isSelected))
                {
                    Select(_state.CurrentChoices[i].Item, ref selectedValue);
                    ImGui.CloseCurrentPopup();
                }

                if (ImGui.IsItemHovered())
                    hovered = _state.CurrentChoices[i].Item;

                if ((_state.Frame.PopupShown > 0 || _state.Frame.NextAction != 0) && isSelected)
                    ImGui.SetScrollHereY();
            }

            if (enterPressed || (!textActive && !ImGui.IsWindowFocused()))
            {
                if (enterPressed)
                    if (_state.SelectedIndex >= 0 &&
                        !_state.AllChoices[_state.SelectedIndex].Item.Equals(selectedValue))
                        Select(_state.AllChoices[_state.SelectedIndex].Item, ref selectedValue);

                _state = default;
                ImGui.CloseCurrentPopup();
            }

            if (ImGui.IsKeyPressed(ImGuiKey.Escape))
            {
                _state = default;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
            ImGui.PopID();
            return true;
        }

        _state = default;
        ImGui.PopID();
        return false;
    }

    private struct DropdownState
    {
        public uint ID;
        public string InputText;
        public string FilterText;
        public (T Item, int Index)[] AllChoices;
        public (T Item, int Index)[] CurrentChoices;
        public int SelectedIndex;
        public CallbackState Frame;
        public bool VisibleLastFrame;

        public void UpdateSelectedIndex(ref T selectedValue)
        {
            SelectedIndex = -1;
            for (var i = 0; i < AllChoices.Length; i++)
            {
                if (AllChoices[i].Item == null)
                {
                    if (selectedValue == null)
                    {
                        SelectedIndex = i;
                        break;
                    }
                }
                else if (AllChoices[i].Item.Equals(selectedValue))
                {
                    SelectedIndex = i;
                    break;
                }
            }
        }

        public int GetCurrentIndex()
        {
            var i = SelectedIndex;
            return Array.FindIndex(CurrentChoices, x => x.Index == i);
        }

        public void UpdateFilter(ref T selectedValue, Func<T, string> displayName)
        {
            if (InputText != FilterText)
            {
                FilterText = InputText;
                if (string.IsNullOrWhiteSpace(FilterText))
                {
                    CurrentChoices = AllChoices;
                    UpdateSelectedIndex(ref selectedValue);
                }
                else
                {
                    var st = FilterText.Trim();
                    _state.CurrentChoices = _state.AllChoices.Where(x => GetName(x.Item, displayName)
                            .Contains(st, StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                    if (GetCurrentIndex() == -1)
                        SelectedIndex = -1;
                }
            }
        }
    }

    private struct CallbackState
    {
        public int NextAction;
        public int PopupShown;
    }
}
