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
        private static TeamWorker singleton;
        private Queue<byte[]> newTeamMessages = new Queue<byte[]>();
        public List<TeamStatus> teams;

        public static TeamWorker fetch
        {
            get
            {
                return singleton;
            }
        }

        public static void Reset()
        {
            if(singleton != null)
            {
                singleton.workerEnabled = false;
            }

            singleton = new TeamWorker();
        }

        private void Update()
        {
            if (!workerEnabled)
                return;

            ProcessTeamMessages();
        }

        public TeamStatus getTeamStatusByTeamName(string teamName)
        {
            foreach(TeamStatus team in teams)
            {
                if (team.teamName.Equals(teamName))
                    return team;
            }
            return null;
        }

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

        public void sendTeamLeaveRequest()
        {
            using (MessageWriter mw = new MessageWriter())
            {
                NetworkWorker.fetch.SendTeamLeaveRequest(mw.GetMessageBytes());
            }
        }

        public void ProcessTeamMessages()
        {
            while (newTeamMessages.Count > 0)
            {
                HandleTeamMessage(newTeamMessages.Dequeue());
            }
        }

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
                DarkLog.Debug("Successfully created team!");
                // Created Team!
                string teamName = mr.Read<string>();
                PlayerStatusWorker.fetch.myPlayerStatus.teamName = teamName;
            }
        }

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

                // Receive funds/reputation/science/research status

                if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                {
                    double funds = mr.Read<double>();
                    ResearchWorker.fetch.syncFundsWithTeam(funds);
                    float reputation = mr.Read<float>();
                    ResearchWorker.fetch.syncReputationWithTeam(reputation);
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

        public void HandleTeamLeaveResponse(byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                bool success = mr.Read<bool>();
                if (!success)
                {
                    string error = mr.Read<string>();
                    DarkLog.Debug("Could not leave team, error: " + error);
                    return;
                }
                DarkLog.Debug("Successfully left team!");
                PlayerStatusWorker.fetch.myPlayerStatus.teamName = "";
            }
        }
        public void HandleTeamMessage(byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                TeamMessageType messageType = (TeamMessageType)mr.Read<int>();
                string teamName = mr.Read<string>();
                switch (messageType)
                {
                    case TeamMessageType.TEAM_JOIN:
                        {
                            string playerName = mr.Read<string>();
                            HandlePlayerJoin(teamName, playerName);
                        }
                        break;
                    case TeamMessageType.TEAM_LEAVE:
                        {
                            string playerName = mr.Read<string>();
                            HandlePlayerLeave(teamName, playerName);
                        }
                        break;
                    case TeamMessageType.TEAM_STATUS:
                        {
                            TeamStatus team = new TeamStatus();
                            team.funds = mr.Read<double>();
                            team.reputation = mr.Read<float>();
                            team.science = mr.Read<float>();
                            int memberCount = mr.Read<int>();
                            List<MemberStatus> members = new List<MemberStatus>();
                            for (int j = 0; j < memberCount; j++)
                            {
                                string memberName = mr.Read<string>();
                                bool online = mr.Read<bool>();
                                members.Add(new MemberStatus(memberName, online));
                            }
                            team.teamMembers = members;
                            HandleTeamStatus(teamName, team);
                        }
                        break;
                    case TeamMessageType.TEAM_LIST:
                        {
                            int teamCount = mr.Read<int>();
                            List<TeamStatus> allTeams = new List<TeamStatus>();
                            for (int i = 0; i < teamCount; i++)
                            {
                                TeamStatus team = new TeamStatus();
                                team.funds = mr.Read<double>();
                                team.reputation = mr.Read<float>();
                                team.science = mr.Read<float>();

                                int memberCount = mr.Read<int>();
                                List<MemberStatus> members = new List<MemberStatus>();
                                for (int j = 0; j < memberCount; j++)
                                {
                                    string memberName = mr.Read<string>();
                                    bool online = mr.Read<bool>();
                                    members.Add(new MemberStatus(memberName, online));
                                }
                                team.teamMembers = members;
                                allTeams.Add(team);
                            }

                            HandleTeamList(allTeams);
                        }
                        break;
                }
            }
        }



        private void HandlePlayerJoin(string teamName, string playerName)
        {
            TeamStatus team = getTeamByTeamName(teamName);
            team.teamMembers.Add(new MemberStatus(playerName));
            // do something else? who caaares right now, check gui updates etc
        }

        private void HandlePlayerLeave(string teamName, string playerName)
        {
            TeamStatus team = getTeamByTeamName(teamName);
            team.removeMember(playerName);
            // ?!?
        }

        private void HandleTeamStatus(string teamName, TeamStatus team)
        {
            // placerholder until we sync all details of the team!
            int idx = teams.FindIndex(t => t.teamName == teamName);
            teams[idx] = team;
        }

        private void HandleTeamList(List<TeamStatus> allTeams)
        {
            this.teams = allTeams;
        }

        // TeamStatus helper functions, could be done with lambda functions ;)

        private TeamStatus getTeamByTeamName(string teamName)
        {
            return teams.Find(team => team.teamName == teamName);
        }

        private TeamStatus getTeamByMemberName(string playerName)
        {
            foreach(TeamStatus team in teams)
            {
                if (team.teamMembers.Any(member => member.memberName == playerName))
                    return team;
            }
            return null;
        }
    }
}
