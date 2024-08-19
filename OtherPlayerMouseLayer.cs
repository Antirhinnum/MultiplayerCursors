using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.UI;

namespace MultiplayerCursors;

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

		MultiplayerCursors.transparentItems = true;
		int originalMyPlayer = Main.myPlayer;
		(int originalMouseX, int originalMouseY) = (Main.mouseX, Main.mouseY);
		Color originalCursorColor = Main.cursorColor;
		Color originalBorderColor = Main.MouseBorderColor;
		bool originalSmartCursorMouse = Main.SmartCursorWanted_Mouse;
		bool originalSmartCursorGamePad = Main.SmartCursorWanted_GamePad;
		bool originalRainbowCursor = Main.LocalPlayer.hasRainbowCursor;
		bool originalHoveringOverAnNPC = Main.HoveringOverAnNPC;
		bool originalCursorItemIconEnabled = Main.LocalPlayer.cursorItemIconEnabled;
		try
		{
			Rectangle screenArea = new(
				(int)Main.Camera.ScaledPosition.X,
				(int)Main.Camera.ScaledPosition.Y,
				(int)Main.Camera.ScaledSize.X,
				(int)Main.Camera.ScaledSize.Y
			);
			for (int i = 0; i < Main.maxPlayers; i++)
			{
				if (i == originalMyPlayer) continue;
				if (!Main.player[i].active) continue;
				if (Main.player[originalMyPlayer].InOpposingTeam(Main.player[i])) continue;

				UnstableData unstableData = CursorTrackingSystem.unstableDataByPlayer[i];
				if (!screenArea.Contains(unstableData.CursorWorldPosition.ToPoint()))
				{
					continue;
				}
				StableData stableData = CursorTrackingSystem.stableDataByPlayer[i];

				Main.myPlayer = i;
				Main.mouseX = (int)(unstableData.CursorWorldPosition.X - Main.screenPosition.X);
				Main.mouseY = (int)(unstableData.CursorWorldPosition.Y - Main.screenPosition.Y);
				Main.cursorColor = CursorColorFromMouseColor(stableData.CursorMainColor);
				Main.MouseBorderColor = stableData.CursorBorderColor;
				Main.SmartCursorWanted_Mouse = unstableData.SmartCursorEnabled;
				Main.LocalPlayer.cursorItemIconEnabled = true;

				if (unstableData.SmartCursorEnabled)
				{
					Main.DrawCursor(Main.DrawThickCursor(smart: true), smart: true);
				}
				else
				{
					Main.DrawCursor(Main.DrawThickCursor());
				}

				if (ModContent.GetInstance<ClientConfig>().ShowItemIcons)
				{
					DrawInterface_40_InteractItemIcon(Main.instance);
				}
			}
		}
		finally
		{
			MultiplayerCursors.transparentItems = false;
			Main.myPlayer = originalMyPlayer;
			(Main.mouseX, Main.mouseY) = (originalMouseX, originalMouseY);
			Main.cursorColor = originalCursorColor;
			Main.MouseBorderColor = originalBorderColor;
			Main.SmartCursorWanted_Mouse = originalSmartCursorMouse;
			Main.SmartCursorWanted_GamePad = originalSmartCursorGamePad;
			Main.LocalPlayer.hasRainbowCursor = originalRainbowCursor;
			Main.HoveringOverAnNPC = originalHoveringOverAnNPC;
			Main.LocalPlayer.cursorItemIconEnabled = originalCursorItemIconEnabled;
		}
		return base.DrawSelf();
	}

	[UnsafeAccessor(UnsafeAccessorKind.Method)]
	private static extern void DrawInterface_40_InteractItemIcon(Main self);

	private static Color CursorColorFromMouseColor(Color cursorMainColor)
	{
		float originalAlpha = Main.cursorAlpha;
		Main.cursorAlpha *= ModContent.GetInstance<ClientConfig>().Transparency;
		float num = (Main.cursorAlpha * 0.3f) + 0.7f;
		byte r = (byte)(cursorMainColor.R * Main.cursorAlpha);
		byte g = (byte)(cursorMainColor.G * Main.cursorAlpha);
		byte b = (byte)(cursorMainColor.B * Main.cursorAlpha);
		byte a = (byte)(255f * num);
		Main.cursorAlpha = originalAlpha;
		return new Color(r, g, b, a);
	}
}