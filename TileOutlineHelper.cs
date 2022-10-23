using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace BulkExtractinator;

internal static class TileOutlineHelper
{
	internal static readonly Dictionary<int, Texture2D> HighlightTextures;

	static TileOutlineHelper()
	{
		HighlightTextures = new Dictionary<int, Texture2D>();
	}

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

			//var tempFilePath = Path.Combine(Main.SavePath, $"temp{tileType}.png");

			var newTex = new Texture2D(Main.graphics.GraphicsDevice, origTex.Width, origTex.Height);
			newTex.SetData(newTexData);
			HighlightTextures[tileType] = newTex;

			//using var stream = new FileStream(tempFilePath, FileMode.Create);
			//newTex.SaveAsPng(stream, origTex.Width, origTex.Height);

			//TextureAssets.HighlightMask[tileType] = ModContent.Request<Texture2D>(Path.ChangeExtension(tempFilePath, null), ReLogic.Content.AssetRequestMode.ImmediateLoad);
			//File.Delete(tempFilePath);
		});
	}
}
