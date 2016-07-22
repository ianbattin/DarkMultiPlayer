using DarkMultiPlayerCommon;
using MessageStream2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DarkMultiPlayerServer.Messages
{
    public class CareerControl
    {
        public static void handleFundsSync(ClientObject client, byte[] messageData)
        {
            if (client.teamName == "")
                return;
            using (MessageReader mr = new MessageReader(messageData))
            {
                double funds = mr.Read<double>();
                DBManager.updateTeamFunds(client.teamName, funds);
                ServerMessage message = new ServerMessage();
                message.type = ServerMessageType.FUNDS_SYNC;
                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<string>(client.teamName);
                    mw.Write<double>(funds);
                    message.data = mw.GetMessageBytes();
                }
                ClientHandler.SendToAll(client, message, true);
            }
        }

        public static void handleReputationSync(ClientObject client, byte[] messageData)
        {
            if (client.teamName == "")
                return;
            using (MessageReader mr = new MessageReader(messageData))
            {
                float reputation = mr.Read<float>();
				DarkLog.Normal("Updating team " + client.teamName + " rep to: " + reputation);

				DBManager.updateTeamReputation(client.teamName, reputation);
                ServerMessage message = new ServerMessage();
                message.type = ServerMessageType.REPUTATION_SYNC;
                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<string>(client.teamName);
                    mw.Write<float>(reputation);
                    message.data = mw.GetMessageBytes();
                }
                ClientHandler.SendToAll(client, message, true);
            }
        }
    }
}
