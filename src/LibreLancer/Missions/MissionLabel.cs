using System.Collections.Generic;
using System.Linq;

namespace LibreLancer.Missions;

public enum NpcState
{
    NotSpawned,
    Alive,
    Dead
}

public class MissionLabel
{
    public string Name;
    private Dictionary<string, NpcState> states = new();

    public IEnumerable<string> Objects => states.Keys;

    public bool IsAllKilled() => states.Values.All(x => x == NpcState.Dead);

    public bool AnyAlive() => states.Values.All(x => x == NpcState.Alive);

    public int DestroyedCount() => states.Values.Count(x => x == NpcState.Dead);

    public MissionLabel(string name, IEnumerable<string> ships)
    {
        Name = name;
        foreach (var sh in ships)
        {
            states.Add(sh, NpcState.NotSpawned);
        }
    }

    public void Spawned(string name)
    {
        if (states.ContainsKey(name))
        {
            states[name] = NpcState.Alive;
        }
    }

    public void Destroyed(string name)
    {
        if (states.ContainsKey(name))
        {
            states[name] = NpcState.Dead;
        }
    }

}
