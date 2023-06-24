using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LibreLancer;
using LibreLancer.Data;
using LibreLancer.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LLServer
{
    public class SqlDesignTimeFactory : IDesignTimeDbContextFactory<LibreLancerContext>
    {
        private string databasePath;
        public SqlDesignTimeFactory(string dbpath)
        {
            databasePath =  Path.GetFullPath(dbpath, Platform.GetBasePath());
        }
        public SqlDesignTimeFactory()
        {
            databasePath =  Path.Combine(Path.GetTempPath(), "librelancer.ef.database.db");
        }
        public LibreLancerContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<LibreLancerContext>();
            optionsBuilder.UseSqlite($"Data Source={databasePath};");
            return new LibreLancerContext(optionsBuilder.Options);
        }
    }
}
