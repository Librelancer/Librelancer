// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using LibreLancer.Database;
using LibreLancer.Entities.Character;
using LibreLancer.Entities.Enums;
using LibreLancer.Net.Protocol;
using Microsoft.EntityFrameworkCore;

namespace LibreLancer.Server
{

    public class DatabaseCharacter
    {
        public long Id;
        private ServerDatabase db;
        private Character cached;

        internal DatabaseCharacter(Character c, ServerDatabase db)
        {
            Id = c.Id;
            cached = c;
            this.db = db;
        }

        public async Task Update(Action<Character> update, bool updatingCargo)
        {
            await db.Run(async () =>
            {
                await using var ctx = db.CreateDbContext();
                Character self;
                if (updatingCargo)
                {
                    self = await ctx.Characters
                        .Include(c => c.Items)
                        .AsSplitQuery()
                        .FirstAsync(c => c.Id == Id);
                }
                else
                {
                    self = await ctx.Characters
                        .FirstAsync(c => c.Id == Id);
                }
                update(self);
                cached = self;
                await ctx.SaveChangesAsync();
            });
        }

        public async Task UpdateFactionReps(IEnumerable<KeyValuePair<string, float>> reps)
        {
            await db.Run(async () =>
            {
                await using var ctx = db.CreateDbContext();
                await ctx.UpsertRepValues(Id, reps);
            });
        }

        public async Task UpdateVisitFlags(IEnumerable<KeyValuePair<uint, Visit>> flags)
        {
            await db.Run(async () =>
            {
                await using var ctx = db.CreateDbContext();
                await ctx.UpsertVisitValues(Id, flags);
            });
        }

        public async Task AddVisitHistory(IEnumerable<VisitHistoryInput> history)
        {
            await db.Run(async () =>
            {
                await using var ctx = db.CreateDbContext();
                await ctx.InsertVisitHistoryNonConflicting(Id, history);
            });
        }

        public async Task<Character> GetCharacter()
        {
            return await db.Run(async () =>
            {
                if (cached != null)
                    return cached;
                await using var ctx = db.CreateDbContext();
                cached = ctx.Characters
                    .Include(c => c.Items)
                    .Include(c => c.Reputations)
                    .Include(c => c.VisitEntries)
                    .First(c => c.Id == Id);
                return cached;
            });
        }
    }

    public record BannedPlayerDescription(Guid? AccountId, string[] Characters, DateTime? BanExpiry);

    public record AdminCharacterDescription(long Id, string Name, string System, string LastDockedLocation);

    public class ServerDatabase : IDisposable
    {
        private GameServer server;
        private BufferBlock<Func<Task>> actions = new();
        private Task actionQueueTask;

		public ServerDatabase(GameServer server)
		{
		    this.server = server;
            actionQueueTask = Task.Run(ProcessTaskQueue);
		}

        // RunContinuationsAsynchronously seems to avoid deadlocks
        internal Task<T> Run<T>(Func<Task<T>> func)
        {
            var compSrc = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            actions.Post(async () =>
            {
                try
                {
                    var result = await func();
                    compSrc.SetResult(result);
                }
                catch (Exception e)
                {
                    compSrc.SetException(e);
                }
            });
            return compSrc.Task;
        }

        internal Task Run(Func<Task> func)
        {
            var compSrc = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            actions.Post(async () =>
            {
                try
                {
                    await func();
                    compSrc.SetResult();
                }
                catch (Exception e)
                {
                    compSrc.SetException(e);
                }
            });
            return compSrc.Task;
        }

        async Task ProcessTaskQueue()
        {
            while (await actions.OutputAvailableAsync())
            {
                var item = await actions.ReceiveAsync();
                await item().ConfigureAwait(false);
            }
        }

        public LibreLancerContext CreateDbContext() => server.DbContextFactory.CreateDbContext(new string[0]);

        public async Task BanAccount(Guid playerGuid, DateTime expiryUtc)
        {
            await using var ctx = CreateDbContext();
            var acc = ctx.Accounts.FirstOrDefault(x => x.AccountIdentifier == playerGuid);
            if(acc != null)
                acc.BanExpiry = expiryUtc;
            await ctx.SaveChangesAsync();
        }

        public async Task UnbanAccount(Guid playerGuid)
        {
            await using var ctx = CreateDbContext();
            var acc = ctx.Accounts.FirstOrDefault(x => x.AccountIdentifier == playerGuid);
            if (acc != null)
                acc.BanExpiry = null;
            await ctx.SaveChangesAsync();
        }

        public AdminCharacterDescription[] GetAdmins()
        {
            using var ctx = CreateDbContext();
            return ctx.Characters.Where(x => x.IsAdmin).Select(x =>
                new AdminCharacterDescription(x.Id, x.Name, x.System, x.Base)).ToArray();
        }

