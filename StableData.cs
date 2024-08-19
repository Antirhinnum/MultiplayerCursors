using Microsoft.Xna.Framework;
using System.IO;
using Terraria;

namespace MultiplayerCursors;

// Data that is unlikely to change per player.
public readonly record struct StableData(
	Point ScreenSize,           // May change if: Player zooms in/out, player resizes window
	Color CursorMainColor,      // May change if: Manually changed by player
	Color CursorBorderColor     // May change if: Manually changed by player
)
{
	public static StableData GenerateFromClient()
	{
		return new()
		{
			ScreenSize = Main.Camera.ScaledSize.ToPoint(),
			CursorMainColor = Main.mouseColor,
			CursorBorderColor = Main.MouseBorderColor
		};
	}

	public static void WriteChangesToNet(BinaryWriter writer, StableData newData, StableData staleData)
	{
		BitsByte changed = new(
			newData.ScreenSize != staleData.ScreenSize,
			newData.CursorMainColor != staleData.CursorMainColor,
			newData.CursorBorderColor != staleData.CursorBorderColor
		);
		writer.Write(changed);

		if (changed[0])
		{
			writer.Write((ushort)newData.ScreenSize.X);
			writer.Write((ushort)newData.ScreenSize.Y);
		}

		if (changed[1])
		{
			writer.WriteRGB(newData.CursorMainColor);
		}

		if (changed[2])
		{
			// WriteRGB doesn't write alpha, which is important for the border.
			writer.Write(newData.CursorBorderColor.PackedValue);
		}
	}

	public static void UpdateFromNet(BinaryReader reader, ref StableData data)
	{
		BitsByte changed = reader.ReadByte();
		Point? newScreenSize = null;
		Color? newCursorMainColor = null;
		Color? newCursorBorderColor = null;

		if (changed[0])
		{
			newScreenSize = new Point(reader.ReadUInt16(), reader.ReadUInt16());
		}

		if (changed[1])
		{
			newCursorMainColor = reader.ReadRGB();
		}

		if (changed[2])
		{
			newCursorBorderColor = new Color() with { PackedValue = reader.ReadUInt32() };
		}

		data = data with
		{
			ScreenSize = newScreenSize ?? data.ScreenSize,
			CursorMainColor = newCursorMainColor ?? data.CursorMainColor,
			CursorBorderColor = newCursorBorderColor ?? data.CursorBorderColor,
		};
	}

	public static void WriteAllToNet(BinaryWriter writer, StableData data)
	{
		writer.Write((ushort)data.ScreenSize.X);
		writer.Write((ushort)data.ScreenSize.Y);
		writer.WriteRGB(data.CursorMainColor);
		writer.Write(data.CursorBorderColor.PackedValue);
	}

	public static StableData ReadAllFromNet(BinaryReader reader)
	{
		return new()
		{
			ScreenSize = new Point(reader.ReadUInt16(), reader.ReadUInt16()),
			CursorMainColor = reader.ReadRGB(),
			CursorBorderColor = default(Color) with { PackedValue = reader.ReadUInt32() }
		};
	}
}