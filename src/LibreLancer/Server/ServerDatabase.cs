// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Threading.Tasks;
using LibreLancer.Database;
using LibreLancer.Entities.Character;
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

        public void Update(Action<Character> update)
        {
            using (var ctx = db.CreateDbContext())
            {
                cached = ctx.Characters
                    .Include(c => c.Items)
                    .Include(c => c.Reputations)
                    .Include(c => c.VisitEntries)
                    .First(c => c.Id == Id);
                update(cached);
                ctx.SaveChanges();
            }
        }

        public Character GetCharacter()
        {
            if (cached != null)
                return cached;
            using (var ctx = db.CreateDbContext())
            {
                cached = ctx.Characters
                    .Include(c => c.Items)
                    .Include(c => c.Reputations)
                    .Include(c => c.VisitEntries)
                    .First(c => c.Id == Id);
            }
            return cached;
        }
    }

    public record BannedPlayerDescription(Guid AccountId, string[] Characters, DateTime Expiry);

    public record AdminCharacterDescription(long Id, string Name);

    public class ServerDatabase
    {
        private GameServer server;
		public ServerDatabase(GameServer server)
		{
		    this.server = server;
		}

        public LibreLancerContext CreateDbContext() => server.DbContextFactory.CreateDbContext(new string[0]);

        public async Task BanAccount(Guid playerGuid, DateTime expiryUtc)
        {
            using (var ctx = CreateDbContext())
            {
                var acc = ctx.Accounts.FirstOrDefault(x => x.AccountIdentifier == playerGuid);
                if(acc != null)
                    acc.BanExpiry = expiryUtc;
                await ctx.SaveChangesAsync();
            }
        }

        public async Task UnbanAccount(Guid playerGuid)
        {
            using (var ctx = CreateDbContext())
            {
                var acc = ctx.Accounts.FirstOrDefault(x => x.AccountIdentifier == playerGuid);
                if (acc != null)
                    acc.BanExpiry = null;
                await ctx.SaveChangesAsync();
            }
        }

        public AdminCharacterDescription[] GetAdmins()
        {
            using var ctx = CreateDbContext();
            return ctx.Characters.Where(x => x.IsAdmin).Select(x =>
                new AdminCharacterDescription(x.Id, x.Name)).ToArray();
        }

        public BannedPlayerDescription[] GetBannedPlayers()
        {
            using (var ctx = CreateDbContext())
            {
                var c = ctx.Accounts.Where(x => x.BanExpiry != null && x.BanExpiry > DateTime.UtcNow)
                    .Select(x => new
                    {
                        AccountId = x.AccountIdentifier,
                        BanExpiry = x.BanExpiry,
                        Characters = x.Characters.Select(y => y.Name).ToArray()
                    });
                return c.Select(x => new BannedPlayerDescription(x.AccountId, x.Characters, x.BanExpiry.Value)).ToArray();
            }
        }

        public long? FindCharacter(string character)
        {
            using (var ctx = CreateDbContext())
            {
                var c = ctx.Characters.Select(x => new {x.Id, x.Name}).FirstOrDefault(c => c.Name == character);
                return c?.Id;
            }
        }


        public async Task AdminCharacter(long character)
        {
            using (var ctx = CreateDbContext())
            {
                var c = ctx.Characters.FirstOrDefault(x => x.Id == character);
                if (c != null)
                {
                    c.IsAdmin = true;
                    await ctx.SaveChangesAsync();
                }
            }
        }

        public async Task DeadminCharacter(long character)
        {
            using (var ctx = CreateDbContext())
            {
                var c = ctx.Characters.FirstOrDefault(x => x.Id == character);
                if (c != null)
                {
                    c.IsAdmin = false;
                    await ctx.SaveChangesAsync();
                }
            }
        }

        public Guid? FindAccount(string character)
        {
            using (var ctx = CreateDbContext())
            {
                var c = ctx.Characters.Include(x => x.Account).FirstOrDefault(x => x.Name == character);
                c ??= ctx.Characters.Include(x => x.Account).FirstOrDefault(x => x.Name.Contains(character));
                return c?.Account?.AccountIdentifier;
            }
        }

        public bool PlayerLogin(Guid playerGuid, out List<SelectableCharacter> characters)
        {
            using (var ctx = CreateDbContext())
            {
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
                    characters = new List<SelectableCharacter>();
                    return true;
                }
                if (acc.BanExpiry.HasValue && acc.BanExpiry > DateTime.UtcNow)
                {
                    characters = null;
                    return false;
                }
                ctx.Entry(acc).Property(x => x.LastLogin).CurrentValue = DateTime.UtcNow;
                ctx.SaveChanges();
                characters = new List<SelectableCharacter>();
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
                return true;
            }
        }

        public void DeleteCharacter(long characterId)
        {
            using (var ctx = CreateDbContext())
            {
                var ch = ctx.Characters.First(x => x.Id == characterId);
                ctx.Characters.Remove(ch);
                ctx.SaveChanges();
            }
        }

        public bool NameInUse(string name)
        {
            using (var ctx = CreateDbContext())
            {
                return ctx.Characters.Any(x => x.Name.Equals(name));
            }
        }

        public DatabaseCharacter GetCharacter(long id)
        {
            var ctx = CreateDbContext();
            var character = ctx.Characters
                    .Include(c => c.Items)
                    .Include(c => c.Reputations)
                    .Include(c => c.VisitEntries)
                    .First(c => c.Id == id);
            return new DatabaseCharacter(character, this);
        }

        public long AddCharacter(Guid playerGuid, Action<Character> fillCharacter)
        {
            using (var ctx = CreateDbContext())
            {
                //Get account
                var acc = ctx.Accounts.First(x => x.AccountIdentifier == playerGuid);
                //Init object
                var c = new Character();
                fillCharacter(c);
                c.UpdateDate = c.CreationDate = DateTime.UtcNow;
                c.Account = acc;
                //Add
                ctx.Characters.Add(c);
                ctx.SaveChanges();
                return c.Id;
            }
        }

    }
}
