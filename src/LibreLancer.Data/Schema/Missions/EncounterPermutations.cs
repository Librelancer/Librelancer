using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Missions;

[ParsedSection]
public partial class EncounterPermutations
{
    public List<Permutation> Permutations = new();

    [EntryHandler("permutation", MinComponents = 2, Multiline = true)]
    void HandlePermutation(Entry e)
    {
        Permutations.Add(new(e[0].ToInt32(), e[1].ToInt32()));
    }
}

public record struct Permutation(int Index, float Weight);
