using MessageStream2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DarkMultiPlayerCommon;
using UnityEngine;

namespace DarkMultiPlayer
{
    public class TeamWorker
    {
        public bool workerEnabled = false;
        private static TeamWorker singleton = new TeamWorker();
        public List<TeamStatus> teams = new List<TeamStatus>();

        public static TeamWorker fetch
        {
            get
            {
                return singleton;
            }
        }

        public static void Reset()
        {
            lock (Client.eventLock)
            {
                if (singleton != null)
                {
                    singleton.workerEnabled = false;
                }
                DarkLog.Debug("Initialized TeamWorker");
                singleton = new TeamWorker();
            }
        }

        private void Update()
        {
            if (!workerEnabled)
                return;
        }

        /// <summary>
        /// Called when the "Create Team" button in TeamWindow.cs was pressed
        /// This sends the server a ClientMessageType.TEAM_LEAVE_REQUEST
        /// </summary>
        public void sendTeamCreateRequest(string teamName, string password)
        {
            if (teamName.Length < 3)
            {
                DarkLog.Debug("sendTeamCreateRequest: teamName too short at least 3 characters please!");
                return;
            }
            using (MessageWriter mw = new MessageWriter())
            {
                DarkLog.Debug("serializing teamcreaterqeuest: ");
                mw.Write<string>(teamName);
                mw.Write<string>(password);
                if(HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                {
                    mw.Write<double>(Funding.Instance.Funds);
                    mw.Write<float>(Reputation.Instance.reputation);
                    mw.Write<float>(ResearchAndDevelopment.Instance.Science);
                } else if(HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX) {
                    mw.Write<float>(ResearchAndDevelopment.Instance.Science);
                }

                // todo later find a way/optional to sync research for now sync only rep/science/funds(maybe not even funds)
                //List<RDNodeStatus> nodesToSync = new List<RDNodeStatus>();
                /*foreach(RDNode node in RDController.Instance.nodes)
                {
                    if ((node.state & RDNode.State.RESEARCHED) != 0)
                    {
                        DarkLog.Debug("sendTeamCreateRequest: techID: " + node.tech.techID + " is researched");
                        nodesToSync.Add(new RDNodeStatus(node.tech.techID, true));
                    } else
                    {
                        DarkLog.Debug("sendTeamCreateRequest: techID: " + node.tech.techID + " is NOT researched");
                        nodesToSync.Add(new RDNodeStatus(node.tech.techID, false));
                    }
                }
                mw.Write<int>(nodesToSync.Count);
                foreach(RDNodeStatus nS in nodesToSync)
                {
                    mw.Write<string>(nS.techID);
                    mw.Write<bool>(nS.researched);
                }*/

                NetworkWorker.fetch.SendTeamCreateRequest(mw.GetMessageBytes());
            }
        }

        /// <summary>
        /// Called when the "Join Team" button in TeamWindow.cs was pressed
        /// This sends the server a ClientMessageType.TEAM_JOIN_REQUEST
        /// </summary>
        public void sendTeamJoinRequest(string teamName, string password)
        {
            if(teamName.Length < 3)
            {
                DarkLog.Debug("sendTeamJoinRequest: teamName too short at least 3 characters please!");
                return;
            }
            using(MessageWriter mw = new MessageWriter())
            {
                mw.Write<string>(teamName);
                mw.Write<string>(password);
                NetworkWorker.fetch.SendTeamJoinRequest(mw.GetMessageBytes());
            }
        }

        /// <summary>
        /// Called when the "Leave Team" button in TeamWindow.cs was pressed
        /// This sends the server a ClientMessageType.TEAM_LEAVE_REQUEST
        /// </summary>
        public void sendTeamLeaveRequest()
        {
            using (MessageWriter mw = new MessageWriter())
            {
                NetworkWorker.fetch.SendTeamLeaveRequest(mw.GetMessageBytes());
            }
        }

        /// <summary>
        /// Called when the client received a ServerMessageType.TEAM_CREATE_RESPONSE
        /// </summary>
        /// <param name="messageData"></param>
        public void HandleTeamCreateResponse(byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                bool success = mr.Read<bool>();
                if (!success)
                {
                    string error = mr.Read<string>();
                    DarkLog.Debug("Could not create team, error: " + error);
                    return;
                }
                // Created Team!
                string teamName = mr.Read<string>();
                DarkLog.Debug("Successfully created team!"+teamName);

                TeamStatus team = new TeamStatus();
                team.teamName = teamName;
                team.funds = mr.Read<double>();
                team.reputation = mr.Read<float>();
                team.science = mr.Read<float>();
                int memberCount = mr.Read<int>();
                DarkLog.Debug("deserializing "+memberCount+" members");
                team.teamMembers = new List<MemberStatus>();
                for (int j = 0; j < memberCount; j++)
                {
                    MemberStatus member = new MemberStatus();
                    member.memberName = mr.Read<string>();
                    member.online = mr.Read<bool>();
                    team.teamMembers.Add(member);
                }
                HandleTeamStatus(teamName, team);


                PlayerStatusWorker.fetch.myPlayerStatus.teamName = teamName;
            }
        }

