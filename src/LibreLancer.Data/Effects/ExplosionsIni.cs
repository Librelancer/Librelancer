using System.Collections.Generic;
using LibreLancer.Data.IO;
using LibreLancer.Ini;

namespace LibreLancer.Data.Effects;

public class ExplosionsIni : IniFile
{
    [Section("Explosion")]
    public List<Explosion> Explosions = new List<Explosion>();
    [Section("Debris")]
    public List<Debris> Debris = new List<Debris>();
    [Section("Simple")]
    public List<Simple> Simples = new List<Simple>();

    public void AddFile(string filename, FileSystem vfs) => ParseAndFill(filename, vfs);
}
