using System;
using LibreLancer.Data;
using WattleScript.Interpreter;

namespace LibreLancer.Interface;

[WattleScriptUserData]
public class KeyMapTable : ITableData
{
    private int groupIndex = 0;

    private InputMap map;
    private InfocardManager infocards;
    
    public KeyMapTable(InputMap map, InfocardManager infocards)
    {
        this.map = map;
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
                return map.Actions[(int) map.KeyGroups[groupIndex][row]].Primary.ToDisplayString(infocards);
            case "secondary":
                return map.Actions[(int) map.KeyGroups[groupIndex][row]].Secondary.ToDisplayString(infocards);
        }
        return "";
    }

    public int GetKeyId(int row) => map.StrId[(int) map.KeyGroups[groupIndex][row]];

    public event Action<KeyCaptureContext> OnCaptureInput;

    private KeyCaptureContext _ctx;

    public void CaptureInput(int index, bool primary, Closure onFinish)
    {
        _ctx = new KeyCaptureContext(map.Actions, map.KeyGroups[groupIndex][index], primary, onFinish);
        OnCaptureInput?.Invoke(_ctx);
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

    public static bool Capturing(KeyCaptureContext ctx)
    {
        return ctx?.active ?? false;
    }
    public KeyCaptureContext(InputBinding[] map, InputAction action, bool primary, Closure onFinish)
    {
        this.map = map;
        active = true;
        this.action = action;
        this.primary = primary;
        this.captureFinish = onFinish;
    }
    
    public void Set(UserInput input)
    {
        if (!active) return;
        if (primary)
            map[(int) action].Primary = input;
        else
            map[(int)action].Secondary = input;
        active = false;
        captureFinish.Call("ok");
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