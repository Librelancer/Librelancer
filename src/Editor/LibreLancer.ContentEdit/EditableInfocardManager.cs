using System;
using System.Collections.Generic;
using System.IO;
using LibreLancer.Data;
using LibreLancer.Dll;

namespace LibreLancer.ContentEdit;

public class EditableInfocardManager : InfocardManager
{
    public int MaxIds => Dlls.Count * 65536;

    private Dictionary<int, string> dirtyStrings = new Dictionary<int, string>();
    private Dictionary<int, string> dirtyInfocards = new Dictionary<int, string>();
    
    public void Reset()
    {
        dirtyStrings = new Dictionary<int, string>();
        dirtyInfocards = new Dictionary<int, string>();
    }

    public EditableInfocardManager(List<ResourceDll> res) : base(res) { }

    public override string GetStringResource(int id)
    {
        if (dirtyStrings.TryGetValue(id, out var s))
            return s;
        return base.GetStringResource(id);
    }

    public override string GetXmlResource(int id)
    {
        if (dirtyInfocards.TryGetValue(id, out var s))
            return s;
        return base.GetXmlResource(id);
    }

    public void SetStringResource(int id, string value)
    {
        if (id <= 0 || id > MaxIds)
            throw new IndexOutOfRangeException($"{id} cannot be stored in dll collection");
        dirtyStrings[id] = value;
    }

    public void SetXmlResource(int id, string value)
    {
        if (id <= 0 || id > MaxIds)
            throw new IndexOutOfRangeException($"{id} cannot be stored in dll collection");
        dirtyInfocards[id] = value;
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
        for (int i = 0; i < Dlls.Count; i++)
        {
            if (!toWrite[i]) continue;
            using (var f = File.Create(Dlls[i].SavePath)) {
                DllWriter.Write(Dlls[i], f);
            }
        }
        dirtyStrings = new Dictionary<int, string>();
        dirtyInfocards = new Dictionary<int, string>();
    }
    
}