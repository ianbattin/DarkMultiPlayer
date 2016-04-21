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

        public void sendTeamCreateRequest(string teamName, string password)
        {
            if (teamName.Length < 3)
            {
                DarkLog.Debug("sendTeamCreateRequest: teamName too short at least 3 characters please!");
                return;
            }
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<string>(teamName);
                mw.Write<string>(password);
                mw.Write<double>(Funding.Instance.Funds);
                mw.Write<float>(Reputation.Instance.reputation);
                mw.Write<float>(ResearchAndDevelopment.Instance.Science);

                List<RDNodeStatus> nodesToSync = new List<RDNodeStatus>();
                ResearchAndDevelopment.Instance.GetTechState("");
                foreach(RDNode node in RDController.Instance.nodes)
                {
                    if ((node.state & RDNode.State.RESEARCHED) != 0)
                    {
                        nodesToSync.Add(new RDNodeStatus(node.tech.techID, true));
                    } else
                    {
                        nodesToSync.Add(new RDNodeStatus(node.tech.techID, false));
                    }
                }
                // Todo serialize nodes! probably just techID

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

        public void sendTeamLeaveRequest(string teamName)
        {
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<string>(teamName);
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

        private void HandleTeamCreateResponse(byte[] messageData)
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
            }
        }

        private void HandleTeamJoinResponse(byte[] messageData)
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
            }
        }

        private void HandleTeamLeaveResponse(byte[] messageData)
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
        private void HandleTeamMessage(byte[] messageData)
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
                            int memberCount = mr.Read<int>();
                            if (memberCount < 1)
                                return;
                            List<string> memberNames = new List<string>();
                            for(int i = 0; i < memberCount; i++)
                            {
                                memberNames.Add(mr.Read<string>());
                            }
                            HandleTeamStatus(teamName, memberNames);
                        }
                        break;
                }
            }
        }

        private void HandlePlayerJoin(string teamName, string playerName)
        {

        }

        private void HandlePlayerLeave(string teamName, string playerName)
        {

        }

        private void HandleTeamStatus(string teamName, List<string> memberNames)
        {

        }
    }
}
