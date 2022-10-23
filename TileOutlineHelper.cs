using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Content.Sources;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

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
			origTex.GetData(origTexData);

			var newTexData = new Color[origTexData.Length];
			for (int j = 0; j < origTex.Height; j++)
			{
				for (int i = 0; i < origTex.Width; i++)
				{
					var index = i + j * origTex.Width;
					if (origTexData[index].A == 0)
						continue;

					newTexData[index] = new Color(252, 252, 252);
				}
			}

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
