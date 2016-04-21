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
                node.state = RDTech.State.Unavailable;
                //ResearchAndDevelopment.Instance.SetTechState(targetAction.host.techID)
            }
        }

        public void syncTechnologyWithTeam()
        {
            
        }
    }
}
