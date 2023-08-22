using Terraria.ObjectData;

namespace BulkExtractinator;

internal class ProvideCandidateHelper
{
	internal static int CurrentTileType;
	internal static TileObjectData CurrentObjectData;

	// Taken from `TileLoader.ModifySmartInteractCoords`
	internal static int CurrentObjectWidth => CurrentObjectData?.Width ?? 0;
	internal static int CurrentObjectHeight => CurrentObjectData?.Height ?? 0;
	internal static int CurrentObjectFrameWidth => (CurrentObjectData?.CoordinateWidth ?? 0) + (CurrentObjectData?.CoordinatePadding ?? 0);
	internal static int CurrentObjectFrameHeight => (CurrentObjectData?.CoordinateHeights[0] ?? 0) + (CurrentObjectData?.CoordinatePadding ?? 0);
	internal static int CurrentObjectExtraY => CurrentObjectData is null ? 0 : CurrentObjectData.CoordinateFullHeight % CurrentObjectFrameHeight;

	static ProvideCandidateHelper()
	{
		CurrentTileType = 0;
		CurrentObjectData = null;
	}
}
