/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2017
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Collections.Generic;
using System.IO;
using MySql.Data.MySqlClient;
using MySql.Data.Types;
namespace LibreLancer
{
	public class ServerDatabase : IDisposable
	{
		MySqlConnection connection;

		public ServerDatabase(string connectionString, LegacyGameData gameData)
		{
			FLLog.Info("MySQL", "Connecting to database");
			connection = new MySqlConnection(connectionString);
			connection.Open();
		}
		//TODO: Fill this in (pending Yuri's work)

		//Account
		public void CreateAccount(PlayerAccount p)
		{
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
			return new List<ListedCharacter>();
		}


		public void Dispose()
		{
			FLLog.Info("MySQL", "Closing connection");
			connection.Close();
		}
	}
}
