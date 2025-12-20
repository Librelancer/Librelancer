using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Effects;

[ParsedIni]
public partial class ExplosionsIni
{
    [Section("Explosion")]
    public List<Explosion> Explosions = new List<Explosion>();
    [Section("Debris")]
    public List<Debris> Debris = new List<Debris>();
    [Section("Simple")]
    public List<Simple> Simples = new List<Simple>();

    public void AddFile(string filename, FileSystem vfs, IniStringPool stringPool = null) =>
        ParseIni(filename, vfs, stringPool);
}
