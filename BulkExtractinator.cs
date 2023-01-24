using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BulkExtractinator;

public class BulkExtractinator : Mod
{
	internal static List<int> ExtractinatorTiles { get; private set; }

	public override void Load()
	{
		ExtractinatorTiles = new List<int>(new int[] { TileID.Extractinator });
		if (!Main.dedServ)
			Main.AssetSourceController.OnResourcePackChange += OnResourcePackChange;
	}

	private void OnResourcePackChange(Terraria.IO.ResourcePackList obj)
	{
		foreach (var tile in ExtractinatorTiles)
			TileOutlineHelper.RegisterHighlightTexture(tile);
	}

	public override void PostSetupContent()
	{
		TileOutlineHelper.SetupHighlight(TileID.Extractinator);
	}

	public override object Call(params object[] args)
	{
		if (args[0].ToString() == "AddExtractinatorTileID")
		{
			if (int.TryParse(args[1].ToString(), out int newTileID))
			{
				ExtractinatorTiles.Add(newTileID);
				TileOutlineHelper.SetupHighlight(newTileID);
				return true;
			}
			return false;
		}
		return base.Call(args);
	}
}