using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;

namespace BulkExtractinator;

internal sealed class ExtractUIState : UIState
{
	ExtractModeButtonElement extractModeButton;

	public override void OnInitialize()
	{
		var extractButton = new ExtractinatorButtonElement();
		extractButton.Left.Set(170, 0f);
		extractButton.Top.Set(290, 0f);
		Append(extractButton);

		extractModeButton = new ExtractModeButtonElement();
		extractModeButton.Left.Set(110, 0);
		extractModeButton.Top.Set(280, 0);
		Append(extractModeButton);


	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		if (Main.recBigList)
			return;

		var player = Main.player[Main.myPlayer];

		var oldScale = Main.inventoryScale;
		Main.inventoryScale = /*0.755f;*/ 0.85f;

		int xPos = 120;
		int yPos = 290;

		if(player.TryGetModPlayer<ExtractinatorPlayer>(out var extractinatorPlr))
		{
			ref Item item = ref extractinatorPlr.ExtractinatorSlotItem;

			if (Utils.FloatIntersect(Main.mouseX, Main.mouseY, 0, 0, xPos, yPos,
				TextureAssets.InventoryBack6.Width() * Main.inventoryScale,
				TextureAssets.InventoryBack6.Height() * Main.inventoryScale)
				&& !extractModeButton.IsMouseHovering)
			{
				ItemSlot.OverrideHover(ref item, ItemSlot.Context.ChestItem);
				if (Main.mouseItem.IsAir
					|| ItemSlot.ControlInUse || ItemSlot.ShiftInUse
					|| ItemID.Sets.ExtractinatorMode[player.HeldItem.type] >= 0)
				{
					ItemSlot.LeftClick(ref item, ItemSlot.Context.ChestItem);
					ItemSlot.RightClick(ref item, ItemSlot.Context.ChestItem);
				}
				ItemSlot.MouseHover(ref item, ItemSlot.Context.ChestItem);
				player.mouseInterface = true;
			}

			ItemSlot.Draw(spriteBatch, ref item, ItemSlot.Context.PrefixItem, new Vector2(xPos, yPos));
		}
		Main.inventoryScale = oldScale;
	}
}