        public BannedPlayerDescription[] GetBannedPlayers()
        {
            using var ctx = CreateDbContext();
            return ctx.Accounts.Where(x => x.BanExpiry != null)
                .Select(x => new BannedPlayerDescription(
                    x.AccountIdentifier,
                    x.Characters.Select(y => y.Name).ToArray(),
                    x.BanExpiry)).ToArray();
        }

        public async Task<long?> FindCharacter(string character)
        {
            return await Run(async () =>
            {
                await using var ctx = CreateDbContext();
                var c = ctx.Characters.Select(x => new {x.Id, x.Name}).FirstOrDefault(c => c.Name == character);
                return c?.Id;
            });
        }

        public async Task AdminCharacter(long character)
        {
            await Run(async () =>
            {
                await using var ctx = CreateDbContext();
                var c = ctx.Characters.FirstOrDefault(x => x.Id == character);
                if (c != null)
                {
                    c.IsAdmin = true;
                    await ctx.SaveChangesAsync();
                }
            });
        }

        public async Task DeadminCharacter(long character)
        {
            await Run(async () =>
            {
                await using var ctx = CreateDbContext();
                var c = ctx.Characters.FirstOrDefault(x => x.Id == character);
                if (c != null)
                {
                    c.IsAdmin = false;
                    await ctx.SaveChangesAsync();
                }
            });
        }

        public async Task<Guid?> FindAccount(string character)
        {
            return await Run(async () =>
            {
                await using var ctx = CreateDbContext();
                var c = ctx.Characters.Include(x => x.Account).FirstOrDefault(x => x.Name == character);
                c ??= ctx.Characters.Include(x => x.Account).FirstOrDefault(x => x.Name.Contains(character));
                return c?.Account?.AccountIdentifier;
            });
        }

        public async Task<List<SelectableCharacter>> PlayerLogin(Guid playerGuid)
        {
            return await Run(async () =>
            {
                await using var ctx = CreateDbContext();
                ctx.ChangeTracker.AutoDetectChangesEnabled = false;
                var acc = ctx.Accounts.Where(x => x.AccountIdentifier == playerGuid)
                    .Include(x => x.Characters)
                    .FirstOrDefault();
                if (acc == null)
                {
                    var utcnow = DateTime.UtcNow;
                    acc = new Account()
                    {
                        AccountIdentifier = playerGuid,
                        LastLogin = utcnow,
                        CreationDate = utcnow
                    };
                    ctx.Accounts.Add(acc);
                    ctx.SaveChanges();
                    return new List<SelectableCharacter>();
                }
                if (acc.BanExpiry.HasValue && acc.BanExpiry > DateTime.UtcNow)
                {
                    return null;
                }
                ctx.Entry(acc).Property(x => x.LastLogin).CurrentValue = DateTime.UtcNow;
                ctx.SaveChanges();
                var characters = new List<SelectableCharacter>();
                foreach (var c in acc.Characters)
                {
                    characters.Add(new SelectableCharacter()
                    {
                        Location = c.System,
                        Funds = c.Money,
                        Name = c.Name,
                        Rank = (int)c.Rank,
                        Ship = c.Ship,
                        Id = c.Id
                    });
                }
                return characters;
            });

        }

        public void DeleteCharacter(long characterId)
        {
            Run(async () =>
            {
                await using var ctx = CreateDbContext();
                var ch = ctx.Characters.First(x => x.Id == characterId);
                ctx.Characters.Remove(ch);
                await ctx.SaveChangesAsync();
            });
        }

        public bool NameInUse(string name)
        {
            using var ctx = CreateDbContext();
            return ctx.Characters.Any(x => x.Name.Equals(name));
        }

        public async Task<DatabaseCharacter> GetCharacter(long id)
        {
            return await Run(async () =>
            {
                await using var ctx = CreateDbContext();
                var character = await ctx.Characters
                    .Include(c => c.Items)
                    .Include(c => c.Reputations)
                    .Include(c => c.VisitEntries)
                    .AsSplitQuery()
                    .FirstAsync(c => c.Id == id);
                return new DatabaseCharacter(character, this);
            });
        }

        public async Task<long> AddCharacter(Guid playerGuid, Action<Character> fillCharacter)
        {
            return await Run(async () =>
            {
                await using var ctx = CreateDbContext();
                //Get account
                var acc = ctx.Accounts.First(x => x.AccountIdentifier == playerGuid);
                //Init object
                var c = new Character();
                fillCharacter(c);
                var nowUtc = DateTime.UtcNow;
                c.UpdateDate = c.CreationDate = nowUtc;
                c.Account = acc;
                //Add
                ctx.Characters.Add(c);
                await ctx.SaveChangesAsync();
                return c.Id;
            });
        }

        public void Dispose()
        {
            actions.Complete();
            actionQueueTask.Wait();
        }
    }
}
