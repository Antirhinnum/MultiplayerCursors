using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace MultiplayerCursors;

public sealed class CursorTrackingSystem : ModSystem
{
	private static readonly OtherPlayerMouseLayer _otherPlayerMouseLayer = new();
	internal static readonly StableData[] stableDataByPlayer = new StableData[Main.maxPlayers];
	internal static readonly UnstableData[] unstableDataByPlayer = new UnstableData[Main.maxPlayers];

	public override void ClearWorld()
	{
		if (Main.netMode == NetmodeID.SinglePlayer)
		{
			return;
		}

		for (int i = 0; i < Main.maxPlayers; i++)
		{
			stableDataByPlayer[i] = default;
			unstableDataByPlayer[i] = default;
		}
	}

	public override void PostUpdateEverything()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			SyncDataFromClient();
		}
		else if (Main.netMode == NetmodeID.Server)
		{
			SyncDataFromServer();
		}
	}

	/// <summary>
	/// Syncs changed data from this client to the server.
	/// </summary>
	private void SyncDataFromClient()
	{
		StableData newStableData = StableData.GenerateFromClient();
		if (stableDataByPlayer[Main.myPlayer] != newStableData)
		{
			ModPacket packet = Mod.GetPacket();
			packet.Write((byte)PacketType.UpdateStableDataFromClient);
			StableData.WriteChangesToNet(packet, newStableData, stableDataByPlayer[Main.myPlayer]);
			packet.Send();

			stableDataByPlayer[Main.myPlayer] = newStableData;
		}

		UnstableData newUnstableData = UnstableData.GenerateFromClient();
		if (unstableDataByPlayer[Main.myPlayer] != newUnstableData)
		{
			ModPacket packet = Mod.GetPacket();
			packet.Write((byte)PacketType.UpdateUnstableDataFromClient);
			UnstableData.WriteToNet(packet, newUnstableData);
			packet.Send();

			unstableDataByPlayer[Main.myPlayer] = newUnstableData;
		}
	}

	/// <summary>
	/// Updates clients with nearby cursor information.
	/// </summary>
	private void SyncDataFromServer()
	{
		List<int> playerIndiciesToSync = new(Main.maxPlayers);

		// Sync other players' cursors to player i
		for (int playerToSyncTo = 0; playerToSyncTo < Main.maxPlayers; playerToSyncTo++)
		{
			if (!Main.player[playerToSyncTo].active)
			{
				continue;
			}

			playerIndiciesToSync.Clear();

			for (int playerToGetDataFrom = 0; playerToGetDataFrom < Main.maxPlayers; playerToGetDataFrom++)
			{
				if (!Main.player[playerToGetDataFrom].active)
				{
					continue;
				}

				// Don't sync a player's cursor to themself
				if (playerToSyncTo == playerToGetDataFrom)
				{
					continue;
				}

				// i and j are now two different active players

				// Don't sync info to opponents, since they can't see it anyways.
				if (Main.player[playerToSyncTo].InOpposingTeam(Main.player[playerToGetDataFrom]))
				{
					continue;
				}

				// Don't sync info from players that this one can't see.
				if (!CanPlayer1SeePlayer2Cursor(playerToSyncTo, playerToGetDataFrom))
				{
					continue;
				}

				playerIndiciesToSync.Add(playerToGetDataFrom);
			}

			if (playerIndiciesToSync.Count == 0)
			{
				continue;
			}

			// We now need to sync cursor datas to the client.
			ModPacket packet = Mod.GetPacket();
			packet.Write((byte)PacketType.SyncNearbyUnstableDataToClientFromServer);
			packet.Write((byte)playerIndiciesToSync.Count);
			foreach (int playerIndex in playerIndiciesToSync)
			{
				packet.Write((byte)playerIndex);
				UnstableData.WriteToNet(packet, unstableDataByPlayer[playerIndex]);
			}
			packet.Send(toClient: playerToSyncTo);
		}
	}

	private static bool CanPlayer1SeePlayer2Cursor(int player1, int player2, int padding = 100)
	{
		if (stableDataByPlayer[player1] == default || stableDataByPlayer[player2] == default)
		{
			return false;
		}

		ref StableData player1StableData = ref stableDataByPlayer[player1];
		Rectangle player1ScreenRectangle = new(
			(int)unstableDataByPlayer[player1].ScreenPosition.X,
			(int)unstableDataByPlayer[player1].ScreenPosition.Y,
			stableDataByPlayer[player1].ScreenSize.X,
			stableDataByPlayer[player1].ScreenSize.Y
		);

		player1ScreenRectangle.Inflate(padding, padding);
		return player1ScreenRectangle.Contains(unstableDataByPlayer[player2].CursorWorldPosition.ToPoint());
	}

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		if (Main.netMode != NetmodeID.MultiplayerClient)
		{
			return;
		}

		// Draw much earlier than the local player's cursor.
		int mouseIndex = layers.FindIndex(l => l.Name == "Vanilla: Diagnose Net");
		if (mouseIndex == -1)
		{
			return;
		}

		layers.Insert(mouseIndex, _otherPlayerMouseLayer);
	}
}