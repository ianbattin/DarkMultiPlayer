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
			researchTech(targetAction.host.techID);
			DarkLog.Debug("Researched: " + targetAction.host.techID);
			if (PlayerStatusWorker.fetch.myPlayerStatus.teamName == "")
                return;
            if (RDTech.OperationResult.Successful.Equals(targetAction.target))
            {
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
				string teamName = mr.Read<string>();
				string techID = mr.Read<string>();
				DarkLog.Debug("Received: RESEARCH_PART_UNLOCKED for part: " + techID);
				List<string> newResearch = new List<string>();
				newResearch.Add(techID);
				if (teamName == PlayerStatusWorker.fetch.myPlayerStatus.teamName)
					syncResearchWithTeam(newResearch);
				//else
					//researchTech(techID);
				TeamWorker.fetch.teams.Find(team => team.teamName == teamName).research.Add(techID);
			}
        }

        public void handlePartPurchased(byte[] messageData)
        {
            using(MessageReader mr = new MessageReader(messageData))
            {
				string teamName = mr.Read<string>();
				string partName = mr.Read<string>();
                DarkLog.Debug("Received: RESEARCH_PART_PURCHASED for part: " + partName);

				List<string> newPurchase = new List<string>();
				newPurchase.Add(partName);
				if (teamName == PlayerStatusWorker.fetch.myPlayerStatus.teamName)
					syncPurchasedWithTeam(newPurchase);
				//else
					//purchasePart(partName);
				TeamWorker.fetch.teams.Find(team => team.teamName == teamName).purchased.Add(partName);
			}
        }

        // Helper functions

        public void purchasePart(string partName)
        {
			try {
				AvailablePart part = PartLoader.getPartInfoByName(partName);
				ProtoTechNode node = ResearchAndDevelopment.Instance.GetTechState(part.TechRequired);
				if (!node.partsPurchased.Contains(part)) {
					node.partsPurchased.Add(part);
					Funding.Instance.AddFunds(-part.cost, TransactionReasons.None);
					ResearchAndDevelopment.Instance.SetTechState(part.TechRequired, node);
				}
			} catch(Exception e) {
				if(e.InnerException is NullReferenceException) {
					DarkLog.Debug("Purchase part failed");
					var scheduler = new Scheduler();
					scheduler.Execute(() => purchasePart(partName), 5000);
				}
			}
        }

		public void researchTech(string techID) {
			try { 
				ProtoRDNode node = new ProtoRDNode();
				node = node.FindNodeByID(techID, AssetBase.RnDTechTree.GetTreeNodes().ToList<ProtoRDNode>());
				if (node.tech.state != RDTech.State.Available) {
					node.tech.state = RDTech.State.Available;
					ResearchAndDevelopment.Instance.SetTechState(node.tech.techID, node.tech);
					DarkLog.Debug("Tech state for: " + techID + " = " + ResearchAndDevelopment.Instance.GetTechState(node.tech.techID) + "   |  node.tech.state");
				}
			} catch(Exception e) {
				if(e.InnerException is NullReferenceException) {
					DarkLog.Debug("Research tech failed");
					var scheduler = new Scheduler();
					scheduler.Execute(() => researchTech(techID), 5000);
				}
			}
		}

		public void syncResearchWithTeam(List<string> research) 
		{
			foreach(string techID in research) {
				DarkLog.Debug("Syncing research with team with partID: " + techID);
				researchTech(techID);
			}
		}

		public void syncPurchasedWithTeam(List<string> purchased) {
			foreach (string partName in purchased) {
				DarkLog.Debug("Syncing purchased with team with partName: " + partName);
				purchasePart(partName);
			}
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
			techList.Add("start");
			/*foreach (AvailablePart part in PartLoader.LoadedPartsList)
            {
                ProtoTechNode tech = ResearchAndDevelopment.Instance.GetTechState(part.TechRequired);
                if (tech.state == RDTech.State.Available)
                    techList.Add(tech.techID);
            }*/

			ProtoRDNode newNode = new ProtoRDNode();
			List<ProtoRDNode> nodes = AssetBase.RnDTechTree.GetTreeNodes().ToList<ProtoRDNode>();
            foreach(ProtoRDNode node in nodes)
            {
				newNode = node;
				DarkLog.Debug("Tech: " + newNode.tech.techID + " | " + newNode.tech.state);
				if(node.tech.state == RDTech.State.Available)
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
