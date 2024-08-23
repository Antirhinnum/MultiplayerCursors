using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace MultiplayerCursors;

internal sealed class ServerConfig : ModConfig
{
	public override ConfigScope Mode { get; } = ConfigScope.ServerSide;

	[DefaultValue(1)]
	[Range(0, 60)]
	public int SyncWaitFrames;
}