        /// <summary>
        /// Called when the client receives a ServerMessageType.TEAM_JOIN_RESPONSE
        /// </summary>
        /// <param name="messageData"></param>
        public void HandleTeamJoinResponse(byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                bool success = mr.Read<bool>();
                if (!success)
                {
                    string error = mr.Read<string>();
                    DarkLog.Debug("Could not join team, error: " + error);
                    return;
                }
                DarkLog.Debug("Successfully joined team!");
                // Joined Team!
                string teamName = mr.Read<string>();
                DarkLog.Debug("teamName is: " + teamName);
                PlayerStatusWorker.fetch.myPlayerStatus.teamName = teamName;
                //HandlePlayerJoin(teamName, PlayerStatusWorker.fetch.myPlayerStatus.playerName);

                // Receive funds/reputation/science/research status

                if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                {
                    //DarkLog.Debug("HandleTeamJoinResponse: ");
                    double funds = mr.Read<double>();
                    CareerWorker.fetch.syncFundsWithTeam(funds);
                    float reputation = mr.Read<float>();
                    CareerWorker.fetch.syncReputationWithTeam(reputation);
                    float science = mr.Read<float>();
                    ScienceWorker.fetch.syncScienceWithTeam(science);
                }
                else if (HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX)
                {
                    float science = mr.Read<float>();
                    DarkLog.Debug("trying to sync science");
                    ScienceWorker.fetch.syncScienceWithTeam(science);
                }

                /*List<RDNodeStatus> RDStatus = new List<RDNodeStatus>();
                int numRD = mr.Read<int>();
                for(int i = 0; i<numRD; i++)
                {
                    string techID = mr.Read<string>();
                    bool researched = mr.Read<bool>();
                    RDStatus.Add(new RDNodeStatus(techID, researched));

                    // ResearchWorker set nodes accordingly
                }*/
            }
        }

