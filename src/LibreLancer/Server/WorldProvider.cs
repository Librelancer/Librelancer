using System;
using System.Collections.Concurrent;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.World;

namespace LibreLancer.Server;

public class WorldProvider
{
    private GameServer server;

    public WorldProvider(GameServer server)
    {
        this.server = server;
    }

    public void RemoveWorld(StarSystem system)
    {
        worlds.TryRemove(system, out _);
    }

    struct WorldState
    {
        public bool Ready;
        public ServerWorld World;
    }

    private ConcurrentDictionary<StarSystem, WorldState> worlds = new ConcurrentDictionary<StarSystem, WorldState>();

    void LoadWorld(StarSystem system, out WorldState ws, PreloadObject[] preloads)
    {
        var x = new WorldState();
        if (worlds.TryAdd(system, new WorldState()))
        {
            x.World = new ServerWorld(system, server);
            server.GameData.PreloadObjects(preloads, server.Resources);
            x.Ready = true;
            server.WorldReady(x.World);
            worlds.AddOrUpdate(
                system,
                _ => x,
                (_, _) => x
            );
        }
        ws = x;
    }
    public void RequestWorld(StarSystem system, Action<ServerWorld> spunUp, PreloadObject[] preloads)
    {
        Task.Run(async () =>
        {
            if (!worlds.TryGetValue(system, out var ws))
                LoadWorld(system, out ws, preloads);
            while (!ws.Ready)
            {
                await Task.Delay(33);
                if (!worlds.TryGetValue(system, out ws))
                    LoadWorld(system, out ws, preloads);
            }

            spunUp(ws.World);
        }).ContinueWith(x =>
        {
            if (x.Exception != null)
                throw x.Exception;
        });
    }
}
