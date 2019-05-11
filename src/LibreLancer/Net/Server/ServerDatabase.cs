// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace LibreLancer
{
	public class ServerDatabase : IDisposable
	{
        //MySqlConnection connection;
        List<PlayerAccount> accounts = new List<PlayerAccount>();
		public ServerDatabase(string connectionString)
		{
		}

		//TODO: Fill this in (pending Yuri's work)

		//Account
		public void CreateAccount(PlayerAccount p)
		{
            accounts.Add(p);
		}

		public PlayerAccount GetAccount(Guid guid)
		{
            return accounts.Where((x) => x.GUID == guid).FirstOrDefault();
		}

		public void AccountAccessed(PlayerAccount account)
		{
            account.LastVisit = DateTime.Now;

		}
		//Character
		public IEnumerable<ServerCharacter> GetOwnedCharacters(PlayerAccount account)
		{
            return account.Characters;
		}

        public void AddCharacter(ServerCharacter character)
        {
            
        }

        public void AddCharacterToAccount(PlayerAccount account, ServerCharacter character)
        {
            account.Characters.Add(character);
        }


		public void Dispose()
		{

		}
	}
}
