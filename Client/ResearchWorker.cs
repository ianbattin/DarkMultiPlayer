using DarkMultiPlayerCommon;
using MessageStream2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DarkMultiPlayer
{
    class ResearchWorker
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
            if (singleton != null)
            {
                singleton.workerEnabled = false;
                GameEvents.OnTechnologyResearched.Remove(singleton.onTechnologyResearched);
                GameEvents.OnPartPurchased.Remove(singleton.onPartPurchased);
            }
            singleton = new ResearchWorker();
            DarkLog.Debug("CareerWorker: Reset");
            GameEvents.OnTechnologyResearched.Add(singleton.onTechnologyResearched);
            GameEvents.OnPartPurchased.Add(singleton.onPartPurchased);
        }

        // GameEvents
        public void onTechnologyResearched(GameEvents.HostTargetAction<RDTech, RDTech.OperationResult> targetAction)
        {
            if (PlayerStatusWorker.fetch.myPlayerStatus.teamName == "")
                return;
            if (RDTech.OperationResult.Successful.Equals(targetAction.target))
            {
                DarkLog.Debug("Researched: " + targetAction.host.techID);
                //ProtoTechNode node = ResearchAndDevelopment.Instance.GetTechState(targetAction.host.techID);
                using (MessageWriter mw = new MessageWriter())
                {
                    mw.Write<string>(targetAction.host.techID);
                    NetworkWorker.fetch.SendResearchTechUnlocked(mw.GetMessageBytes());
                } 

            }
        }

        public void onPartPurchased(AvailablePart part)
        {
            if (PlayerStatusWorker.fetch.myPlayerStatus.teamName == "")
                return;
            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<string>(part.name);
                NetworkWorker.fetch.SendPartPurchased(mw.GetMessageBytes());
            }
        }


        // Network

        public void sendInitialTechState()
        {
            List<string> techIDs = getAvailableTechIDs();
            List<string> parts = getPurchasedParts();

            using (MessageWriter mw = new MessageWriter())
            {
                mw.Write<string[]>(techIDs.ToArray());
                mw.Write<string[]>(parts.ToArray());
                NetworkWorker.fetch.SendResearchTechState(mw.GetMessageBytes());
            }
        }

        public void handleResearchTechState(byte[] messageData)
        {
            List<string> techIDs;
            List<string> parts;

            using(MessageReader mr = new MessageReader(messageData))
            {
                techIDs = mr.Read<string[]>().ToList();
                parts = mr.Read<string[]>().ToList();
            }
            List<ProtoRDNode> nodes = AssetBase.RnDTechTree.GetTreeNodes().ToList<ProtoRDNode>();
            DarkLog.Debug("handleResearchTechState: found " + nodes.Count + " ProtoRDNodes");
            foreach(ProtoRDNode node in nodes)
            {
                if (techIDs.Contains(node.tech.techID))
                    node.tech.state = RDTech.State.Available;
                else
                    node.tech.state = RDTech.State.Unavailable;

                node.tech.partsPurchased.Clear();
                foreach(string pName in parts)
                {
                    AvailablePart ap = PartLoader.getPartInfoByName(pName);

                    if(ap.TechRequired == node.tech.techID)
                    {
                        DarkLog.Debug("handleResearchTechState: added part: " + pName + " to purchased parts of techID: "+node.tech.techID);
                        node.tech.partsPurchased.Add(ap);
                    }
                }

                ResearchAndDevelopment.Instance.SetTechState(node.tech.techID, node.tech);
            }
        }

        public void handleResearchTechUnlocked(byte[] messageData)
        {
            using (MessageReader mr = new MessageReader(messageData))
            {
                string techID = mr.Read<string>();
                researchTech(techID);
            }
        }

        public void handlePartPurchased(byte[] messageData)
        {
            using(MessageReader mr = new MessageReader(messageData))
            {
                string partName = mr.Read<string>();
                DarkLog.Debug("Received: RESEARCH_PART_PURCHASED for part: " + partName);
                purchasePart(partName);
            }
        }

        // Helper functions

        public void purchasePart(string partName)
        {
            AvailablePart part = PartLoader.getPartInfoByName(partName);
            ProtoTechNode node = ResearchAndDevelopment.Instance.GetTechState(part.TechRequired);
            if (!node.partsPurchased.Contains(part))
            {
                DarkLog.Debug("purchasePart: adding part: " + partName + " to techID: " + part.TechRequired);
                node.partsPurchased.Add(part);
                ResearchAndDevelopment.Instance.SetTechState(part.TechRequired, node);
            }
        }

        public void researchTech(string techID)
        {
            ProtoTechNode node = ResearchAndDevelopment.Instance.GetTechState(techID);
            node.state = RDTech.State.Available;
            ResearchAndDevelopment.Instance.SetTechState(techID, node);
        }

        /// <summary>
        /// Career mode only
        /// Called when the user creates a team
        /// </summary>
        /// <returns>List of part names that have been purchased</returns>
        public List<string> getPurchasedParts()
        {
            List<string> parts = new List<string>();
            foreach (AvailablePart part in PartLoader.LoadedPartsList)
            {
                if (ResearchAndDevelopment.PartModelPurchased(part))
                {
                    parts.Add(part.name);
                }
            }
            return parts;
        }

        /// <summary>
        /// Career/Science mode only
        /// </summary>
        /// <returns>List of techIDs that are researched</returns>
        public List<string> getAvailableTechIDs()
        {
            List<string> techList = new List<string>();
            /*foreach (AvailablePart part in PartLoader.LoadedPartsList)
            {
                ProtoTechNode tech = ResearchAndDevelopment.Instance.GetTechState(part.TechRequired);
                if (tech.state == RDTech.State.Available && !techList.Contains(tech.techID))
                    techList.Add(tech.techID);
            }*/

            List<ProtoRDNode> nodes = AssetBase.RnDTechTree.GetTreeNodes().ToList<ProtoRDNode>().FindAll(node => node.tech.state == RDTech.State.Available);
            foreach(ProtoRDNode node in nodes)
            {
                techList.Add(node.tech.techID);
            }
            return techList;
        }

        // DEBUG
        public static void enumerateRDTech()
        {
            /*
            foreach (AvailablePart part in PartLoader.LoadedPartsList)
            {
                if (ResearchAndDevelopment.PartTechAvailable(part))
                {
                    DarkLog.Debug("Part: " + part.name + " is available, required techname: " + part.TechRequired);
                }
                if (ResearchAndDevelopment.PartModelPurchased(part))
                {
                    DarkLog.Debug("Part: " + part.name + " is purchased, required techname: " + part.TechRequired);
                }
            }*/
            foreach(string techID in ResearchWorker.fetch.getAvailableTechIDs())
            {
                DarkLog.Debug("techID: " + techID);
            }
            /*foreach(ProtoRDNode node in AssetBase.RnDTechTree.GetTreeNodes())
            {
                
                DarkLog.Debug("Found ProtoRDNode: " + node.tech.techID + " with state: " + node.tech.state);
            }*/
        }
    }
}
