﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using System.IO;
using DarkMultiPlayerCommon;

namespace DarkMultiPlayerServer
{

    public class DBManager
    {
        private static string dbFileName;
        private static SQLiteConnection m_dbConnection;

        public static void Init()
        {
            DarkLog.Debug("Initializing DBManager");
            dbFileName = Path.Combine(Server.universeDirectory,"dmp.sqlite");
            if (!File.Exists(dbFileName))
            {
                setupDatabase();
            }
            else
            {
                m_dbConnection = new SQLiteConnection("Data Source=Universe/dmp.sqlite;Version=3;");
                m_dbConnection.Open();
            }
        }

        /// <summary>
        /// Creates the database and required tables
        /// </summary>
        private static void setupDatabase()
        {
            DarkLog.Debug("DBManager.setupDatabase()");
            m_dbConnection = new SQLiteConnection("Data Source=Universe/dmp.sqlite;Version=3;");
            m_dbConnection.Open();
            try {
                string sql = "BEGIN;";
                sql += "CREATE TABLE team (id integer PRIMARY KEY AUTOINCREMENT, name text, password text, funds real, reputation real, science real);";
                sql += "CREATE TABLE team_members(id integer, name text, pubkey text, FOREIGN KEY(id) REFERENCES team(id) ON DELETE CASCADE);";
                sql += "CREATE TABLE team_research (id integer, techID  text, UNIQUE (id,techID) ON CONFLICT REPLACE, FOREIGN KEY(id) REFERENCES team(id) ON DELETE CASCADE);";
                sql += "CREATE TABLE team_parts (id integer, partName  text, UNIQUE (id,partName) ON CONFLICT REPLACE, FOREIGN KEY(id) REFERENCES team(id) ON DELETE CASCADE);";
				sql += "CREATE TABLE team_acceptedContracts (id integer, contractTitle, UNIQUE (id,contractTitle) ON CONFLICT REPLACE, FOREIGN KEY(id) REFERENCES team(id) ON DELETE CASCADE);";
				sql += "CREATE TABLE team_cancelledContracts (id integer, contractTitle, UNIQUE (id,contractTitle) ON CONFLICT REPLACE, FOREIGN KEY(id) REFERENCES team(id) ON DELETE CASCADE);";
				sql += "CREATE TABLE team_completedContracts (id integer, contractTitle, UNIQUE (id,contractTitle) ON CONFLICT REPLACE, FOREIGN KEY(id) REFERENCES team(id) ON DELETE CASCADE);";
				sql += "CREATE TABLE team_declinedContracts (id integer, contractTitle, UNIQUE (id,contractTitle) ON CONFLICT REPLACE, FOREIGN KEY(id) REFERENCES team(id) ON DELETE CASCADE);";
				sql += "CREATE TABLE team_failedContracts (id integer, contractTitle, UNIQUE (id,contractTitle) ON CONFLICT REPLACE, FOREIGN KEY(id) REFERENCES team(id) ON DELETE CASCADE);";
				sql += "CREATE TABLE team_finishedContracts (id integer, contractTitle, UNIQUE (id,contractTitle) ON CONFLICT REPLACE, FOREIGN KEY(id) REFERENCES team(id) ON DELETE CASCADE);";
				sql += "CREATE TABLE team_offeredContracts (id integer, contractTitle, UNIQUE (id,contractTitle) ON CONFLICT REPLACE, FOREIGN KEY(id) REFERENCES team(id) ON DELETE CASCADE);";
				sql += "CREATE TRIGGER del_team_on_last_member AFTER DELETE on team_members BEGIN DELETE FROM team  WHERE team.id IN (SELECT team.id FROM team LEFT JOIN team_members ON team.id = team_members.id GROUP BY team.id HAVING COUNT(team_members.id) = 0);END; ";
                sql += "COMMIT;";
                executeQry(sql);
            }
            catch (SQLiteException e)
            {
                DarkLog.Debug(e.Message);
                executeQry("ROLLBACK;");
                Environment.Exit(0);
            }
        }
        /// <summary>
        /// Creates a new team in the database
        /// </summary>
        /// <param name="name"></param>
        /// <param name="password"></param>
        /// <param name="funds"></param>
        /// <param name="reputation"></param>
        /// <param name="science"></param>
        /// <param name="creator"></param>
        /// <param name="creator_pubKey"></param>
        /// <returns></returns>
        public static TeamStatus createNewTeam(string name, string password, double funds, float reputation, float science, List<string> research, List<string> purchased, List<List<string>> contracts, string creator, string creator_pubKey)
        {
            try {
                string sql = "BEGIN;";
                sql += "INSERT INTO team (name, password, funds, reputation, science) VALUES (@name,@password,@funds,@reputation,@science);";
                sql += "SELECT last_insert_rowid()";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                command.Parameters.Add(new SQLiteParameter("@name", name));
                command.Parameters.Add(new SQLiteParameter("@password", password));
                command.Parameters.Add(new SQLiteParameter("@funds", funds));
				command.Parameters.Add(new SQLiteParameter("@reputation", reputation));
				command.Parameters.Add(new SQLiteParameter("@science", science));
				SQLiteDataReader reader = command.ExecuteReader();
                reader.Read();
                int rowid = reader.GetInt32(0);
                DarkLog.Debug("rowid: " + rowid.ToString());
                sql = "INSERT INTO team_members(id, name, pubkey) VALUES (@id,@name,@pubkey);";
                sql += "COMMIT;";
                command = new SQLiteCommand(sql, m_dbConnection);
                command.Parameters.Clear();
                command.Parameters.Add(new SQLiteParameter("@id", rowid));
                command.Parameters.Add(new SQLiteParameter("@name", creator));
                command.Parameters.Add(new SQLiteParameter("@pubkey", creator_pubKey));
                command.ExecuteNonQuery();

                TeamStatus team = new TeamStatus();
                team.teamID = rowid;
                team.teamName = name;
                team.funds = funds;
                team.reputation = reputation;
                team.science = science;
				team.research = research;
				team.purchased = purchased;
				team.contracts = contracts;
                team.teamMembers = new List<MemberStatus>();
                MemberStatus member = new MemberStatus();
                member.memberName = creator;
                member.online = true;
                team.teamMembers.Add(member);

                return team;
            } catch (SQLiteException e)
            {
                DarkLog.Debug(e.Message);
                executeQry("ROLLBACK;");
                return null;
            }
        }

