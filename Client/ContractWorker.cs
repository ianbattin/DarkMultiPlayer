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
				GameEvents.Contract.onAccepted.Remove(singleton.onContractAccepted);
				GameEvents.Contract.onCancelled.Remove(singleton.onContractCancelled);
				GameEvents.Contract.onCompleted.Remove(singleton.onContractCompleted);
				GameEvents.Contract.onDeclined.Remove(singleton.onContractDeclined);
				GameEvents.Contract.onFailed.Remove(singleton.onContractFailed);
				GameEvents.Contract.onFinished.Remove(singleton.onContractFinished);
				GameEvents.Contract.onOffered.Remove(singleton.onContractOffered);
			}
			singleton = new ContractWorker();
			DarkLog.Debug("ContractWorker: Reset");
			GameEvents.Contract.onAccepted.Add(singleton.onContractAccepted);
			GameEvents.Contract.onCancelled.Add(singleton.onContractCancelled);
			GameEvents.Contract.onCompleted.Add(singleton.onContractCompleted);
			GameEvents.Contract.onDeclined.Add(singleton.onContractDeclined);
			GameEvents.Contract.onFailed.Add(singleton.onContractFailed);
			GameEvents.Contract.onFinished.Add(singleton.onContractFinished);
			GameEvents.Contract.onOffered.Add(singleton.onContractOffered);
		}

		private void onContractAccepted(Contract data) {
			if (PlayerStatusWorker.fetch.myPlayerStatus.teamName == "")
				return;
			using (MessageWriter mw = new MessageWriter()) {
				mw.Write<string>(data.Title);
				NetworkWorker.fetch.SendContractAcceptedMessage(mw.GetMessageBytes());
			}
		}

		private void onContractCancelled(Contract data) {
			if (PlayerStatusWorker.fetch.myPlayerStatus.teamName == "")
				return;
			using (MessageWriter mw = new MessageWriter()) {
				mw.Write<string>(data.Title);
				NetworkWorker.fetch.SendContractCancelledMessage(mw.GetMessageBytes());
			}
		}

		private void onContractCompleted(Contract data) {
			if (PlayerStatusWorker.fetch.myPlayerStatus.teamName == "")
				return;
			using (MessageWriter mw = new MessageWriter()) {
				mw.Write<string>(data.Title);
				NetworkWorker.fetch.SendContractCompletedMessage(mw.GetMessageBytes());
			}
		}

		private void onContractDeclined(Contract data) {
			if (PlayerStatusWorker.fetch.myPlayerStatus.teamName == "")
				return;
			using (MessageWriter mw = new MessageWriter()) {
				mw.Write<string>(data.Title);
				NetworkWorker.fetch.SendContractDeclinedMessage(mw.GetMessageBytes());
			}
		}

		private void onContractFailed(Contract data) {
			if (PlayerStatusWorker.fetch.myPlayerStatus.teamName == "")
				return;
			using (MessageWriter mw = new MessageWriter()) {
				mw.Write<string>(data.Title);
				NetworkWorker.fetch.SendContractFailedMessage(mw.GetMessageBytes());
			}
		}

		private void onContractFinished(Contract data) {
			if (PlayerStatusWorker.fetch.myPlayerStatus.teamName == "")
				return;
			using (MessageWriter mw = new MessageWriter()) {
				mw.Write<string>(data.Title);
				NetworkWorker.fetch.SendContractFinishedMessage(mw.GetMessageBytes());
			}
		}

		private void onContractOffered(Contract data) {
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
			}
		}

		public void handleContractDeclinedMessage(byte[] messageData) {
			using (MessageReader mr = new MessageReader(messageData)) {
				string teamName = mr.Read<string>();
				string contractTitle = mr.Read<string>();
				DarkLog.Debug("Recieved message for declined contract: " + contractTitle);
				TeamStatus teamStatus = TeamWorker.fetch.teams.Find(team => team.teamName == teamName);
				teamStatus.contracts[3].Add(contractTitle);
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
			}
		}

		public void syncContractsWithTeam(List<List<string>> contracts) {
			DarkLog.Debug("syncing contracts with team");
			for(int i = 0; i < contracts.Count(); i++) {
				foreach(string contractTitle in contracts[i]) {
					switch(i) {
						case 0:
							ContractSystem.Instance.Contracts.Find(contract => contract.Title == contractTitle).Accept();
							break;
						case 1:
							ContractSystem.Instance.Contracts.Find(contract => contract.Title == contractTitle).Cancel();
							break;
						case 2:
							ContractSystem.Instance.Contracts.Find(contract => contract.Title == contractTitle).Complete();
							break;
						case 3:
							ContractSystem.Instance.Contracts.Find(contract => contract.Title == contractTitle).Decline();
							break;
						case 4:
							ContractSystem.Instance.Contracts.Find(contract => contract.Title == contractTitle).Fail();
							break;
						case 5:
							ContractSystem.Instance.Contracts.Find(contract => contract.Title == contractTitle).IsFinished();
							break;
						case 6:
							ContractSystem.Instance.Contracts.Find(contract => contract.Title == contractTitle).Offer();
							break;
						default:
							DarkLog.Debug("CONTRACT SYNC FAILED");
							break;
					}
					
				}
			}
		}

		public List<string> getContractsOfType(string type) {
			DarkLog.Debug("Getting contracts of type: " + type);
			List<string> contractNames = new List<string>();
			switch(type.ToUpper()) {
				case "ACCEPTED":
					List<Contract> acceptedContracts = ContractSystem.Instance.Contracts.FindAll(contract => contract.ContractState == Contract.State.Offered);
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
