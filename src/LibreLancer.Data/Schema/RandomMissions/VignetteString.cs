using System.Linq;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.RandomMissions;

public struct VignetteString
{
    public string Target;
    public int Ids;
    public string[] Arguments;

    public static bool TryParse(bool useEntryName, Entry e, out VignetteString str)
    {
        str = new VignetteString();
        str.Target = useEntryName ? e.Name : e[0].ToString();
        int idx = useEntryName ? 0 : 1;
        if (!e[idx].TryToInt32(out str.Ids))
            return false;
        if (e.Count >= idx + 1)
        {
            str.Arguments = new string[e.Count - (idx + 1)];
            for (int i = 0; i < str.Arguments.Length; i++)
            {
                idx++;
                str.Arguments[i] = e[idx].ToString();
            }
        }
        else
        {
            str.Arguments = [];
        }
        return true;
    }
}
