using DarkMultiPlayerCommon;
using MessageStream2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Contracts;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

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
				mw.Write<string>(Common.serializedContract(data));
				NetworkWorker.fetch.SendContractAcceptedMessage(mw.GetMessageBytes());
			}
		}

		public void onContractCancelled(Contract data) {
			if (ContractWorker.fetch.syncingContracts)
				return;
			if (PlayerStatusWorker.fetch.myPlayerStatus.teamName == "")
				return;
			using (MessageWriter mw = new MessageWriter()) {
				mw.Write<string>(Common.serializedContract(data));
				NetworkWorker.fetch.SendContractCancelledMessage(mw.GetMessageBytes());
			}
		}

		public void onContractCompleted(Contract data) {
			if (ContractWorker.fetch.syncingContracts)
				return;
			if (PlayerStatusWorker.fetch.myPlayerStatus.teamName == "")
				return;
			using (MessageWriter mw = new MessageWriter()) {
				mw.Write<string>(Common.serializedContract(data));
				NetworkWorker.fetch.SendContractCompletedMessage(mw.GetMessageBytes());
			}
		}

		public void onContractDeclined(Contract data) {
			if (ContractWorker.fetch.syncingContracts)
				return;
			if (PlayerStatusWorker.fetch.myPlayerStatus.teamName == "")
				return;
			using (MessageWriter mw = new MessageWriter()) {
				mw.Write<string>(Common.serializedContract(data));
				NetworkWorker.fetch.SendContractDeclinedMessage(mw.GetMessageBytes());
			}
		}

		public void onContractFailed(Contract data) {
			if (ContractWorker.fetch.syncingContracts)
				return;
			if (PlayerStatusWorker.fetch.myPlayerStatus.teamName == "")
				return;
			using (MessageWriter mw = new MessageWriter()) {
				mw.Write<string>(Common.serializedContract(data));
				NetworkWorker.fetch.SendContractFailedMessage(mw.GetMessageBytes());
			}
		}

		public void onContractFinished(Contract data) {
			if (ContractWorker.fetch.syncingContracts)
				return;
			if (PlayerStatusWorker.fetch.myPlayerStatus.teamName == "")
				return;
			using (MessageWriter mw = new MessageWriter()) {
				mw.Write<string>(Common.serializedContract(data));
				NetworkWorker.fetch.SendContractFinishedMessage(mw.GetMessageBytes());
			}
		}

		public void onContractOffered(Contract data) {
			if (ContractWorker.fetch.syncingContracts)
				return;
			if (PlayerStatusWorker.fetch.myPlayerStatus.teamName == "")
				return;
			using (MessageWriter mw = new MessageWriter()) {
				mw.Write<string>(Common.serializedContract(data));
				NetworkWorker.fetch.SendContractOfferedMessage(mw.GetMessageBytes());
			}
		}

		public void handleContractAcceptedMessage(byte[] messageData) {
			using (MessageReader mr = new MessageReader(messageData)) {
				string teamName = mr.Read<string>();
				Contract contract = Common.deserializedContract(mr.Read<string>());
				DarkLog.Debug("Recieved message for accepted contract: " + contract.Title);
				TeamStatus teamStatus = TeamWorker.fetch.teams.Find(team => team.teamName == teamName);
				teamStatus.contracts[0].Add(contract);
				teamStatus.contracts[6].Remove(contract);
				if (teamName == PlayerStatusWorker.fetch.myPlayerStatus.teamName)
					ContractWorker.fetch.syncContractsWithTeam(teamStatus.contracts);
			}
		}

		public void handleContractCancelledMessage(byte[] messageData) {
			using (MessageReader mr = new MessageReader(messageData)) {
				string teamName = mr.Read<string>();
				Contract contract = Common.deserializedContract(mr.Read<string>());
				DarkLog.Debug("Recieved message for cancelled contract: " + contract.Title);
				TeamStatus teamStatus = TeamWorker.fetch.teams.Find(team => team.teamName == teamName);
				teamStatus.contracts[1].Add(contract);
				teamStatus.contracts[0].Remove(contract);
				teamStatus.contracts[6].Remove(contract);
				if (teamName == PlayerStatusWorker.fetch.myPlayerStatus.teamName)
					ContractWorker.fetch.syncContractsWithTeam(teamStatus.contracts);
			}
		}

		public void handleContractCompletedMessage(byte[] messageData) {
			using (MessageReader mr = new MessageReader(messageData)) {
				string teamName = mr.Read<string>();
				Contract contract = Common.deserializedContract(mr.Read<string>());
				DarkLog.Debug("Recieved message for completed contract: " + contract.Title);
				TeamStatus teamStatus = TeamWorker.fetch.teams.Find(team => team.teamName == teamName);
				teamStatus.contracts[2].Add(contract);
				teamStatus.contracts[0].Remove(contract);
				teamStatus.contracts[6].Remove(contract);
				if (teamName == PlayerStatusWorker.fetch.myPlayerStatus.teamName)
					ContractWorker.fetch.syncContractsWithTeam(teamStatus.contracts);
			}
		}

		public void handleContractDeclinedMessage(byte[] messageData) {
			using (MessageReader mr = new MessageReader(messageData)) {
				string teamName = mr.Read<string>();
				Contract contract = Common.deserializedContract(mr.Read<string>());
				DarkLog.Debug("Recieved message for declined contract: " + contract.Title);
				TeamStatus teamStatus = TeamWorker.fetch.teams.Find(team => team.teamName == teamName);
				teamStatus.contracts[3].Add(contract);
				teamStatus.contracts[0].Remove(contract);
				teamStatus.contracts[6].Remove(contract);
				if (teamName == PlayerStatusWorker.fetch.myPlayerStatus.teamName)
					ContractWorker.fetch.syncContractsWithTeam(teamStatus.contracts);
			}
		}

		public void handleContractFailedMessage(byte[] messageData) {
			using (MessageReader mr = new MessageReader(messageData)) {
				string teamName = mr.Read<string>();
				Contract contract = Common.deserializedContract(mr.Read<string>());
				DarkLog.Debug("Recieved message for failed contract: " + contract.Title);
				TeamStatus teamStatus = TeamWorker.fetch.teams.Find(team => team.teamName == teamName);
				teamStatus.contracts[4].Add(contract);
				teamStatus.contracts[0].Remove(contract);
				teamStatus.contracts[6].Remove(contract);
				if (teamName == PlayerStatusWorker.fetch.myPlayerStatus.teamName)
					ContractWorker.fetch.syncContractsWithTeam(teamStatus.contracts);
			}
		}

		public void handleContractFinishedMessage(byte[] messageData) {
			using (MessageReader mr = new MessageReader(messageData)) {
				string teamName = mr.Read<string>();
				Contract contract = Common.deserializedContract(mr.Read<string>());
				DarkLog.Debug("Recieved message for finished contract: " + contract.Title);
				TeamStatus teamStatus = TeamWorker.fetch.teams.Find(team => team.teamName == teamName);
				teamStatus.contracts[5].Add(contract);
				teamStatus.contracts[0].Remove(contract);
				teamStatus.contracts[6].Remove(contract);
				if (teamName == PlayerStatusWorker.fetch.myPlayerStatus.teamName)
					ContractWorker.fetch.syncContractsWithTeam(teamStatus.contracts);
			}
		}

		public void handleContractOfferedMessage(byte[] messageData) {
			using (MessageReader mr = new MessageReader(messageData)) {
				string teamName = mr.Read<string>();
				Contract contract = Common.deserializedContract(mr.Read<string>());
				DarkLog.Debug("Recieved message for offered contract: " + contract.Title);
				TeamStatus teamStatus = TeamWorker.fetch.teams.Find(team => team.teamName == teamName);
				teamStatus.contracts[6].Add(contract);
				teamStatus.contracts[0].Remove(contract);
				teamStatus.contracts[1].Remove(contract);
				teamStatus.contracts[2].Remove(contract);
				teamStatus.contracts[3].Remove(contract);
				teamStatus.contracts[4].Remove(contract);
				teamStatus.contracts[5].Remove(contract);
				if (teamName == PlayerStatusWorker.fetch.myPlayerStatus.teamName)
					ContractWorker.fetch.syncContractsWithTeam(teamStatus.contracts);
			}
		}

		public void syncContractsWithTeam(List<List<Contract>> contracts) {
			DarkLog.Debug("syncing contracts with team");

			double funds = Funding.Instance.Funds;
			float rep = Reputation.Instance.reputation;
			float science = ResearchAndDevelopment.Instance.Science;
			ContractWorker.fetch.syncingContracts = true;
			syncingContracts = true;

			for (int i = 0; i < contracts.Count(); i++) {
				foreach (Contract contract in contracts[i]) {
					try {
						DarkLog.Debug("Syncing contract: " + contract.Title + " type: " + i + " | num contracts in type: " + contracts[i].Count());
						Contract generatedContract = ContractSystem.Instance.GenerateContract(contract.MissionSeed, contract.Prestige, null);

						DarkLog.Debug("switcing on generatedContract type");
						DarkLog.Debug("generatedContract state = " + generatedContract.ContractState);

						switch (i) {
							case 0:
								DarkLog.Debug("Contracts: " + contract.Title + " accepted");
								if (generatedContract.ContractState != Contract.State.Offered && generatedContract.ContractState != Contract.State.Active) generatedContract.Offer();
								if (generatedContract.ContractState != Contract.State.Active) generatedContract.Accept();
								break;
							case 1:
								DarkLog.Debug("Contracts: " + contract.Title + " cancelled");
								if (generatedContract.ContractState != Contract.State.Offered && generatedContract.ContractState != Contract.State.Active && generatedContract.ContractState != Contract.State.Cancelled) generatedContract.Offer();
								if (generatedContract.ContractState != Contract.State.Active && generatedContract.ContractState != Contract.State.Cancelled) generatedContract.Accept();
								if (generatedContract.ContractState != Contract.State.Cancelled) generatedContract.Cancel();
								break;
							case 2:
								DarkLog.Debug("Contracts: " + contract.Title + " completed");
								if (generatedContract.ContractState != Contract.State.Offered && generatedContract.ContractState != Contract.State.Active && generatedContract.ContractState != Contract.State.Completed) generatedContract.Offer();
								if (generatedContract.ContractState != Contract.State.Active && generatedContract.ContractState != Contract.State.Completed) generatedContract.Accept();
								if (generatedContract.ContractState != Contract.State.Completed) generatedContract.Complete();
								break;
							case 3:
								DarkLog.Debug("Contracts: " + contract.Title + " declined");
								if (generatedContract.ContractState != Contract.State.Offered && generatedContract.ContractState != Contract.State.Declined) generatedContract.Offer();
								if (generatedContract.ContractState != Contract.State.Declined) generatedContract.Decline();
								break;
							case 4:
								DarkLog.Debug("Contracts: " + contract.Title + " failed");
								if (generatedContract.ContractState != Contract.State.Offered && generatedContract.ContractState != Contract.State.Active && generatedContract.ContractState != Contract.State.Failed) generatedContract.Offer();
								if (generatedContract.ContractState != Contract.State.Active && generatedContract.ContractState != Contract.State.Failed) generatedContract.Accept();
								if (generatedContract.ContractState != Contract.State.Failed) generatedContract.Fail();
								break;
							case 5:
								DarkLog.Debug("Contracts: " + contract.Title + " finished");
								if (generatedContract.ContractState != Contract.State.Offered && generatedContract.ContractState != Contract.State.Active && generatedContract.ContractState != Contract.State.Completed) generatedContract.Offer();
								if (generatedContract.ContractState != Contract.State.Active && generatedContract.ContractState != Contract.State.Completed) generatedContract.Accept();
								if (generatedContract.ContractState != Contract.State.Completed) generatedContract.Complete();
								break;
							case 6:
								DarkLog.Debug("Contracts: " + contract.Title + " offered");
								if (generatedContract.ContractState == Contract.State.Active && generatedContract.ContractState != Contract.State.Offered) generatedContract.Cancel();
								if (generatedContract.ContractState != Contract.State.Offered) generatedContract.Offer();
								break;
							default:
								DarkLog.Debug("CONTRACT SYNC FAILED");
								break;
						}
					} catch (Exception e) {
						DarkLog.Debug(e.Message);
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
				foreach(List<Contract> contractList in getContractState()) {
					List<string> rawData = new List<string>();
					foreach(Contract contract in contractList) {
						rawData.Add(Common.serializedContract(contract));
					}
					mw.Write<string[]>(rawData.ToArray());
				}
				NetworkWorker.fetch.SendContractState(mw.GetMessageBytes());
			}
		}

		public List<List<Contract>> getContractState() {
			List<List<Contract>> contracts = new List<List<Contract>>();
			contracts.Add(getContractsOfType("ACCEPTED"));
			contracts.Add(getContractsOfType("CANCELLED"));
			contracts.Add(getContractsOfType("COMPLETED"));
			contracts.Add(getContractsOfType("DECLINED"));
			contracts.Add(getContractsOfType("FAILED"));
			contracts.Add(getContractsOfType("FINISHED"));
			contracts.Add(getContractsOfType("OFFERED"));
			return contracts;
		}

		public List<Contract> getContractsOfType(string type) {
			DarkLog.Debug("Getting contracts of type: " + type);
			List<Contract> contracts = new List<Contract>();
			switch(type.ToUpper()) {
				case "ACCEPTED":
					List<Contract> acceptedContracts = ContractSystem.Instance.Contracts.FindAll(contract => contract.ContractState == Contract.State.Active);
					foreach(Contract contract in acceptedContracts) {
						contracts.Add(contract);
					}
					break;
				case "CANCELLED":
					List<Contract> cancelledContracts = ContractSystem.Instance.Contracts.FindAll(contract => contract.ContractState == Contract.State.Cancelled);
					foreach (Contract contract in cancelledContracts) {
						contracts.Add(contract);
					}
					break;
				case "COMPLETED":
					List<Contract> completedContracts = ContractSystem.Instance.Contracts.FindAll(contract => contract.ContractState == Contract.State.Completed);
					foreach (Contract contract in completedContracts) {
						contracts.Add(contract);
					}
					break;
				case "DECLINED":
					List<Contract> declinedContracts = ContractSystem.Instance.Contracts.FindAll(contract => contract.ContractState == Contract.State.Declined);
					foreach (Contract contract in declinedContracts) {
						contracts.Add(contract);
					}
					break;
				case "FAILED":
					List<Contract> failedContracts = ContractSystem.Instance.Contracts.FindAll(contract => contract.ContractState == Contract.State.Failed);
					foreach (Contract contract in failedContracts) {
						contracts.Add(contract);
					}
					break;
				case "FINISHED":
					List<Contract> finishedContracts = ContractSystem.Instance.Contracts.FindAll(contract => contract.ContractState == Contract.State.OfferExpired);
					foreach (Contract contract in finishedContracts) {
						contracts.Add(contract);
					}
					break;
				case "OFFERED":
					List<Contract> offeredContracts = ContractSystem.Instance.Contracts.FindAll(contract => contract.ContractState == Contract.State.Offered);
					foreach (Contract contract in offeredContracts) {
						contracts.Add(contract);
					}
					break;
				default:
					DarkLog.Debug("Something went wrong with getting contracts");
					return null;
			}
			foreach(Contract contract in contracts) {
				DarkLog.Debug("Got contract of type: " + type + " named: " + contract.Title);
			}

			return contracts;
		}
	}
}
