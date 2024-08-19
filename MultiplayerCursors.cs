using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace MultiplayerCursors;

public sealed class MultiplayerCursors : Mod
{
	/// <summary>
	/// Index 1: The player the data was synced to.
	/// <br/> Index 2: The player the data belongs to.
	/// </summary>
	private static readonly StableData[,] _lastSyncedStableDataPerPlayer = new StableData[Main.maxPlayers, Main.maxPlayers];
	internal static bool transparentItems = false;

	public override void HandlePacket(BinaryReader reader, int whoAmI)
	{
		PacketType type = (PacketType)reader.ReadByte();
		switch (type)
		{
			case PacketType.UpdateStableDataFromClient when Main.netMode == NetmodeID.Server:
			{
				StableData.UpdateFromNet(reader, ref CursorTrackingSystem.stableDataByPlayer[whoAmI]);
				for (int i = 0; i < Main.maxPlayers; i++)
				{
					if (i == whoAmI) continue;
					if (!Netplay.Clients[i].IsActive) continue;

					// Stable data changes rarely, so tell all players if it does.
					// This removes the need for clients to keep track of stable data for updating.
					ModPacket packet = GetPacket();
					packet.Write((byte)PacketType.UpdateStableDataFromServer);
					packet.Write((byte)whoAmI);
					StableData.WriteChangesToNet(packet, CursorTrackingSystem.stableDataByPlayer[whoAmI], _lastSyncedStableDataPerPlayer[i, whoAmI]);
					packet.Send(toClient: i);

					_lastSyncedStableDataPerPlayer[i, whoAmI] = CursorTrackingSystem.stableDataByPlayer[whoAmI];
				}
				break;
			}

			case PacketType.UpdateStableDataFromServer when Main.netMode == NetmodeID.MultiplayerClient:
			{
				// Update this client's copy of a player's stable data.
				int updateeIndex = reader.ReadByte();
				StableData.UpdateFromNet(reader, ref CursorTrackingSystem.stableDataByPlayer[updateeIndex]);
				break;
			}

			case PacketType.UpdateUnstableDataFromClient when Main.netMode == NetmodeID.Server:
			{
				CursorTrackingSystem.unstableDataByPlayer[whoAmI] = UnstableData.ReadFromNet(reader);
				break;
			}

			case PacketType.SyncNearbyUnstableDataToClientFromServer when Main.netMode == NetmodeID.MultiplayerClient:
			{
				int count = reader.ReadByte();
				for (int i = 0; i < count; i++)
				{
					int playerIndex = reader.ReadByte();
					CursorTrackingSystem.unstableDataByPlayer[playerIndex] = UnstableData.ReadFromNet(reader);
				}
				break;
			}

			case PacketType.RequestAllDataFromClient when Main.netMode == NetmodeID.Server:
			{
				List<int> playersToWrite = [];
				for (int i = 0; i < Main.maxPlayers; i++)
				{
					if (i == whoAmI) continue;
					if (!Main.player[i].active) continue;

					playersToWrite.Add(i);
				}

				ModPacket packet = GetPacket();
				packet.Write((byte)PacketType.ReceiveAllDataFromServer);
				packet.Write((byte)playersToWrite.Count);
				foreach (int playerIndex in playersToWrite)
				{
					packet.Write((byte)playerIndex);
					StableData.WriteAllToNet(packet, CursorTrackingSystem.stableDataByPlayer[playerIndex]);
					_lastSyncedStableDataPerPlayer[whoAmI, playerIndex] = CursorTrackingSystem.stableDataByPlayer[playerIndex];
					UnstableData.WriteToNet(packet, CursorTrackingSystem.unstableDataByPlayer[playerIndex]);
				}
				packet.Send(toClient: whoAmI);
				break;
			}

			case PacketType.ReceiveAllDataFromServer when Main.netMode == NetmodeID.MultiplayerClient:
			{
				int count = reader.ReadByte();
				for (int i = 0; i < count; i++)
				{
					int playerIndex = reader.ReadByte();
					CursorTrackingSystem.stableDataByPlayer[playerIndex] = StableData.ReadAllFromNet(reader);
					CursorTrackingSystem.unstableDataByPlayer[playerIndex] = UnstableData.ReadFromNet(reader);
				}
				break;
			}
		}
	}

	public override void Load()
	{
		if (!Main.dedServ)
		{
			On_ItemSlot.GetItemLight_refColor_refSingle_int_bool += TransparentifyItems;
		}
	}

	private static Color TransparentifyItems(On_ItemSlot.orig_GetItemLight_refColor_refSingle_int_bool orig, ref Color currentColor, ref float scale, int type, bool outInTheWorld)
	{
		Color originalReturn = orig(ref currentColor, ref scale, type, outInTheWorld);
		if (transparentItems)
		{
			float alpha = 0.5f;
			currentColor *= alpha;
		}
		return originalReturn;
	}
}