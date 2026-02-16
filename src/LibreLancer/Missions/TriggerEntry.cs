using System;
using System.Numerics;
using LibreLancer.Data.Ini;

namespace LibreLancer.Missions;

public abstract class TriggerEntry
{
    protected static void WarnMissing(string name, int index, Entry e) =>
        FLLog.Warning("Mission", $"Missing arg #{index+1} {name} in {e.Name} (line {e.Line})");

    static bool CheckArg(string name, int index, Entry e)
    {
        if (e.Count > index)
            return true;
        WarnMissing(name, index, e);
        return false;
    }

    protected bool ParseBoolean(ValueBase value)
    {
        bool? result = value.ToString().ToLowerInvariant() switch
        {
            "1" => true,
            "accept" => true,
            "active" => true,
            "yes" => true,
            "on" => true,
            "succeed" => true,
            "true" => true,
            "lock" => true,
            "0" => false,
            "off" => false,
            "no" => false,
            "reject" => false,
            "fail" => false,
            "false" => false,
            "unlock" => false,
            _ => null
        };

        if (result is not null)
        {
            return result.Value;
        }

        FLLog.Warning("Missions", $"Unable to parse boolean value '{value}'");
        return false;
    }

    protected void GetEnum<T>(string name, int index, out T result, Entry e, T def = default) where T : struct
    {
        result = def;
        if (CheckArg(name, index, e))
        {
            if (!Enum.TryParse(e[index].ToString()!, true, out result))
            {
                FLLog.Warning("Missions", $"Unknown enum #{index+1} {name} in {e.Name} (line {e.Line})");
            }
        }
    }

    protected void GetVector3(string name, int index, out Vector3 result, Entry e)
    {
        result = Vector3.Zero;
        if (e.Count > index + 2)
        {
            result = new(e[index].ToSingle(), e[index + 1].ToSingle(), e[index + 2].ToSingle());
        }
        else
        {
            FLLog.Warning("Mission", $"Missing arg #{index+1} {name} in {e.Name} (line {e.Line})");
        }
    }

    protected void GetQuaternion(string name, int index, out Quaternion result, Entry e)
    {
        result = Quaternion.Identity;
        if (e.Count > index + 2)
        {
            //W = 0, X = 1, Y = 2, Z = 3
            result = new(e[index + 1].ToSingle(), e[index + 2].ToSingle(), e[index + 3].ToSingle(), e[index].ToSingle());
        }
        else
        {
            FLLog.Warning("Mission", $"Missing arg #{index+1} {name} in {e.Name} (line {e.Line})");
        }
    }
    protected void GetString(string name, int index, out string result, Entry e)
    {
        result = "";
        if (CheckArg(name, index, e))
        {
            result = e[index].ToString();
        }
    }

    protected void GetInt(string name, int index, out int result, Entry e)
    {
        result = 0;
        if (CheckArg(name, index, e))
        {
            result = e[index].ToInt32();
        }
    }

    protected void GetFloat(string name, int index, out float result, Entry e)
    {
        result = 0;
        if (CheckArg(name, index, e))
        {
            result = e[index].ToSingle();
        }
    }

    protected void GetBoolean(string name, int index, out bool result, Entry e)
    {
        result = false;
        if (CheckArg(name, index, e))
        {
            result = ParseBoolean(e[index]);
        }
    }
}
