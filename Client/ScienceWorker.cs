using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DarkMultiPlayerCommon;
using UnityEngine;
using MessageStream2;

namespace DarkMultiPlayer
{
    public class ScienceWorker
    {
        public bool workerEnabled = false;
        private static ScienceWorker singleton;

        public static ScienceWorker fetch
        {
            get
            {
                return singleton;
            }
        }

        public static void Reset()
        {
            lock(Client.eventLock) {
                if (singleton != null)
                {
                    singleton.workerEnabled = false;
                    GameEvents.OnScienceChanged.Remove(singleton.onScienceChanged);
                    //GameEvents.OnScienceRecieved.Remove(singleton.onScienceReceived);
                }
                singleton = new ScienceWorker();
                DarkLog.Debug("ScienceWorker: loaded");
                GameEvents.OnScienceChanged.Add(singleton.onScienceChanged);
                // yes I can actually spell received
                //GameEvents.OnScienceRecieved.Add(singleton.onScienceReceived);
            }
        }


        public void onScienceReceived(float science, ScienceSubject subject, ProtoVessel vessel, bool something)
        {
            Debug.Log("onScienceReceived: received " + science.ToString() + " science with subject: " + subject.title);
			if (ContractWorker.fetch.syncingContracts || ResearchWorker.fetch.syncingResearch)
				return;
			if (PlayerStatusWorker.fetch.myPlayerStatus.teamName == "")
                return;
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<float>(science);
                NetworkWorker.fetch.SendScienceSyncMessage(mw.GetMessageBytes());
            }
        }

        /// <summary>
        /// When the player receives or spends science this function is called. Sends the new total science value to all players in the team
        /// </summary>
        /// <param name="science">New total science</param>
        /// <param name="reasons">The reason why the science was changed</param>
        public void onScienceChanged(float science, TransactionReasons reasons)
        {
			if(ContractWorker.fetch.syncingContracts)
				return;
            if (reasons == TransactionReasons.None)
                return;
            if (PlayerStatusWorker.fetch.myPlayerStatus.teamName == "")
                return;
            Debug.Log("science has changed to value: " + science.ToString() + " with reason: " + reasons.ToString());
            //ScreenMessages.PostScreenMessage("science has changed to value: " + science.ToString() + " with reason: " + reasons.ToString(),1f,ScreenMessageStyle.UPPER_CENTER);

            // send new science value via network
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<float>(science);
                NetworkWorker.fetch.SendScienceSyncMessage(mw.GetMessageBytes());
            }
        }

        public void handleScienceSyncMessage(byte[] messageData)
        {
            using(MessageReader mr = new MessageReader(messageData))
            {
                string teamName = mr.Read<string>();
                float science = mr.Read<float>();
                if(teamName == PlayerStatusWorker.fetch.myPlayerStatus.teamName)
                    syncScienceWithTeam(science);
				TeamWorker.fetch.teams.Find(team => team.teamName == teamName).science = science;
			}
        }
        /// <summary>
        /// Sets the players science value to the value of the parameter
        /// </summary>
        /// <param name="science">Desired total science</param>
        public void syncScienceWithTeam(float science)
        {
			try {
				DarkLog.Debug("syncing science with team to target science: " + science.ToString());
				float diff = science - ResearchAndDevelopment.Instance.Science;
				ResearchAndDevelopment.Instance.AddScience(diff, TransactionReasons.None);
			} catch(Exception e) {
				if (e.InnerException is NullReferenceException) {
					DarkLog.Debug("Science sync failed");
					//var scheduler = new Scheduler();
					//scheduler.Execute(() => syncScienceWithTeam(science), 5000);
				}
			}
        }
    }
}