        /// <summary>
        /// Called when the client receives a ServerMessageType.TEAM_LEAVE_REQUEST
        /// </summary>
        /// <param name="messageData"></param>
        public void HandleTeamLeaveResponse(byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                bool success = mr.Read<bool>();
                if (!success)
                {
                    string error = mr.Read<string>();
                    DarkLog.Debug("Could not leave team sorry, error: " + error);
                    return;
                }
                DarkLog.Debug("Successfully left team "+ PlayerStatusWorker.fetch.myPlayerStatus.teamName);
                PlayerStatusWorker.fetch.myPlayerStatus.teamName = "";
            }
        }

        /// <summary>
        /// Called when the client receives a ServerMessageType.TEAM_STATUS
        /// </summary>
        /// <param name="messageData"></param>
        public void HandleTeamMessage(byte[] messageData)
        {
            DarkLog.Debug("HandleTeamMessage: Handling TEAM_STATUS");
           
            using (MessageReader mr = new MessageReader(messageData))
            {
                TeamMessageType messageType = (TeamMessageType)mr.Read<int>();
                DarkLog.Debug("Received TeamMessageType: " + messageType.ToString());
                switch (messageType)
                {
                    case TeamMessageType.TEAM_JOIN:
                        {
                            string teamName = mr.Read<string>();
                            string playerName = mr.Read<string>();
                            HandlePlayerJoin(teamName, playerName);
                        }
                        break;
                    case TeamMessageType.TEAM_LEAVE:
                        {
                            string teamName = mr.Read<string>();
                            string playerName = mr.Read<string>();
                            HandlePlayerLeave(teamName, playerName);
                        }
                        break;
                    case TeamMessageType.TEAM_STATUS:
                        {
                            DarkLog.Debug("TeamMessageType is TEAM_STATUS");
                            string teamName = mr.Read<string>();
                            TeamStatus team = new TeamStatus();
                            team.funds = mr.Read<double>();
                            team.reputation = mr.Read<float>();
                            team.science = mr.Read<float>();
                            int memberCount = mr.Read<int>();
                            DarkLog.Debug("deserializing members");
                            team.teamMembers = new List<MemberStatus>();
                            for (int j = 0; j < memberCount; j++)
                            {
                                MemberStatus member = new MemberStatus();
                                member.memberName = mr.Read<string>();
                                member.online = mr.Read<bool>();
                                team.teamMembers.Add(member);
                            }
                            HandleTeamStatus(teamName, team);
                        }
                        break;
                    case TeamMessageType.TEAM_LIST:
                        {
                            int teamCount = mr.Read<int>();
                            DarkLog.Debug("Receiving " + teamCount.ToString() + "teams");
                            if (teamCount > 0)
                            {
                                List<TeamStatus> allTeams = new List<TeamStatus>();
                                for (int i = 0; i < teamCount; i++)
                                {
                                    TeamStatus team = new TeamStatus();
                                    team.teamName = mr.Read<string>();
                                    team.funds = mr.Read<double>();
                                    team.reputation = mr.Read<float>();
                                    team.science = mr.Read<float>();

                                    int memberCount = mr.Read<int>();
                                    DarkLog.Debug("TEAM_STATUS: memberCount is " + memberCount.ToString());
                                    team.teamMembers = new List<MemberStatus>();
                                    for (int j = 0; j < memberCount; j++)
                                    {
                                        MemberStatus member = new MemberStatus();
                                        member.memberName = mr.Read<string>();
                                        member.online = mr.Read<bool>();
                                        team.teamMembers.Add(member);
                                    }
                                    allTeams.Add(team);
                                }
                                HandleTeamList(allTeams);
                            }
                        }
                        break;
                }
            }
        }


        /// <summary>
        /// Called when the server notifies us that a player has joined a team
        /// </summary>
        /// <param name="teamName"></param>
        /// <param name="playerName"></param>
        private void HandlePlayerJoin(string teamName, string playerName)
        {
            DarkLog.Debug("got HandlePlayerJoin for playerName: " + playerName+ " and teamName: "+teamName);
            int idx = teams.FindIndex(team => team.teamName == teamName);
            if (idx >= 0)
            {
                DarkLog.Debug("Modifying teams["+idx.ToString()+"]");
                MemberStatus member = new MemberStatus();
                member.memberName = playerName;
                member.online = true;
                teams[idx].teamMembers.Add(member);

                int psIdx = PlayerStatusWorker.fetch.playerStatusList.FindIndex(player => player.playerName == playerName);
                PlayerStatusWorker.fetch.playerStatusList[psIdx].teamName = teamName;
            }
            else
            {
                DarkLog.Debug("Tried to HandlePlayerJoin on team that does not exist: " + teamName);
            }
        }

        /// <summary>
        /// Called when the server notifies us that a player has left a team
        /// </summary>
        /// <param name="teamName"></param>
        /// <param name="playerName"></param>
        private void HandlePlayerLeave(string teamName, string playerName)
        {
            DarkLog.Debug("HandlePlayerLeave-> teamName: " + teamName + " playerName: " + playerName);

            int idx = teams.FindIndex(team => team.teamName == teamName);
            if (idx >= 0)
            {
                DarkLog.Debug("Modifying teams[" + idx.ToString() + "]");
                teams[idx].teamMembers.RemoveAll(member => member.memberName == playerName);
                if(teams[idx].teamMembers.Count == 0)
                {
                    DarkLog.Debug("Last member left the team, deleting team");
                    teams.RemoveAt(idx);
                }

                int psIdx = PlayerStatusWorker.fetch.playerStatusList.FindIndex(player => player.playerName == playerName);
                PlayerStatusWorker.fetch.playerStatusList[psIdx].teamName = "";
            }
            else
            {
                DarkLog.Debug("Player: " + playerName + " left team: " + teamName + " but that team does not exist");
            }
        }

        /// <summary>
        /// Called when the server sends us an updated status for a specific team
        /// </summary>
        /// <param name="teamName"></param>
        /// <param name="team"></param>
        private void HandleTeamStatus(string teamName, TeamStatus team)
        {
            int idx = teams.FindIndex(t => t.teamName == teamName);
            DarkLog.Debug("HandleTeamStatus idx is: " + idx);
            if (idx != -1)
                teams[idx] = team;
            else
                teams.Add(team);

            foreach(MemberStatus member in team.teamMembers)
            {
                int index = PlayerStatusWorker.fetch.playerStatusList.FindIndex(player => player.playerName == member.memberName);
                if (index >= 0)
                {
                    PlayerStatusWorker.fetch.playerStatusList[index].teamName = teamName;
                }

            }

        }

        /// <summary>
        /// Called when the server sends us the initial team state
        /// Usually during initialisation
        /// </summary>
        /// <param name="allTeams"></param>
        private void HandleTeamList(List<TeamStatus> allTeams)
        {
            this.teams = allTeams;
        }
    }
}
