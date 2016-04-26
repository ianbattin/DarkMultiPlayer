using DarkMultiPlayerCommon;
using MessageStream2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DarkMultiPlayerServer.Messages
{
    public class ScienceSync
    {
        public static void HandleScienceSync(ClientObject client, byte[] messageData)
        {
            DarkLog.Debug("HandleScienceSync: received new Science");
            if (client.teamName != "")
            {
                TeamStatus team = DBManager.getTeamStatus(client.teamName);
                using (MessageReader mr = new MessageReader(messageData))
                {
                    float science = mr.Read<float>();
                    float newScience = (team.science + science);
                    DBManager.updateTeamScience(client.teamName, newScience);

                    ServerMessage message = new ServerMessage();
                    message.type = ServerMessageType.SCIENCE_SYNC;
                    using (MessageWriter mw = new MessageWriter())
                    {
                        mw.Write<string>(client.teamName);
                        mw.Write<float>(science);
                        message.data = mw.GetMessageBytes();
                    }
                    ClientHandler.SendToAll(client, message, true);
                }

            }
        }
    }
}
