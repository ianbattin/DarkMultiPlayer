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
            if (client.authenticated)
            {
                string name = client.playerName;
                if (client.teamName == "")
                    return;
                int teamid = DBManager.getTeamIdByPlayerName(name);
            }
        }
    }
}
