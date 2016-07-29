using DarkMultiPlayerCommon;
using MessageStream2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Contracts;

namespace DarkMultiPlayer {
	public class ContractWorker {
		public bool workerEnabled = false;
		public bool syncingContracts = false;
		private Contract.State[] myContractStates = {
			Contract.State.Active,
			Contract.State.Cancelled,
			Contract.State.Completed,
			Contract.State.Declined,
			Contract.State.Failed,
			Contract.State.DeadlineExpired,
			Contract.State.Offered
		};

		private static ContractWorker singleton;

		public static ContractWorker fetch {
			get {
				return singleton;
			}
		}

		public static void Reset() {
			if (singleton != null) {
				singleton.workerEnabled = false;
				singleton.syncingContracts = false;
				GameEvents.Contract.onAccepted.Remove(singleton.onContractAccepted);
				GameEvents.Contract.onCancelled.Remove(singleton.onContractCancelled);
				GameEvents.Contract.onCompleted.Remove(singleton.onContractCompleted);
				GameEvents.Contract.onDeclined.Remove(singleton.onContractDeclined);
				GameEvents.Contract.onFailed.Remove(singleton.onContractFailed);
				GameEvents.Contract.onFinished.Remove(singleton.onContractFinished);
				GameEvents.Contract.onOffered.Remove(singleton.onContractOffered);
			}
			singleton = new ContractWorker();
			singleton.syncingContracts = false;
			DarkLog.Debug("ContractWorker: Reset");
			GameEvents.Contract.onAccepted.Add(singleton.onContractAccepted);
			GameEvents.Contract.onCancelled.Add(singleton.onContractCancelled);
			GameEvents.Contract.onCompleted.Add(singleton.onContractCompleted);
			GameEvents.Contract.onDeclined.Add(singleton.onContractDeclined);
			GameEvents.Contract.onFailed.Add(singleton.onContractFailed);
			GameEvents.Contract.onFinished.Add(singleton.onContractFinished);
			GameEvents.Contract.onOffered.Add(singleton.onContractOffered);
		}

		public void onContractAccepted(Contract data) {
			if (ContractWorker.fetch.syncingContracts)
				return;
			if (PlayerStatusWorker.fetch.myPlayerStatus.teamName == "")
				return;
			using (MessageWriter mw = new MessageWriter()) {
				mw.Write<string>(data.Title);
				NetworkWorker.fetch.SendContractAcceptedMessage(mw.GetMessageBytes());
			}
		}

		public void onContractCancelled(Contract data) {
			if (ContractWorker.fetch.syncingContracts)
				return;
			if (PlayerStatusWorker.fetch.myPlayerStatus.teamName == "")
				return;
			using (MessageWriter mw = new MessageWriter()) {
				mw.Write<string>(data.Title);
				NetworkWorker.fetch.SendContractCancelledMessage(mw.GetMessageBytes());
			}
		}

		public void onContractCompleted(Contract data) {
			if (ContractWorker.fetch.syncingContracts)
				return;
			if (PlayerStatusWorker.fetch.myPlayerStatus.teamName == "")
				return;
			using (MessageWriter mw = new MessageWriter()) {
				mw.Write<string>(data.Title);
				NetworkWorker.fetch.SendContractCompletedMessage(mw.GetMessageBytes());
			}
		}

		public void onContractDeclined(Contract data) {
			if (ContractWorker.fetch.syncingContracts)
				return;
			if (PlayerStatusWorker.fetch.myPlayerStatus.teamName == "")
				return;
			using (MessageWriter mw = new MessageWriter()) {
				mw.Write<string>(data.Title);
				NetworkWorker.fetch.SendContractDeclinedMessage(mw.GetMessageBytes());
			}
		}

		public void onContractFailed(Contract data) {
			if (ContractWorker.fetch.syncingContracts)
				return;
			if (PlayerStatusWorker.fetch.myPlayerStatus.teamName == "")
				return;
			using (MessageWriter mw = new MessageWriter()) {
				mw.Write<string>(data.Title);
				NetworkWorker.fetch.SendContractFailedMessage(mw.GetMessageBytes());
			}
		}

		public void onContractFinished(Contract data) {
			if (ContractWorker.fetch.syncingContracts)
				return;
			if (PlayerStatusWorker.fetch.myPlayerStatus.teamName == "")
				return;
			using (MessageWriter mw = new MessageWriter()) {
				mw.Write<string>(data.Title);
				NetworkWorker.fetch.SendContractFinishedMessage(mw.GetMessageBytes());
			}
		}