        /// <summary>
        /// Adds a new player to a team
        /// </summary>
        /// <param name="teamName">Name of the team</param>
        /// <param name="password">Supplied password</param>
        /// <param name="playerName">Name of the player</param>
        /// <param name="pubKey">Public key of the client</param>
        /// <returns></returns>
        public static bool addPlayerToTeam(string teamName, string password, string playerName, string pubKey)
        {
            try { 
                string sql = "SELECT id, password FROM team WHERE name = @teamName";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                command.Parameters.Add(new SQLiteParameter("@teamName", teamName));
                SQLiteDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    reader.Read();
                    int id = reader.GetInt32(0);
                    string pw = reader.GetString(1);
                    if (password != pw)
                        return false;
                    else
                    {
                        // add user!
                        sql = "INSERT INTO team_members(id, name, pubkey) VALUES (@id,@name,@pubkey);";
                        command = new SQLiteCommand(sql, m_dbConnection);
                        command.Parameters.Clear();
                        command.Parameters.Add(new SQLiteParameter("@id", id));
                        command.Parameters.Add(new SQLiteParameter("@name", playerName));
                        command.Parameters.Add(new SQLiteParameter("@pubkey", pubKey));
                        command.ExecuteNonQuery();
                        return true;
                    }
                }
                else
                    return false;
            }
            catch (SQLiteException e)
            {
                DarkLog.Debug(e.Message);
                return false;
            }
        }

        /// <summary>
        /// Removes a player from a team in the database
        /// </summary>
        /// <param name="teamName">Name of the team</param>
        /// <param name="playerName">Name of the player</param>
        public static void removePlayerFromTeam(string teamName, string playerName)
        {
            try {
                string sql = "DELETE FROM team_members WHERE team_members.id in (SELECT team.id FROM team WHERE team.name = @teamName) AND team_members.name = @playerName";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                command.Parameters.Add(new SQLiteParameter("@teamName", teamName));
                command.Parameters.Add(new SQLiteParameter("@playerName", playerName));
                command.ExecuteNonQuery();
            }
            catch (SQLiteException e)
            {
                DarkLog.Debug(e.Message);
            }
        }

        /// <summary>
        /// Fetches the TeamStatus including all team members from the database
        /// </summary>
        /// <param name="teamName">The name of the team</param>
        /// <returns></returns>
        public static TeamStatus getTeamStatus(string teamName)
        {
            try { 
                string sql = "SELECT id,name,funds,reputation,science FROM team WHERE name = @teamName";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                command.Parameters.Add(new SQLiteParameter("@teamName", teamName));
                SQLiteDataReader reader = command.ExecuteReader();
                if (!reader.Read())
                    return null;
                TeamStatus team = parseTeamStatus(reader);

                return team;
            } catch (SQLiteException e)
            {
                DarkLog.Debug(e.Message);
                return null;
            }
        }

        public static TeamStatus getTeamStatusByPlayerName(string playerName)
        {
            try
            {
                string sql = "SELECT id,name,funds,reputation,science FROM team INNER JOIN team_members on team.id = team_members.id WHERE team_members.name = @playerName";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                command.Parameters.Add(new SQLiteParameter("@playerName", playerName));
                SQLiteDataReader reader = command.ExecuteReader();
                if (!reader.Read())
                    return null;
                TeamStatus team = parseTeamStatus(reader);

                return team;
            }
            catch (SQLiteException e)
            {
                DarkLog.Debug(e.Message);
                return null;
            }
        }

