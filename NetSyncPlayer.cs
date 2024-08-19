using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace MultiplayerCursors;

public sealed class NetSyncPlayer : ModPlayer
{
	public override void OnEnterWorld()
	{
		if (Main.netMode == NetmodeID.SinglePlayer)
		{
			return;
		}

		ModPacket packet = Mod.GetPacket();
		packet.Write((byte)PacketType.RequestAllDataFromClient);
		packet.Send();
	}
}