		public void onContractOffered(Contract data) {
			if (ContractWorker.fetch.syncingContracts)
				return;
			if (PlayerStatusWorker.fetch.myPlayerStatus.teamName == "")
				return;
			using (MessageWriter mw = new MessageWriter()) {
				mw.Write<string>(data.Title);
				NetworkWorker.fetch.SendContractOfferedMessage(mw.GetMessageBytes());
			}
		}

		public void handleContractAcceptedMessage(byte[] messageData) {
			using (MessageReader mr = new MessageReader(messageData)) {
				string teamName = mr.Read<string>();
				string contractTitle = mr.Read<string>();
				DarkLog.Debug("Recieved message for accepted contract: " + contractTitle);
				TeamStatus teamStatus = TeamWorker.fetch.teams.Find(team => team.teamName == teamName);
				teamStatus.contracts[0].Add(contractTitle);
				teamStatus.contracts[6].Remove(contractTitle);
				if (teamName == PlayerStatusWorker.fetch.myPlayerStatus.teamName)
					ContractWorker.fetch.syncContractsWithTeam(teamStatus.contracts);
			}
		}

		public void handleContractCancelledMessage(byte[] messageData) {
			using (MessageReader mr = new MessageReader(messageData)) {
				string teamName = mr.Read<string>();
				string contractTitle = mr.Read<string>();
				DarkLog.Debug("Recieved message for cancelled contract: " + contractTitle);
				TeamStatus teamStatus = TeamWorker.fetch.teams.Find(team => team.teamName == teamName);
				teamStatus.contracts[1].Add(contractTitle);
				teamStatus.contracts[0].Remove(contractTitle);
				teamStatus.contracts[6].Remove(contractTitle);
				if (teamName == PlayerStatusWorker.fetch.myPlayerStatus.teamName)
					ContractWorker.fetch.syncContractsWithTeam(teamStatus.contracts);
			}
		}

		public void handleContractCompletedMessage(byte[] messageData) {
			using (MessageReader mr = new MessageReader(messageData)) {
				string teamName = mr.Read<string>();
				string contractTitle = mr.Read<string>();
				DarkLog.Debug("Recieved message for completed contract: " + contractTitle);
				TeamStatus teamStatus = TeamWorker.fetch.teams.Find(team => team.teamName == teamName);
				teamStatus.contracts[2].Add(contractTitle);
				teamStatus.contracts[0].Remove(contractTitle);
				teamStatus.contracts[6].Remove(contractTitle);
				if (teamName == PlayerStatusWorker.fetch.myPlayerStatus.teamName)
					ContractWorker.fetch.syncContractsWithTeam(teamStatus.contracts);
			}
		}

		public void handleContractDeclinedMessage(byte[] messageData) {
			using (MessageReader mr = new MessageReader(messageData)) {
				string teamName = mr.Read<string>();
				string contractTitle = mr.Read<string>();
				DarkLog.Debug("Recieved message for declined contract: " + contractTitle);
				TeamStatus teamStatus = TeamWorker.fetch.teams.Find(team => team.teamName == teamName);
				teamStatus.contracts[3].Add(contractTitle);
				teamStatus.contracts[0].Remove(contractTitle);
				teamStatus.contracts[6].Remove(contractTitle);
				if (teamName == PlayerStatusWorker.fetch.myPlayerStatus.teamName)
					ContractWorker.fetch.syncContractsWithTeam(teamStatus.contracts);
			}
		}

		public void handleContractFailedMessage(byte[] messageData) {
			using (MessageReader mr = new MessageReader(messageData)) {
				string teamName = mr.Read<string>();
				string contractTitle = mr.Read<string>();
				DarkLog.Debug("Recieved message for failed contract: " + contractTitle);
				TeamStatus teamStatus = TeamWorker.fetch.teams.Find(team => team.teamName == teamName);
				teamStatus.contracts[4].Add(contractTitle);
				teamStatus.contracts[0].Remove(contractTitle);
				teamStatus.contracts[6].Remove(contractTitle);
				if (teamName == PlayerStatusWorker.fetch.myPlayerStatus.teamName)
					ContractWorker.fetch.syncContractsWithTeam(teamStatus.contracts);
			}
		}

