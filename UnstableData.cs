using Microsoft.Xna.Framework;
using System.IO;
using Terraria;

namespace MultiplayerCursors;

// Data that will likely change every frame.
// Both contained values change when a player moves.
public readonly record struct UnstableData(
	Vector2 CursorWorldPosition,
	Vector2 ScreenPosition,
	bool SmartCursorEnabled
)
{
	public static UnstableData GenerateFromClient()
	{
		return new()
		{
			CursorWorldPosition = Main.MouseWorld,
			ScreenPosition = Main.Camera.ScaledPosition, // Take zoom into account
			SmartCursorEnabled = Main.SmartCursorIsUsed
		};
	}

	internal static void WriteToNet(BinaryWriter writer, UnstableData data)
	{
		writer.WriteWorldPosition(data.CursorWorldPosition);
		writer.WriteWorldPosition(data.ScreenPosition);
		writer.Write(data.SmartCursorEnabled);
	}

	internal static UnstableData ReadFromNet(BinaryReader reader)
	{
		return new()
		{
			CursorWorldPosition = reader.ReadWorldPosition(),
			ScreenPosition = reader.ReadWorldPosition(),
			SmartCursorEnabled = reader.ReadBoolean()
		};
	}
}