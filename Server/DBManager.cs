using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using System.IO;

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

        private static void setupDatabase()
        {
            DarkLog.Debug("DBManager.setupDatabase()");
            m_dbConnection = new SQLiteConnection("Data Source=Universe/dmp.sqlite;Version=3;");
            m_dbConnection.Open();

            string sql = "BEGIN;";
            sql += "CREATE TABLE team (id integer PRIMARY KEY AUTOINCREMENT, name text, password text, funds real, reputation real, science real);";
            sql += "CREATE TABLE team_members(id integer, name text, pubkey text, FOREIGN KEY(id) REFERENCES team(id) ON DELETE CASCADE);";
            sql += "CREATE TABLE team_research(id integer, rdtechname text, FOREIGN KEY(id) REFERENCES team(id) ON DELETE CASCADE); ";
            sql += "COMMIT;";
            executeQry(sql);
            createNewTeam("testteam", "test", 400000d, 3f, 1337f, "bawki", "bawkKey");
        }

        public static int createNewTeam(string name, string password, double funds, float reputation, float science, string creator, string creator_pubKey)
        {
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

            return 0;
        }

        /// <summary>
        /// Returns the teamid of the supplied playername
        /// </summary>
        /// <param name="name">ClientObject.playerName</param>
        /// <returns></returns>
        public static int getTeamIdByPlayerName(string name)
        {
            string sql = "SELECT id FROM team AS C JOIN team_members AS R ON C.id=R.id WHERE R.name = @name";
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

        public static void updateTeamScience(int teamid, float science)
        {
            string sql = "UPDATE team SET science = @science WHERE id = @teamid";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.Parameters.Add(new SQLiteParameter("@science", science));
            command.Parameters.Add(new SQLiteParameter("@teamid", teamid));
            int ret = command.ExecuteNonQuery();
            DarkLog.Debug("DBManager: updated team science of teamid: " + teamid.ToString() + " to science: " + science.ToString() + "rows changed: "+ret.ToString());
        }

        private static void executeQry(string qry)
        {
            DarkLog.Debug("Executing qry: " + qry);
            SQLiteCommand command = new SQLiteCommand(qry, m_dbConnection);
            command.ExecuteNonQuery();
            
        }
    }
}
