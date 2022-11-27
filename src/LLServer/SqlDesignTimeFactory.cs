using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LibreLancer.Data;
using LibreLancer.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LLServer
{
    public class SqlDesignTimeFactory : IDesignTimeDbContextFactory<LibreLancerContext>
    {
        private Config config;
        public SqlDesignTimeFactory(Config config)
        {
            this.config = config;
        }
        public SqlDesignTimeFactory()
        {
            config = new Config();
            config.UseLazyLoading = true;
            config.DatabasePath =  Path.Combine(Path.GetTempPath(), "librelancer.ef.database.db");
        }
        public LibreLancerContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<LibreLancerContext>();
            optionsBuilder.UseSqlite($"Data Source={config.DatabasePath};");
            if (config.UseLazyLoading)
                optionsBuilder.UseLazyLoadingProxies();
            return new LibreLancerContext(optionsBuilder.Options);
        }
    }
}
