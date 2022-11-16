using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace BulkExtractinator;

internal sealed class ExtractinatorTile : GlobalTile
{
	public override void RightClick(int i, int j, int type)
	{
		if (BulkExtractinator.ExtractinatorTiles.Contains(type))
		{
			var tileData = TileObjectData.GetTileData(Main.tile[i, j]);
			int frameX = Main.tile[i, j].TileFrameX;
			int frameY = Main.tile[i, j].TileFrameY;

			int partFrameX = frameX % tileData.CoordinateFullWidth;
			int partFrameY = frameY % tileData.CoordinateFullHeight;

			int partX = partFrameX / (tileData.CoordinateWidth + tileData.CoordinatePadding);
			int partY = 0;
			int remainingFrame = partFrameY;
			while (remainingFrame > 0)
			{
				remainingFrame -= tileData.CoordinateHeights[partY] + tileData.CoordinatePadding;
				partY++;
			}	

			var multitileRect = new Rectangle(i - partX, j - partY, tileData.Width, tileData.Height);

			if (Main.player[Main.myPlayer].TryGetModPlayer<ExtractinatorPlayer>(out var modPlr))
			{
				if (modPlr.CurrentOpenExtractinator == multitileRect)
				{
					if (modPlr.HasExtractinatorOpen)
						modPlr.CloseExtractinator();
					else
						modPlr.OpenExtractinator();
				}
				else
				{
					modPlr.CurrentOpenExtractinator = multitileRect;
					modPlr.OpenExtractinator();
				}
			}
		}
	}
}
