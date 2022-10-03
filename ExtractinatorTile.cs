using Terraria;
using Terraria.ModLoader;

namespace BulkExtractinator;

internal sealed class ExtractinatorTile : GlobalTile
{
	public override void RightClick(int i, int j, int type)
	{
		if (BulkExtractinator.ExtractinatorTiles.Contains(type))
		{
			Main.playerInventory = true;
			if (ExtractinatorUI.IsUIOpen)
				ExtractinatorUI.CloseUI();
			else
				ExtractinatorUI.OpenUI();
		}
	}
}
