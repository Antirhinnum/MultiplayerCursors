using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace MultiplayerCursors;

internal sealed class ClientConfig : ModConfig
{
	public override ConfigScope Mode { get; } = ConfigScope.ClientSide;

	[DefaultValue(0.5f)]
	[Slider]
	public float Transparency;

	[DefaultValue(true)]
	public bool ShowItemIcons;

	[DefaultValue(1)]
	[Range(0, 60)]
	public int SyncWaitFrames;
}