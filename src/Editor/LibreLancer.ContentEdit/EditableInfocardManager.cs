using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibreLancer.Data;
using LibreLancer.Data.Dll;

namespace LibreLancer.ContentEdit;

public class EditableInfocardManager : InfocardManager
{
    public int MaxIds => Dlls.Count * 65536;
    public enum ResourceType { String, Infocard }

    private Dictionary<int, string> dirtyStrings = new Dictionary<int, string>();
    private Dictionary<int, string> dirtyInfocards = new Dictionary<int, string>();

    private List<int> removedStrings = new List<int>();
    private List<int> removedInfocards = new List<int>();

    public bool Dirty => dirtyStrings.Count > 0 || 
                         dirtyInfocards.Count > 0 ||
                         removedStrings.Count > 0 ||
                         removedInfocards.Count > 0;
    
    public void Reset()
    {
        dirtyStrings = new Dictionary<int, string>();
        dirtyInfocards = new Dictionary<int, string>();
        removedStrings = new List<int>();
        removedInfocards = new List<int>();
    }

    public EditableInfocardManager(List<ResourceDll> res) : base(res) { }

    protected override IEnumerable<KeyValuePair<int, string>> IterateStrings()
    {
        if (removedStrings.Count == 0 && dirtyStrings.Count == 0)
            return base.IterateStrings();
        return base.IterateStrings()
                .Where(x => !removedStrings.Contains(x.Key) && !dirtyStrings.ContainsKey(x.Key))
                .Concat(dirtyStrings)
                .OrderBy(x => x.Key);
    }

    protected override IEnumerable<KeyValuePair<int, string>> IterateXml()
    {
        if (removedInfocards.Count == 0 && dirtyInfocards.Count == 0)
            return base.IterateXml();
        return base.IterateXml()
            .Where(x => !removedInfocards.Contains(x.Key) && !dirtyInfocards.ContainsKey(x.Key))
            .Concat(dirtyInfocards)
            .OrderBy(x => x.Key);
    }

    public bool StringExists(int id)
    {
        if (id < 0 || id > MaxIds) return false;
        if (removedStrings.Contains(id)) return false;
        if (dirtyStrings.ContainsKey(id)) return true;
        var (x, y) = (id >> 16, id & 0xFFFF);
        return Dlls[x].Strings.ContainsKey(y);
    }
    
    public bool XmlExists(int id)
    {
        if (id < 0 || id > MaxIds) return false;
        if (removedInfocards.Contains(id)) return false;
        if (dirtyInfocards.ContainsKey(id)) return true;
        var (x, y) = (id >> 16, id & 0xFFFF);
        return Dlls[x].Infocards.ContainsKey(y);
    }

    public override string GetStringResource(int id)
    {
        if (removedStrings.Contains(id)) 
            return "";
        if (dirtyStrings.TryGetValue(id, out var s))
            return s;
        return base.GetStringResource(id);
    }

    public override string GetXmlResource(int id)
    {
        if (removedInfocards.Contains(id))
            return null;
        if (dirtyInfocards.TryGetValue(id, out var s))
            return s;
        return base.GetXmlResource(id);
    }

    public void RemoveStringResource(int id)
    {
        dirtyStrings.Remove(id);
        var (x, y) = (id >> 16, id & 0xFFFF);
        if(Dlls[x].Strings.ContainsKey(y))
            removedStrings.Add(id);
    }

    public void RemoveXmlResource(int id)
    {
        dirtyInfocards.Remove(id);
        var (x, y) = (id >> 16, id & 0xFFFF);
        if(Dlls[x].Infocards.ContainsKey(y))
            removedInfocards.Add(id);
    }

    public int NextFreeId(int start)
    {
        //We don't return 0
        if (start <= 0 || start >= Dlls.Count * 65536) start = 1;
        for (int i = start + 1; i < Dlls.Count * 65536; i++)
        {
            if (!StringExists(i) && !XmlExists(i)) return i;
        }
        for (int i = 1; i <= start; i++)
        {
            if (!StringExists(i) && !XmlExists(i)) return i;
        }
        return -1;
    }

    public int HighestFreeId(ResourceType type)
    {
        // Pick the function to check if an ID exists for this resource type
        int highestExisting = -1;
        Func<int, bool> existsCheck = type switch {
            ResourceType.String => StringExists,
            ResourceType.Infocard => XmlExists,
            _ => id => false
        };
        
        // First loop: find the highest ID currently in use for this type
        for (int i = 1; i < Dlls.Count * 65536; i++)
        {
            if (existsCheck(i) && i > highestExisting)
                highestExisting = i;
        }
        
        // No IDs in use yet → start at 1
        if (highestExisting == -1) 
            return 1;
        
        // If the next number after the highest is completely free, use it
        int nextId = highestExisting + 1;
        if (nextId < Dlls.Count * 65536 &&
            !StringExists(nextId) && !XmlExists(nextId))
            return nextId;
        
        // Second loop: otherwise, find the next free slot after the highest ID
        // Two loops are used so we always start after the true highest used ID,
        // instead of filling the first small gap found.
        for (int i = nextId; i < Dlls.Count * 65536; i++)
        {
            if (!StringExists(i) && !XmlExists(i))
                return i;
        }
        
        // No free ID available
        return -1;
    }

    public void SetStringResource(int id, string value)
    {
        if (id <= 0 || id > MaxIds)
            throw new IndexOutOfRangeException($"{id} cannot be stored in dll collection");
        removedStrings.Remove(id);
        var (x, y) = (id >> 16, id & 0xFFFF);
        if (Dlls[x].Strings.TryGetValue(id, out var existing) &&
            existing == value)
        {
            dirtyStrings.Remove(id);
        }
        else
        {
            dirtyStrings[id] = value;
        }
    }

    public void SetXmlResource(int id, string value)
    {
        if (id <= 0 || id > MaxIds)
            throw new IndexOutOfRangeException($"{id} cannot be stored in dll collection");
        removedInfocards.Remove(id);
        var (x, y) = (id >> 16, id & 0xFFFF);
        if (Dlls[x].Infocards.TryGetValue(id, out var existing) &&
            existing == value)
        {
            dirtyInfocards.Remove(id);
        }
        else
        {
            dirtyInfocards[id] = value;
        }        
    }

    public void Save()
    {
        var toWrite = new BitArray128();
        foreach (var s in dirtyStrings) {
            var (x, y) = (s.Key >> 16, s.Key & 0xFFFF);
            Dlls[x].Strings[y] = s.Value;
            toWrite[x] = true;
        }
        foreach (var s in dirtyInfocards) {
             var (x, y) = (s.Key >> 16, s.Key & 0xFFFF);
             Dlls[x].Infocards[y] = s.Value;
             toWrite[x] = true;
        }

        foreach (var s in removedStrings)
        {
            var (x, y) = (s >> 16, s & 0xFFFF);
            Dlls[x].Strings.Remove(y);
            toWrite[x] = true;
        }
        
        foreach (var s in removedInfocards)
        {
            var (x, y) = (s >> 16, s & 0xFFFF);
            Dlls[x].Infocards.Remove(y);
            toWrite[x] = true;
        }
        
        for (int i = 0; i < Dlls.Count; i++)
        {
            if (!toWrite[i]) continue;
            using (var f = File.Create(Dlls[i].SavePath)) {
                DllWriter.Write(Dlls[i], f);
            }
        }
        
        dirtyStrings = new Dictionary<int, string>();
        dirtyInfocards = new Dictionary<int, string>();
        removedStrings = new List<int>();
        removedInfocards = new List<int>();
    }
    
}