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

    public class DatabaseCharacter : IDisposable
    {
        public Character Character;
        private DbContext context;

        internal DatabaseCharacter(Character c, DbContext ctx)
        {
            Character = c;
            context = ctx;
        }
        public void ApplyChanges() => context.SaveChanges();

        public void Dispose()
        {
            context.Dispose();
        }
    }

    public class ServerDatabase
    {
        private GameServer server;
		public ServerDatabase(GameServer server)
		{
		    this.server = server;
		}

        LibreLancerContext CreateDbContext() => server.DbContextFactory.CreateDbContext(new string[0]);

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

        public Guid? FindAccount(string character)
        {
            using (var ctx = CreateDbContext())
            {
                var c = ctx.Characters.FirstOrDefault(x => x.Name == character);
                c ??= ctx.Characters.FirstOrDefault(x => x.Name.Contains(character));
                return c?.Account.AccountIdentifier;
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
            return new DatabaseCharacter(character, ctx);
        }

        public void AddCharacter(Guid playerGuid, Action<Character> fillCharacter)
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
            }
        }
        
    }
}
