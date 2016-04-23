using MessageStream2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DarkMultiPlayerCommon;

namespace DarkMultiPlayerServer.Messages
{
    public class TeamControl
    {

        public static void SendOwnTeamData(ClientObject client)
        {
            int teamid = DBManager.getTeamIdByPlayerName(client.playerName);
            if (teamid >= 0) { 
                // player is in a team this is to initialize after connection

                // first we set the teamName for future reference
                ServerMessage message = new ServerMessage();
                message.type = ServerMessageType.TEAM_JOIN_RESPONSE;
                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<bool>(true);
                    mw.Write<string>(client.teamName);

                    TeamStatus team = DBManager.getTeamStatusWithoutMembers(client.teamName);
                    if (team == null)
                        return;
                    switch (Settings.settingsStore.gameMode)
                    {
                        case GameMode.CAREER:
                            {
                                mw.Write<double>(team.funds);
                                mw.Write<float>(team.reputation);
                                mw.Write<float>(team.science);
                            }
                            break;
                        case GameMode.SCIENCE:
                            {
                                mw.Write<float>(team.science);
                            }
                            break;
                    }
                    message.data = mw.GetMessageBytes();
                }
                ClientHandler.SendToClient(client, message, true);
            }
        }

        public static void handleTeamCreateRequest(ClientObject client, byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                string teamName = mr.Read<string>();
                if(teamName.Length < 3)
                {
                    // send create response with error
                    DarkLog.Debug("Could not create team: less than three characters");
                    return;
                }
                string password = mr.Read<string>();
                double funds = 0d;
                float reputation = 0f;
                float science = 0f;
                switch (Settings.settingsStore.gameMode)
                {
                    case GameMode.CAREER:
                        {
                            funds = mr.Read<double>();
                            reputation = mr.Read<float>();
                            science = mr.Read<float>();
                        }
                        break;
                    case GameMode.SCIENCE:
                        {
                            science = mr.Read<float>();
                        }
                        break;
                }

                int teamid = DBManager.createNewTeam(teamName, password, funds, reputation, science, client.playerName, client.publicKey);
                if (teamid >= 0)
                    DarkLog.Debug("Successfully created team: " + teamName);
                else
                    DarkLog.Debug("Team creation failed with errorcode: " + teamid);

                // now send new info  and responses!

            }
        }

        public static void handleTeamJoinRequest(ClientObject client, byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                string teamName = mr.Read<string>();
                string password = mr.Read<string>();
                if (DBManager.addPlayerToTeam(teamName, password, client.playerName, client.publicKey))
                {
                    // success
                    ServerMessage message = new ServerMessage();
                    message.type = ServerMessageType.TEAM_JOIN_RESPONSE;
                    using (MessageWriter mw = new MessageWriter())
                    {
                        mw.Write<bool>(true);
                        mw.Write<string>(client.teamName);

                        TeamStatus team = DBManager.getTeamStatusWithoutMembers(client.teamName);
                        switch (Settings.settingsStore.gameMode)
                        {
                            case GameMode.CAREER:
                                {
                                    mw.Write<double>(team.funds);
                                    mw.Write<float>(team.reputation);
                                    mw.Write<float>(team.science);
                                }
                                break;
                            case GameMode.SCIENCE:
                                {
                                    mw.Write<float>(team.science);
                                }
                                break;
                        }
                        message.data = mw.GetMessageBytes();
                    }
                    ClientHandler.SendToClient(client, message, true);
                }
                else
                {
                    ServerMessage message = new ServerMessage();
                    message.type = ServerMessageType.TEAM_JOIN_RESPONSE;
                    using (MessageWriter mw = new MessageWriter())
                    {
                        mw.Write<bool>(false);
                        mw.Write<string>("Could not add you to the database, needs further investigation :P");
                        message.data = mw.GetMessageBytes();
                    }
                    ClientHandler.SendToClient(client, message, true);
                }
            }
        }

        public static void handleTeamLeaveRequest(ClientObject client, byte[] messageData)
        {
            if (client.teamName != "")
            {
                DBManager.removePlayerFromTeam(client.teamName, client.playerName);
                ServerMessage message = new ServerMessage();
                message.type = ServerMessageType.TEAM_LEAVE_RESPONSE;
                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<bool>(true);
                    message.data = mw.GetMessageBytes();
                }
                ClientHandler.SendToClient(client, message, true);
            }
        }
    }
}
