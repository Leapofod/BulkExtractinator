using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Content.Sources;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace BulkExtractinator;

internal static class TileOutlineHelper
{
	internal static void SetupHighlight(int tileType)
	{
		if (Main.dedServ || tileType <= 0 || tileType >= TileLoader.TileCount)
			return;

		TileID.Sets.HasOutlines[tileType] = true;
		RegisterHighlightTexture(tileType);
	}

	internal static void RegisterHighlightTexture(int tileType)
	{
		Main.QueueMainThreadAction(() =>
		{
			if (!TextureAssets.Tile[tileType].IsLoaded)
				Main.instance.LoadTiles(tileType);

			var origTex = TextureAssets.Tile[tileType].Value;
			var origTexData = new Color[origTex.Width * origTex.Height];
			var newTexData = new Color[origTexData.Length];
			origTex.GetData(origTexData);

			var tileData = TileObjectData.GetTileData(tileType, 0);
			var padding = tileData.CoordinatePadding;
			var width = tileData.CoordinateWidth;
			var heights = tileData.CoordinateHeights;
			for (int j = 0; j < origTex.Height; j++)
			{
				for (int i = 0; i < origTex.Width; i++)
				{
					var index = i + j * origTex.Width;
					if (origTexData[index].A == 0)
						continue;

					bool isHighlight = false;
					if (i < 2 || i >= origTex.Width - 2 - padding || j < 2 || j >= origTex.Height - 2 - padding)
						isHighlight = true;
					if (!isHighlight)
					{
						int frameX = i % tileData.CoordinateFullWidth / (width + padding);
						int xPart = i % (width + padding);

						if ((frameX == 0 && xPart < 2) || 
							(frameX == tileData.Width - 1 && xPart >= width - 2))
						{
							isHighlight = true;
							goto END;
						}

						int l1Index = index - 1;
						int l2Index = index - 2;
						if (xPart - 1 < 0) l1Index -= padding;
						if (xPart - 2 < 0) l2Index -= padding;
						
						int r1Index = index + 1;
						int r2Index = index + 2;
						if (xPart + 1 >= width) r1Index += padding;
						if (xPart + 2 >= width) r2Index += padding;

						if (origTexData[l1Index].A == 0 || origTexData[l2Index].A == 0 ||
						origTexData[r1Index].A == 0 || origTexData[r2Index].A == 0)
						{
							isHighlight = true;
							goto END;
						}

						int yPart = j;
						int frameY = 0;
						while (yPart >= heights[frameY] + padding)
						{
							yPart -= heights[frameY] + padding;
							frameY++;
							frameY %= tileData.Height;
						}

						if ((frameY == 0 && yPart < 2) || 
							(frameY == tileData.Height - 1 && yPart >= heights[frameY] - 2))
						{
							isHighlight = true;
							goto END;
						}

						int u1Index = index - origTex.Width;
						int u2Index = index - (2 * origTex.Width);
						if (yPart - 1 < 0) u1Index -= padding * origTex.Width;
						if (yPart - 2 < 0) u2Index -= padding * origTex.Width;

						int d1Index = index + origTex.Width;
						int d2Index = index + (2 * origTex.Width);
						if (yPart + 1 >= heights[frameY]) d1Index += padding * origTex.Width;
						if (yPart + 2 >= heights[frameY]) d2Index += padding * origTex.Width;

						if (origTexData[u1Index].A == 0 || origTexData[u2Index].A == 0 ||
						origTexData[d1Index].A == 0 || origTexData[d2Index].A == 0)
							isHighlight = true;

						END:;
					}
					if (isHighlight)
						newTexData[index] = new Color(252, 252, 252);
				}
			}

			// this works and adds the texture properly but it isn't using it
			var newTex = new Texture2D(Main.graphics.GraphicsDevice, origTex.Width, origTex.Height);
			newTex.SetData(newTexData);

			var assetPath = $"Tiles\\Extractinator\\Tile_{tileType}_Highlight";
			assetPath = assetPath.Replace('\\', System.IO.Path.DirectorySeparatorChar);

			var assetCtor = typeof(Asset<Texture2D>).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)[0];
			var assetSetLoaded = typeof(Asset<Texture2D>).GetMethod("SubmitLoadedContent", BindingFlags.NonPublic | BindingFlags.Instance);

			var asset = assetCtor.Invoke(new object[] { assetPath }) as Asset<Texture2D>;
			assetSetLoaded.Invoke(asset, new object[] { newTex, new FileSystemContentSource(assetPath) });

			TextureAssets.HighlightMask[tileType] = asset;
		});
	}
}