        /// <summary>
        /// Fetches all the teams from the database and returns them as a list
        /// </summary>
        /// <returns></returns>
        public static List<TeamStatus> getTeamStatusList()
        {
            List<TeamStatus> teamList = new List<TeamStatus>();

            string sql = "SELECT id,name,funds,reputation,science FROM team";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                TeamStatus team = parseTeamStatus(reader);
                teamList.Add(team);
            }
            return teamList;
        }

        /// <summary>
        /// internal helper function to get the status of a team
        /// </summary>
        /// <param name="reader">Requires the following fields from team: id,name,funds,reputation,science</param>
        /// <returns></returns>
        private static TeamStatus parseTeamStatus(SQLiteDataReader reader)
        {
            TeamStatus team = new TeamStatus();
            team.teamID = reader.GetInt32(0);
            team.teamName = reader.GetString(1);
            team.funds = reader.GetDouble(2);
			team.reputation = reader.GetFloat(3);
			team.science = reader.GetFloat(4);
			team.research = getTeamResearch(team.teamName);
			team.purchased = getTeamParts(team.teamName);
			team.contracts = getTeamContracts(team.teamName);

            string sql = "SELECT name FROM team_members where id = @id";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.Parameters.Add(new SQLiteParameter("@id", team.teamID));
            team.teamMembers = new List<MemberStatus>();
            SQLiteDataReader reader2 = command.ExecuteReader();
            while (reader2.Read())
            {
                MemberStatus member = new MemberStatus();
                member.memberName = reader2.GetString(0);
                if (ClientHandler.GetActivePlayerNames().Contains(member.memberName))
                {
                    member.online = true;
                } else
                    member.online = false;
                team.teamMembers.Add(member);
            }

            return team;
        }

        /// <summary>
        /// Returns the teamid of the supplied playername
        /// </summary>
        /// <param name="name">ClientObject.playerName</param>
        /// <returns></returns>
        public static int getTeamIdByPlayerName(string name)
        {
            try {
                string sql = "SELECT team.id FROM team INNER JOIN team_members on team.id = team_members.id WHERE team_members.name = @name";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                command.Parameters.Add(new SQLiteParameter("@name", name));
                SQLiteDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    reader.Read();
                    return reader.GetInt32(0);
                } else
                {
                    // player is not in a team
                    return -1;
                }
            }
            catch (SQLiteException e)
            {
                DarkLog.Debug(e.Message);
                return -1;
            }
        }

        public static int getTeamIdByTeamName(string name)
        {
            try
            {
                string sql = "SELECT id FROM team WHERE name  = @teamName";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                command.Parameters.Add(new SQLiteParameter("@teamName", name));
                SQLiteDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    reader.Read();
                    return reader.GetInt32(0);
                }
                else
                {
                    // player is not in a team
                    return -1;
                }
            }
            catch (SQLiteException e)
            {
                DarkLog.Debug(e.Message);
                return -1;
            }
        }

        /// <summary>
        /// Fetches the team name of a supplied playername and returns it
        /// </summary>
        /// <param name="playerName">Name of the player</param>
        /// <returns></returns>
        public static string getTeamNameByPlayerName(string playerName)
        {
            try {
                string sql = "SELECT team.name FROM team INNER JOIN team_members on team.id = team_members.id WHERE team_members.name = @name";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                command.Parameters.Add(new SQLiteParameter("@name", playerName));
                SQLiteDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    reader.Read();
                    return reader.GetString(0);
                }
                else
                {
                    // player is not in a team
                    return "";
                }
            }
            catch (SQLiteException e)
            {
                DarkLog.Debug(e.Message);
                return "";
            }
        }

        /// <summary>
        /// Sets the science of a team to the supplied value
        /// </summary>
        /// <param name="teamid">id of the team</param>
        /// <param name="science">total science(not diff!)</param>
        public static void updateTeamScience(string teamName, float science)
        {
            try {
                string sql = "UPDATE team SET science = @science WHERE name = @teamName";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                command.Parameters.Add(new SQLiteParameter("@science", science));
                command.Parameters.Add(new SQLiteParameter("@teamName", teamName));
                int ret = command.ExecuteNonQuery();
                DarkLog.Debug("DBManager: updated team science of team: " + teamName + " to science: " + science.ToString() + "rows changed: " + ret.ToString());
            } catch (SQLiteException e)
            {
                DarkLog.Debug(e.Message);
            }
        }

        public static void updateTeamFunds(string teamName, double funds)
        {
            try
            {
                string sql = "UPDATE team SET funds = @funds WHERE name = @teamName";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                command.Parameters.Add(new SQLiteParameter("@funds", funds));
                command.Parameters.Add(new SQLiteParameter("@teamName", teamName));
                int ret = command.ExecuteNonQuery();
                DarkLog.Debug("DBManager: updated team funds of team: " + teamName + " to funds: " + funds.ToString() + "rows changed: " + ret.ToString());
            }
            catch (SQLiteException e)
            {
                DarkLog.Debug(e.Message);
            }
        }

        public static void updateTeamReputation(string teamName, float reputation)
        {
            try
            {
                string sql = "UPDATE team SET reputation = @reputation WHERE name = @teamName";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                command.Parameters.Add(new SQLiteParameter("@reputation", reputation));
                command.Parameters.Add(new SQLiteParameter("@teamName", teamName));
                int ret = command.ExecuteNonQuery();
                DarkLog.Debug("DBManager: updated team reputation of team: " + teamName + " to reputation: " + reputation.ToString() + "rows changed: " + ret.ToString());
            }
            catch (SQLiteException e)
            {
                DarkLog.Debug(e.Message);
            }
        }

        /// <summary>
        /// Adds new research to a team
        /// </summary>
        /// <param name="teamName"></param>
        /// <param name="techID"></param>
        public static void addResearchTech(string teamName, string techID)
        {
            try
            {
                string sql = "INSERT INTO team_research(id,techID) VALUES((SELECT id FROM TEAM WHERE name = @teamName),@techID);";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                command.Parameters.Add(new SQLiteParameter("@teamName", teamName));
                command.Parameters.Add(new SQLiteParameter("@techID", techID));
                int ret = command.ExecuteNonQuery();
                DarkLog.Debug("DBManager: added research to team: " + teamName + " with techID: " + techID + "rows changed: " + ret.ToString());
            }
            catch (SQLiteException e)
            {
                DarkLog.Debug(e.Message);
            }
        }

        public static void addResearchTech(int teamID, string techID)
        {
            try
            {
                string sql = "INSERT INTO team_research(id,techID) VALUES(@teamID,@techID);";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                command.Parameters.Add(new SQLiteParameter("@teamID", teamID));
                command.Parameters.Add(new SQLiteParameter("@techID", techID));
                int ret = command.ExecuteNonQuery();
                DarkLog.Debug("DBManager: added research to teamID: " + teamID + " with techID: " + techID + "rows changed: " + ret.ToString());
            }
            catch (SQLiteException e)
            {
                DarkLog.Debug(e.Message);
            }
        }

        /// <summary>
        /// Adds a purchased part to a team
        /// </summary>
        /// <param name="teamName"></param>
        /// <param name="partName"></param>
        public static void addPurchasedPart(string teamName, string partName)
        {
            try
            {
                string sql = "INSERT INTO team_parts(id,partName) VALUES((SELECT id FROM TEAM WHERE name = @teamName),@partName);";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                command.Parameters.Add(new SQLiteParameter("@teamName", teamName));
                command.Parameters.Add(new SQLiteParameter("@partName", partName));
                int ret = command.ExecuteNonQuery();
                DarkLog.Debug("DBManager: added purchasedPart to team: " + teamName + " with partName: " + partName + "rows changed: " + ret.ToString());
            }
            catch (SQLiteException e)
            {
                DarkLog.Debug(e.Message);
            }
        }

        public static void addPurchasedPart(int teamID, string partName)
        {
            try
            {
                string sql = "INSERT INTO team_parts(id,partName) VALUES(@teamID,@partName);";
                SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
                command.Parameters.Add(new SQLiteParameter("@teamID", teamID));
                command.Parameters.Add(new SQLiteParameter("@partName", partName));
                int ret = command.ExecuteNonQuery();
                DarkLog.Debug("DBManager: added purchasedPart to teamID: " + teamID + " with partName: " + partName + "rows changed: " + ret.ToString());
            }
            catch (SQLiteException e)
            {
                DarkLog.Debug(e.Message);
            }
        }

        public static void setInitialTechState(string teamName, List<string> techIDs, List<string> parts)
        {
            try
            {
                int teamID = DBManager.getTeamIdByTeamName(teamName);
                if (teamID < 0)
                {
                    DarkLog.Debug("DBManager.setInitialTechState: could not find teamName: " + teamName);
                    return;
                }
                DarkLog.Debug("setInitialTechState: teamName: " + teamName + " techIDs.Count: " + techIDs.Count + " parts.Count: " + parts.Count);
                using(SQLiteTransaction tr = m_dbConnection.BeginTransaction())
                {
                    using(SQLiteCommand cmd = m_dbConnection.CreateCommand())
                    {
                        cmd.Transaction = tr;
                        foreach (string techID in techIDs)
                        {
                            cmd.CommandText = "INSERT INTO team_research (id,techID) VALUES(@teamID,@techID);";
                            cmd.Parameters.Add(new SQLiteParameter("@teamID", teamID));
                            cmd.Parameters.Add(new SQLiteParameter("@techID", techID));
                            cmd.ExecuteNonQuery();
                            cmd.Parameters.Clear();
                        }
                        foreach(string partName in parts)
                        {
                            cmd.CommandText = "INSERT INTO team_parts (id,partName) VALUES(@teamID,@partName);";
                            cmd.Parameters.Add(new SQLiteParameter("@teamID", teamID));
                            cmd.Parameters.Add(new SQLiteParameter("@partName", partName));
                            cmd.ExecuteNonQuery();
                            cmd.Parameters.Clear();
                        }
                        tr.Commit();
                    }
                }
            }
            catch (SQLiteException e)
            {
                DarkLog.Debug(e.Message);
            }
        }

		public static void setInitialContractState(string teamName, List<List<string>> contracts) {
			try {
				int teamID = DBManager.getTeamIdByTeamName(teamName);
				if (teamID < 0) {
					DarkLog.Debug("DBManager.setInitialTechState: could not find teamName: " + teamName);
					return;
				}
				DarkLog.Debug("setInitialContractState: teamName: " + teamName + " contracts.Count: " + contracts.Count);
				using (SQLiteTransaction tr = m_dbConnection.BeginTransaction()) {
					using (SQLiteCommand cmd = m_dbConnection.CreateCommand()) {
						cmd.Transaction = tr;
						for(int i = 0; i < 7; i++) {
							switch(i) {
								case 0:
									foreach(string contractTitle in contracts.ElementAt(i)) {
										cmd.CommandText = "INSERT INTO team_acceptedContracts (id,contractTitle) VALUES(@teamID,@contractTitle);";
										cmd.CommandText += "DELETE FROM team_offeredContracts WHERE team_offeredContracts.contractTitle  = @contractTitle;";
										cmd.Parameters.Add(new SQLiteParameter("@teamID", teamID));
										cmd.Parameters.Add(new SQLiteParameter("@contractTitle", contractTitle));
										cmd.ExecuteNonQuery();
										cmd.Parameters.Clear();
									}
									break;
								case 1:
									foreach (string contractTitle in contracts.ElementAt(i)) {
										cmd.CommandText = "INSERT INTO team_cancelledContracts (id,contractTitle) VALUES(@teamID,@contractTitle);";
										cmd.CommandText += "DELETE FROM team_acceptedContracts WHERE team_acceptedContracts.contractTitle  = @contractTitle;";
										cmd.CommandText += "DELETE FROM team_offeredContracts WHERE team_offeredContracts.contractTitle  = @contractTitle;";
										cmd.Parameters.Add(new SQLiteParameter("@teamID", teamID));
										cmd.Parameters.Add(new SQLiteParameter("@contractTitle", contractTitle));
										cmd.ExecuteNonQuery();
										cmd.Parameters.Clear();
									}
									break;
								case 2:
									foreach (string contractTitle in contracts.ElementAt(i)) {
										cmd.CommandText = "INSERT INTO team_completedContracts (id,contractTitle) VALUES(@teamID,@contractTitle);";
										cmd.CommandText += "DELETE FROM team_acceptedContracts WHERE team_acceptedContracts.contractTitle  = @contractTitle;";
										cmd.CommandText += "DELETE FROM team_offeredContracts WHERE team_offeredContracts.contractTitle  = @contractTitle;";
										cmd.Parameters.Add(new SQLiteParameter("@teamID", teamID));
										cmd.Parameters.Add(new SQLiteParameter("@contractTitle", contractTitle));
										cmd.ExecuteNonQuery();
										cmd.Parameters.Clear();
									}
									break;
								case 3:
									foreach (string contractTitle in contracts.ElementAt(i)) {
										cmd.CommandText = "INSERT INTO team_declinedContracts (id,contractTitle) VALUES(@teamID,@contractTitle);";
										cmd.CommandText += "DELETE FROM team_acceptedContracts WHERE team_acceptedContracts.contractTitle  = @contractTitle;";
										cmd.CommandText += "DELETE FROM team_offeredContracts WHERE team_offeredContracts.contractTitle  = @contractTitle;";
										cmd.Parameters.Add(new SQLiteParameter("@teamID", teamID));
										cmd.Parameters.Add(new SQLiteParameter("@contractTitle", contractTitle));
										cmd.ExecuteNonQuery();
										cmd.Parameters.Clear();
									}
									break;
								case 4:
									foreach (string contractTitle in contracts.ElementAt(i)) {
										cmd.CommandText = "INSERT INTO team_failedContracts (id,contractTitle) VALUES(@teamID,@contractTitle);";
										cmd.CommandText += "DELETE FROM team_acceptedContracts WHERE team_acceptedContracts.contractTitle  = @contractTitle;";
										cmd.CommandText += "DELETE FROM team_offeredContracts WHERE team_offeredContracts.contractTitle  = @contractTitle;";
										cmd.Parameters.Add(new SQLiteParameter("@teamID", teamID));
										cmd.Parameters.Add(new SQLiteParameter("@contractTitle", contractTitle));
										cmd.ExecuteNonQuery();
										cmd.Parameters.Clear();
									}
									break;
								case 5:
									foreach (string contractTitle in contracts.ElementAt(i)) {
										cmd.CommandText = "INSERT INTO team_finishedContracts (id,contractTitle) VALUES(@teamID,@contractTitle);";
										cmd.CommandText += "DELETE FROM team_acceptedContracts WHERE team_acceptedContracts.contractTitle  = @contractTitle;";
										cmd.CommandText += "DELETE FROM team_offeredContracts WHERE team_offeredContracts.contractTitle  = @contractTitle;";
										cmd.Parameters.Add(new SQLiteParameter("@teamID", teamID));
										cmd.Parameters.Add(new SQLiteParameter("@contractTitle", contractTitle));
										cmd.ExecuteNonQuery();
										cmd.Parameters.Clear();
									}
									break;
								case 6:
									foreach (string contractTitle in contracts.ElementAt(i)) {
										cmd.CommandText = "INSERT INTO team_offeredContracts (id,contractTitle) VALUES(@teamID,@contractTitle);";
										cmd.CommandText += "DELETE FROM team_acceptedContracts WHERE team_acceptedContracts.contractTitle  = @contractTitle;";
										cmd.CommandText += "DELETE FROM team_cancelledContracts WHERE team_cancelledContracts.contractTitle  = @contractTitle;";
										cmd.CommandText += "DELETE FROM team_completedContracts WHERE team_completedContracts.contractTitle  = @contractTitle;";
										cmd.CommandText += "DELETE FROM team_declinedContracts WHERE team_declinedContracts.contractTitle  = @contractTitle;";
										cmd.CommandText += "DELETE FROM team_failedContracts WHERE team_failedContracts.contractTitle  = @contractTitle;";
										cmd.CommandText += "DELETE FROM team_finishedContracts WHERE team_finishedContracts.contractTitle  = @contractTitle;";
										cmd.Parameters.Add(new SQLiteParameter("@teamID", teamID));
										cmd.Parameters.Add(new SQLiteParameter("@contractTitle", contractTitle));
										cmd.ExecuteNonQuery();
										cmd.Parameters.Clear();
									}
									break;
							}
						}
						tr.Commit();
					}
				}
			}
			catch (SQLiteException e) {
				DarkLog.Debug(e.Message);
			}
		}

		/// <summary>
		/// Updates team contracts
		/// </summary>
		/// <param name="teamName"></param>
		/// <param name="contractTitle"></param>
		/// <param name="contractType"></param>
		public static void updateTeamContracts(string teamName, string contractTitle, string contractType) {
			switch(contractType.ToUpper()) {
				case "ACCEPTED":
					try {
						string sql = "INSERT INTO team_acceptedContracts(id,contractTitle) VALUES((SELECT id FROM TEAM WHERE name = @teamName),@contractTitle);";
						sql += "DELETE FROM team_offeredContracts WHERE team_offeredContracts.contractTitle  = @contractTitle;";
						SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
						command.Parameters.Add(new SQLiteParameter("@teamName", teamName));
						command.Parameters.Add(new SQLiteParameter("@contractTitle", contractTitle));
						int ret = command.ExecuteNonQuery();
						DarkLog.Normal("DBManager: added contract to team: " + teamName + " with contractTitle: " + contractTitle + "rows changed: " + ret.ToString());
					}
					catch (SQLiteException e) {
						DarkLog.Debug(e.Message);
					}
					break;
				case "CANCELLED":
					try {
						string sql = "INSERT INTO team_cancelledContracts(id,contractTitle) VALUES((SELECT id FROM TEAM WHERE name = @teamName),@contractTitle);";
						sql += "DELETE FROM team_acceptedContracts WHERE team_acceptedContracts.contractTitle  = @contractTitle;";
						sql += "DELETE FROM team_offeredContracts WHERE team_offeredContracts.contractTitle  = @contractTitle;";
						SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
						command.Parameters.Add(new SQLiteParameter("@teamName", teamName));
						command.Parameters.Add(new SQLiteParameter("@contractTitle", contractTitle));
						int ret = command.ExecuteNonQuery();
						DarkLog.Normal("DBManager: added contract to team: " + teamName + " with contractTitle: " + contractTitle + "rows changed: " + ret.ToString());
					}
					catch (SQLiteException e) {
						DarkLog.Debug(e.Message);
					}
					break;
				case "COMPLETED":
					try {
						string sql = "INSERT INTO team_completedContracts(id,contractTitle) VALUES((SELECT id FROM TEAM WHERE name = @teamName),@contractTitle);";
						sql += "DELETE FROM team_acceptedContracts WHERE team_acceptedContracts.contractTitle  = @contractTitle;";
						sql += "DELETE FROM team_offeredContracts WHERE team_offeredContracts.contractTitle  = @contractTitle;";
						SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
						command.Parameters.Add(new SQLiteParameter("@teamName", teamName));
						command.Parameters.Add(new SQLiteParameter("@contractTitle", contractTitle));
						int ret = command.ExecuteNonQuery();
						DarkLog.Normal("DBManager: added contract to team: " + teamName + " with contractTitle: " + contractTitle + "rows changed: " + ret.ToString());
					}
					catch (SQLiteException e) {
						DarkLog.Debug(e.Message);
					}
					break;
				case "DECLINED":
					try {
						string sql = "INSERT INTO team_declinedContracts(id,contractTitle) VALUES((SELECT id FROM TEAM WHERE name = @teamName),@contractTitle);";
						sql += "DELETE FROM team_acceptedContracts WHERE team_acceptedContracts.contractTitle  = @contractTitle;";
						sql += "DELETE FROM team_offeredContracts WHERE team_offeredContracts.contractTitle  = @contractTitle;";
						SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
						command.Parameters.Add(new SQLiteParameter("@teamName", teamName));
						command.Parameters.Add(new SQLiteParameter("@contractTitle", contractTitle));
						int ret = command.ExecuteNonQuery();
						DarkLog.Normal("DBManager: added contract to team: " + teamName + " with contractTitle: " + contractTitle + "rows changed: " + ret.ToString());
					}
					catch (SQLiteException e) {
						DarkLog.Debug(e.Message);
					}
					break;
				case "FAILED":
					try {
						string sql = "INSERT INTO team_failedContracts(id,contractTitle) VALUES((SELECT id FROM TEAM WHERE name = @teamName),@contractTitle);";
						sql += "DELETE FROM team_acceptedContracts WHERE team_acceptedContracts.contractTitle  = @contractTitle;";
						sql += "DELETE FROM team_offeredContracts WHERE team_offeredContracts.contractTitle  = @contractTitle;";
						SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
						command.Parameters.Add(new SQLiteParameter("@teamName", teamName));
						command.Parameters.Add(new SQLiteParameter("@contractTitle", contractTitle));
						int ret = command.ExecuteNonQuery();
						DarkLog.Normal("DBManager: added contract to team: " + teamName + " with contractTitle: " + contractTitle + "rows changed: " + ret.ToString());
					}
					catch (SQLiteException e) {
						DarkLog.Debug(e.Message);
					}
					break;
				case "FINISHED":
					try {
						string sql = "INSERT INTO team_finishedContracts(id,contractTitle) VALUES((SELECT id FROM TEAM WHERE name = @teamName),@contractTitle);";
						sql += "DELETE FROM team_acceptedContracts WHERE team_acceptedContracts.contractTitle  = @contractTitle;";
						sql += "DELETE FROM team_offeredContracts WHERE team_offeredContracts.contractTitle  = @contractTitle;";
						SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
						command.Parameters.Add(new SQLiteParameter("@teamName", teamName));
						command.Parameters.Add(new SQLiteParameter("@contractTitle", contractTitle));
						int ret = command.ExecuteNonQuery();
						DarkLog.Normal("DBManager: added contract to team: " + teamName + " with contractTitle: " + contractTitle + "rows changed: " + ret.ToString());
					}
					catch (SQLiteException e) {
						DarkLog.Debug(e.Message);
					}
					break;
				case "OFFERED":
					try {
						string sql = "INSERT INTO team_offeredContracts(id,contractTitle) VALUES((SELECT id FROM TEAM WHERE name = @teamName),@contractTitle);";
						sql += "DELETE FROM team_acceptedContracts WHERE team_acceptedContracts.contractTitle  = @contractTitle;";
						sql += "DELETE FROM team_cancelledContracts WHERE team_cancelledContracts.contractTitle  = @contractTitle;";
						sql += "DELETE FROM team_completedContracts WHERE team_completedContracts.contractTitle  = @contractTitle;";
						sql += "DELETE FROM team_declinedContracts WHERE team_declinedContracts.contractTitle  = @contractTitle;";
						sql += "DELETE FROM team_failedContracts WHERE team_failedContracts.contractTitle  = @contractTitle;";
						sql += "DELETE FROM team_finishedContracts WHERE team_finishedContracts.contractTitle  = @contractTitle;";
						SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
						command.Parameters.Add(new SQLiteParameter("@teamName", teamName));
						command.Parameters.Add(new SQLiteParameter("@contractTitle", contractTitle));
						int ret = command.ExecuteNonQuery();
						DarkLog.Normal("DBManager: added contract to team: " + teamName + " with contractTitle: " + contractTitle + "rows changed: " + ret.ToString());
					}
					catch (SQLiteException e) {
						DarkLog.Debug(e.Message);
					}
					break;
				default:
					DarkLog.Normal("ERROR: DBMANAGER RECEIEVED WRON CONTRACT TYPE");
					break;
			}
		}

		public static List<string> getTeamResearch(string teamName)
        {
            try
            {
                int teamID = DBManager.getTeamIdByTeamName(teamName);
                if (teamID < 0)
                {
                    DarkLog.Debug("DBManager.getTeamResearch: could not find teamName: " + teamName);
                    return null;
                }
                string sql = "SELECT techID FROM team_research WHERE id = @id;";
                SQLiteCommand cmd = new SQLiteCommand(sql, m_dbConnection);
                cmd.Parameters.Add(new SQLiteParameter("@id", teamID));
                SQLiteDataReader reader = cmd.ExecuteReader();
                List<string> techIDs = new List<string>();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        techIDs.Add(reader.GetString(0));
                    }
                } else
                {
                    DarkLog.Debug("Team: " + teamName + " does not have any research available!");
                    return null;
                }
                return techIDs;
            } catch(SQLiteException e)
            {
                DarkLog.Debug("getTeamResearch: "+e.Message);
                return null;
            }
        }

        public static List<string> getTeamParts(string teamName)
        {
            try
            {
                int teamID = DBManager.getTeamIdByTeamName(teamName);
                if (teamID < 0)
                {
                    DarkLog.Debug("DBManager.getTeamParts: could not find teamName: " + teamName);
                    return null;
                }
                string sql = "SELECT partName FROM team_parts WHERE id = @id;";
                SQLiteCommand cmd = new SQLiteCommand(sql, m_dbConnection);
                cmd.Parameters.Add(new SQLiteParameter("@id", teamID));
                SQLiteDataReader reader = cmd.ExecuteReader();
                List<string> parts = new List<string>();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        parts.Add(reader.GetString(0));
                    }
                }
                else
                {
                    DarkLog.Debug("Team: " + teamName + " does not have any research available!");
                    return null;
                }
                return parts;
            }
            catch (SQLiteException e)
            {
                DarkLog.Debug("getTeamResearch: " + e.Message);
                return null;
            }
        }

		public static List<List<string>> getTeamContracts(string teamName) {
			try {
				int teamID = DBManager.getTeamIdByTeamName(teamName);
				if (teamID < 0) {
					DarkLog.Debug("DBManager.getTeamParts: could not find teamName: " + teamName);
					return null;
				}
				List<List<string>> contracts = new List<List<string>>();
				contracts.Add(getTeamAcceptedContracts(teamName));
				contracts.Add(getTeamCancelledContracts(teamName));
				contracts.Add(getTeamCompletedContracts(teamName));
				contracts.Add(getTeamDeclinedContracts(teamName));
				contracts.Add(getTeamFailedContracts(teamName));
				contracts.Add(getTeamFinishedContracts(teamName));
				contracts.Add(getTeamOfferedContracts(teamName));

				return contracts;
			}
			catch (SQLiteException e) {
				DarkLog.Debug("getTeamResearch: " + e.Message);
				return null;
			}
		}

		private static List<string> getTeamAcceptedContracts(string teamName) {
			try {
				int teamID = DBManager.getTeamIdByTeamName(teamName);
				if (teamID < 0) {
					DarkLog.Debug("DBManager.getTeamParts: could not find teamName: " + teamName);
					return new List<string>();
				}
				string sql = "SELECT contractTitle FROM team_acceptedContracts WHERE id = @id;";
				SQLiteCommand cmd = new SQLiteCommand(sql, m_dbConnection);
				cmd.Parameters.Add(new SQLiteParameter("@id", teamID));
				SQLiteDataReader reader = cmd.ExecuteReader();
				List<string> acceptedContracts = new List<string>();
				if (reader.HasRows) {
					while (reader.Read()) {
						acceptedContracts.Add(reader.GetString(0));
					}
				}
				else {
					DarkLog.Debug("Team: " + teamName + " does not have any research available!");
					return new List<string>();
				}
				return acceptedContracts;
			}
			catch (SQLiteException e) {
				DarkLog.Debug("getTeamResearch: " + e.Message);
				return new List<string>();
			}
		}

		private static List<string> getTeamCancelledContracts(string teamName) {
			try {
				int teamID = DBManager.getTeamIdByTeamName(teamName);
				if (teamID < 0) {
					DarkLog.Debug("DBManager.getTeamParts: could not find teamName: " + teamName);
					return new List<string>();
				}
				string sql = "SELECT contractTitle FROM team_cancelledContracts WHERE id = @id;";
				SQLiteCommand cmd = new SQLiteCommand(sql, m_dbConnection);
				cmd.Parameters.Add(new SQLiteParameter("@id", teamID));
				SQLiteDataReader reader = cmd.ExecuteReader();
				List<string> cancelledContracts = new List<string>();
				if (reader.HasRows) {
					while (reader.Read()) {
						cancelledContracts.Add(reader.GetString(0));
					}
				}
				else {
					DarkLog.Debug("Team: " + teamName + " does not have any research available!");
					return new List<string>();
				}
				return cancelledContracts;
			}
			catch (SQLiteException e) {
				DarkLog.Debug("getTeamResearch: " + e.Message);
				return new List<string>();
			}
		}

		private static List<string> getTeamCompletedContracts(string teamName) {
			try {
				int teamID = DBManager.getTeamIdByTeamName(teamName);
				if (teamID < 0) {
					DarkLog.Debug("DBManager.getTeamParts: could not find teamName: " + teamName);
					return new List<string>();
				}
				string sql = "SELECT contractTitle FROM team_completedContracts WHERE id = @id;";
				SQLiteCommand cmd = new SQLiteCommand(sql, m_dbConnection);
				cmd.Parameters.Add(new SQLiteParameter("@id", teamID));
				SQLiteDataReader reader = cmd.ExecuteReader();
				List<string> completedContracts = new List<string>();
				if (reader.HasRows) {
					while (reader.Read()) {
						completedContracts.Add(reader.GetString(0));
					}
				}
				else {
					DarkLog.Debug("Team: " + teamName + " does not have any research available!");
					return new List<string>();
				}
				return completedContracts;
			}
			catch (SQLiteException e) {
				DarkLog.Debug("getTeamResearch: " + e.Message);
				return new List<string>();
			}
		}

		private static List<string> getTeamDeclinedContracts(string teamName) {
			try {
				int teamID = DBManager.getTeamIdByTeamName(teamName);
				if (teamID < 0) {
					DarkLog.Debug("DBManager.getTeamParts: could not find teamName: " + teamName);
					return new List<string>();
				}
				string sql = "SELECT contractTitle FROM team_declinedContracts WHERE id = @id;";
				SQLiteCommand cmd = new SQLiteCommand(sql, m_dbConnection);
				cmd.Parameters.Add(new SQLiteParameter("@id", teamID));
				SQLiteDataReader reader = cmd.ExecuteReader();
				List<string> declinedContracts = new List<string>();
				if (reader.HasRows) {
					while (reader.Read()) {
						declinedContracts.Add(reader.GetString(0));
					}
				}
				else {
					DarkLog.Debug("Team: " + teamName + " does not have any research available!");
					return new List<string>();
				}
				return declinedContracts;
			}
			catch (SQLiteException e) {
				DarkLog.Debug("getTeamResearch: " + e.Message);
				return new List<string>();
			}
		}

		private static List<string> getTeamFailedContracts(string teamName) {
			try {
				int teamID = DBManager.getTeamIdByTeamName(teamName);
				if (teamID < 0) {
					DarkLog.Debug("DBManager.getTeamParts: could not find teamName: " + teamName);
					return new List<string>();
				}
				string sql = "SELECT contractTitle FROM team_failedContracts WHERE id = @id;";
				SQLiteCommand cmd = new SQLiteCommand(sql, m_dbConnection);
				cmd.Parameters.Add(new SQLiteParameter("@id", teamID));
				SQLiteDataReader reader = cmd.ExecuteReader();
				List<string> failedContracts = new List<string>();
				if (reader.HasRows) {
					while (reader.Read()) {
						failedContracts.Add(reader.GetString(0));
					}
				}
				else {
					DarkLog.Debug("Team: " + teamName + " does not have any research available!");
					return new List<string>();
				}
				return failedContracts;
			}
			catch (SQLiteException e) {
				DarkLog.Debug("getTeamResearch: " + e.Message);
				return new List<string>();
			}
		}

		private static List<string> getTeamFinishedContracts(string teamName) {
			try {
				int teamID = DBManager.getTeamIdByTeamName(teamName);
				if (teamID < 0) {
					DarkLog.Debug("DBManager.getTeamParts: could not find teamName: " + teamName);
					return new List<string>();
				}
				string sql = "SELECT contractTitle FROM team_finishedContracts WHERE id = @id;";
				SQLiteCommand cmd = new SQLiteCommand(sql, m_dbConnection);
				cmd.Parameters.Add(new SQLiteParameter("@id", teamID));
				SQLiteDataReader reader = cmd.ExecuteReader();
				List<string> finishedContracts = new List<string>();
				if (reader.HasRows) {
					while (reader.Read()) {
						finishedContracts.Add(reader.GetString(0));
					}
				}
				else {
					DarkLog.Debug("Team: " + teamName + " does not have any research available!");
					return new List<string>();
				}
				return finishedContracts;
			}
			catch (SQLiteException e) {
				DarkLog.Debug("getTeamResearch: " + e.Message);
				return new List<string>();
			}
		}

		private static List<string> getTeamOfferedContracts(string teamName) {
			try {
				int teamID = DBManager.getTeamIdByTeamName(teamName);
				if (teamID < 0) {
					DarkLog.Debug("DBManager.getTeamParts: could not find teamName: " + teamName);
					return new List<string>();
				}
				string sql = "SELECT contractTitle FROM team_offeredContracts WHERE id = @id;";
				SQLiteCommand cmd = new SQLiteCommand(sql, m_dbConnection);
				cmd.Parameters.Add(new SQLiteParameter("@id", teamID));
				SQLiteDataReader reader = cmd.ExecuteReader();
				List<string> offeredContracts = new List<string>();
				if (reader.HasRows) {
					while (reader.Read()) {
						offeredContracts.Add(reader.GetString(0));
					}
				}
				else {
					DarkLog.Debug("Team: " + teamName + " does not have any research available!");
					return new List<string>();
				}
				return offeredContracts;
			}
			catch (SQLiteException e) {
				DarkLog.Debug("getTeamResearch: " + e.Message);
				return new List<string>();
			}
		}

		/// <summary>
		/// Internal helper to execute some SQL commands
		/// possibly deprecated
		/// </summary>
		/// <param name="qry"></param>
		private static void executeQry(string qry)
        {
            DarkLog.Debug("Executing qry: " + qry);
            SQLiteCommand command = new SQLiteCommand(qry, m_dbConnection);
            command.ExecuteNonQuery();
            
        }
    }
}
