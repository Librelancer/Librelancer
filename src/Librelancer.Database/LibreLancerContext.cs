// MIT License - Copyright (c) Lazrius
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibreLancer.Entities.Abstract;
using LibreLancer.Entities.Enums;

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
            modelBuilder.Entity<Reputation>().Property(x => x.RepGroup).HasColumnType("TEXT COLLATE NOCASE");
            // Indexes
            modelBuilder.Entity<Account>().HasIndex(x => x.AccountIdentifier);

            modelBuilder.Entity<VisitEntry>().HasIndex(x => new { x.CharacterId, x.Hash }).IsUnique();
            modelBuilder.Entity<VisitEntry>().HasIndex(x => x.CharacterId);

            modelBuilder.Entity<Reputation>().HasIndex(x => new { x.CharacterId, x.RepGroup }).IsUnique();
            modelBuilder.Entity<Reputation>().HasIndex(x => x.CharacterId);
            // Define relationships
            modelBuilder.Entity<Account>().HasMany(x => x.Characters).WithOne(x => x.Account);
            modelBuilder.Entity<Character>().HasMany(x => x.Items).WithOne().OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Character>().HasMany(x => x.Reputations)
                .WithOne(x => x.Character)
                .HasForeignKey(x => x.CharacterId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            modelBuilder.Entity<Character>().HasMany(x => x.VisitEntries)
                .WithOne(x => x.Character)
                .HasForeignKey(x => x.CharacterId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        }

        public async Task UpsertRepValues(long characterId, KeyValuePair<string, float>[] reps)
        {
            await using var transaction = await Database.BeginTransactionAsync();
            var nowUtc = DateTime.UtcNow;
            foreach (var f in reps)
            {
                await Database.ExecuteSqlInterpolatedAsync(@$"
INSERT INTO VisitEntry(Id, CharacterId, RepGroup, ReputationValue, UpdateDate, CreationDate)
VALUES (NULL, {characterId}, {f.Key}, {f.Value}, {nowUtc}, {nowUtc})
ON CONFLICT(CharacterId, Hash) DO UPDATE SET
VisitValue=excluded.VisitValue, UpdateDate=excluded.UpdateDate WHERE VisitValue <> excluded.VisitValue");
            }
            await transaction.CommitAsync();
        }

        public async Task UpsertVisitValues(long characterId, KeyValuePair<uint, Visit>[] flags)
        {
            await using var transaction = await Database.BeginTransactionAsync();
            var nowUtc = DateTime.UtcNow;
            foreach (var f in flags)
            {
                await Database.ExecuteSqlInterpolatedAsync(@$"
INSERT INTO VisitEntry(Id, CharacterId, Hash, VisitValue, UpdateDate, CreationDate)
VALUES (NULL, {characterId}, {f.Key}, {(int)f.Value}, {nowUtc}, {nowUtc})
ON CONFLICT(CharacterId, Hash) DO UPDATE SET
VisitValue=excluded.VisitValue, UpdateDate=excluded.UpdateDate WHERE VisitValue <> excluded.VisitValue");
            }
            await transaction.CommitAsync();
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
