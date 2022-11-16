using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;

namespace BulkExtractinator;

internal sealed class ExtractUIState : UIState
{
	private const int XPOS = 100;
	private const int YPOS = 270;

	ExtractModeButtonElement extractModeButton;

	public override void OnInitialize()
	{
		var extractButton = new ExtractinatorButtonElement();
		extractButton.Left.Set(XPOS + 50, 0f);
		extractButton.Top.Set(YPOS, 0f);
		Append(extractButton);

		extractModeButton = new ExtractModeButtonElement();
		extractModeButton.Left.Set(XPOS - 10, 0);
		extractModeButton.Top.Set(YPOS - 10, 0);
		Append(extractModeButton);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		if (Main.recBigList)
			return;

		var player = Main.player[Main.myPlayer];

		var oldScale = Main.inventoryScale;
		Main.inventoryScale = 0.85f;

		if (player.TryGetModPlayer<ExtractinatorPlayer>(out var extractinatorPlr))
		{
			ref Item item = ref extractinatorPlr.ExtractinatorSlotItem;

			if (Utils.FloatIntersect(Main.mouseX, Main.mouseY, 0, 0, XPOS, YPOS,
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

			if (item.IsAir)
				Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, Terraria.Localization.Language.GetTextValue("Mods.BulkExtractinator.InsertToExtractinator"), XPOS + 60, YPOS, Color.White, Color.Black, Vector2.Zero);
			
			ItemSlot.Draw(spriteBatch, ref item, ItemSlot.Context.PrefixItem, new Vector2(XPOS, YPOS));
		}
		Main.inventoryScale = oldScale;
	}
}
