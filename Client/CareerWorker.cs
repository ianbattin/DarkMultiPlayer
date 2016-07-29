using DarkMultiPlayerCommon;
using MessageStream2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Contracts;

namespace DarkMultiPlayer
{
	public class CareerWorker {
		public bool workerEnabled = false;
		private static CareerWorker singleton;

		public static CareerWorker fetch {
			get {
				return singleton;
			}
		}

		public static void Reset() {
			if (singleton != null) {
				singleton.workerEnabled = false;
				GameEvents.OnFundsChanged.Remove(singleton.onFundsChanged);
				GameEvents.OnReputationChanged.Remove(singleton.onReputationChanged);
			}
			singleton = new CareerWorker();
			DarkLog.Debug("CareerWorker: Reset");
			GameEvents.OnFundsChanged.Add(singleton.onFundsChanged);
			GameEvents.OnReputationChanged.Add(singleton.onReputationChanged);
		}

		public void onFundsChanged(double funds, TransactionReasons reasons)
        {
			if(ContractWorker.fetch.syncingContracts || ResearchWorker.fetch.syncingResearch)
				return;
            if (reasons == TransactionReasons.None)
                return;
            if (PlayerStatusWorker.fetch.myPlayerStatus.teamName == "")
                return;
            DarkLog.Debug("onFundsChanged: new funds is: " + funds.ToString());
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<double>(funds);
                NetworkWorker.fetch.SendFundsChangedMessage(mw.GetMessageBytes());
            }
        }

        public void onReputationChanged(float reputation, TransactionReasons reasons)
        {
			if (ContractWorker.fetch.syncingContracts || ResearchWorker.fetch.syncingResearch)
				return;
            if (reasons == TransactionReasons.None)
                return;
            if (PlayerStatusWorker.fetch.myPlayerStatus.teamName == "")
                return;
            DarkLog.Debug("onReputationChanged: new reputation is: " + reputation.ToString());
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<float>(reputation);
                NetworkWorker.fetch.SendReputationChangedMessage(mw.GetMessageBytes());
            }
        }

        // Networking

        public void handleFundsChanged(byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                string teamName = mr.Read<string>();
                double funds = mr.Read<double>();
                if (teamName == PlayerStatusWorker.fetch.myPlayerStatus.teamName)
                    syncFundsWithTeam(funds);
                TeamWorker.fetch.teams.Find(team => team.teamName == teamName).funds = funds;
            }
        }

        public void handleReputationChanged(byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                string teamName = mr.Read<string>();
                float reputation = mr.Read<float>();
                if (teamName == PlayerStatusWorker.fetch.myPlayerStatus.teamName)
                    syncReputationWithTeam(reputation);
                TeamWorker.fetch.teams.Find(team => team.teamName == teamName).reputation = reputation;
            }
        }

        public void syncFundsWithTeam(double funds)
        {
			try {
				DarkLog.Debug("Syncing funds with team to target funds: " + funds.ToString());
				double diff = funds - Funding.Instance.Funds;
				Funding.Instance.AddFunds(diff, TransactionReasons.None);
				DarkLog.Debug("Funds succesfully synced");
			} catch(Exception e) {
				if(e.InnerException is NullReferenceException) {
					DarkLog.Debug("Funds sync failed");
					//var scheduler = new Scheduler();
					//scheduler.Execute(() => syncFundsWithTeam(funds), 5000);
				}
			}
        }

        public void syncReputationWithTeam(float rep)
        {
			try {
				DarkLog.Debug("Syncing reputation with team to target reputation: " + rep.ToString());
				float diff = rep - Reputation.Instance.reputation;
				Reputation.Instance.AddReputation(rep, TransactionReasons.None);
				DarkLog.Debug("Reputation succesfully synced");
			} catch(Exception e) {
				if(e.InnerException is NullReferenceException) {
					DarkLog.Debug("Rep sync failed");
					//var scheduler = new Scheduler();
					//scheduler.Execute(() => syncReputationWithTeam(rep), 5000);
				}
			}
		}
    }
}
