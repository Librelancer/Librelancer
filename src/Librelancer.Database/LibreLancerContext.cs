// MIT License - Copyright (c) Lazrius
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibreLancer.Entities.Abstract;

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
            modelBuilder.Entity<Character>().HasMany(x => x.Items).WithOne().OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Character>().HasMany(x => x.Reputations).WithOne().OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Character>().HasMany(x => x.VisitEntries).WithOne().OnDelete(DeleteBehavior.Cascade);
        }

        void UpdateTimestamps()
        {
            foreach (var update in ChangeTracker
                         .Entries()
                         .Where(x => x.Entity is BaseEntity && x.State == EntityState.Modified || 
                                     x.State == EntityState.Added)
                         .Select(x => new { Entity = (BaseEntity)x.Entity, State = x.State })
                    )
            {
                var nowUtc = DateTime.UtcNow;
                update.Entity.UpdateDate = nowUtc;
                if (update.State == EntityState.Added)
                    update.Entity.CreationDate = nowUtc;
            }
        }
        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken token = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, token);
        }
    }
}
