using System;
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
        private static DBManager instance;
        private static string dbFileName;
        private static SQLiteConnection m_dbConnection;

        public static DBManager fetch
        {
            get
            {
                if(instance == null)
                {
                    instance = new DBManager();
                }
                return instance;
            }
        }

        public static void Reset()
        {
            dbFileName = Path.Combine(Server.universeDirectory,"dmp.sqlite");
            DarkLog.Debug("DBManager.Reset()");
        }

        /// <summary>
        /// Initializes the database connection
        /// Calls setupDatabase if the database does not exist
        /// Does NOT check the integrity of the tables
        /// </summary>
        public static void Load()
        {
            if (!File.Exists(dbFileName))
            {
                setupDatabase();
            } else
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
                sql += "CREATE TABLE team_research(id integer, rdtechname text, FOREIGN KEY(id) REFERENCES team(id) ON DELETE CASCADE); ";
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
        public static TeamStatus createNewTeam(string name, string password, double funds, float reputation, float science, string creator, string creator_pubKey)
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
                team.teamName = name;
                team.funds = funds;
                team.reputation = reputation;
                team.science = science;
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
            int teamid = reader.GetInt32(0);
            TeamStatus team = new TeamStatus();
            team.teamName = reader.GetString(1);
            team.funds = reader.GetDouble(2);
            team.reputation = reader.GetFloat(3);
            team.science = reader.GetFloat(4);

            string sql = "SELECT name FROM team_members where id = @id";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.Parameters.Add(new SQLiteParameter("@id", teamid));
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
