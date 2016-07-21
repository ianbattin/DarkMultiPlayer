using DarkMultiPlayerCommon;
using MessageStream2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DarkMultiPlayerServer.Messages
{
    public class ResearchControl
    {
        /// <summary>
        /// Client sends this whenever the GameEvent is triggered
        /// </summary>
        /// <param name="client"></param>
        /// <param name="messageData"></param>
        public static void handleResearchTechUnlocked(ClientObject client, byte[] messageData)
        {
            if (client.teamName == "")
                return;
            using (MessageReader mr = new MessageReader(messageData))
            {
                string techID = mr.Read<string>();
                DBManager.addResearchTech(client.teamName, techID);
                ServerMessage message = new ServerMessage();
                message.type = ServerMessageType.RESEARCH_TECH_UNLOCKED;
                using (MessageWriter mw = new MessageWriter())
                {
					mw.Write<string>(client.teamName);
					mw.Write<string>(techID);
                    message.data = mw.GetMessageBytes();
                }
                ClientHandler.SendToTeam(client, message, true);
            }
        }

        public static void handleResearchPartPurchased(ClientObject client, byte[] messageData)
        {
            if (client.teamName == "")
                return;
            using (MessageReader mr = new MessageReader(messageData))
            {
                string partName = mr.Read<string>();
                DBManager.addPurchasedPart(client.teamName, partName);
                ServerMessage message = new ServerMessage();
                message.type = ServerMessageType.RESEARCH_PART_PURCHASED;
                using (MessageWriter mw = new MessageWriter())
                {
					mw.Write<string>(client.teamName);
					mw.Write<string>(partName);
                    message.data = mw.GetMessageBytes();
                }
                ClientHandler.SendToTeam(client, message, true);
            }
        }

        /// <summary>
        /// Client sends this after TeamCreateResponse has been received(client side) with success=true
        /// </summary>
        /// <param name="client"></param>
        /// <param name="messageData"></param>
        public static void handleResearchTechState(ClientObject client, byte[] messageData)
        {
            if (client.teamName == "")
                return;
            using (MessageReader mr = new MessageReader(messageData))
            {
                List<string> techIDs = mr.Read<string[]>().ToList();
                List<string> parts = mr.Read<string[]>().ToList();
                DBManager.setInitialTechState(client.teamName, techIDs, parts);
            }
        }
    }
}
