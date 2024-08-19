namespace MultiplayerCursors;

internal enum PacketType : byte
{
	UpdateStableDataFromClient,
	UpdateStableDataFromServer,
	UpdateUnstableDataFromClient,

	SyncNearbyUnstableDataToClientFromServer,

	RequestAllDataFromClient,
	ReceiveAllDataFromServer
}