		public void handleContractFinishedMessage(byte[] messageData) {
			using (MessageReader mr = new MessageReader(messageData)) {
				string teamName = mr.Read<string>();
				string contractTitle = mr.Read<string>();
				DarkLog.Debug("Recieved message for finished contract: " + contractTitle);
				TeamStatus teamStatus = TeamWorker.fetch.teams.Find(team => team.teamName == teamName);
				teamStatus.contracts[5].Add(contractTitle);
				teamStatus.contracts[0].Remove(contractTitle);
				teamStatus.contracts[6].Remove(contractTitle);
				if (teamName == PlayerStatusWorker.fetch.myPlayerStatus.teamName)
					ContractWorker.fetch.syncContractsWithTeam(teamStatus.contracts);
			}
		}

		public void handleContractOfferedMessage(byte[] messageData) {
			using (MessageReader mr = new MessageReader(messageData)) {
				string teamName = mr.Read<string>();
				string contractTitle = mr.Read<string>();
				DarkLog.Debug("Recieved message for offered contract: " + contractTitle);
				TeamStatus teamStatus = TeamWorker.fetch.teams.Find(team => team.teamName == teamName);
				teamStatus.contracts[6].Add(contractTitle);
				teamStatus.contracts[0].Remove(contractTitle);
				teamStatus.contracts[1].Remove(contractTitle);
				teamStatus.contracts[2].Remove(contractTitle);
				teamStatus.contracts[3].Remove(contractTitle);
				teamStatus.contracts[4].Remove(contractTitle);
				teamStatus.contracts[5].Remove(contractTitle);
				if (teamName == PlayerStatusWorker.fetch.myPlayerStatus.teamName)
					ContractWorker.fetch.syncContractsWithTeam(teamStatus.contracts);
			}
		}

		public void syncContractsWithTeam(List<List<string>> contracts) {
			DarkLog.Debug("syncing contracts with team");

			double funds = Funding.Instance.Funds;
			float rep = Reputation.Instance.reputation;
			float science = ResearchAndDevelopment.Instance.Science;
			ContractWorker.fetch.syncingContracts = true;
			syncingContracts = true;

			for (int i = 0; i < contracts.Count(); i++) {
				foreach (string contractTitle in contracts[i]) {
					try {
						DarkLog.Debug("Syncing contract: " + contractTitle + " type: " + i + " | num contracts in type: " + contracts[i].Count());
						Contract contract = ContractSystem.Instance.Contracts.Find(someContract => someContract.Title == contractTitle);
						DarkLog.Debug("switcing on contract type");
						DarkLog.Debug("contract state = " + contract.ContractState);

						switch (i) {
							case 0:
								DarkLog.Debug("Contracts: " + contractTitle + " accepted");
								if (contract.ContractState != Contract.State.Offered) contract.Offer();
								if (contract.ContractState != Contract.State.Active) contract.Accept();
								break;
							case 1:
								DarkLog.Debug("Contracts: " + contractTitle + " cancelled");
								if (contract.ContractState != Contract.State.Offered) contract.Offer();
								if (contract.ContractState != Contract.State.Active) contract.Accept();
								if (contract.ContractState != Contract.State.Cancelled) contract.Cancel();
								break;
							case 2:
								DarkLog.Debug("Contracts: " + contractTitle + " completed");
								if (contract.ContractState != Contract.State.Offered) contract.Offer();
								if (contract.ContractState != Contract.State.Active) contract.Accept();
								if (contract.ContractState != Contract.State.Completed) contract.Complete();
								break;
							case 3:
								DarkLog.Debug("Contracts: " + contractTitle + " declined");
								if (contract.ContractState != Contract.State.Offered) contract.Offer();
								if (contract.ContractState != Contract.State.Declined) contract.Decline();
								break;
							case 4:
								DarkLog.Debug("Contracts: " + contractTitle + " failed");
								if (contract.ContractState != Contract.State.Offered) contract.Offer();
								if (contract.ContractState != Contract.State.Active) contract.Accept();
								if (contract.ContractState != Contract.State.Failed) contract.Fail();
								break;
							case 5:
								DarkLog.Debug("Contracts: " + contractTitle + " finished");
								if (contract.ContractState != Contract.State.Offered) contract.Offer();
								if (contract.ContractState != Contract.State.Active) contract.Accept();
								if (contract.ContractState != Contract.State.Completed) contract.Complete();
								break;
							case 6:
								DarkLog.Debug("Contracts: " + contractTitle + " offered");
								if (contract.ContractState == Contract.State.Active) contract.Cancel();
								if (contract.ContractState != Contract.State.Offered) contract.Offer();
								break;
							default:
								DarkLog.Debug("CONTRACT SYNC FAILED");
								break;
						}
					} catch (Exception e) {
						DarkLog.Debug("SYNCING CONTRACTS OF TYPE: " + i + " WITH TEAM FAILED");
					}
				}
			}

			Funding.Instance.AddFunds(funds - Funding.Instance.Funds, TransactionReasons.None); //prevents being paid twice
			Reputation.Instance.AddReputation(rep - Reputation.Instance.reputation, TransactionReasons.None);
			ResearchAndDevelopment.Instance.AddScience(science - ResearchAndDevelopment.Instance.Science, TransactionReasons.None);
			ContractWorker.fetch.syncingContracts = false;
			syncingContracts = false;
			/*
			//Make sure team is synced from database
			CareerWorker.fetch.syncFundsWithTeam(TeamWorker.fetch.teams.Find(teamName => teamName.teamName == PlayerStatusWorker.fetch.myPlayerStatus.teamName).funds);
			CareerWorker.fetch.syncReputationWithTeam(TeamWorker.fetch.teams.Find(teamName => teamName.teamName == PlayerStatusWorker.fetch.myPlayerStatus.teamName).reputation);
			ScienceWorker.fetch.syncScienceWithTeam(TeamWorker.fetch.teams.Find(teamName => teamName.teamName == PlayerStatusWorker.fetch.myPlayerStatus.teamName).science);
			*/
		}

