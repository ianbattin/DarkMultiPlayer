using System;
using System.IO;
using MessageStream2;
using DarkMultiPlayerCommon;
using System.Text.RegularExpressions;

namespace DarkMultiPlayerServer.Messages
{
    public class ScenarioData
    {
        public static void SendScenarioModules(ClientObject client)
        {
            int numberOfScenarioModules = Directory.GetFiles(Path.Combine(Server.universeDirectory, "Scenarios", client.playerName)).Length;
            int currentScenarioModule = 0;
            string[] scenarioNames = new string[numberOfScenarioModules];
            byte[][] scenarioDataArray = new byte[numberOfScenarioModules][];
            foreach (string file in Directory.GetFiles(Path.Combine(Server.universeDirectory, "Scenarios", client.playerName)))
            {
                //Remove the .txt part for the name
                scenarioNames[currentScenarioModule] = Path.GetFileNameWithoutExtension(file);
                scenarioDataArray[currentScenarioModule] = File.ReadAllBytes(file);
                if(client.teamName != "")
                {
                    // alter data!
                    TeamStatus team = DBManager.getTeamStatusWithoutMembers(client.teamName);
                    if (team != null)
                    {
                        if (scenarioNames[currentScenarioModule].Equals("ResearchAndDevelopment"))
                        {
                            // alter the save file on the server for the new team data!
                            string rdData = System.Text.Encoding.UTF8.GetString(scenarioDataArray[currentScenarioModule]);
                            string pattern = @"(sci = )([0-9]*\.*[0-9]*)";
                            Regex rgx = new Regex(pattern);
                            rdData = rgx.Replace(rdData, "$1 " + team.science, 1);
                            scenarioDataArray[currentScenarioModule] = System.Text.Encoding.UTF8.GetBytes(rdData);
                        }
                        if (scenarioNames[currentScenarioModule].Equals("Funding"))
                        {
                            // alter the save file on the server for the new team data!
                            string rdData = System.Text.Encoding.UTF8.GetString(scenarioDataArray[currentScenarioModule]);
                            string pattern = @"(funds = )([0-9]*\.*[0-9]*)";
                            Regex rgx = new Regex(pattern);
                            rdData = rgx.Replace(rdData, "$1 " + team.funds, 1);
                            scenarioDataArray[currentScenarioModule] = System.Text.Encoding.UTF8.GetBytes(rdData);
                        }
                        if (scenarioNames[currentScenarioModule].Equals("Reputation"))
                        {
                            // alter the save file on the server for the new team data!
                            string rdData = System.Text.Encoding.UTF8.GetString(scenarioDataArray[currentScenarioModule]);
                            string pattern = @"(rep = )([0-9]*\.*[0-9]*)";
                            Regex rgx = new Regex(pattern);
                            rdData = rgx.Replace(rdData, "$1 " + team.reputation, 1);
                            scenarioDataArray[currentScenarioModule] = System.Text.Encoding.UTF8.GetBytes(rdData);
                        }
                    }
                }

                currentScenarioModule++;
            }
            ServerMessage newMessage = new ServerMessage();
            newMessage.type = ServerMessageType.SCENARIO_DATA;
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<string[]>(scenarioNames);
                foreach (byte[] scenarioData in scenarioDataArray)
                {
                    if (client.compressionEnabled)
                    {
                        mw.Write<byte[]>(Compression.CompressIfNeeded(scenarioData));
                    }
                    else
                    {
                        mw.Write<byte[]>(Compression.AddCompressionHeader(scenarioData, false));
                    }
                }
                newMessage.data = mw.GetMessageBytes();
            }
            ClientHandler.SendToClient(client, newMessage, true);
        }

        public static void HandleScenarioModuleData(ClientObject client, byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                //Don't care about subspace / send time.
                string[] scenarioName = mr.Read<string[]>();
                DarkLog.Debug("Saving " + scenarioName.Length + " scenario modules from " + client.playerName);

                for (int i = 0; i < scenarioName.Length; i++)
                {
                    byte[] scenarioData = Compression.DecompressIfNeeded(mr.Read<byte[]>());
                    File.WriteAllBytes(Path.Combine(Server.universeDirectory, "Scenarios", client.playerName, scenarioName[i] + ".txt"), scenarioData);
                }
            }
        }
    }
}

