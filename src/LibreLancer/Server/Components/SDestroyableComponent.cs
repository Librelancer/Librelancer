using System;
using LibreLancer.World;

namespace LibreLancer.Server.Components;

public class SDestroyableComponent : GameComponent
{
    public ServerWorld Server;
    public Action OnKilled;

    public SDestroyableComponent(GameObject parent, ServerWorld server) : base(parent)
    {
        Server = server;
    }

    public void Destroy(bool exploded)
    {
        OnKilled?.Invoke();
        if (Parent.TryGetComponent<SPlayerComponent>(out var player))
        {
            player.Killed();
        }
        else
        {
            Server.RemoveSpawnedObject(Parent, exploded);
        }
    }
}
