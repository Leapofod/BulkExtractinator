using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BulkExtractinator;

internal sealed class ExtractinatorPlayer : ModPlayer
{
	public Item ExtractinatorSlotItem;

	public ExtractinatorPlayer()
	{
		ExtractinatorSlotItem = new Item();
	}

	public override void PreUpdate() // or PreUpdateMovement
	{
		if (BulkExtractinator.ExtractinatorTiles
			.Contains(Main.tile[Player.tileTargetX, Player.tileTargetY].TileType))
		{
			// ItemID.Sets.ExtractinatorMode >= 0; // Check if item is extractable
			Player.noThrow = 2;
			Player.cursorItemIconEnabled = true;
			Player.cursorItemIconID = ItemID.Extractinator;
		}
	}

}
