using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Interface;

namespace LibreLancer.Input;

public class InputMap
{
    public InputBinding[] Actions = new InputBinding[(int) InputAction.COUNT];
    public InputBinding[] DefaultMapping = new InputBinding[(int) InputAction.COUNT];
    public int[] StrId = new int[(int) InputAction.COUNT];
    public int[] InfoId = new int[(int) InputAction.COUNT];

    public InputAction[][] KeyGroups;

    public string FilePath;

    public InputMap(string filePath)
    {
        FilePath = filePath;
    }

    static readonly Dictionary<string, KeyModifiers> modifierTable =
        new (StringComparer.OrdinalIgnoreCase)
        {
            { "control", KeyModifiers.Control },
            { "shift", KeyModifiers.Shift },
            { "alt", KeyModifiers.Alt }
        };

    static UserInput ParseKey(string[] key)
    {
        KeyModifiers mods = KeyModifiers.None;
        if (key.Length > 1) {
            modifierTable.TryGetValue(key[1], out mods);
        }
        int vk = 0;
        if (key[0][0] == '"') {
            vk = (int)key[0][1];
        } else {
            vk = int.Parse(key[0]);
        }
        return VKMap.Map(vk, mods);
    }

    IEnumerable<InputAction> ParseGroup(IEnumerable<string> group)
    {
        foreach (var g in group)
        {
            if (!Enum.TryParse(g, true, out InputAction action))
            {
                FLLog.Warning("Keylist", "Unknown key command: " + g);
                continue;
            }
            yield return action;
        }
    }

    public void LoadFromKeymap(KeymapIni ini, KeyListIni keyList)
    {
        foreach (var cmd in ini.KeyCmd)
        {
            if (!Enum.TryParse(cmd.Nickname, true, out InputAction action))
            {
                FLLog.Warning("Keymap", "Unknown key command: " + cmd.Nickname);
                continue;
            }
            StrId[(int) action] = cmd.IdsName;
            InfoId[(int) action] = cmd.IdsInfo;
            if(cmd.Keys.Count > 0) DefaultMapping[(int) action].Primary = ParseKey(cmd.Keys[0]);
            if (cmd.Keys.Count > 1) DefaultMapping[(int) action].Secondary = ParseKey(cmd.Keys[1]);
        }
        KeyGroups = keyList.Groups.Select(x => ParseGroup(x.Keys).ToArray()).ToArray();
        Array.Copy(DefaultMapping, Actions, Actions.Length);
    }

    public void LoadMapping()
    {
        if (!File.Exists(FilePath)) return;
        using var stream = File.OpenRead(FilePath);
        for (int i = 0; i < Actions.Length; i++) Actions[i] = new InputBinding();
        foreach (var section in IniFile.ParseFile(FilePath, stream))
        {
            if (section.Name.ToLowerInvariant() != "inputmap") continue;
            foreach (var e in section)
            {
                if (!Enum.TryParse(e.Name, true, out InputAction action))
                    continue;
                if (e.Count != 2)
                    continue;
                Actions[(int)action].Primary = UserInput.FromInt32(e[0].ToInt32());
                Actions[(int) action].Secondary = UserInput.FromInt32(e[1].ToInt32());
            }
        }
    }

    public void WriteMapping()
    {
        var builder = new StringBuilder();
        builder.AppendLine("[InputMap]");
        for (int i = 0; i < Actions.Length; i++) {
            if (Actions[i].Primary.NonEmpty ||
                Actions[i].Secondary.NonEmpty)
            {
                builder.AppendFormat("{0} = {1},{2}\n", (InputAction) i, Actions[i].Primary.ToInt32(),
                    Actions[i].Secondary.ToInt32());
            }
        }
        File.WriteAllText(FilePath, builder.ToString());
    }
}
