using System.Linq;
using LibreLancer;
using LibreLancer.Data;
using LibreLancer.Data.IO;
using LibreLancer.Net;
using LibreLancer.Server;
using Microsoft.EntityFrameworkCore;

namespace LLServer;

public class ServerApp
{
    public GameServer Server;
    public ServerConfig Config;

    public ServerApp(ServerConfig config)
    {
        Config = config;
    }

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
        using (var ctx = ctxFactory.CreateDbContext(new string[0]))
        {
            if (ctx.Database.GetPendingMigrations().Any())
            {
                FLLog.Info("Server", "Migrating database");
                ctx.Database.Migrate();
            }
        }
        Server = new GameServer(FileSystem.FromPath(Config.FreelancerPath));
        Server.DbContextFactory = ctxFactory;
        Server.ServerName = Config.ServerName;
        Server.ServerDescription = Config.ServerDescription;
        Server.LoginUrl = Config.LoginUrl;
        Server.Listener.Port = Config.Port > 0 ? Config.Port : LNetConst.DEFAULT_PORT;
        Server.Start();
        return true;
    }

    public void StopServer()
    {
        Server?.Stop();
        Server = null;
    }
}
