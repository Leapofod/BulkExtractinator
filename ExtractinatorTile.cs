using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace BulkExtractinator;

internal sealed class ExtractinatorTile : GlobalTile
{
	public override void RightClick(int i, int j, int type)
	{
		if (BulkExtractinator.ExtractinatorTiles.Contains(type)
			|| BulkExtractinator.ChloroExtractinatorTiles.Contains(type))
		{
			// This part just gets the multitile data for the targeted extractinator.
			var tileData = TileObjectData.GetTileData(Main.tile[i, j]);
			int frameX = Main.tile[i, j].TileFrameX;
			int frameY = Main.tile[i, j].TileFrameY;

			int partFrameX = frameX % tileData.CoordinateFullWidth;
			int partFrameY = frameY % tileData.CoordinateFullHeight;

			// partX and partY are relative coordinates of where on the multitile i and j are.
			int partX = partFrameX / (tileData.CoordinateWidth + tileData.CoordinatePadding);
			int partY = 0;
			int remainingFrame = partFrameY;
			while (remainingFrame > 0)
			{
				remainingFrame -= tileData.CoordinateHeights[partY] + tileData.CoordinatePadding;
				partY++;
			}

			// Current open extractinator data
			var multitileRect = new Rectangle(i - partX, j - partY, tileData.Width, tileData.Height);
			var openExtractorType = BulkExtractinator.ExtractinatorTiles.Contains(type) ?
				ExtractinatorType.Normal : ExtractinatorType.Chlorophyte;

			if (ExtractinatorPlayer.GetLocalModPlayer(out var modPlr))
			{
				// If the tile is the same.
				if (modPlr.CurrentOpenExtractinator == multitileRect 
					&& modPlr.OpenExtractinatorType == openExtractorType)
				{
					if (modPlr.HasExtractinatorOpen)
						modPlr.CloseExtractinator();
					else
						modPlr.OpenExtractinator();
				}
				else
				{
					modPlr.OpenExtractinatorType = openExtractorType;
					modPlr.CurrentOpenExtractinator = multitileRect;
					modPlr.OpenExtractinator();
				}
			}
		}
	}
}
