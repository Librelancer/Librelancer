// MIT License - Copyright (c) Lazrius
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Database
{
    using LibreLancer.Entities.Character;

    using Microsoft.EntityFrameworkCore;

    public class LibreLancerContext : DbContext
    {
        public LibreLancerContext(DbContextOptions<LibreLancerContext> options) : base(options)
        {
        }

        public DbSet<Character> Characters { get; set; }
        public DbSet<Account> Accounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Sqlite things
            modelBuilder.Entity<Character>().Property(x => x.Name).HasColumnType("TEXT COLLATE NOCASE");
            // Indexes
            modelBuilder.Entity<Account>().HasIndex(x => x.AccountIdentifier);

            // Define relationships
            modelBuilder.Entity<Account>().HasMany(x => x.Characters).WithOne(x => x.Account);
        }
    }
}
