using DarkMultiPlayerCommon;
using MessageStream2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DarkMultiPlayerServer.Messages {
	public class ContractControl {
		public static void handleContractAcceptedMessage(ClientObject client, byte[] messageData) {
			if (client.teamName == "")
				return;
			using (MessageReader mr = new MessageReader(messageData)) {
				string contractTitle = mr.Read<string>();
				DBManager.updateTeamContracts(client.teamName, contractTitle, "accepted");
				ServerMessage message = new ServerMessage();
				message.type = ServerMessageType.CONTRACT_ACCEPTED;
				using (MessageWriter mw = new MessageWriter()) {
					mw.Write<string>(client.teamName);
					mw.Write<string>(contractTitle);
					message.data = mw.GetMessageBytes();
				}
				ClientHandler.SendToAll(client, message, true);
			}
		}

		public static void handleContractCancelledMessage(ClientObject client, byte[] messageData) {
			if (client.teamName == "")
				return;
			using (MessageReader mr = new MessageReader(messageData)) {
				string contractTitle = mr.Read<string>();
				DBManager.updateTeamContracts(client.teamName, contractTitle, "cancelled");
				ServerMessage message = new ServerMessage();
				message.type = ServerMessageType.CONTRACT_CANCELLED;
				using (MessageWriter mw = new MessageWriter()) {
					mw.Write<string>(client.teamName);
					mw.Write<string>(contractTitle);
					message.data = mw.GetMessageBytes();
				}
				ClientHandler.SendToAll(client, message, true);
			}
		}

		public static void handleContractCompletedMessage(ClientObject client, byte[] messageData) {
			if (client.teamName == "")
				return;
			using (MessageReader mr = new MessageReader(messageData)) {
				string contractTitle = mr.Read<string>();
				DBManager.updateTeamContracts(client.teamName, contractTitle, "completed");
				ServerMessage message = new ServerMessage();
				message.type = ServerMessageType.CONTRACT_COMPLETED;
				using (MessageWriter mw = new MessageWriter()) {
					mw.Write<string>(client.teamName);
					mw.Write<string>(contractTitle);
					message.data = mw.GetMessageBytes();
				}
				ClientHandler.SendToAll(client, message, true);
			}
		}

		public static void handleContractDeclinedMessage(ClientObject client, byte[] messageData) {
			if (client.teamName == "")
				return;
			using (MessageReader mr = new MessageReader(messageData)) {
				string contractTitle = mr.Read<string>();
				DBManager.updateTeamContracts(client.teamName, contractTitle, "declined");
				ServerMessage message = new ServerMessage();
				message.type = ServerMessageType.CONTRACT_DECLINED;
				using (MessageWriter mw = new MessageWriter()) {
					mw.Write<string>(client.teamName);
					mw.Write<string>(contractTitle);
					message.data = mw.GetMessageBytes();
				}
				ClientHandler.SendToAll(client, message, true);
			}
		}

		public static void handleContractFailedMessage(ClientObject client, byte[] messageData) {
			if (client.teamName == "")
				return;
			using (MessageReader mr = new MessageReader(messageData)) {
				string contractTitle = mr.Read<string>();
				DBManager.updateTeamContracts(client.teamName, contractTitle, "failed");
				ServerMessage message = new ServerMessage();
				message.type = ServerMessageType.CONTRACT_FAILED;
				using (MessageWriter mw = new MessageWriter()) {
					mw.Write<string>(client.teamName);
					mw.Write<string>(contractTitle);
					message.data = mw.GetMessageBytes();
				}
				ClientHandler.SendToAll(client, message, true);
			}
		}

		public static void handleContractFinishedMessage(ClientObject client, byte[] messageData) {
			if (client.teamName == "")
				return;
			using (MessageReader mr = new MessageReader(messageData)) {
				string contractTitle = mr.Read<string>();
				DBManager.updateTeamContracts(client.teamName, contractTitle, "finished");
				ServerMessage message = new ServerMessage();
				message.type = ServerMessageType.CONTRACT_FINISHED;
				using (MessageWriter mw = new MessageWriter()) {
					mw.Write<string>(client.teamName);
					mw.Write<string>(contractTitle);
					message.data = mw.GetMessageBytes();
				}
				ClientHandler.SendToAll(client, message, true);
			}
		}

		public static void handleContractOfferedMessage(ClientObject client, byte[] messageData) {
			if (client.teamName == "")
				return;
			using (MessageReader mr = new MessageReader(messageData)) {
				string contractTitle = mr.Read<string>();
				DBManager.updateTeamContracts(client.teamName, contractTitle, "offered");
				ServerMessage message = new ServerMessage();
				message.type = ServerMessageType.CONTRACT_OFFERED;
				using (MessageWriter mw = new MessageWriter()) {
					mw.Write<string>(client.teamName);
					mw.Write<string>(contractTitle);
					message.data = mw.GetMessageBytes();
				}
				ClientHandler.SendToAll(client, message, true);
			}
		}

		/// <summary>
		/// Client sends this after TeamCreateResponse has been received(client side) with success=true
		/// </summary>
		/// <param name="client"></param>
		/// <param name="messageData"></param>
		public static void handleContractState(ClientObject client, byte[] messageData) {
			if (client.teamName == "")
				return;
			using (MessageReader mr = new MessageReader(messageData)) {
				List<List<string>> contracts = new List<List<string>>();
				contracts.Add(mr.Read<string[]>().ToList());
				contracts.Add(mr.Read<string[]>().ToList());
				contracts.Add(mr.Read<string[]>().ToList());
				contracts.Add(mr.Read<string[]>().ToList());
				contracts.Add(mr.Read<string[]>().ToList());
				contracts.Add(mr.Read<string[]>().ToList());
				contracts.Add(mr.Read<string[]>().ToList());
				DBManager.setInitialContractState(client.teamName, contracts);
			}
		}
	}
}
