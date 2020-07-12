using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LibreLancer.Data;
using LibreLancer.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Server
{
    public class SqlDesignTimeFactory : IDesignTimeDbContextFactory<LibreLancerContext>
    {
        public LibreLancerContext CreateDbContext(string[] args)
        {
            if (!File.Exists("librelancerserver.config.json"))
            { 
                Console.Error.WriteLine($"Can't find {Directory.GetCurrentDirectory()}/librelancerserver.config.json"); 
                throw new IOException();
            }

            var config = JSON.Deserialize<Config>(File.ReadAllText("librelancerserver.config.json"));
            var optionsBuilder = new DbContextOptionsBuilder<LibreLancerContext>();
            optionsBuilder.UseSqlite(config.DbConnectionString);

            if (config.UseLazyLoading)
                optionsBuilder.UseLazyLoadingProxies();

            return new LibreLancerContext(optionsBuilder.Options);
        }
    }
}
