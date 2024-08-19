using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
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

		int mouseIndex = layers.FindIndex(l => l.Name == "Vanilla: Cursor");
		if (mouseIndex == -1)
		{
			Main.NewText("what");
			return;
		}

		layers.Insert(mouseIndex + 1, _otherPlayerMouseLayer);
	}
}

internal sealed class OtherPlayerMouseLayer : GameInterfaceLayer
{
	public OtherPlayerMouseLayer() : base($"{nameof(MultiplayerCursors)}: Other Player Cursors", InterfaceScaleType.UI)
	{
	}

	protected override bool DrawSelf()
	{
		// Adapted from Main::DrawInterface_36_Cursor
		Main.spriteBatch.End();
		Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.SamplerStateForCursor, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

		(int originalMouseX, int originalMouseY) = (Main.mouseX, Main.mouseY);
		Color originalCursorColor = Main.cursorColor;
		Color originalBorderColor = Main.MouseBorderColor;
		bool originalSmartCursorMouse = Main.SmartCursorWanted_Mouse;
		bool originalSmartCursorGamePad = Main.SmartCursorWanted_GamePad;
		bool originalRainbowCursor = Main.LocalPlayer.hasRainbowCursor;
		try
		{
			Rectangle screenArea = new(
				(int)Main.Camera.ScaledPosition.X - 20,
				(int)Main.Camera.ScaledPosition.Y - 20,
				(int)Main.Camera.ScaledSize.X + 40,
				(int)Main.Camera.ScaledSize.Y + 40
			);
			for (int i = 0; i < Main.maxPlayers; i++)
			{
				if (i == Main.myPlayer) continue;
				if (!Main.player[i].active) continue;
				if (Main.LocalPlayer.InOpposingTeam(Main.player[i])) continue;

				UnstableData unstableData = CursorTrackingSystem.unstableDataByPlayer[i];
				if (!screenArea.Contains(unstableData.CursorWorldPosition.ToPoint()))
				{
					Main.NewText("Doesn't contain mouse");
					continue;
				}

				StableData stableData = CursorTrackingSystem.stableDataByPlayer[i];

				Main.mouseX = (int)(unstableData.CursorWorldPosition.X - Main.screenPosition.X);
				Main.mouseY = (int)(unstableData.CursorWorldPosition.Y - Main.screenPosition.Y);
				Main.cursorColor = CursorColorFromMouseColor(stableData.CursorMainColor);
				Main.MouseBorderColor = stableData.CursorBorderColor;
				Main.SmartCursorWanted_Mouse = unstableData.SmartCursorEnabled;

				if (unstableData.SmartCursorEnabled)
				{
					Main.DrawCursor(Main.DrawThickCursor(smart: true), smart: true);
				}
				else
				{
					Main.DrawCursor(Main.DrawThickCursor());
				}
			}
		}
		finally
		{
			(Main.mouseX, Main.mouseY) = (originalMouseX, originalMouseY);
			Main.cursorColor = originalCursorColor;
			Main.MouseBorderColor = originalBorderColor;
			Main.SmartCursorWanted_Mouse = originalSmartCursorMouse;
			Main.SmartCursorWanted_GamePad = originalSmartCursorGamePad;
			Main.LocalPlayer.hasRainbowCursor = originalRainbowCursor;
		}
		return base.DrawSelf();
	}

	private static Color CursorColorFromMouseColor(Color cursorMainColor)
	{
		float num = (Main.cursorAlpha * 0.3f) + 0.7f;
		byte r = (byte)(cursorMainColor.R * Main.cursorAlpha);
		byte g = (byte)(cursorMainColor.G * Main.cursorAlpha);
		byte b = (byte)(cursorMainColor.B * Main.cursorAlpha);
		byte a = (byte)(255f * num);
		return new Color(r, g, b, a);
	}
}