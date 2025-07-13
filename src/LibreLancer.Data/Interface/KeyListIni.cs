using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Interface;

[ParsedSection]
public partial class KeyGroup
{
    [Entry("group_num")]
    public int GroupNum;
    [Entry("name")]
    public int Name; //Undo

    public List<string> Keys = new List<string>();
}

public class KeyListIni
{
    public List<KeyGroup> Groups = new List<KeyGroup>();
    public KeyListIni() { }
    public KeyListIni(string path, FileSystem VFS)
    {
        KeyGroup currentGroup = null;
        foreach (var section in IniFile.ParseFile(path, VFS))
        {
            if (section.Name.Equals("group", StringComparison.OrdinalIgnoreCase))
            {
                if(currentGroup != null) Groups.Add(currentGroup);
                KeyGroup.TryParse(section, out currentGroup);
            }
            else if (section.Name.Equals("key", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var entry in section)
                {
                    if(entry.Count > 0 && entry.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                        currentGroup?.Keys.Add(entry[0].ToString());
                }
            }
        }
        if(currentGroup != null) Groups.Add(currentGroup);
        Groups.Sort((x, y) => x.GroupNum.CompareTo(y.GroupNum));
    }
}
