using System;
using LibreLancer.Data;
using LibreLancer.Input;
using WattleScript.Interpreter;

namespace LibreLancer.Interface;

[WattleScriptUserData]
public class KeyMapTable : ITableData
{
    private int groupIndex = 0;

    private InputMap map;
    private InfocardManager infocards;

    private InputBinding[] mapCopy;
    
    public KeyMapTable(InputMap map, InfocardManager infocards)
    {
        this.map = map;
        mapCopy = new InputBinding[map.Actions.Length];
        Array.Copy(map.Actions, mapCopy, map.Actions.Length);
        this.infocards = infocards;
    }

    public void SetGroup(int index) => groupIndex = index;
    public int Count => map.KeyGroups[groupIndex].Length;
    public int Selected { get; set; } = 1;
    public string GetContentString(int row, string column)
    {
        if (row < 0 || row >= map.KeyGroups[groupIndex].Length) return "";
        switch (column)
        {
            case "key":
                return infocards.GetStringResource(map.StrId[(int)map.KeyGroups[groupIndex][row]]);
            case "primary":
                return mapCopy[(int) map.KeyGroups[groupIndex][row]].Primary.ToDisplayString(infocards);
            case "secondary":
                return mapCopy[(int) map.KeyGroups[groupIndex][row]].Secondary.ToDisplayString(infocards);
        }
        return "";
    }

    public int GetKeyId(int row) => map.StrId[(int) map.KeyGroups[groupIndex][row]];

    public event Action<KeyCaptureContext> OnCaptureInput;

    private KeyCaptureContext _ctx;

    public void CaptureInput(int index, bool primary, Closure onFinish)
    {
        _ctx = new KeyCaptureContext(mapCopy, map.KeyGroups[groupIndex][index], primary, infocards, map.StrId, onFinish);
        OnCaptureInput?.Invoke(_ctx);
    }

    public void DefaultBindings()
    {
        Array.Copy(map.DefaultMapping, mapCopy, mapCopy.Length);
    }

    public void ResetBindings()
    {
        Array.Copy(map.Actions, mapCopy, mapCopy.Length);
    }

    public void Save()
    {
        Array.Copy(mapCopy, map.Actions, map.Actions.Length);
        try
        {
            map.WriteMapping();
        }
        catch (Exception e)
        {
            FLLog.Error("Save", $"Could not write keymap.ini. {e.Message}");
        }
    }
    
    public void CancelCapture()
    {
        _ctx?.Cancel();
    }

    public void ClearCapture()
    {
        _ctx?.Clear();
    }

    public bool ValidSelection()
    {
        return Selected >= 0 && Selected < map.KeyGroups[groupIndex].Length;
    }
}

public class KeyCaptureContext
{
    private InputBinding[] map;
    private bool active;
    private InputAction action;
    private bool primary;
    private Closure captureFinish;
    private int[] strid;
    private InfocardManager ic;

    public static bool Capturing(KeyCaptureContext ctx)
    {
        return ctx?.active ?? false;
    }
    public KeyCaptureContext(InputBinding[] map, InputAction action, bool primary, InfocardManager ic, int[] strid, Closure onFinish)
    {
        this.map = map;
        active = true;
        this.action = action;
        this.primary = primary;
        this.ic = ic;
        this.strid = strid;
        this.captureFinish = onFinish;
    }

    InputAction FindAction(UserInput input)
    {
        for (int i = 0; i < map.Length; i++)
        {
            if (map[i].Primary == input ||
                map[i].Secondary == input)
                return (InputAction) i;
        }
        return InputAction.COUNT;
    }

    void SetOverwrite(UserInput input)
    {
        for (int i = 0; i < map.Length; i++)
        {
            if (map[i].Primary == input) map[i].Primary = default;
            if (map[i].Secondary == input) map[i].Secondary = default;
        }
        if (primary)
            map[(int) action].Primary = input;
        else
            map[(int)action].Secondary = input;
    }
    
    public void Set(UserInput input)
    {
        if (!active) return;
        active = false;
        InputAction found;
        if ((found = FindAction(input)) != InputAction.COUNT)
        {
            captureFinish.Call("overwrite", input.ToDisplayString(ic), strid[(int) found], (Action) (() =>
            {
                SetOverwrite(input);
            }));
        }
        else
        {
            if (primary)
                map[(int) action].Primary = input;
            else
                map[(int) action].Secondary = input;
            captureFinish.Call("ok");
        }
    }

    public void Cancel()
    {
        if (!active) return;
        active = false;
        captureFinish.Call("ok");
    }

    public void Clear()
    {
        if (!active) return;
        active = false;
        if (primary)
            map[(int) action].Primary = default;
        else
            map[(int) action].Secondary = default;
        captureFinish.Call("ok");
    }
    
}