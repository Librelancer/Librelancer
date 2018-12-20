// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using MySql.Data.MySqlClient;
using MySql.Data.Types;
namespace LibreLancer
{
	public class ServerDatabase : IDisposable
	{
        //MySqlConnection connection;
        List<PlayerAccount> accounts = new List<PlayerAccount>();
		public ServerDatabase(string connectionString, GameDataManager gameData)
		{
			//FLLog.Info("MySQL", "Connecting to database");
			//connection = new MySqlConnection(connectionString);
			//connection.Open();
		}

		//TODO: Fill this in (pending Yuri's work)

		//Account
		public void CreateAccount(PlayerAccount p)
		{
            accounts.Add(p);
			/*NonQuery("INSERT INTO accounts (accountGUID,lastvisit,registered,email) VALUES (@guid, @_lastvisit, @_registered, @_email)",
					   "@guid", p.GUID.ToString(),
					   "@_lastvisit", p.LastVisit,
			           "@_registered", p.Registered,
					   "@_email", p.Email);
			var res = Scalar("SELECT accountID FROM accounts WHERE accountGUID=@guid", "@guid", p.GUID.ToString());
			p.ID = Convert.ToInt32(res);*/
		}

		public PlayerAccount GetAccount(Guid guid)
		{
            return accounts.Where((x) => x.GUID == guid).FirstOrDefault();
			PlayerAccount acc = null;
			/*Reader("SELECT * FROM accounts WHERE accountGUID=@guid", (reader) =>
			{
				acc = new PlayerAccount();
				acc.GUID = guid;
				acc.ID = reader.GetInt32(0);
				acc.Email = reader.GetNullableString(2);
				acc.Registered = reader.GetDateTime(3);
				acc.LastVisit = reader.GetDateTime(4);
				return false;
			}, "@guid", guid.ToString());*/
			return acc;
		}

		public void AccountAccessed(PlayerAccount account)
		{
            account.LastVisit = DateTime.Now;
			/*account.LastVisit = DateTime.Now;
			NonQuery("UPDATE accounts SET lastvisit = @visit WHERE accountID = @id",
					   "@id", account.ID,
					   "@visit", account.LastVisit);*/
		}
		//Character
		public IEnumerable<ListedCharacter> GetOwnedCharacters(PlayerAccount account)
		{

            /*return Reader(
				"SELECT c.characterID, c.callsign, c.credits, s.nickname FROM characters c" +
				"LEFT JOIN systems s ON (c.systemID = s.systemID) WHERE c.accountID = @acc", 
				(reader) =>
			{
				var lc = new ListedCharacter();
				lc.ID = reader.GetInt32(0);
				lc.Name = reader.GetString(1);
				lc.Credits = reader.GetInt64(2);
				lc.Location = reader.GetString(3);
				return lc;
			}, "@acc", account.ID);*/
            return account.Characters;
		}

        public void AddCharacter(ListedCharacter character)
        {
            
        }
        public void AddCharacterToAccount(PlayerAccount account, ListedCharacter character)
        {
            account.Characters.Add(character);
        }


		public void Dispose()
		{
			//FLLog.Info("MySQL", "Closing connection");
			//connection.Close();
		}
	}
}
