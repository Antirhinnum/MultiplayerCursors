using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader;
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
		// Using GameViewMatrix.ZoomMatrix here because cursors are drawn based on their world positions, not the local player's screen position.
		Main.spriteBatch.End();
		Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.SamplerStateForCursor, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.ZoomMatrix);

		int originalMyPlayer = Main.myPlayer;
		(int originalMouseX, int originalMouseY) = (Main.mouseX, Main.mouseY);
		float originalCursorScale = Main.cursorScale;
		Color originalCursorColor = Main.cursorColor;
		Color originalBorderColor = Main.MouseBorderColor;
		bool originalSmartCursorMouse = Main.SmartCursorWanted_Mouse;
		bool originalSmartCursorGamePad = Main.SmartCursorWanted_GamePad;
		bool originalHoveringOverAnNPC = Main.HoveringOverAnNPC;

		MultiplayerCursors.transparentItems = true;
		// Multiply by UIScale to properly scale, divide by game scale since using Main.GameViewMatrix autoscales by it
		// Without the division, cursors mysteriously scale up if you zoom in, despite being UI elements.
		Main.cursorScale = originalCursorScale * Main.UIScale / Main.GameViewMatrix.Zoom.X;
		try
		{
			// The cursor position is where the tip draws, so we need to offset so that cursors whose tails are onscreen still draw
			Rectangle screenArea = new(
				(int)Main.Camera.ScaledPosition.X - 200,
				(int)Main.Camera.ScaledPosition.Y - 200,
				(int)(Main.Camera.ScaledSize.X * Main.UIScale) + 400,
				(int)(Main.Camera.ScaledSize.Y * Main.UIScale) + 400
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
			Main.cursorScale = originalCursorScale;
			Main.cursorColor = originalCursorColor;
			Main.MouseBorderColor = originalBorderColor;
			Main.SmartCursorWanted_Mouse = originalSmartCursorMouse;
			Main.SmartCursorWanted_GamePad = originalSmartCursorGamePad;
			Main.HoveringOverAnNPC = originalHoveringOverAnNPC;
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