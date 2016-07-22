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
        public static List<TeamStatus> teams = new List<TeamStatus>();

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
				List<string> research = new List<string>();
				List<string> purchased = new List<string>();
                switch (Settings.settingsStore.gameMode)
                {
                    case GameMode.CAREER:
                        {
                            funds = mr.Read<double>();
                            reputation = mr.Read<float>();
                            science = mr.Read<float>();
							research = new List<string>(mr.Read<string[]>());
							purchased = new List<string>(mr.Read<string[]>());
                        }
                        break;
                    case GameMode.SCIENCE:
                        {
                            science = mr.Read<float>();
                        }
                        break;
                }

                TeamStatus team = DBManager.createNewTeam(teamName, password, funds, reputation, science, research, purchased, client.playerName, client.publicKey);
                teams.Add(team);

                // now send new info and responses!
                ServerMessage message = new ServerMessage();
                message.type = ServerMessageType.TEAM_CREATE_RESPONSE;

                using (MessageWriter mw = new MessageWriter())
                {
                    if (team != null)
                    {
                        DarkLog.Debug("Successfully created team: " + teamName);
                        mw.Write<bool>(true);
                        mw.Write<string>(teamName);
                        client.teamName = teamName;
                        mw.Write<double>(team.funds);
                        mw.Write<float>(team.reputation);
                        mw.Write<float>(team.science);
						mw.Write<string[]>(team.research.ToArray());
						mw.Write<string[]>(team.purchased.ToArray());
                        mw.Write<int>(team.teamMembers.Count);
                        DarkLog.Debug("handleTeamCreateRequest: sending " + team.teamMembers.Count + " teamMembers");
                        foreach(MemberStatus member in team.teamMembers)
                        {
                            mw.Write<string>(member.memberName);
                            // we can hack here, since we know that the creator is online
                            mw.Write<bool>(true);
                        }
                    }
                    else
                    {
                        DarkLog.Debug("Team creation failed for teamName: " + team.teamName);
                        mw.Write<bool>(false);
                        mw.Write<string>("Could not create team");
                    }
                    message.data = mw.GetMessageBytes();
                }
                ClientHandler.SendToClient(client, message, true);
                sendTeamStatus(client, team);
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
                    client.teamName = teamName;
                    // success
                    ServerMessage tJoinResp = new ServerMessage();
                    tJoinResp.type = ServerMessageType.TEAM_JOIN_RESPONSE;
                    using (MessageWriter mw = new MessageWriter())
                    {
                        TeamStatus team = DBManager.getTeamStatus(client.teamName);
                        if (team != null)
                        {
                            mw.Write<bool>(true);
                            mw.Write<string>(client.teamName);
                            switch (Settings.settingsStore.gameMode)
                            {
                                case GameMode.CAREER:
                                    {
                                        mw.Write<double>(team.funds);
                                        mw.Write<float>(team.reputation);
                                        mw.Write<float>(team.science);
										mw.Write<string[]>(team.research.ToArray());
										mw.Write<string[]>(team.purchased.ToArray());
                                    }
                                    break;
                                case GameMode.SCIENCE:
                                    {
                                        mw.Write<float>(team.science);
                                    }
                                    break;
                            }
                        } else
                        {
                            DarkLog.Debug("Got TeamJoinRequest from " + client.playerName + " for team: " + client.teamName + " but team does not exist");
                        }
                        tJoinResp.data = mw.GetMessageBytes();
                    }
                    ClientHandler.SendToClient(client, tJoinResp, true);
                    sendTeamStatusJoin(client);

                    ServerMessage tState = new ServerMessage();
                    tState.type = ServerMessageType.RESEARCH_TECH_STATE;
                    using (MessageWriter mw = new MessageWriter())
                    {
                        List<string> techIDs = DBManager.getTeamResearch(client.teamName);
                        List<string> parts = DBManager.getTeamParts(client.teamName);
                        DarkLog.Debug("HandleTeamJoinRequest: sending RESEARCH_TECH_STATE to: " + client.playerName + " with techIDs.Count: " + techIDs.Count + " and partNames: " + parts.Count);
                        mw.Write<string[]>(techIDs.ToArray());
                        mw.Write<string[]>(parts.ToArray());
                        tState.data = mw.GetMessageBytes();
                    }
                    ClientHandler.SendToTeam(client, tState, true);
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
            DarkLog.Debug("Received TeamLeaveRequest from client: " + client.playerName + " team is: "+client.teamName);
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
                sendTeamStatusLeave(client);
            }
        }

        public static void sendTeamStatusJoin(ClientObject client)
        {
            ServerMessage tStatus = new ServerMessage();
            tStatus.type = ServerMessageType.TEAM_STATUS;
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<int>((int)TeamMessageType.TEAM_JOIN);
                mw.Write<string>(client.teamName);
                mw.Write<string>(client.playerName);
                tStatus.data = mw.GetMessageBytes();
            }
            ClientHandler.SendToAll(client, tStatus, true);
        }

        public static void sendTeamStatusLeave(ClientObject client)
        {
            ServerMessage tStatus = new ServerMessage();
            tStatus.type = ServerMessageType.TEAM_STATUS;
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<int>((int)TeamMessageType.TEAM_LEAVE);
                mw.Write<string>(client.teamName);
                mw.Write<string>(client.playerName);
                tStatus.data = mw.GetMessageBytes();
            }
            ClientHandler.SendToAll(null, tStatus, true);
        }

        public static void sendTeamStatus(ClientObject client, TeamStatus team)
        {
            ServerMessage tStatus = new ServerMessage();
            tStatus.type = ServerMessageType.TEAM_STATUS;
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<int>((int)TeamMessageType.TEAM_STATUS);
                mw.Write<string>(team.teamName);
                mw.Write<double>(team.funds);
                mw.Write<float>(team.reputation);
                mw.Write<float>(team.science);
				mw.Write<string[]>(team.research.ToArray());
				mw.Write<string[]>(team.purchased.ToArray());

				// serialize member list
				mw.Write<int>(team.teamMembers.Count);
                foreach(MemberStatus member in team.teamMembers)
                {
                    mw.Write<string>(member.memberName);
                    mw.Write<bool>(member.online);
                }

                tStatus.data = mw.GetMessageBytes();
            }
            ClientHandler.SendToAll(client, tStatus, true);
        }

        public static void sendTeamList(ClientObject client)
        {
            List<TeamStatus> teamList = DBManager.getTeamStatusList();
            DarkLog.Debug("Sending " + teamList.Count + " teams");
            ServerMessage message = new ServerMessage();
            message.type = ServerMessageType.TEAM_STATUS;

            using(MessageWriter mw = new MessageWriter())
            {
                mw.Write<int>((int)TeamMessageType.TEAM_LIST);
                mw.Write<int>(teamList.Count);
                foreach(TeamStatus team in teamList)
                {
                    mw.Write<string>(team.teamName);
                    mw.Write<double>(team.funds);
                    mw.Write<float>(team.reputation);
                    mw.Write<float>(team.science);
					mw.Write<string[]>(team.research.ToArray());
					mw.Write<string[]>(team.purchased.ToArray());

					mw.Write<int>(team.teamMembers.Count);
                    foreach(MemberStatus member in team.teamMembers)
                    {
                        mw.Write<string>(member.memberName);
                        mw.Write<bool>(member.online);
                    }
                }
                message.data = mw.GetMessageBytes();
            }
            ClientHandler.SendToClient(client, message, true);
        }

		public static void updateTeams(List<TeamStatus> teamsList) {
			foreach (TeamStatus team in teamsList)
				teams.Add(team); 
		}
    }
}
