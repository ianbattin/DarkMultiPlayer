﻿using DarkMultiPlayerCommon;
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
				string contract = mr.Read<string>();
				if (!DBManager.getTeamAcceptedContracts(client.teamName).Contains(contract)) DBManager.updateTeamContracts(client.teamName, contract, "accepted");
				ServerMessage message = new ServerMessage();
				message.type = ServerMessageType.CONTRACT_ACCEPTED;
				using (MessageWriter mw = new MessageWriter()) {
					mw.Write<string>(client.teamName);
					mw.Write<string>(contract);
					message.data = mw.GetMessageBytes();
				}
				ClientHandler.SendToAll(client, message, true);
			}
		}

		public static void handleContractCancelledMessage(ClientObject client, byte[] messageData) {
			if (client.teamName == "")
				return;
			using (MessageReader mr = new MessageReader(messageData)) {
				string contract = mr.Read<string>();
				if (!DBManager.getTeamCancelledContracts(client.teamName).Contains(contract)) DBManager.updateTeamContracts(client.teamName, contract, "cancelled");
				ServerMessage message = new ServerMessage();
				message.type = ServerMessageType.CONTRACT_CANCELLED;
				using (MessageWriter mw = new MessageWriter()) {
					mw.Write<string>(client.teamName);
					mw.Write<string>(contract);
					message.data = mw.GetMessageBytes();
				}
				ClientHandler.SendToAll(client, message, true);
			}
		}

		public static void handleContractCompletedMessage(ClientObject client, byte[] messageData) {
			if (client.teamName == "")
				return;
			using (MessageReader mr = new MessageReader(messageData)) {
				string contract = mr.Read<string>();
				if (!DBManager.getTeamCompletedContracts(client.teamName).Contains(contract)) DBManager.updateTeamContracts(client.teamName, contract, "completed");
				ServerMessage message = new ServerMessage();
				message.type = ServerMessageType.CONTRACT_COMPLETED;
				using (MessageWriter mw = new MessageWriter()) {
					mw.Write<string>(client.teamName);
					mw.Write<string>(contract);
					message.data = mw.GetMessageBytes();
				}
				ClientHandler.SendToAll(client, message, true);
			}
		}

		public static void handleContractDeclinedMessage(ClientObject client, byte[] messageData) {
			if (client.teamName == "")
				return;
			using (MessageReader mr = new MessageReader(messageData)) {
				string contract = mr.Read<string>();
				if (!DBManager.getTeamDeclinedContracts(client.teamName).Contains(contract)) DBManager.updateTeamContracts(client.teamName, contract, "declined");
				ServerMessage message = new ServerMessage();
				message.type = ServerMessageType.CONTRACT_DECLINED;
				using (MessageWriter mw = new MessageWriter()) {
					mw.Write<string>(client.teamName);
					mw.Write<string>(contract);
					message.data = mw.GetMessageBytes();
				}
				ClientHandler.SendToAll(client, message, true);
			}
		}

		public static void handleContractFailedMessage(ClientObject client, byte[] messageData) {
			if (client.teamName == "")
				return;
			using (MessageReader mr = new MessageReader(messageData)) {
				string contract = mr.Read<string>();
				if (!DBManager.getTeamFailedContracts(client.teamName).Contains(contract)) DBManager.updateTeamContracts(client.teamName, contract, "failed");
				ServerMessage message = new ServerMessage();
				message.type = ServerMessageType.CONTRACT_FAILED;
				using (MessageWriter mw = new MessageWriter()) {
					mw.Write<string>(client.teamName);
					mw.Write<string>(contract);
					message.data = mw.GetMessageBytes();
				}
				ClientHandler.SendToAll(client, message, true);
			}
		}

		//Make it same as completed so the game doesnt break
		public static void handleContractFinishedMessage(ClientObject client, byte[] messageData) {
			if (client.teamName == "")
				return;
			using (MessageReader mr = new MessageReader(messageData)) {
				string contract = mr.Read<string>();
				if (!DBManager.getTeamCompletedContracts(client.teamName).Contains(contract)) DBManager.updateTeamContracts(client.teamName, contract, "completed");
				ServerMessage message = new ServerMessage();
				message.type = ServerMessageType.CONTRACT_FINISHED;
				using (MessageWriter mw = new MessageWriter()) {
					mw.Write<string>(client.teamName);
					mw.Write<string>(contract);
					message.data = mw.GetMessageBytes();
				}
				ClientHandler.SendToAll(client, message, true);
			}
		}

		public static void handleContractOfferedMessage(ClientObject client, byte[] messageData) {
			if (client.teamName == "")
				return;
			using (MessageReader mr = new MessageReader(messageData)) {
				string contract = mr.Read<string>();
				if(!DBManager.getTeamAcceptedContracts(client.teamName).Contains(contract) && !DBManager.getTeamOfferedContracts(client.teamName).Contains(contract))
					DBManager.updateTeamContracts(client.teamName, contract, "offered");
				ServerMessage message = new ServerMessage();
				message.type = ServerMessageType.CONTRACT_OFFERED;
				using (MessageWriter mw = new MessageWriter()) {
					mw.Write<string>(client.teamName);
					mw.Write<string>(contract);
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
