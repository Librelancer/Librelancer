using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LibreLancer;
using LibreLancer.Data;
using LibreLancer.Data.IO;
using LibreLancer.Net;
using LibreLancer.Server;
using Microsoft.EntityFrameworkCore;

namespace LLServer;

public class ServerApp(ServerConfig config)
{
    public GameServer? Server;
    private readonly ServerConfig Config = config;

    public bool StartServer()
    {
        if (!string.IsNullOrWhiteSpace(Config.LoginUrl) &&
            !Config.LoginUrl.StartsWith("https://"))
        {
            FLLog.Error("Config", "Only HTTPS login servers are supported");
            return false;
        }
        if (!GameConfig.CheckFLDirectory(Config.FreelancerPath))
        {
            FLLog.Error("Config", $"'{Config.FreelancerPath ?? "NULL"}' is not a valid game folder");
            return false;
        }
        var ctxFactory = new SqlDesignTimeFactory(Config.DatabasePath);
        using (var ctx = ctxFactory.CreateDbContext([]))
        {
            if (ctx.Database.GetPendingMigrations().Any())
            {
                FLLog.Info("Server", "Migrating database");
                ctx.Database.Migrate();
            }
        }

        Server = new GameServer(FileSystem.FromPath(Config.FreelancerPath))
        {
            DbContextFactory = ctxFactory,
            ServerName = Config.ServerName,
            ServerDescription = Config.ServerDescription,
            ScriptsFolder = Path.Combine(GetBasePath(), "scripts"),
            LoginUrl = Config.LoginUrl,
            Listener =
            {
                Port = Config.Port > 0 ? Config.Port : LNetConst.DEFAULT_PORT
            }
        };

        Server.Start();
        return true;
    }

    public void WaitExit()
    {
        Server?.JoinThread();
    }

    private static string GetBasePath()
    {
        using var processModule = Process.GetCurrentProcess().MainModule;
        return Path.GetDirectoryName(processModule?.FileName) ?? AppDomain.CurrentDomain.BaseDirectory;
    }

    public void StopServer()
    {
        Server?.Stop();
        Server = null;
    }
}
