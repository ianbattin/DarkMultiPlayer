using DarkMultiPlayerCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DarkMultiPlayer
{
    public class CareerWorker
    {
        public bool workerEnabled = false;
        private static CareerWorker singleton;

        public static CareerWorker fetch
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

        }

        public void onReputationChanged(float reputation, TransactionReasons reasons)
        {

        }

        public void syncFundsWithTeam(double funds)
        {
            DarkLog.Debug("Syncing funds with team to target funds: " + funds.ToString());
            double diff = funds - Funding.Instance.Funds;
            Funding.Instance.AddFunds(diff, TransactionReasons.None);
        }

        public void syncReputationWithTeam(float rep)
        {
            DarkLog.Debug("Syncing reputation with team to target reputation: " + rep.ToString());
            float diff = rep - Reputation.Instance.reputation;
            Reputation.Instance.AddReputation(rep, TransactionReasons.None);
        }
    }
}
