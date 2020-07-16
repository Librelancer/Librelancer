// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Collections.Generic;
using LibreLancer.Database;
using LibreLancer.Entities.Character;
using Microsoft.EntityFrameworkCore;

namespace LibreLancer
{
	public class ServerDatabase
    {
        private GameServer server;
		public ServerDatabase(GameServer server)
		{
		    this.server = server;
		}

        LibreLancerContext CreateDbContext() => server.DbContextFactory.CreateDbContext(new string[0]);
        public List<SelectableCharacter> PlayerLogin(Guid playerGuid)
        {
            using (var ctx = CreateDbContext())
            {
                
                var acc = ctx.Accounts.FirstOrDefault(x => x.AccountIdentifier == playerGuid);
                if (acc == null)
                {
                    ctx.ChangeTracker.AutoDetectChangesEnabled = false;
                    acc = new Account() {AccountIdentifier = playerGuid, CreationDate = DateTime.UtcNow};
                    ctx.Accounts.Add(acc);
                }
                acc.LastLogin = DateTime.UtcNow;
                ctx.SaveChanges();
                var res = new List<SelectableCharacter>();
                foreach (var c in acc.Characters)
                {
                    res.Add(new SelectableCharacter()
                    {
                        Location = c.System,
                        Funds = c.Money,
                        Name = c.Name,
                        Rank = (int)c.Rank,
                        Ship = c.Ship,
                        Id = c.Id
                    });
                }
                return res;
            }
        }

        public bool NameInUse(string name)
        {
            using (var ctx = CreateDbContext())
            {
                return ctx.Characters.Any(x => x.Name == name);
            }
        }
        
        public Character GetCharacter(long id)
        {
            using (var ctx = CreateDbContext())
            {
                return ctx.Characters
                    .Include(c => c.Equipment)
                    .Include(c => c.Cargo)
                    .Include(c => c.Reputations)
                    .Include(c => c.VisitEntries)
                    .First(c => c.Id == id);
            }
        }

        public void UpdateCharacter(long id, Action<Character> updateAction)
        {
            using (var ctx = CreateDbContext())
            {
                var c = ctx.Characters.First(c => c.Id == id);
                updateAction?.Invoke(c);
                c.UpdateDate = DateTime.UtcNow;
                ctx.SaveChanges();
            }
        }
        
        public void AddCharacter(Guid playerGuid, Action<Character> fillCharacter)
        {
            using (var ctx = CreateDbContext())
            {
                ctx.ChangeTracker.AutoDetectChangesEnabled = false;
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