		public void SendInitialContractState() {
			using (MessageWriter mw = new MessageWriter()) {
				foreach(List<string> contractList in getContractState()) {
					mw.Write<string[]>(contractList.ToArray());
				}
				NetworkWorker.fetch.SendContractState(mw.GetMessageBytes());
			}
		}

		public List<List<string>> getContractState() {
			List<List<string>> contracts = new List<List<string>>();
			contracts.Add(getContractsOfType("ACCEPTED"));
			contracts.Add(getContractsOfType("CANCELLED"));
			contracts.Add(getContractsOfType("COMPLETED"));
			contracts.Add(getContractsOfType("DECLINED"));
			contracts.Add(getContractsOfType("FAILED"));
			contracts.Add(getContractsOfType("FINISHED"));
			contracts.Add(getContractsOfType("OFFERED"));
			return contracts;
		}

		public List<string> getContractsOfType(string type) {
			DarkLog.Debug("Getting contracts of type: " + type);
			List<string> contractNames = new List<string>();
			switch(type.ToUpper()) {
				case "ACCEPTED":
					List<Contract> acceptedContracts = ContractSystem.Instance.Contracts.FindAll(contract => contract.ContractState == Contract.State.Active);
					foreach(Contract contract in acceptedContracts) {
						contractNames.Add(contract.Title);
					}
					break;
				case "CANCELLED":
					List<Contract> cancelledContracts = ContractSystem.Instance.Contracts.FindAll(contract => contract.ContractState == Contract.State.Cancelled);
					foreach (Contract contract in cancelledContracts) {
						contractNames.Add(contract.Title);
					}
					break;
				case "COMPLETED":
					List<Contract> completedContracts = ContractSystem.Instance.Contracts.FindAll(contract => contract.ContractState == Contract.State.Completed);
					foreach (Contract contract in completedContracts) {
						contractNames.Add(contract.Title);
					}
					break;
				case "DECLINED":
					List<Contract> declinedContracts = ContractSystem.Instance.Contracts.FindAll(contract => contract.ContractState == Contract.State.Declined);
					foreach (Contract contract in declinedContracts) {
						contractNames.Add(contract.Title);
					}
					break;
				case "FAILED":
					List<Contract> failedContracts = ContractSystem.Instance.Contracts.FindAll(contract => contract.ContractState == Contract.State.Failed);
					foreach (Contract contract in failedContracts) {
						contractNames.Add(contract.Title);
					}
					break;
				case "FINISHED":
					List<Contract> finishedContracts = ContractSystem.Instance.Contracts.FindAll(contract => contract.ContractState == Contract.State.OfferExpired);
					foreach (Contract contract in finishedContracts) {
						contractNames.Add(contract.Title);
					}
					break;
				case "OFFERED":
					List<Contract> offeredContracts = ContractSystem.Instance.Contracts.FindAll(contract => contract.ContractState == Contract.State.Offered);
					foreach (Contract contract in offeredContracts) {
						contractNames.Add(contract.Title);
					}
					break;
				default:
					DarkLog.Debug("Something went wrong with getting contracts");
					return null;
			}
			foreach(string contractName in contractNames) {
				DarkLog.Debug("Got contract of type: " + type + " named: " + contractName);
			}

			return contractNames;
		}
	}
}
