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

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder
                .Properties<DateTime>()
                .HaveConversion(typeof(DateTimeToJulianConverter));
        }

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

            modelBuilder.Entity<VisitHistoryEntry>().HasIndex(x => x.CharacterId);
            modelBuilder.Entity<VisitHistoryEntry>()
                .HasIndex(x => new { x.CharacterId, x.Kind, x.Hash })
                .IsUnique();

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

            modelBuilder.Entity<Character>().HasMany(x => x.VisitHistoryEntries)
                .WithOne(x => x.Character)
                .HasForeignKey(x => x.CharacterId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        }

        public async Task InsertVisitHistoryNonConflicting(long characterId, IEnumerable<VisitHistoryInput> history)
        {
            await using var transaction = await Database.BeginTransactionAsync();
            var nowUtc = DateTimeToJulianConverter.ToJulianDays(DateTime.UtcNow);
            foreach (var h in history)
            {
                await Database.ExecuteSqlInterpolatedAsync(@$"
INSERT INTO VisitHistory(Id, CharacterId, Kind, Hash, UpdateDate, CreationDate)
VALUES (NULL, {characterId}, {h.Kind}, {h.Hash}, {nowUtc}, {nowUtc})
ON CONFLICT IGNORE;");
            }
            await transaction.CommitAsync();
        }

        public async Task UpsertRepValues(long characterId, IEnumerable<KeyValuePair<string, float>> reps)
        {
            await using var transaction = await Database.BeginTransactionAsync();
            var nowUtc = DateTimeToJulianConverter.ToJulianDays(DateTime.UtcNow);
            foreach (var f in reps)
            {
                await Database.ExecuteSqlInterpolatedAsync(@$"
INSERT INTO Reputation(Id, CharacterId, RepGroup, ReputationValue, UpdateDate, CreationDate)
VALUES (NULL, {characterId}, {f.Key}, {f.Value}, {nowUtc}, {nowUtc})
ON CONFLICT(CharacterId, RepGroup) DO UPDATE SET
ReputationValue=excluded.ReputationValue, UpdateDate=excluded.UpdateDate WHERE ReputationValue <> excluded.ReputationValue");
            }
            await transaction.CommitAsync();
        }

        public async Task UpsertVisitValues(long characterId, IEnumerable<KeyValuePair<uint, Visit>> flags)
        {
            await using var transaction = await Database.BeginTransactionAsync();
            var nowUtc = DateTimeToJulianConverter.ToJulianDays(DateTime.UtcNow);
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
            var nowUtc = DateTime.UtcNow;
            foreach (var update in ChangeTracker
                         .Entries()
                         .Where(x => x.Entity is BaseEntity && x.State == EntityState.Modified ||
                                     x.State == EntityState.Added)
                         .Select(x => new { Entity = (BaseEntity)x.Entity, State = x.State })
                    )
            {
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
