using DarkMultiPlayerCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DarkMultiPlayer
{
    public class ResearchWorker
    {
        public bool workerEnabled = false;
        private static ResearchWorker singleton;

        public static ResearchWorker fetch
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
                GameEvents.OnTechnologyResearched.Remove(singleton.onTechnologyResearched);
            }
            singleton = new ResearchWorker();
            DarkLog.Debug("ResearchWorker: loaded");
            GameEvents.OnTechnologyResearched.Add(singleton.onTechnologyResearched);
        }

        public void onTechnologyResearched(GameEvents.HostTargetAction<RDTech, RDTech.OperationResult> targetAction)
        {
            if(RDTech.OperationResult.Successful.Equals(targetAction.target))
            {
                DarkLog.Debug("Researched: " + targetAction.host.techID);
                ProtoTechNode node = ResearchAndDevelopment.Instance.GetTechState(targetAction.host.techID);
                //node.state = RDTech.State.Unavailable;
                //ResearchAndDevelopment.Instance.SetTechState(targetAction.host.techID)
            }
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
            Reputation.Instance.addReputation_discrete(diff,TransactionReasons.None);
        }

        public void modifyRDNodes(RDNodeStatus status)
        {
            //RDController.Instance.nodes;
            if (status.researched)
            {
            }
            //ResearchAndDevelopment.Instance.SetTechState(status.techID, node);

            /*
            
        private void OnResearchAllConfirm()
        {
            foreach (RDNode node in FindObjectsOfType(typeof(RDNode)))
            {
                if (node.tech != null && node.IsResearched)
                {
                    node.tech.AutoPurchaseAllParts();
                    node.graphics.SetAvailablePartsCircle(node.PartsNotUnlocked());
                }
            }
            OnGUIRnDComplexDespawn();
        }
            */
        }
    }
}
