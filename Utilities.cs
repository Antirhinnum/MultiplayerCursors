using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.DataStructures;

namespace MultiplayerCursors;

internal static class Utilities
{
	internal static void WriteWorldPosition(this BinaryWriter writer, Vector2 worldPosition)
	{
		Point16 tilePosition = worldPosition.ToTileCoordinates16();
		writer.Write7BitEncodedInt(tilePosition.X);
		writer.Write7BitEncodedInt(tilePosition.Y);

		// Both in integer range [0, 16) (4 bits each)
		byte offsetX = (byte)((int)worldPosition.X % 16);
		byte offsetY = (byte)((int)worldPosition.Y % 16);
		byte packedOffset = (byte)((offsetX << 4) | (offsetY));
		writer.Write(packedOffset);
	}

	internal static Vector2 ReadWorldPosition(this BinaryReader reader)
	{
		ushort tileX = (ushort)reader.Read7BitEncodedInt();
		ushort tileY = (ushort)reader.Read7BitEncodedInt();
		byte packedOffset = reader.ReadByte();
		int offsetX = packedOffset >> 4;
		int offsetY = packedOffset & 0xF;
		return new Point16(tileX, tileY).ToWorldCoordinates(offsetX, offsetY);
	}
}